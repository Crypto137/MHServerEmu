using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Populations
{
    #region Enums
    [Flags]
    public enum ClusterObjectFlag
    {
        None            = 0,
        Leader          = 1 << 0,
        Henchmen        = 1 << 1,
        HasModifiers    = 1 << 2,
        Hostile         = 1 << 3,
        ProjectToFloor  = 1 << 4,
        SkipFormation   = 1 << 5,
    }

    [Flags]
    public enum SpawnFlags
    {
        None            = 0,
        flag1           = 1 << 0,
        flag2           = 1 << 1,
        flag4           = 1 << 2,
        flag8           = 1 << 3,
        IgnoreBlackout  = 1 << 4,
        Spawned         = 1 << 5,
    }
    #endregion

    public class ClusterObject
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public bool DebugLog = false;
        public GRandom Random { get; private set; }
        public Region Region { get; private set; }
        public ClusterGroup Parent { get; private set; }
        public ClusterObjectFlag Flags { get; set; }
        public Transform3 Transform { get; private set; }
        public Vector3 Position { get; private set; }
        public Orientation Orientation { get; private set; }
        public float Radius { get; set; }
        public float Height { get; set; }
        public PathFlags PathFlags { get; set; }

        public ClusterObject(Region region, GRandom random, ClusterGroup parent)
        {
            DebugLog = false;
            Random = random;
            Region = region;
            Parent = parent;
            Radius = 0.0f;
            Height = 0.0f;
            PathFlags = PathFlags.None;
            Flags = ClusterObjectFlag.None;
            Transform = Transform3.Identity();
            Position = Vector3.Zero;
            Orientation = Orientation.Zero;
        }

        public Vector3 GetParentRelativePosition() => Position;

        public void SetParentRelativePosition(Vector3 position)
        {
            Position = position;
            Transform = Transform3.BuildTransform(Position, Orientation);
            Parent?.UpdateBounds(this);
            SetLocationDirty();
        }

        public void SetParentRelativeOrientation(Orientation orientation)
        {
            Orientation = orientation;
            Transform = Transform3.BuildTransform(Position, Orientation);
            SetLocationDirty();
        }

        public Vector3 GetAbsolutePosition()
        {
            return GetAbsoluteTransform().Translation;
        }

        public Orientation GetAbsoluteOrientation()
        {
            return GetAbsoluteTransform().Orientation;
        }

        public Transform3 GetAbsoluteTransform()
        {
            return Parent != null ? Parent.GetAbsoluteTransform() * Transform : Transform;
        }

        public virtual void UpdateBounds(ClusterObject clusterObject) { }
        public virtual void SetLocationDirty() { }
        public virtual bool IsFormationObject() => false;
        public virtual bool Initialize() => false;
        public virtual ulong Spawn(SpawnGroup group, WorldEntity spawner, List<WorldEntity> entities) { return 0; }
        public virtual void UpgradeToRank(RankPrototype upgradeRank, int num) { }
        public virtual void AssignAffixes(RankPrototype rankProto, List<PrototypeId> affixes) { }
        public virtual bool TestLayout() => false;
    }

    public class ClusterGroup : ClusterObject
    {
        public PopulationObjectPrototype ObjectProto { get; private set; }
        public PropertyCollection Properties { get; private set; }
        public float SubObjectRadiusMax { get; private set; }
        public SpawnFlags SpawnFlags { get; set; }
        public List<ClusterObject> Objects { get; private set; }
        public PrototypeId MissionRef { get; private set; }
        public KeyValuePair<PrototypeId, Vector3> BlackOutZone { get; internal set; }

        public ClusterGroup(Region region, GRandom random, PopulationObjectPrototype populationObject,
            ClusterGroup parent, PropertyCollection properties, SpawnFlags flags)
            : base(region, random, parent)
        {
            ObjectProto = populationObject;

            Properties = new();
            if (properties != null)
            {
                Properties.FlattenCopyFrom(properties, false);
                MissionRef = properties[PropertyEnum.MissionPrototype];
            }

            Objects = new();
            SubObjectRadiusMax = 0.0f;
            SpawnFlags = flags;
            BlackOutZone = new(PrototypeId.Invalid, Vector3.Zero);
            ObjectProto?.BuildCluster(this, ClusterObjectFlag.None);
        }

        public override bool Initialize()
        {
            PathFlags = (PathFlags)0xFFFF;

            if (Objects.Count == 0)
            {
                Logger.Warn($"[DESIGN] Cluster contains no valid entity objects. OBJECT={GameDatabase.GetFormattedPrototypeName(MissionRef)}");
                return false;
            }

            foreach (var obj in Objects)
            {
                if (obj == null) continue;

                obj.Initialize();

                if (obj.IsFormationObject()) SubObjectRadiusMax = MathF.Max(SubObjectRadiusMax, obj.Radius);
                if (obj.Flags.HasFlag(ClusterObjectFlag.Hostile)) Flags |= ClusterObjectFlag.Hostile;

                PathFlags &= obj.PathFlags;
            }

            if (SpawnFlags.HasFlag(SpawnFlags.IgnoreBlackout) == false && Flags.HasFlag(ClusterObjectFlag.Hostile))
                SpawnFlags |= ObjectProto.IgnoreBlackout ? SpawnFlags.IgnoreBlackout : 0;

            InitializeRankAndMods();

            if (Radius <= 0.0f) return false;

            if (Flags.HasFlag(ClusterObjectFlag.SkipFormation) == false && SubObjectRadiusMax > 0.0f)
            {
                FormationTypePrototype formationTypeProto = ObjectProto.GetFormation();
                if (formationTypeProto == null) return false;

                if (formationTypeProto is BoxFormationTypePrototype boxProto)
                    DoBox(boxProto);
                else if (formationTypeProto is LineFormationTypePrototype lineProto)
                    DoLine(lineProto);
                else if (formationTypeProto is ArcFormationTypePrototype arcProto)
                    DoArc(arcProto);
                else if (formationTypeProto is FixedFormationTypePrototype fixedProto)
                    DoFixed(fixedProto);
            }

            return true;
        }

        public override bool IsFormationObject()
        {
            if (Flags.HasFlag(ClusterObjectFlag.SkipFormation)) return false;

            foreach (var obj in Objects)
            {
                if (obj == null) continue;
                if (obj.Flags.HasFlag(ClusterObjectFlag.SkipFormation)) continue;
                if (obj.IsFormationObject()) return true;
            }

            return false;
        }

        private void DoFixed(FixedFormationTypePrototype fixedProto)
        {
            if (fixedProto == null || fixedProto.Slots.IsNullOrEmpty()) return;

            if (GetFormationObjects(out List<ClusterObject> formationObjects))
            {
                int num = formationObjects.Count;
                int slots = fixedProto.Slots.Length;

                if (slots < num)
                {
                    Logger.Warn($"[DESIGN] PopulationObject using FixedFormation with fewer slots than mobs in population. OBJECT={ObjectProto}");
                    return;
                }

                foreach (ClusterObject obj in formationObjects)
                    obj?.SetParentRelativePosition(Vector3.Zero);

                int numSlots = Math.Min(num, slots);
                for (int slot = 0; slot < numSlots; slot++)
                {
                    ClusterObject obj = formationObjects[slot];
                    if (obj == null) continue;
                    FormationSlotPrototype formationSlotProto = fixedProto.Slots[slot];
                    if (formationSlotProto == null) continue;

                    Vector3 pos = new(formationSlotProto.X, formationSlotProto.Y, 0f);
                    obj.SetParentRelativePosition(pos);

                    Orientation orientation = Orientation.Zero;
                    if (fixedProto.Facing == FormationFacing.None)
                        orientation.Yaw = MathHelper.ToRadians(formationSlotProto.Yaw);
                    else
                        orientation = DoFacing(fixedProto.Facing, pos);

                    obj.SetParentRelativeOrientation(orientation);
                }
            }
        }

        private void DoArc(ArcFormationTypePrototype arcProto)
        {
            if (arcProto == null || arcProto.ArcRadians <= 0) return;

            if (GetFormationObjects(out List<ClusterObject> formationObjects))
            {
                int num = formationObjects.Count;

                float length = 0.0f;
                foreach (ClusterObject obj in formationObjects)
                {
                    if (obj == null) continue;
                    length += 2.0f * obj.Radius;
                }

                length += num * arcProto.Spacing;

                float arcSector = MathF.Max(length / arcProto.ArcRadians, SubObjectRadiusMax + arcProto.Spacing);

                float requiredArcLength = arcSector * arcProto.ArcRadians;
                float extraSpace = (requiredArcLength - length) / num;

                if (num > 2)
                {
                    List<ClusterObject> oldObjects = new(formationObjects);
                    for (int i = 0; i < oldObjects.Count; i++)
                        formationObjects[GetAlternatingIndex(i, oldObjects.Count)] = oldObjects[i];
                }

                Vector3 pos = Vector3.Forward;
                pos *= arcSector;
                pos = Vector3.AxisAngleRotate(pos, Vector3.ZAxis, arcProto.ArcRadians / 2.0f);

                float spacing = arcProto.Spacing / 2f + extraSpace / 2f;

                foreach (ClusterObject obj in formationObjects)
                {
                    if (obj == null) continue;

                    float angle = (obj.Radius + spacing) / arcSector;
                    pos = Vector3.AxisAngleRotate(pos, Vector3.ZAxis, angle);

                    obj.SetParentRelativePosition(pos);
                    obj.SetParentRelativeOrientation(DoFacing(arcProto.Facing, pos));

                    pos = Vector3.AxisAngleRotate(pos, Vector3.ZAxis, angle);
                }
            }
        }

        private void DoLine(LineFormationTypePrototype lineProto)
        {
            if (lineProto == null) return;
            if (GetFormationObjects(out List<ClusterObject> formationObjects))
            {
                int numRows = lineProto.Rows.HasValue() ? lineProto.Rows.Length : 1;
                float center = 0f;
                float width = 0f;
                if (numRows > 1)
                {
                    float length = (numRows - 1) * 2f * (SubObjectRadiusMax + lineProto.Spacing);
                    width = length / (numRows - 1);
                    center = length * 0.5f;
                }

                int rowIndex = 0;
                int formationObjectNum = formationObjects.Count;
                for (int row = 0; row < numRows; row++)
                {
                    int numObjectsInRow = numRows == 1 ? formationObjectNum : lineProto.Rows[row].Num;

                    float rowWidth = 0f;
                    var currentRow = new ClusterObject[numObjectsInRow];
                    for (int objectIndex = 0; objectIndex < numObjectsInRow; objectIndex++)
                    {
                        int adjusted = rowIndex + objectIndex;
                        if (adjusted >= formationObjectNum)
                        {
                            Logger.Warn($"PopulationObject using LineFormation but there aren't enough spawns to fill the row! OBJECT={ObjectProto}");
                            continue;
                        }

                        ClusterObject obj = formationObjects[adjusted];
                        if (obj == null) continue;
                        rowWidth += 2.0f * obj.Radius;

                        currentRow[GetAlternatingIndex(objectIndex, numObjectsInRow)] = obj;
                    }

                    rowIndex += numObjectsInRow;
                    rowWidth += (numObjectsInRow - 1) * lineProto.Spacing;

                    Vector3 pos = Vector3.Forward * (center - width * row);
                    pos += Vector3.Right * (rowWidth * -0.5f);

                    for (int objectIndex = 0; objectIndex < currentRow.Length; objectIndex++)
                    {
                        ClusterObject obj = currentRow[objectIndex];
                        if (obj == null) continue;

                        var rightOffset = Vector3.Right * (obj.Radius + lineProto.Spacing * 0.5f);
                        var rightRadius = Vector3.Right * obj.Radius;

                        pos += objectIndex != 0 ? rightOffset : rightRadius;

                        obj.SetParentRelativePosition(pos);
                        obj.SetParentRelativeOrientation(DoFacing(lineProto.Facing, pos));

                        pos += objectIndex != currentRow.Length - 1 ? rightOffset : rightRadius;
                    }
                }
            }
        }

        private static int GetAlternatingIndex(int index, int length)
        {
            return length / 2 + GetAlternatingOffset(index);
        }

        private static int GetAlternatingOffset(int index)
        {
            if (index % 2 == 0)
                return index / 2;
            else
                return -(index + 1) / 2;
        }

        private void DoBox(BoxFormationTypePrototype boxProto)
        {
            if (boxProto == null || SubObjectRadiusMax <= 0.0f) return;
            const int MaxObjects = 4;
            float width = SubObjectRadiusMax * 2.0f;

            if (GetFormationObjects(out List<ClusterObject> formationObjects))
            {
                int box = 0;
                int formationIndex = 0;
                while (box < MaxObjects && formationIndex < formationObjects.Count)
                {
                    ClusterObject obj = formationObjects[formationIndex];
                    if (obj == null) break;
                    if (box == 0)
                    {
                        obj.SetParentRelativePosition(Vector3.Zero);
                        obj.SetParentRelativeOrientation(Orientation.Zero);
                        if (++formationIndex == formationObjects.Count) return;
                    }
                    else
                    {
                        int boxOffset = Math.Max(box - 1, 0);
                        int maxOffset = Math.Max(box * 2 - 1, 0);

                        for (int offset = 0; offset < maxOffset; offset++)
                        {
                            for (int side = 1; side <= 4; side++)
                            {
                                Point2 point = side switch
                                {
                                    1 => new(-box, -boxOffset + offset),
                                    2 => new(-boxOffset + offset, box),
                                    3 => new(box, boxOffset - offset),
                                    4 => new(boxOffset - offset, -box),
                                    _ => new(0, 0),
                                };

                                Vector3 pos = Vector3.Zero;
                                pos.X = point.X * width;
                                pos.Y = point.Y * width;
                                obj.SetParentRelativePosition(pos);
                                obj.SetParentRelativeOrientation(DoFacing(boxProto.Facing, pos));
                                if (++formationIndex == formationObjects.Count) return;
                                obj = formationObjects[formationIndex];
                            }
                        }
                    }
                    box++;
                }
            }
        }

        private static Orientation DoFacing(FormationFacing facing, Vector3 pos)
        {
            return facing switch
            {
                FormationFacing.FaceParentInverse => Orientation.FromDeltaVector2D(Vector3.Back),
                FormationFacing.FaceOrigin => Orientation.FromDeltaVector2D(-pos),
                FormationFacing.FaceOriginInverse => Orientation.FromDeltaVector2D(pos),
                _ => Orientation.Zero
            };
        }

        private bool GetFormationObjects(out List<ClusterObject> formationObjects)
        {
            formationObjects = new();
            foreach (ClusterObject obj in Objects)
                if (obj?.IsFormationObject() == true)
                    formationObjects.Add(obj);

            return formationObjects.Count > 0;
        }

        private void InitializeRankAndMods()
        {
            // TODO affixes system
            /*      
                PopulationGlobalsPrototype popGlobals = GameDatabase.GetPopulationGlobalsPrototype();
                if (popGlobals == null) return;

                TuningTable difficulty = Region.Difficulty;
                if (difficulty == null) return;

                TuningPrototype tuningProto = difficulty.GetPrototype();
                if (tuningProto == null) return;

                GRandom random = Region.Game.Random; // GetCurrent

                HashSet<PrototypeId> overrides = GetMobAffixesFromProperties();
                RankPrototype popcornRank = popGlobals.GetRankByEnum(Rank.Popcorn);
                Region.ApplyRegionAffixesEnemyBoosts(popcornRank.DataRef, overrides);

                if (overrides.Count == 0 && HasModifiableEntities() == false) return;

                HashSet<PrototypeId> exemptOverrides = new();
                ShiftExemptFromOverrides(overrides, exemptOverrides);

                List<RankPrototype> ranks = new();
                GetRanks(ranks);

                RankPrototype rollRank = difficulty.RollRank(ranks, overrides);

                int numUpgrade = -1;
                if (rollRank.Rank == Rank.MiniBoss) numUpgrade = 1;

                UpgradeToRank(rollRank, numUpgrade);

                ranks.Clear();
                GetRanks(ranks);

                HashSet<PrototypeId> affixesSet = new();
                foreach (RankPrototype rankProto in ranks)
                {
                    RankAffixEntryPrototype rankEntryProto = tuningProto.GetDifficultyRankEntry(Region.GetDifficultyTierRef(), rankProto);

                    overrides = GetMobAffixesFromProperties();
                    Region.ApplyRegionAffixesEnemyBoosts(rankProto.DataRef, overrides);
                    ShiftExemptFromOverrides(overrides, exemptOverrides);
                    affixesSet.UnionWith(overrides);

                    int maxAffixes = (rankEntryProto != null) ? rankEntryProto.GetMaxAffixes() : 0;
                    List<PrototypeId> slots = new (maxAffixes);

                    if (overrides.Count > 0 && rankEntryProto != null)
                    {
                        for (int slot = maxAffixes - 1; slot >= 0; slot--)
                        {
                            AffixTableEntryPrototype affixProto = rankEntryProto.GetAffixSlot(slot);
                            if (affixProto == null) continue;

                            if (slots[slot] == PrototypeId.Invalid)
                            {
                                foreach (PrototypeId overrideRef in overrides)
                                {
                                    if (affixProto.AffixTable == PrototypeId.Invalid || affixProto.GetAffixTablePrototype().Contains(overrideRef))
                                    {
                                        slots[slot] = overrideRef;
                                        overrides.Remove(overrideRef);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (overrides.Count > 0)
                    {
                        for (int slot = maxAffixes - 1; slot >= 0; slot--)
                        {
                            if (slots[slot] == PrototypeId.Invalid)
                            {
                                if (overrides.Count > 0)
                                {
                                    PrototypeId overrideRef = overrides.First();
                                    slots[slot] = overrideRef;
                                    overrides.Remove(overrideRef);
                                }
                            }
                        }
                    }

                    if (overrides.Count > 0)
                    {
                        slots.AddRange(overrides);
                        overrides.Clear();
                    }

                    if (rankEntryProto != null)
                    {
                        HashSet<PrototypeId> currentAffixes = new ();
                        HashSet<PrototypeId> excludeAffixes = new ();
                        currentAffixes.UnionWith(affixesSet);

                        for (int slot = 0; slot < slots.Count; ++slot)
                        {
                            if (slots[slot] != PrototypeId.Invalid)
                            {
                                currentAffixes.Remove(slots[slot]);
                                excludeAffixes.Add(slots[slot]);
                            }
                        }

                        for (int slot = 0; slot < slots.Count; ++slot)
                        {
                            if (slots[slot] == PrototypeId.Invalid)
                            {
                                AffixTableEntryPrototype affixProto = rankEntryProto.GetAffixSlot(slot);
                                if (affixProto == null) continue;

                                PrototypeId affixRef = affixProto.RollAffix(random, currentAffixes, excludeAffixes);
                                if (affixRef != PrototypeId.Invalid)
                                {
                                    slots[slot] = affixRef;
                                    affixesSet.Add(affixRef);
                                    currentAffixes.Remove(affixRef);
                                    excludeAffixes.Add(affixRef);
                                }
                            }
                        }
                    }

                    if (exemptOverrides.Count > 0)
                    {
                        for (int slot = 0; slot < slots.Count; ++slot)
                        {
                            if (slots[slot] == PrototypeId.Invalid)
                            {
                                if (exemptOverrides.Count > 0)
                                {
                                    PrototypeId overrideR = exemptOverrides.First();
                                    slots[slot] = overrideR;
                                    exemptOverrides.Remove(overrideR);
                                }
                            }
                        }
                    }

                    if (exemptOverrides.Count > 0)
                        slots.AddRange(exemptOverrides);

                    exemptOverrides.Clear();

                    AssignAffixes(rankProto, slots);
                }

                foreach (var obj in Objects)
                {   
                    if (obj is ClusterEntity entity)
                    {
                        bool twinBoost = false;
                        foreach (var modRef in entity.Modifiers)
                        {
                            if (modRef == popGlobals.TwinEnemyBoost)
                            {
                                twinBoost = true;
                                break;
                            }
                        }

                        if (twinBoost)
                        {
                            if (entity.EntityProto.Rank != PrototypeId.Invalid && entity.EntityProto.GetRankPrototype().IsRankBoss())
                            {
                                ClusterEntity newEntity = CreateClusterEntity(entity.EntityRef);
                                if (newEntity != null)
                                {
                                    newEntity.RankProto = popGlobals.TwinEnemyRank; // Prototype <T> operator = (PrototypeId)
                                    newEntity.Modifiers = entity.Modifiers;
                                }
                                break;
                            }
                        }
                    }
                }
            */
        }

        public override void UpgradeToRank(RankPrototype upgradeRank, int num)
        {
            throw new NotImplementedException();
        }

        public override void AssignAffixes(RankPrototype rankProto, List<PrototypeId> affixes)
        {
            throw new NotImplementedException();
        }

        private void GetRanks(List<RankPrototype> ranks)
        {
            throw new NotImplementedException();
        }

        private void ShiftExemptFromOverrides(HashSet<PrototypeId> overrides, HashSet<PrototypeId> exemptOverrides)
        {
            throw new NotImplementedException();
        }

        private bool HasModifiableEntities()
        {
            throw new NotImplementedException();
        }

        private HashSet<PrototypeId> GetMobAffixesFromProperties()
        {
            throw new NotImplementedException();
        }

        public ClusterEntity CreateClusterEntity(PrototypeId entityRef)
        {
            if (entityRef == PrototypeId.Invalid) return null;
            ClusterEntity clusterEntity = new(Region, Random, entityRef, this);
            Objects.Add(clusterEntity);

            return clusterEntity;
        }

        public ClusterGroup CreateClusterGroup(PopulationObjectPrototype objectProto)
        {
            if (objectProto == null) return null;
            ClusterGroup clusterGroup = new(Region, Random, objectProto, this, Properties, SpawnFlags);
            Objects.Add(clusterGroup);
            return clusterGroup;
        }

        public override void SetLocationDirty()
        {
            foreach (var obj in Objects)
                obj?.SetLocationDirty();
        }

        public override void UpdateBounds(ClusterObject child)
        {
            Vector3 childPos = child.GetParentRelativePosition();
            float radius = Vector3.Distance2D(Vector3.Zero, childPos) + child.Radius;

            Radius = MathF.Max(Radius, radius);
            Height = MathF.Max(Height, child.Height);

            Parent?.UpdateBounds(this);
        }

        public override ulong Spawn(SpawnGroup group, WorldEntity spawner, List<WorldEntity> entities)
        {
            var manager = Region.PopulationManager;

            if (Parent == null && group == null)
            {
                group = manager.CreateSpawnGroup();
                group.Transform = Transform3.BuildTransform(GetAbsolutePosition(), Orientation.Zero);
                if (ObjectProto is PopulationEncounterPrototype populationEncounter)
                    group.EncounterRef = populationEncounter.GetEncounterRef();
                group.ObjectProto = ObjectProto;
                group.MissionRef = MissionRef;
                group.SpawnerId = spawner != null ? spawner.Id : 0;
            }
            if (group == null) return 0;

            foreach (var obj in Objects) obj.Spawn(group, spawner, entities);

            var position = GetAbsolutePosition();
            if (ObjectProto.Riders.HasValue())
                foreach (var rider in ObjectProto.Riders)
                    if (rider is PopulationRiderBlackOutPrototype blackOutProto)
                    {
                        var blackout = GameDatabase.GetPrototype<BlackOutZonePrototype>(blackOutProto.BlackOutZone);
                        manager.SpawnBlackOutZone(position, blackout.BlackOutRadius, MissionRef);
                    }

            if (BlackOutZone.Key != PrototypeId.Invalid)
            {
                var blackout = GameDatabase.GetPrototype<BlackOutZonePrototype>(BlackOutZone.Key);
                manager.SpawnBlackOutZone(position, blackout.BlackOutRadius, MissionRef);
            }
            return group.Id;
        }

        public bool PickPositionInSector(Vector3 position, Orientation orientation, int minDistance, int maxDistance)
        {
            if (minDistance < 0.0f || maxDistance <= 0.0f || minDistance > maxDistance || Radius == 0)
            {
                SetParentRelativePosition(position);
                SetParentRelativeOrientation(orientation);
                if (TestLayout()) return false;
                minDistance = (int)Radius;
                maxDistance = (int)Radius * 4;
            }

            const int MaxSectors = 5; // DistanceMax 250 / 50 (Average Cluster) = 5 sectors
            float clusterSize = Radius * 2.0f;
            float clusterPI = MathHelper.TwoPi / clusterSize;
            float minClusterDistance = minDistance + Radius;
            float maxClusterDistance = minDistance + clusterSize;

            if (maxDistance - minDistance < clusterSize)
                maxDistance = (int)maxClusterDistance;

            var sectorPicker = new Picker<int>(Random);
            float distance = minClusterDistance;
            for (int sector = 0; sector < MaxSectors; sector++)
            {
                int numClusters = (int)Math.Floor(clusterPI * distance);
                sectorPicker.Add(sector, numClusters);

                distance += clusterSize;
                maxClusterDistance += clusterSize;
                if (maxClusterDistance > maxDistance) break;
            }
            float shiftAngle = Random.NextFloat() * 2;
            while (sectorPicker.Empty() == false)
            {
                if (sectorPicker.PickRemove(out int sector) == false) return false;
                distance = minClusterDistance + sector * clusterSize;
                int numClusters = (int)MathF.Floor(clusterPI * distance);
                int startCluster = Random.Next(numClusters);
                float clusterAngle = MathHelper.TwoPi / numClusters;

                for (int cluster = 0; cluster < numClusters; cluster++)
                {
                    int div = startCluster + cluster;
                    float angle = clusterAngle * div + shiftAngle;
                    (float rotSin, float rotCos) = MathF.SinCos(Orientation.WrapAngleRadians(angle));
                    Vector3 testPosition = new(position.X + distance * rotCos, position.Y + distance * rotSin, position.Z);
                    if (Region.GetCellAtPosition(testPosition) == null) continue;
                    SetParentRelativePosition(testPosition);
                    if (DebugLog) Logger.Debug($"testPostions = {testPosition}");
                    SetParentRandomOrientation(orientation);
                    // Logger.Debug($"AbsolutePostions = {GetAbsolutePosition().ToStringFloat()}");
                    if (TestLayout()) return true;
                }
            }
            return false;
        }

        public bool PickPositionInBounds(in Aabb bound)
        {
            if (Radius == 0 || bound.Width < Radius || bound.Length < Radius) return false;

            var min = bound.Min;
            var max = bound.Max;
            var center = bound.Center;
            float clusterSize = Radius;
            List<Point2> points = new();

            for (float x = min.X; x < max.X; x += clusterSize)
                for (float y = min.Y; y < max.Y; y += clusterSize)
                    points.Add(new(x, y));

            Random.ShuffleList(points);
            int tries = Math.Min(points.Count, 256);

            for (int i = 0; i < tries; i++)
            {
                var point = points[i];
                Vector3 testPosition = new(point.X, point.Y, center.Z);
                SetParentRelativePosition(testPosition);
                Orientation orientation = new(-MathHelper.PiOver2, 0.0f, 0.0f);
                SetParentRandomOrientation(orientation);
                if (TestLayout()) return true;
            }

            return false;
        }

        private void SetParentRandomOrientation(Orientation orientation)
        {
            orientation.Yaw += Random.NextFloat(-MathHelper.PiOver4, MathHelper.PiOver4);
            SetParentRelativeOrientation(orientation);
        }

        public override bool TestLayout()
        {
            foreach (var obj in Objects)
                if (obj?.TestLayout() == false) return false;
            return true;
        }

    }

    public class ClusterEntity : ClusterObject
    {
        public EntitySelectorPrototype EntitySelectorProto { get; private set; }
        public PrototypeId EntityRef { get; private set; }
        public WorldEntityPrototype EntityProto { get; private set; }
        public bool? SnapToFloor { get; set; }
        public uint EncounterSpawnPhase { get; set; }
        public Bounds Bounds { get; set; }
        public RankPrototype RankProto { get; set; }
        public HashSet<PrototypeId> Modifiers { get; set; }
        public SpawnFlags SpawnFlags => Parent != null ? Parent.SpawnFlags : SpawnFlags.None;

        public ClusterEntity(Region region, GRandom random, PrototypeId selectorRef, ClusterGroup parent)
            : base(region, random, parent)
        {
            Modifiers = new();
            Bounds = new();
            SnapToFloor = null;
            EncounterSpawnPhase = 0;

            Prototype entity = GameDatabase.GetPrototype<Prototype>(selectorRef);
            if (entity is EntitySelectorPrototype entitySelector)
            {
                EntitySelectorProto = entitySelector;
                PrototypeId entityRef = entitySelector.SelectEntity(random, region);
                if (entityRef != PrototypeId.Invalid)
                    EntityRef = entityRef;
            }
            else
            {
                EntityRef = selectorRef;
            }

            EntityProto = GameDatabase.GetPrototype<WorldEntityPrototype>(EntityRef);
            // Logger.Debug($"Add ClusterEntity [{GameDatabase.GetFormattedPrototypeName(EntityRef)}]");
        }

        public override bool Initialize()
        {
            if (EntityProto == null) return false;
            if (EntityProto.Bounds != null)
            {
                Bounds.InitializeFromPrototype(EntityProto.Bounds);
                Radius = Bounds.Radius;
                Height = Bounds.HalfHeight;

                Parent?.UpdateBounds(this);
            }
            else
            {
                // Logger.Warn($"Zounds! Entity {EntityProto} has no Bounds!");
                // Spawner have not Bounds
            }

            if (AlliancePrototype.IsHostileToPlayerAlliance(EntityProto.AlliancePrototype))
                Flags |= ClusterObjectFlag.Hostile;

            PathFlags = Locomotor.GetPathFlags(EntityProto.NaviMethod);

            RankProto = EntityProto.RankPrototype;
            /*
            if (Parent != null)
            {
                PrototypeId rankRef = Parent.Properties.GetProperty<PrototypeId>(PropertyEnum.Rank);
                RankProto = RankPrototype.DoOverride(RankProto, rankRef);
            }*/

            if ((EntityProto.ModifierSetEnable
                || EntityProto.ModifiersGuaranteed.HasValue())
                && Flags.HasFlag(ClusterObjectFlag.Hostile))
            {
                Flags |= ClusterObjectFlag.HasModifiers;
            }

            return true;
        }

        public override bool TestLayout()
        {
            Vector3 regionPos = ProjectToFloor(Region);

            if (Vector3.IsFinite(regionPos) == false)
                return false;

            if (PathFlags != PathFlags.None
                && Region.NaviMesh.Contains(regionPos, Radius, new DefaultContainsPathFlagsCheck(PathFlags)) == false)
                return false;

            if (SpawnFlags.HasFlag(SpawnFlags.IgnoreBlackout) == false
                && Region.PopulationManager.InBlackOutZone(regionPos, Radius, Parent.MissionRef))
                return false;

            Bounds bounds = new(Bounds)
            {
                Center = regionPos + new Vector3(0.0f, 0.0f, Bounds.HalfHeight)
            };

            if (SpawnFlags.HasFlag(SpawnFlags.Spawned) == false)
            {
                Sphere sphere = new(bounds.Center, bounds.Radius);
                foreach (var entity in Region.IterateEntitiesInVolume(sphere, new()))
                    if (Region.IsBoundsBlockedByEntity(bounds, entity, BlockingCheckFlags.CheckSpawns))
                        return false;
            }
            return true;
        }

        public Vector3 ProjectToFloor(Region region)
        {
            Vector3 regionPos = GetAbsolutePosition();
            if (Flags.HasFlag(ClusterObjectFlag.ProjectToFloor))
                return regionPos;

            Vector3 position = RegionLocation.ProjectToFloor(region, regionPos);
            if (DebugLog) Logger.Debug($"ProjectPostions [{GameDatabase.GetFormattedPrototypeName(EntityRef)}] {regionPos} {position}");
            // Debug.Assert(Vector3.DistanceSquared2D(regionPos, position) < Segment.Epsilon);

            Vector3 offset = position - regionPos;
            Vector3 relativePosition = GetParentRelativePosition();
            SetParentRelativePosition(relativePosition + offset);

            Flags |= ClusterObjectFlag.ProjectToFloor;

            return position;
        }


        public override ulong Spawn(SpawnGroup group, WorldEntity spawner, List<WorldEntity> entities)
        {
            var manager = Region.PopulationManager;
            if (group == null) return 0;
            // PropertyCollection, events

            var pos = GetAbsolutePosition();
            var groupPosition = group.Transform.Translation;

            if (Region.Bound.IntersectsXY(pos))
                pos = ProjectToFloor(Region);
            else
                pos = groupPosition;

            var offsetPos = pos - groupPosition;
            var rot = GetAbsoluteOrientation();

            var spec = manager.CreateSpawnSpec(group);
            spec.EntityRef = EntityRef;
            spec.Transform = Transform3.BuildTransform(offsetPos, rot);
            spec.SnapToFloor = SnapToFloor;
            spec.EntitySelectorProto = EntitySelectorProto;
            if (EntityProto != null)
                spec.AppendActions(EntityProto.EntitySelectorActions);
            if (EntitySelectorProto != null)
                spec.AppendActions(EntitySelectorProto.EntitySelectorActions);
            spec.MissionRef = Parent.MissionRef;
            spec.Properties = new();
            spec.Properties.FlattenCopyFrom(Parent.Properties, false);
            // TODO set Rank

            spec.Spawn();
            if (spec.ActiveEntity != null)
                entities.Add(spec.ActiveEntity);

            return group.Id;
        }

        public override bool IsFormationObject()
        {
            if (Flags.HasFlag(ClusterObjectFlag.SkipFormation)) return false;

            bool blocksSpawns = EntityProto != null && EntityProto.Bounds != null && EntityProto.Bounds.BlocksSpawns;
            bool blocking = Bounds.CollisionType == BoundsCollisionType.Blocking;

            return blocksSpawns || blocking;
        }

        public override void SetLocationDirty()
        {
            Flags &= ~ClusterObjectFlag.ProjectToFloor;
        }

        public override void UpgradeToRank(RankPrototype upgradeRank, int num)
        {
            throw new NotImplementedException();
        }

        public override void AssignAffixes(RankPrototype rankProto, List<PrototypeId> affixes)
        {
            throw new NotImplementedException();
        }
    }
}
