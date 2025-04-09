using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
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
        IgnoreSimulated = 1 << 0,
        RetryIgnoringBlackout           = 1 << 1,
        RetryForce           = 1 << 2,
        flag8           = 1 << 3,
        IgnoreBlackout  = 1 << 4,
        IgnoreSpawned   = 1 << 5,
        Cleanup         = 1 << 6,
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

        public void SetParentRelativePosition(in Vector3 position)
        {
            Position = position;
            Transform = Transform3.BuildTransform(Position, Orientation);
            Parent?.UpdateBounds(this);
            SetLocationDirty();
        }

        public void SetParentRelativeOrientation(in Orientation orientation)
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
        public virtual ulong Spawn(SpawnGroup group, WorldEntity spawner, SpawnHeat spawnHeat, List<WorldEntity> entities) { return SpawnGroup.InvalidId; }
        public virtual void UpgradeToRank(RankPrototype upgradeRank, ref int numUpgrade) { }
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
        public KeyValuePair<PrototypeId, Vector3> BlackOutZone { get; set; }
        public SpawnReservation Reservation { get; set; }

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
            Region.EvalRegionProperties(populationObject.EvalSpawnProperties, Properties);

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

            if (SpawnFlags.HasFlag(SpawnFlags.IgnoreBlackout) == false 
                && Flags.HasFlag(ClusterObjectFlag.Hostile) 
                && ObjectProto.IgnoreBlackout)
                SpawnFlags |= SpawnFlags.IgnoreBlackout;

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

            List<ClusterObject> formationObjects = ListPool<ClusterObject>.Instance.Get();
            if (GetFormationObjects(formationObjects))
            {
                int num = formationObjects.Count;
                int slots = fixedProto.Slots.Length;

                if (slots < num)
                {
                    Logger.Warn($"[DESIGN] PopulationObject using FixedFormation with fewer slots than mobs in population. OBJECT={ObjectProto}");
                    ListPool<ClusterObject>.Instance.Return(formationObjects);
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
            ListPool<ClusterObject>.Instance.Return(formationObjects);
        }

        private void DoArc(ArcFormationTypePrototype arcProto)
        {
            if (arcProto == null || arcProto.ArcRadians <= 0) return;

            List<ClusterObject> formationObjects = ListPool<ClusterObject>.Instance.Get();
            if (GetFormationObjects(formationObjects))
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
                    List<ClusterObject> oldObjects = ListPool<ClusterObject>.Instance.Get(formationObjects);
                    for (int i = 0; i < oldObjects.Count; i++)
                        formationObjects[GetAlternatingIndex(i, oldObjects.Count)] = oldObjects[i];
                    ListPool<ClusterObject>.Instance.Return(oldObjects);
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
            ListPool<ClusterObject>.Instance.Return(formationObjects);
        }

        private void DoLine(LineFormationTypePrototype lineProto)
        {
            if (lineProto == null) return;
            List<ClusterObject> formationObjects = ListPool<ClusterObject>.Instance.Get();
            if (GetFormationObjects(formationObjects))
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
            ListPool<ClusterObject>.Instance.Return(formationObjects);
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

            List<ClusterObject> formationObjects = ListPool<ClusterObject>.Instance.Get();
            if (GetFormationObjects(formationObjects))
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
                        if (++formationIndex == formationObjects.Count)
                        {
                            ListPool<ClusterObject>.Instance.Return(formationObjects);
                            return;
                        }
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
                                if (++formationIndex == formationObjects.Count)
                                {
                                    ListPool<ClusterObject>.Instance.Return(formationObjects);
                                    return;
                                }
                                obj = formationObjects[formationIndex];
                            }
                        }
                    }
                    box++;
                }
            }
            ListPool<ClusterObject>.Instance.Return(formationObjects);
        }

        private static Orientation DoFacing(FormationFacing facing, in Vector3 delta)
        {
            return facing switch
            {
                FormationFacing.FaceParentInverse => Orientation.FromDeltaVector2D(Vector3.Back),
                FormationFacing.FaceOrigin => Orientation.FromDeltaVector2D(-delta),
                FormationFacing.FaceOriginInverse => Orientation.FromDeltaVector2D(delta),
                _ => Orientation.Zero
            };
        }

        private static Orientation DoTestFacing(FormationFacing facing, in Vector3 delta, in Orientation orientation)
        {
            return facing switch
            {
                FormationFacing.FaceParentInverse => new(orientation.Yaw + MathHelper.Pi, orientation.Pitch, orientation.Roll),
                FormationFacing.FaceOrigin => Orientation.FromDeltaVector2D(delta),
                FormationFacing.FaceOriginInverse => Orientation.FromDeltaVector2D(-delta),
                _ => orientation
            };
        }

        private bool GetFormationObjects(List<ClusterObject> formationObjects)
        {
            foreach (ClusterObject obj in Objects)
                if (obj?.IsFormationObject() == true)
                    formationObjects.Add(obj);

            return formationObjects.Count > 0;
        }

        private void InitializeRankAndMods()
        {                  
            var popGlobals = GameDatabase.PopulationGlobalsPrototype;
            if (popGlobals == null) return;

            var difficulty = Region.TuningTable;
            if (difficulty == null) return;

            var tuningProto = difficulty.Prototype;
            if (tuningProto == null) return;

            var random = Region.Game.Random;

            HashSet<PrototypeId> overrides = HashSetPool<PrototypeId>.Instance.Get();
            GetMobAffixesFromProperties(overrides);

            var popcornRank = popGlobals.GetRankByEnum(Rank.Popcorn);
            Region.ApplyRegionAffixesEnemyBoosts(popcornRank.DataRef, overrides);

            if (overrides.Count == 0 && HasModifiableEntities() == false)
            {
                HashSetPool<PrototypeId>.Instance.Return(overrides);
                return;
            }

            HashSet<PrototypeId> exemptOverrides = HashSetPool<PrototypeId>.Instance.Get();
            ShiftExemptFromOverrides(overrides, exemptOverrides);

            List<RankPrototype> ranks = ListPool<RankPrototype>.Instance.Get();
            GetRanks(ranks);

            var rollRank = difficulty.RollRank(ranks, overrides.Count == 0);

            int numUpgrade = -1;
            if (rollRank.Rank == Rank.MiniBoss) numUpgrade = 1;

            UpgradeToRank(rollRank, ref numUpgrade);

            ranks.Clear();
            GetRanks(ranks);

            HashSet<PrototypeId> affixesSet = HashSetPool<PrototypeId>.Instance.Get();
            List<PrototypeId> slots = ListPool<PrototypeId>.Instance.Get();
            HashSet<PrototypeId> currentAffixes = HashSetPool<PrototypeId>.Instance.Get();
            HashSet<PrototypeId> excludeAffixes = HashSetPool<PrototypeId>.Instance.Get();

            foreach (var rankProto in ranks)
            {
                var rankEntryProto = tuningProto.GetDifficultyRankEntry(Region.DifficultyTierRef, rankProto);

                GetMobAffixesFromProperties(overrides);
                Region.ApplyRegionAffixesEnemyBoosts(rankProto.DataRef, overrides);
                ShiftExemptFromOverrides(overrides, exemptOverrides);
                affixesSet.Insert(overrides);

                int maxAffixes = (rankEntryProto != null) ? rankEntryProto.GetMaxAffixes() : 0;
                slots.Clear();
                for (int slot = 0; slot < maxAffixes; slot++) slots.Add(PrototypeId.Invalid); 

                if (overrides.Count > 0 && rankEntryProto != null)
                    for (int slot = maxAffixes - 1; slot >= 0; slot--)
                    {
                        var affixProto = rankEntryProto.GetAffixSlot(slot);
                        if (affixProto == null) continue;

                        if (slots[slot] == PrototypeId.Invalid)
                            foreach (var overrideRef in overrides)
                                if (affixProto.AffixTable == PrototypeId.Invalid || affixProto.AffixTablePrototype.Contains(overrideRef))
                                {
                                    slots[slot] = overrideRef;
                                    overrides.Remove(overrideRef);
                                    break;
                                }
                    }

                if (overrides.Count > 0)
                    for (int slot = maxAffixes - 1; slot >= 0; slot--)
                        if (slots[slot] == PrototypeId.Invalid)
                        {
                            var overrideRef = overrides.First();
                            slots[slot] = overrideRef;
                            overrides.Remove(overrideRef);
                            if (overrides.Count == 0) break;
                        }

                if (overrides.Count > 0)
                {
                    slots.AddRange(overrides);
                    overrides.Clear();
                }

                if (rankEntryProto != null)
                {
                    currentAffixes.Set(affixesSet);
                    excludeAffixes.Clear();

                    foreach (var slot in slots)
                        if (slot != PrototypeId.Invalid)
                        {
                            currentAffixes.Remove(slot);
                            excludeAffixes.Add(slot);
                        }

                    for (int slot = 0; slot < slots.Count; slot++)
                        if (slots[slot] == PrototypeId.Invalid)
                        {
                            var affixProto = rankEntryProto.GetAffixSlot(slot);
                            if (affixProto == null) continue;

                            var affixRef = affixProto.RollAffix(random, currentAffixes, excludeAffixes);
                            if (affixRef != PrototypeId.Invalid)
                            {
                                slots[slot] = affixRef;
                                affixesSet.Add(affixRef);
                                currentAffixes.Remove(affixRef);
                                excludeAffixes.Add(affixRef);
                            }
                        }
                }

                if (exemptOverrides.Count > 0)
                    for (int slot = 0; slot < slots.Count; slot++)
                        if (slots[slot] == PrototypeId.Invalid)
                        {
                            var overrideRef = exemptOverrides.First();
                            slots[slot] = overrideRef;
                            exemptOverrides.Remove(overrideRef);
                            if (exemptOverrides.Count == 0) break;
                        }

                if (exemptOverrides.Count > 0)
                    slots.AddRange(exemptOverrides);

                exemptOverrides.Clear();

                AssignAffixes(rankProto, slots);
            }

            foreach (var obj in Objects)
                if (obj is ClusterEntity entity)
                {
                    bool twinBoss = false;
                    foreach (var modRef in entity.Modifiers)
                        if (modRef == popGlobals.TwinEnemyBoost)
                        {
                            twinBoss = true;
                            break;
                        }

                    if (twinBoss)
                        if (entity.EntityProto.Rank != PrototypeId.Invalid && entity.EntityProto.RankPrototype.IsRankBoss)
                        {
                            var newEntity = CreateClusterEntity(entity.EntityRef);
                            if (newEntity != null)
                            {
                                newEntity.RankProto = popGlobals.TwinEnemyRank.As<RankPrototype>();
                                newEntity.Modifiers.Set(entity.Modifiers);
                            }
                            break;
                        }
                }

            HashSetPool<PrototypeId>.Instance.Return(overrides);
            HashSetPool<PrototypeId>.Instance.Return(exemptOverrides);
            ListPool<RankPrototype>.Instance.Return(ranks);
            HashSetPool<PrototypeId>.Instance.Return(affixesSet);
            ListPool<PrototypeId>.Instance.Return(slots);
            HashSetPool<PrototypeId>.Instance.Return(currentAffixes);
            HashSetPool<PrototypeId>.Instance.Return(excludeAffixes);
        }

        public override void UpgradeToRank(RankPrototype upgradeRank, ref int numUpgrade)
        {
            foreach (var clusterObject in Objects)
                clusterObject?.UpgradeToRank(upgradeRank, ref numUpgrade);
        }

        public override void AssignAffixes(RankPrototype rankProto, List<PrototypeId> affixes)
        {
            foreach (var clusterObject in Objects)
                clusterObject?.AssignAffixes(rankProto, affixes);
        }

        private static void ShiftExemptFromOverrides(HashSet<PrototypeId> overrides, HashSet<PrototypeId> exemptOverrides)
        {
            List<PrototypeId> toRemove = ListPool<PrototypeId>.Instance.Get();

            foreach (var overrideRef in overrides)
            {
                var overrideProto = overrideRef.As<EnemyBoostPrototype>();
                if (overrideProto == null || overrideProto.CountsAsAffixSlot == false)
                {
                    exemptOverrides.Add(overrideRef);
                    toRemove.Add(overrideRef);
                }
            }

            foreach (var overrideRef in toRemove)
                overrides.Remove(overrideRef);

            ListPool<PrototypeId>.Instance.Return(toRemove);
        }

        private void GetRanks(List<RankPrototype> ranks)
        {
            foreach (var obj in Objects)
            {
                if (obj is ClusterGroup group)
                    group.GetRanks(ranks);
                else if (obj is ClusterEntity entity && entity.RankProto != null)
                    if (ranks.Contains(entity.RankProto) == false)
                        ranks.Add(entity.RankProto);                    
            }
        }

        private bool HasModifiableEntities()
        {
            foreach (var obj in Objects)
            {
                if (obj is ClusterGroup group && group.HasModifiableEntities()) return true;
                else if (obj is ClusterEntity entity && entity.Flags.HasFlag(ClusterObjectFlag.HasModifiers)) return true;
            }
            return false;
        }

        private bool GetMobAffixesFromProperties(HashSet<PrototypeId> mobAffixes)
        {
            mobAffixes.Clear();
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.EnemyBoost))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId affix);
                mobAffixes.Add(affix);
            }
            return mobAffixes.Count > 0;
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

        public override ulong Spawn(SpawnGroup group, WorldEntity spawner, SpawnHeat spawnHeat, List<WorldEntity> entities)
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
                group.Reservation = Reservation;
                group.SpawnHeat = spawnHeat;
                group.SpawnCleanup = SpawnFlags.HasFlag(SpawnFlags.Cleanup);
                group.SpawnerId = spawner != null ? spawner.Id : Entity.InvalidId;
            }
            if (group == null) return SpawnGroup.InvalidId;

            foreach (var obj in Objects)
                if (obj.Spawn(group, spawner, spawnHeat, entities) == SpawnGroup.InvalidId)
                {
                    if (Parent == null) 
                        manager.RemoveSpawnGroup(group.Id);
                    return 0;
                }

            if (ObjectProto is PopulationEncounterPrototype encounter && Reservation != null)
            {
                var cell = Region.GetCellAtPosition(GetAbsolutePosition());
                if (cell != null && Reservation.Cell == cell && encounter.HasClientData())
                {
                    cell.AddEncounter(encounter.EncounterResource, Reservation.Id, encounter.UseMarkerOrientation);
                    group.EncounterCell = cell;
                }
            }

            if (ObjectProto.Riders.HasValue())
                foreach (var rider in ObjectProto.Riders)
                    if (rider is PopulationRiderBlackOutPrototype blackOutProto)
                        manager.SpawnBlackOutZoneForGroup(group, blackOutProto.BlackOutZone);

            if (BlackOutZone.Key != PrototypeId.Invalid)
                manager.SpawnBlackOutZoneForGroup(group, BlackOutZone.Key);

            return group.Id;
        }

        public bool PickPositionInSector(in Vector3 position, in Orientation orientation, int minDistance, int maxDistance, FormationFacing spawnFacing = FormationFacing.FaceParent)
        {
            if (minDistance < 0.0f || maxDistance <= 0.0f || minDistance > maxDistance || Radius == 0)
                return false;

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
                int numClusters = MathHelper.RoundDownToInt(clusterPI * distance);
                sectorPicker.Add(sector, numClusters);

                distance += clusterSize;
                maxClusterDistance += clusterSize;
                if (maxClusterDistance > maxDistance) break;
            }
            while (sectorPicker.Empty() == false)
            {
                if (sectorPicker.PickRemove(out int sector) == false) return false;
                distance = minClusterDistance + sector * clusterSize;
                int numClusters = MathHelper.RoundDownToInt(clusterPI * distance);
                int startCluster = Random.Next(numClusters);
                float clusterAngle = MathHelper.TwoPi / numClusters;

                for (int cluster = 0; cluster < numClusters; cluster++)
                {
                    int angle = (startCluster + cluster) % numClusters;
                    (float rotSin, float rotCos) = MathF.SinCos(Orientation.WrapAngleRadians(clusterAngle * angle));
                    Vector3 testPosition = new(position.X + distance * rotCos, position.Y + distance * rotSin, position.Z);
                    if (Region.GetCellAtPosition(testPosition) == null) continue;
                    Orientation testOrientation = orientation;
                    if (spawnFacing != FormationFacing.FaceParent)
                        testOrientation = DoTestFacing(spawnFacing, position - testPosition, orientation);
                    if (SetParentRelative(testPosition, testOrientation)) return true;
                }
            }
            return false;
        }

        public bool SetParentRelative(in Vector3 position, in Orientation orientation)
        {
            SetParentRelativePosition(position);
            SetParentRelativeOrientation(orientation);
            return TestLayout();
        }

        public bool PickPositionInBounds(in Aabb bound)
        {
            if (Radius == 0 || bound.Width < Radius || bound.Length < Radius) return false;

            var min = bound.Min;
            var max = bound.Max;
            var center = bound.Center;
            float clusterSize = Math.Max(Radius, 32.0f);
            List<Point2> points = ListPool<Point2>.Instance.Get();

            for (float x = min.X; x < max.X; x += clusterSize)
                for (float y = min.Y; y < max.Y; y += clusterSize)
                    points.Add(new(x, y));

            Random.ShuffleList(points);
            int tries = Math.Min(points.Count, 256);
            bool success = false;

            for (int i = 0; i < tries; i++)
            {
                var point = points[i];
                Vector3 testPosition = new(point.X, point.Y, center.Z);
                Orientation orientation = Orientation.Player;
                if (SetParentRelative(testPosition, orientation))
                {
                    success = true;
                    break;
                }
            }

            ListPool<Point2>.Instance.Return(points);
            return success;
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
        public int EncounterSpawnPhase { get; set; }
        public Bounds Bounds { get; set; }
        public RankPrototype RankProto { get; set; }
        public HashSet<PrototypeId> Modifiers { get; set; }
        public SpawnFlags SpawnFlags => Parent != null ? Parent.SpawnFlags : SpawnFlags.None;

        public PrototypeId RankRef { get => RankProto != null ? RankProto.DataRef : PrototypeId.Invalid; }

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
            
            if (Parent != null)
            {
                PrototypeId rankRef = Parent.Properties[PropertyEnum.Rank];
                RankProto = RankPrototype.DoOverride(RankProto, rankRef.As<RankPrototype>());
            }

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

            if (SpawnFlags.HasFlag(SpawnFlags.IgnoreSpawned) == false)
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

        public override ulong Spawn(SpawnGroup group, WorldEntity spawner, SpawnHeat spawnHeat, List<WorldEntity> entities)
        {
            var manager = Region.PopulationManager;
            if (group == null) return SpawnGroup.InvalidId;
            // PropertyCollection, events

            var pos = GetAbsolutePosition();
            var groupPosition = group.Transform.Translation;

            if (Region.Aabb.IntersectsXY(pos))
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

            spec.Properties = new();
            spec.Properties.FlattenCopyFrom(Parent.Properties, false);

            spec.Properties[PropertyEnum.Rank] = RankRef;

            foreach (var modRef in Modifiers)
                spec.Properties[PropertyEnum.EnemyBoost, modRef] = true;

            if (EntityProto != null)
                spec.AppendActions(EntityProto.EntitySelectorActions);

            if (EntitySelectorProto != null)
            {
                Region.EvalRegionProperties(EntitySelectorProto.EvalSpawnProperties, spec.Properties);
                spec.AppendActions(EntitySelectorProto.EntitySelectorActions);
                if (EntitySelectorProto.IgnoreMissionOwnerForTargeting)
                    spec.Properties[PropertyEnum.IgnoreMissionOwnerForTargeting] = true;
            }

            spec.MissionRef = Parent.MissionRef;
            spec.EncounterSpawnPhase = EncounterSpawnPhase;
            
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

        public override void UpgradeToRank(RankPrototype upgradeRank, ref int numUpgrade)
        {
            bool upgrade = (numUpgrade < 0) || (numUpgrade > 0);
            if (numUpgrade > 0) numUpgrade--;

            if (upgrade && Flags.HasFlag(ClusterObjectFlag.HasModifiers))
                RankProto = RankPrototype.DoOverride(RankProto, upgradeRank);
        }

        public override void AssignAffixes(RankPrototype rankProto, List<PrototypeId> affixes)
        {
            if (RankProto == rankProto)
                foreach (var affixRef in affixes)
                    if (affixRef != PrototypeId.Invalid)
                        Modifiers.Add(affixRef);

            if (EntityProto.ModifiersGuaranteed.HasValue())
                Modifiers.Insert(EntityProto.ModifiersGuaranteed);
        }
    }
}
