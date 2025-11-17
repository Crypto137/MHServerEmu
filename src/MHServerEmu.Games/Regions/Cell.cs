using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common.SpatialPartitions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Regions
{
    [Flags]
    public enum CellStatusFlag
    {
        PostInitialize = 1 << 0,
        Generated = 1 << 1,
    }

    public class Cell
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private CellStatusFlag _status;
        private float _playableNavArea;
        private float _spawnableNavArea;
        private PrototypeId _populationThemeRef;

        private int _numInterestedPlayers = 0;

        private byte _hotspotMask = 0;
        private Dictionary<PrototypeGuid, ulong> _hotspotDict = [];

        public Event<PlayerEnteredCellGameEvent> PlayerEnteredCellEvent = new();
        public Event<PlayerLeftCellGameEvent> PlayerLeftCellEvent = new();
        public uint Id { get; }

        public CellPrototype Prototype { get; private set; }
        public PrototypeId PrototypeDataRef { get => Prototype.DataRef; }
        public string PrototypeName { get => GameDatabase.GetFormattedPrototypeName(PrototypeDataRef); }

        public CellSettings Settings { get; private set; }
        public Type CellType { get; private set; }
        public int Seed { get; private set; }
        public PrototypeId PopulationThemeOverrideRef { get; private set; }
        public Aabb RegionBounds { get; private set; }
        public Region Region { get => Area?.Region; }
        public Area Area { get; private set; }
        public Game Game { get => Area?.Game; }
        public IEnumerable<Entity> Entities { get => Game.EntityManager.IterateEntities(this); } // TODO: Optimize

        public List<uint> CellConnections = new();
        public List<ReservedSpawn> Encounters { get; } = new();
        public SpawnSpecScheduler SpawnSpecScheduler { get; }
        public float PlayableArea { get => (_playableNavArea != -1.0) ? _playableNavArea : 0.0f; }
        public float SpawnableArea { get => (_spawnableNavArea != -1.0) ? _spawnableNavArea : 0.0f; }
        public PopulationArea PopulationArea { get => Area.PopulationArea ; }
        public PopulationManager PopulationManager { get => Region.PopulationManager; }
        public CellRegionSpatialPartitionLocation SpatialPartitionLocation { get; }
        public Vector3 AreaOffset { get; private set; }
        public Vector3 AreaPosition { get; private set; }
        public Orientation AreaOrientation { get; private set; }
        public Transform3 AreaTransform { get; private set; }
        public Transform3 RegionTransform { get; private set; }

        public bool HasAnyInterest { get => _numInterestedPlayers > 0; }
        public bool HasNavigationData { get => Prototype.HasNavigationData; }

        public Cell(Area area, uint id)
        {
            RegionBounds = Aabb.Zero;
            AreaPosition = Vector3.Zero;
            AreaOrientation = Orientation.Zero;
            Area = area;
            Id = id;
            _playableNavArea = -1.0f;
            _spawnableNavArea = -1.0f;
            SpatialPartitionLocation = new(this);
            SpawnSpecScheduler = new();
        }

        public bool Initialize(CellSettings settings)
        {
            if (Area == null) return false;
            if (settings.CellRef == PrototypeId.Invalid) return false;

            Prototype = GameDatabase.GetPrototype<CellPrototype>(settings.CellRef);
            if (Prototype == null) return false;

            _spawnableNavArea = Prototype.NaviPatchSource.SpawnableArea;
            _playableNavArea = Prototype.NaviPatchSource.PlayableArea;

            if (_playableNavArea == -1.0f)
                _playableNavArea = 0.0f;

            if (_spawnableNavArea == -1.0f && _playableNavArea >= 0.0f)
                _spawnableNavArea = _playableNavArea;

            CellType = Prototype.Type;
            Seed = settings.Seed;
            PopulationThemeOverrideRef = settings.PopulationThemeOverrideRef;            

            if (settings.ConnectedCells != null && settings.ConnectedCells.Count > 0)
                CellConnections.AddRange(settings.ConnectedCells);

            Settings = settings;
            SetAreaPosition(settings.PositionInArea, settings.OrientationInArea);

            _populationThemeRef = PopulationThemeOverrideRef; // override this?

            return true;
        }

        public bool PostInitialize()
        {
            MarkerSetOptions options = MarkerSetOptions.Default;            
            if (Prototype.IsOffsetInMapFile == false) options |= MarkerSetOptions.NoOffset;
            if (_status.HasFlag(CellStatusFlag.PostInitialize) == false)
            {
                _status |= CellStatusFlag.PostInitialize;
                InstanceMarkerSet(Prototype.InitializeSet, Transform3.Identity(), options);
            }

            if (Prototype.HotspotPrototypes.HasValue())
                for (int i = 0; i < Prototype.HotspotPrototypes.Length; i++)
                    if (Prototype.HotspotPrototypes[i] != PrototypeGuid.Invalid)
                        _hotspotMask |= (byte)(1 << i);

            return true;
        }

        public bool GetHotspotIndexData(Cell previousCell, int index, byte hotspotData, out byte outData)
        {
            outData = 0;

            if (this != previousCell || _hotspotMask == 0 || hotspotData == 0) return false;

            var oldProto = previousCell.Prototype;
            var newProto = Prototype;

            if (previousCell.PrototypeDataRef == PrototypeDataRef)
            {
                byte indexData = (byte)(1 << index);
                if ((hotspotData & indexData) != 0)
                {
                    outData = indexData;
                    return true;
                }
                return false;
            }

            var prevGuid = oldProto.HotspotPrototypes[index];
            int newIndex = Array.IndexOf(newProto.HotspotPrototypes, prevGuid);
            if (newIndex > 0 && newIndex < 8)
            {
                byte indexData = (byte)(1 << newIndex);
                if ((hotspotData & indexData) != 0)
                {
                    outData = indexData;
                    return true;
                }
            }

            return false;
        }

        public void OnHotspotEnter(WorldEntity whom, PrototypeGuid hotspotGuid)
        {
            if (GetHotspot(hotspotGuid, out Hotspot hotspot))
                hotspot.OnOverlapBegin(whom, whom.RegionLocation.Position, whom.RegionLocation.Position);
        }

        private bool GetHotspot(PrototypeGuid hotspotGuid, out Hotspot hotspot)
        {
            hotspot = null;
            if (hotspotGuid == PrototypeGuid.Invalid) return false;

            var manager = Game.EntityManager;
            var region = Region;

            if (_hotspotDict.TryGetValue(hotspotGuid, out ulong hotspotId))
            {
                hotspot = manager.GetEntity<Hotspot>(hotspotId);
                if (hotspot != null)
                    return true;
                else
                    _hotspotDict.Remove(hotspotGuid);
            }

            if (manager.IsDestroyingAllEntities || region.TestStatus(RegionStatus.Shutdown)) 
                return false;

            var hotspotRef = GameDatabase.GetDataRefByPrototypeGuid(hotspotGuid);
            var hotspotProto = GameDatabase.GetPrototype<WorldEntityPrototype>(hotspotRef);
            if (hotspotProto == null) return false;

            using EntitySettings hotspotSettings = ObjectPoolManager.Instance.Get<EntitySettings>();
            hotspotSettings.EntityRef = hotspotRef;
            hotspotSettings.HotspotSkipCollide = true;

            using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            int level = region.RegionLevel;
            settingsProperties[PropertyEnum.CharacterLevel] = level;
            settingsProperties[PropertyEnum.CombatLevel] = level;
            hotspotSettings.Properties = settingsProperties;

            hotspot = manager.CreateEntity(hotspotSettings) as Hotspot;
            if (hotspot == null) return false;
            _hotspotDict[hotspotGuid] = hotspot.Id;
           
            hotspot.EnterWorld(region, RegionBounds.Center, Orientation.Zero);
            hotspot.SetSimulated(true);

            return true;
        }

        public void OnHotspotLeave(WorldEntity whom, PrototypeGuid hotspotGuid)
        {
            if (GetHotspot(hotspotGuid, out Hotspot hotspot))
                hotspot.OnOverlapEnd(whom);
        }

        public void Generate()
        {
            if (_status.HasFlag(CellStatusFlag.Generated) == false)
            {
                _status |= CellStatusFlag.Generated;
                Region.CellCreatedEvent.Invoke(new(this));
                SpawnMarkerSet(MarkerSetOptions.NoSpawnMissionAssociated);

                PostGenerate();
            }
        }

        public void PostGenerate()
        {
            // SpawnMarker Prop type
            VisitPropSpawns(new InstanceMarkerSetPropSpawnVisitor(this));
        }

        public void Shutdown()
        {
            var manager = Game.EntityManager;
            foreach (ulong hotspotId in _hotspotDict.Values)
            {
                var hotspot = manager.GetEntity<Hotspot>(hotspotId);
                hotspot?.OnExitedWorld();
            }
            _hotspotDict.Clear();

            Region region = Region;
            if (region != null && SpatialPartitionLocation.IsValid())
                region.PartitionCell(this, RegionPartitionContext.Remove);
        }

        public void SetAreaPosition(Vector3 positionInArea, Orientation orientationInArea)
        {
            if (Prototype == null) return;

            if (SpatialPartitionLocation.IsValid())
                Region.PartitionCell(this, RegionPartitionContext.Remove);

            AreaPosition = positionInArea;
            AreaOrientation = orientationInArea;

            AreaTransform = Transform3.BuildTransform(positionInArea, orientationInArea);
            RegionTransform = Transform3.BuildTransform(positionInArea + Area.Origin, orientationInArea);

            AreaOffset = Area.AreaToRegion(positionInArea);

            RegionBounds = Prototype.BoundingBox.Translate(AreaOffset);
            RegionBounds.RoundToNearestInteger();

            if (SpatialPartitionLocation.IsValid() == false)
                Region.PartitionCell(this, RegionPartitionContext.Insert);
        }

        public void AddNavigationDataToRegion()
        {
             Region region = Region;
             if (region == null) return;
             NaviMesh naviMesh = region.NaviMesh;
             if (Prototype == null) return;

             Transform3 transform;
             if (Prototype.IsOffsetInMapFile == false)
                 transform = RegionTransform;
             else
                 transform = Transform3.BuildTransform(Area.Origin, Orientation.Zero);

             if (naviMesh.Stitch(Prototype.NaviPatchSource.NaviPatch, transform) == false) return;
             if (naviMesh.StitchProjZ(Prototype.NaviPatchSource.PropPatch, transform) == false) return;

             VisitPropSpawns(new NaviPropSpawnVisitor(naviMesh, transform)); 
             VisitEncounters(new NaviEncounterVisitor(naviMesh, transform)); // this code used ?
        }

        public void AddCellConnection(uint id)
        {
            CellConnections.Add(id);
        }

        public static bool DetermineType(ref Type type, Vector3 position)
        {
            Vector3 northVector = new(1, 0, 0);
            Vector3 eastVector = new(0, 1, 0);

            Vector3 normalizedVector = Vector3.Normalize2D(position);

            float northDot = Vector3.Dot(northVector, normalizedVector);
            float eastDot = Vector3.Dot(eastVector, normalizedVector);

            if (northDot >= 0.75)
            {
                type |= Type.N;
                return true;
            }
            else if (northDot <= -0.75)
            {
                type |= Type.S;
                return true;
            }
            else if (eastDot >= 0.75)
            {
                type |= Type.E;
                return true;
            }
            else if (eastDot <= -0.75)
            {
                type |= Type.W;
                return true;
            }

            return false;
        }

        public bool InstanceMarkerSet(MarkerSetPrototype markerSet, in Transform3 transform, MarkerSetOptions instanceMarkerSetOptions)
        {
            if (instanceMarkerSetOptions.HasFlag(MarkerSetOptions.SpawnMissionAssociated) &&
                instanceMarkerSetOptions.HasFlag(MarkerSetOptions.NoSpawnMissionAssociated))
            {
                return Logger.WarnReturn(false,
                    "InstanceMarkerSet(): SpawnMissionAssociated and NoSpawnMissionAssociated cannot be set at the same time");
            }

            if (markerSet.Markers.HasValue())
            {
                foreach (MarkerPrototype marker in markerSet.Markers)
                {
                    if (marker != null)
                        SpawnMarker(marker, transform, instanceMarkerSetOptions);
                }
            }

            return true;
        }

        public void SpawnMarker(MarkerPrototype marker, in Transform3 transform, MarkerSetOptions options)
        {
            if (marker is not EntityMarkerPrototype entityMarker) return;

            PrototypeId filterRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.FilterGuid);
            if (Region.CheckMarkerFilter(filterRef) == false) return;

            PrototypeId dataRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
            Prototype entity = GameDatabase.GetPrototype<Prototype>(dataRef);
                                
            if (entity is BlackOutZonePrototype blackOutZone)   // Spawn Blackout zone
                SpawnBlackOutZone(entityMarker, blackOutZone, transform, options);
            else if (entity is WorldEntityPrototype worldEntity) // Spawn Entity from Cell
                SpawnEntityMarker(entityMarker, worldEntity, transform, options);
        }

        private void SpawnBlackOutZone(EntityMarkerPrototype entityMarker, BlackOutZonePrototype blackOutZone, in Transform3 transform, MarkerSetOptions options)
        {
            CalcMarkerTransform(entityMarker, transform, options, out Vector3 position, out _);
            PopulationManager.CreateBlackOutZone(position, blackOutZone.BlackOutRadius, PrototypeId.Invalid);
        }

        public void SpawnEntityMarker(EntityMarkerPrototype entityMarker, WorldEntityPrototype entityProto, in Transform3 transform, MarkerSetOptions options)
        {
            if (options.HasFlag(MarkerSetOptions.SpawnMissionAssociated) || options.HasFlag(MarkerSetOptions.NoSpawnMissionAssociated))
            {
                bool missionAssociated = GameDatabase.InteractionManager.IsMissionAssociated(entityProto);
                bool spawn = missionAssociated ? options.HasFlag(MarkerSetOptions.SpawnMissionAssociated) : options.HasFlag(MarkerSetOptions.NoSpawnMissionAssociated);
                if (spawn == false) return;
            }

            CalcMarkerTransform(entityMarker, transform, options, out Vector3 entityPosition, out Orientation entityOrientation);
            if (RegionBounds.Intersects(entityPosition) == false) entityPosition.RoundToNearestInteger();

            var region = Region;
            var destructibleKeyword = GameDatabase.KeywordGlobalsPrototype.DestructibleKeywordPrototype;            
            if (region.Prototype.RespawnDestructibles && entityProto.HasKeyword(destructibleKeyword))
            {
                SpawnGroup group = PopulationManager.CreateSpawnGroup();
                group.Transform = Transform3.BuildTransform(entityPosition, entityOrientation);
                group.SpawnCleanup = false;

                SpawnSpec spec = PopulationManager.CreateSpawnSpec(group);
                spec.EntityRef = entityProto.DataRef;
                spec.Transform = Transform3.Identity();
                spec.SnapToFloor = SpawnSpec.SnapToFloorConvert(entityMarker.OverrideSnapToFloor, entityMarker.OverrideSnapToFloorValue);
                SpawnSpecScheduler.Schedule(spec);
                return;
            }

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = entityProto.DataRef;

            if (entityMarker.OverrideSnapToFloor)
                settings.OptionFlags |= EntitySettingsOptionFlags.HasOverrideSnapToFloor;

            if (entityMarker.OverrideSnapToFloorValue)
                settings.OptionFlags |= EntitySettingsOptionFlags.OverrideSnapToFloorValue;

            // 13763955919309774578 == Resource/Cells/Hydra_Island/MandarinLair/Mandarin_C/Mandarin_Super_X1_Y0_A.cell
            // 3814814281271024430  == Entity/Props/Destructibles/DestructibleMandarinPowerCell.prototype
            if (PrototypeDataRef == (PrototypeId)13763955919309774578 && entityProto.DataRef == (PrototypeId)3814814281271024430)
                settings.OptionFlags |= EntitySettingsOptionFlags.HasOverrideSnapToFloor;   // Fix Mandarin

            if (entityProto.Bounds != null)
                entityPosition.Z += entityProto.Bounds.GetBoundHalfHeight();

            using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            settings.Properties = settingsProperties;
            int level = Area.GetCharacterLevel(entityProto); 
            settings.Properties[PropertyEnum.CharacterLevel] = level;
            settings.Properties[PropertyEnum.CombatLevel] = level;

            settings.Position = entityPosition;
            settings.Orientation = entityOrientation;
            settings.RegionId = region.Id;
            settings.Cell = this;

            Game.EntityManager.CreateEntity(settings);
        }

        public void CalcMarkerTransform(EntityMarkerPrototype entityMarker, in Transform3 transform, MarkerSetOptions options,
            out Vector3 markerPosition, out Orientation markerOrientation)
        {
            Transform3 markerTransform = transform * Transform3.BuildTransform(entityMarker.Position, entityMarker.Rotation);

            if (options.HasFlag(MarkerSetOptions.NoOffset))
                markerTransform = RegionTransform * markerTransform;
            else
                markerTransform = Transform3.BuildTransform(AreaOffset, Orientation.Zero) * markerTransform;

            markerPosition = markerTransform.Translation;
            markerOrientation = Orientation.FromTransform3(markerTransform);
        }

        public static Type BuildTypeFromWalls(Walls walls)
        {
            Type type = Type.None;

            if (!walls.HasFlag(Walls.N)) type |= Type.N;
            if (!walls.HasFlag(Walls.E)) type |= Type.E;
            if (!walls.HasFlag(Walls.S)) type |= Type.S;
            if (!walls.HasFlag(Walls.W)) type |= Type.W;

            if (!walls.HasFlag(Walls.E | Walls.N) && walls.HasFlag(Walls.NE)) type |= Type.dNE;
            if (!walls.HasFlag(Walls.S | Walls.E) && walls.HasFlag(Walls.SE)) type |= Type.dSE;
            if (!walls.HasFlag(Walls.W | Walls.S) && walls.HasFlag(Walls.SW)) type |= Type.dSW;
            if (!walls.HasFlag(Walls.W | Walls.N) && walls.HasFlag(Walls.NW)) type |= Type.dNW;

            return type;
        }

        public static Walls WallsRotate(Walls walls, int clockwiseRotation)
        {
            if (clockwiseRotation == 0 || clockwiseRotation >= 8)
                return walls;

            int rotatedWalls = ((int)walls & 0xFF << clockwiseRotation);
            Walls ret = (walls & Walls.C) | (Walls)((rotatedWalls | (rotatedWalls >> 8)) & 0xFF);
            
            if (ret >= Walls.All)
                return walls;

            return ret;
        }

        public override string ToString()
        {
            return $"{GameDatabase.GetPrototypeName(PrototypeDataRef)}, cellid={Id}, cellpos={RegionBounds.Center}, game={Game}";
        }

        public bool IntersectsXY(Vector3 position)
        {
            return RegionBounds.IntersectsXY(position);
        }

        public Vector3 CalcMarkerPosition(Vector3 markerPos)
        {
            return RegionBounds.Center + markerPos - Prototype.BoundingBox.Center;
        }

        private void VisitPropSpawns(PropSpawnVisitor visitor)
        {
            PropTable propTable = Area.PropTable;
            if (propTable == null) return;

            int randomSeed = Seed;
            GRandom random = new(randomSeed);
            int randomNext = 0;

            CellPrototype cellProto = Prototype;
            if (cellProto == null) return;

            foreach (MarkerPrototype marker in cellProto.MarkerSet.Markers)
            {
                if (marker is not EntityMarkerPrototype entityMarker) continue;
                PrototypeId propMarkerRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);

                var propMarkerProto = entityMarker.GetMarkedPrototype<PropMarkerPrototype>();
                if (propMarkerProto != null && propMarkerRef != PrototypeId.Invalid && propMarkerProto.Type == MarkerType.Prop)
                {
                    PrototypeId propDensityRef = Area.Prototype.PropDensity;
                    if (propDensityRef != PrototypeId.Invalid)
                    {
                        var propDensityProto = GameDatabase.GetPrototype<PropDensityPrototype>(propDensityRef);
                        if (propDensityProto != null && random.Next(0, 101) > propDensityProto.GetPropDensity(propMarkerRef))
                            continue;
                    }

                    if (propTable.GetRandomPropMarkerOfType(random, propMarkerRef, out var propGroup))
                        visitor.Visit(randomSeed + randomNext++, propTable, propGroup.PropSetRef, propGroup.PropGroup, entityMarker);
                }
            }
        }

        public void AddEncounter(AssetId asset, int id, bool useMarkerOrientation)
        {
            Encounters.Add(new(asset, id, useMarkerOrientation));
        }

        public void RemoveEncounter(int id)
        {
            var reservedSpawn = Encounters.FirstOrDefault(enc => enc.Id == id);
            if (reservedSpawn != null) Encounters.Remove(reservedSpawn);
        }

        public void VisitEncounters(EncounterVisitor visitor)
        {
            // server doesn't have Encounters yet
            foreach (var encounter in Encounters)
            {
                PrototypeId encounterResourceRef = GameDatabase.GetDataRefByAsset(encounter.Asset);
                if (encounterResourceRef == PrototypeId.Invalid) continue;
                var encounterProto = GameDatabase.GetPrototype<EncounterResourcePrototype>(encounterResourceRef);
                if (encounterProto == null) continue;

                SpawnReservation reservation = Region.SpawnMarkerRegistry.GetReservationInCell(Id, encounter.Id);
                PopulationEncounterPrototype popProto = null;
                PrototypeId missionRef = PrototypeId.Invalid;
                visitor.Visit(encounterResourceRef, reservation, popProto, missionRef, encounter.UseMarkerOrientation);
            }
        }

        public bool FindTargetLocation(ref Vector3 markerPos, ref Orientation markerRot, PrototypeId entityProtoRef)
        {
            if (Prototype == null) return false;
            if (Prototype.InitializeSet.Markers.IsNullOrEmpty()) return false;

            foreach (MarkerPrototype marker in Prototype.InitializeSet.Markers)
            {
                if (marker is not EntityMarkerPrototype entityMarker)
                    continue;

                PrototypeId markerEntityProtoRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                if (markerEntityProtoRef == entityProtoRef)
                {
                    markerPos = CalcMarkerPosition(marker.Position) + TransitionPrototype.CalcSpawnOffset(entityMarker);
                    markerRot = entityMarker.Rotation;
                    return true;
                }
            }

            return false;
        }

        public void BlackOutZonesRebuild()
        {
            if (PlayableArea == 0.0f) return;

            var region = Region;
            var naviMesh = region.NaviMesh;
            if (naviMesh.IsMeshValid == false) return;

            foreach (var zone in region.PopulationManager.IterateBlackOutZoneInVolume(RegionBounds))
                naviMesh.SetBlackOutZone(zone.Sphere.Center, zone.Sphere.Radius);

            _spawnableNavArea = naviMesh.CalcSpawnableArea(RegionBounds);
            PopulationArea.UpdateSpawnCell(this);
        }

        public void EnemySpawn()
        {
            PopulationArea.AddEnemyWeight(this);
        }

        public void EnemyDespawn()
        {
            PopulationArea.RemoveEnemyWeight(this);
        }

        public void SpawnMarkerSet(MarkerSetOptions options)
        {
            options |= MarkerSetOptions.Default;
            CellPrototype cellProto = Prototype;

            if (cellProto.IsOffsetInMapFile == false)
                options |= MarkerSetOptions.NoOffset;

            var districtRef = Area.DistrictDataRef;
            if (districtRef != PrototypeId.Invalid)
            {
                var districtProto = GameDatabase.GetPrototype<DistrictPrototype>(districtRef);
                if (districtProto != null)
                    InstanceMarkerSet(districtProto.MarkerSet, Transform3.Identity(), MarkerSetOptions.None);
            }

            InstanceMarkerSet(cellProto.MarkerSet, Transform3.Identity(), options);
        }

        public PrototypeId GetPopulationTheme(PopulationPrototype populationProto)
        {
            if (_populationThemeRef == PrototypeId.Invalid) 
                _populationThemeRef = populationProto.PickTheme(Game.Random);
            return _populationThemeRef;
        }

        public bool GetEntitiesInCellBounds(List<WorldEntity> entityList)
        {
            Region region = Region;
            if (region == null) return Logger.WarnReturn(false, "GetEntitiesInCellBounds(): region == null");

            region.GetEntitiesInVolume(entityList, RegionBounds, new(EntityRegionSPContextFlags.All));
            return true;
        }

        public void OnAddedToAOI()
        {
            Generate();
            _numInterestedPlayers++;
            //Logger.Debug($"OnAddedToAOI(): {PrototypeName}[{Id}] (_numInterestedPlayers={_numInterestedPlayers})");

            if (_numInterestedPlayers == 1)
            {
                SpawnSpecScheduler.Spawn(false);

                List<WorldEntity> entityList = ListPool<WorldEntity>.Instance.Get();
                GetEntitiesInCellBounds(entityList);

                foreach (WorldEntity worldEntity in entityList)
                    worldEntity.UpdateSimulationState();

                ListPool<WorldEntity>.Instance.Return(entityList);
            } 
            else
                SpawnSpecScheduler.Spawn(true);

            Region.SpawnMarkerRegistry.OnSimulation(this, _numInterestedPlayers);
        }

        public void OnRemovedFromAOI()
        {
            _numInterestedPlayers--;
            //Logger.Debug($"OnRemovedFromAOI(): {PrototypeName}[{Id}] (_numInterestedPlayers={_numInterestedPlayers})");

            if (_numInterestedPlayers < 0)
            {
                Logger.Warn("OnRemovedFromAOI(): _numInterestedPlayers < 0");
                _numInterestedPlayers = 0;
            }

            if (_numInterestedPlayers == 0)
            {
                List<WorldEntity> entityList = ListPool<WorldEntity>.Instance.Get();
                GetEntitiesInCellBounds(entityList);

                foreach (WorldEntity worldEntity in entityList)
                    worldEntity.UpdateSimulationState();

                ListPool<WorldEntity>.Instance.Return(entityList);
            }
        }

        #region Enums

        [AssetEnum((int)None)]      // DRAG/RegionGenerators/Edges.type
        [Flags]
        public enum Type
        {
            None = 0,        // 0000
            N = 1,           // 0001
            E = 2,           // 0010
            S = 4,           // 0100
            W = 8,           // 1000
            NS = 5,          // 0101
            EW = 10,         // 1010
            NE = 3,          // 0011
            NW = 9,          // 1001
            ES = 6,          // 0110
            SW = 12,         // 1100
            ESW = 14,        // 1110
            NSW = 13,        // 1101
            NEW = 11,        // 1011
            NES = 7,         // 0111
            NESW = 15,       // 1111
            // Dot
            dN = 128,        // 1000 0000
            dE = 64,         // 0100 0000
            dS = 32,         // 0010 0000
            dW = 16,         // 0001 0000
            dNE = 192,       // 1100 0000
            dSE = 96,        // 0110 0000
            dSW = 48,        // 0011 0000
            dNW = 144,       // 1001 0000
            NESWdNW = 159,   // 1001 1111
            NESWdNE = 207,   // 1100 1111
            NESWdSW = 63,    // 0011 1111
            NESWdSE = 111,   // 0110 1111
            dNESW = 240,     // 1111 0000
            DotMask = 480,   // 1 1110 0000 !!! Error Mask
            // c
            NESWcN = 351,    // 1 0101 1111
            NESWcE = 303,    // 1 0010 1111
            NESWcS = 159,    // 0 0111 1111
            NESWcW = 207,    // 0 1100 1111
        }

        [Flags]
        public enum Walls
        {
            None = 0,   // 000000000
            N = 1,      // 000000001
            NE = 2,     // 000000010
            E = 4,      // 000000100
            SE = 8,     // 000001000
            S = 16,     // 000010000
            SW = 32,    // 000100000
            W = 64,     // 001000000
            NW = 128,   // 010000000
            C = 256,    // 100000000
            All = 511,  // 111111111
        }

        [AssetEnum((int)WideNESW)]      // DRAG/CellWallTypes.type
        [Flags]
        public enum WallGroup
        {
            N = 254,
            E = 251,
            S = 239,
            W = 191,
            NE = 250,
            ES = 235,
            SW = 175,
            NW = 190,
            NS = 238,
            EW = 187,
            NES = 234,
            ESW = 171,
            NSW = 174,
            NEW = 186,
            NESW = 170,
            WideNE = 248,
            WideES = 227,
            WideSW = 143,
            WideNW = 62,
            WideNES = 224,
            WideESW = 131,
            WideNSW = 14,
            WideNEW = 56,
            WideNESW = 0,
            WideNESWcN = 130,
            WideNESWcE = 10,
            WideNESWcS = 40,
            WideNESWcW = 160,
        }

        [Flags]
        public enum Filler
        {
            N = 1,
            NE = 2,
            E = 4,
            SE = 8,
            S = 16,
            SW = 32,
            W = 64,
            NW = 128,
            C = 256,
            None = 0,
        }

        #endregion
    }

    public class CellRegionSpatialPartitionLocation : QuadtreeLocation<Cell>
    {
        public CellRegionSpatialPartitionLocation(Cell element) : base(element) { }
        public override Aabb GetBounds() => Element.RegionBounds;
    }

    public class CellSpatialPartition : Quadtree<Cell>
    {
        public CellSpatialPartition(in Aabb bound) : base(bound, 128.0f) { }

        public override QuadtreeLocation<Cell> GetLocation(Cell element) => element.SpatialPartitionLocation;
        public override Aabb GetElementBounds(Cell element) => element.RegionBounds;
    }
}
