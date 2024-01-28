using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.Generators.Navi;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Generators;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.Missions;
using MHServerEmu.Networking;

namespace MHServerEmu.Games.Regions
{
    public class RegionSettings
    {
        public int EndlessLevel;
        public int Seed;
        public bool GenerateAreas;
        public PrototypeId DifficultyTierRef;
        public ulong InstanceAddress; // region ID
        public Aabb Bound;

        public List<PrototypeId> Affixes;
        public int Level;
        public bool DebugLevel;
        public PrototypeId RegionDataRef;
        public ulong MatchNumber;
    }

    public class Region
    {
        // Old
        public RegionPrototypeId PrototypeId { get; private set; }
        public ulong Id { get; private set; }
        public int RandomSeed { get; private set; }
        public byte[] ArchiveData { get; private set; }
        public Vector3 Min { get; private set; }
        public Vector3 Max { get; private set; }
        public CreateRegionParams CreateParams { get; private set; }

        public List<Area> AreaList { get; } = new();

        public Vector3 EntrancePosition { get; set; }
        public Vector3 EntranceOrientation { get; set; }
        public Vector3 WaypointPosition { get; set; }
        public Vector3 WaypointOrientation { get; set; }

        public int CellsInRegion { get; set; }

        // New

        public static readonly Logger Logger = LogManager.CreateLogger();
        public Aabb Bound { get; set; }
        public bool AvatarSwapEnabled { get; private set; }
        public object RestrictedRosterEnabled { get; private set; }
        public Game Game { get; private set; }

        private Area _startArea;
        public Area StartArea
        {
            get
            {
                if (_startArea == null && AreaList.Any()) _startArea = IterateAreas().First();
                return _startArea;
            }
            set
            {
                _startArea = value;
            }
        }
        public RegionPrototype RegionPrototype { get; set; }
        public RegionSettings Settings { get; private set; }
        public RegionProgressionGraph ProgressionGraph { get; set; } // Region progression graph 
        public ObjectiveGraph ObjectiveGraph { get; private set; }
        public PathCache PathCache { get; private set; }
        public SpawnMarkerRegistry SpawnMarkerRegistry { get; private set; }
        public EntityTracker EntityTracker { get; private set; } // Entity tracker
        public TuningTable Difficulty { get; private set; } // Difficulty table
        public MissionManager MissionManager { get; private set; } // Mission manager
        public EntityRegionSpatialPartition EntitySpatialPartition { get; private set; } // Entity spatial partition
        public CellSpatialPartition CellSpatialPartition { get; private set; } // Cell spatial partition
        public List<DividedStartLocation> DividedStartLocations { get; } = new();
        public int RegionLevel { get; private set; }
        public IEnumerable<Cell> Cells { get => IterateCellsInVolume(Bound); }
        public IEnumerable<Entity> Entities { get => Game.EntityManager.GetEntities(this); }
        public List<ulong> MetaGames { get; private set; } = new();

        public Region(RegionPrototypeId prototype, int randomSeed, byte[] archiveData, Vector3 min, Vector3 max, CreateRegionParams createParams) // Old
        {
            Id = IdGenerator.Generate(IdType.Region);

            PrototypeId = prototype;
            RandomSeed = randomSeed;
            ArchiveData = archiveData;
            Min = min;
            Max = max;
            CreateParams = createParams;
        }

        public void AddArea(Area area) => AreaList.Add(area); // Old

        public Region(Game game)
        {
            Game = game;
            SpawnMarkerRegistry = new(this);
            Settings = new();
            PathCache = new();
        }

        public bool Initialize(RegionSettings settings)
        {
            // "Region_Initialize" ProfileTimer
            if (Game == null) return false;
            Settings = settings;
            //Bind(this, 0xEF);

            Id = settings.InstanceAddress; // Region Id
            if (Id == 0) return false;
            PrototypeId = (RegionPrototypeId)settings.RegionDataRef;
            RegionPrototype = GameDatabase.GetPrototype<RegionPrototype>(settings.RegionDataRef);
            if (RegionPrototype == null) return false;

            RegionPrototype regionProto = RegionPrototype;
            RandomSeed = settings.Seed;
            Bound = settings.Bound;
            AvatarSwapEnabled = RegionPrototype.EnableAvatarSwap;
            RestrictedRosterEnabled = (RegionPrototype.RestrictedRoster.IsNullOrEmpty() == false);

            SetRegionLevel();

            //FlattenCopyFrom(settings.PropertyCollection, false); 
            // unk1 = settings.unk1;
            // SetProperty(settings.EndlessLevel, PropertyEnum.EndlessLevel);
            // SequenceRegionGeneratorPrototype sequenceRegionGenerator = regionProto.RegionGenerator as SequenceRegionGeneratorPrototype;
            // SetProperty(sequenceRegionGenerator != null ? sequenceRegionGenerator.EndlessLevelsPerTheme : 0, PropertyEnum.EndlessLevelsTotal);

            EntityTracker = new(this);
            //LowResMapResolution = GetLowResMapResolution();

            GlobalsPrototype globals = GameDatabase.GetGlobalsPrototype();
            if (globals == null)
            {
                Logger.Error("Unable to get globals prototype for region initialize");
                return false;
            }

            Difficulty = new(this);

            RegionDifficultySettingsPrototype difficultySettings = regionProto.GetDifficultySettings();
            if (difficultySettings != null)
            {
                Difficulty.SetTuningTable(difficultySettings.TuningTable);

                /* if (HasProperty(PropertyEnum.DifficultyIndex))
                       TuningTable.SetDifficultyIndex(GetProperty<int>(PropertyEnum.DifficultyIndex), false);
                */
            }

            CreateParams = new((uint)RegionLevel, (DifficultyTier)settings.DifficultyTierRef); // OLD params

            if (regionProto.DividedStartLocations.IsNullOrEmpty() == false)
                InitDividedStartLocations(regionProto.DividedStartLocations);

            // if (!NaviSystem.Initialize(this))  return false;
            if (!Bound.IsZero())
            {
                if (settings.GenerateAreas == false)
                {
                    InitializeSpacialPartition(Bound);
                    // NaviMesh.Initialize(Bound, 1000.0f, this);
                }
            }

            SpawnMarkerRegistry.Initialize();
            ProgressionGraph = new();
            ObjectiveGraph = new(Game, this);

            if (MissionManager != null && !MissionManager.InitializeForRegion(this)) return false;

            /* if (Settings.Affixes.Any)
             {
                 RegionAffixTablePrototype affixTableP = GameDatabase.GetPrototype<RegionAffixTablePrototype>(regionProto.AffixTable);
                 if (affixTableP != null)
                 {
                     foreach (var affix in Settings.Affixes)
                     {
                         RegionAffixPrototype regionAffixProto = GameDatabase.GetPrototype<RegionAffixPrototype>(affix);
                         if (regionAffixProto != null)
                         {
                             AdjustProperty(regionAffixProto.AdditionalLevels, PropertyEnum.EndlessLevelsTotal);
                             if (regionAffixProto.Eval != null)
                             {
                                 EvalContextData context = new (Game);
                                 context.SetVar_PropertyCollectionPtr(0, this);
                                 Eval.Run<bool>(regionAffixProto.Eval, context);
                             }
                         }
                     }
                 }
             }

             if (HasProperty(PropertyEnum.DifficultyTier) && settings.DifficultyTierRef != 0)
                 SetProperty<PrototypeDataRef>(settings.DifficultyTierRef, PropertyEnum.DifficultyTier);
            */

            if (settings.GenerateAreas)
            {
                if (!GenerateAreas())
                {
                    Logger.Error($"Failed to generate areas for\n  region: {this}\n    seed: {RandomSeed}");
                    return false;
                }
            }
            /*
            if (Settings.Affixes.Any())
            {
                RegionAffixTablePrototype affixTableProto = GameDatabase.GetPrototype<RegionAffixTablePrototype>(regionProto.AffixTable);
                if (affixTableProto != null)
                {
                    foreach (var affix in Settings.Affixes)
                    {
                        RegionAffixPrototype regionAffixProto = GameDatabase.GetPrototype<RegionAffixPrototype>(affix);
                        if (regionAffixProto != null)
                        {
                            SetProperty<bool>(true, new (PropertyEnum.RegionAffix, affix));
                            AdjustProperty(regionAffixProto.Difficulty, PropertyEnum.RegionAffixDifficulty);

                            if (regionAffixProto.CanApplyToRegion(this))
                            {
                                if (regionAffixProto.MetaState != 0)
                                    SetProperty<bool>(true, new (PropertyEnum.MetaStateApplyOnInit, regionAffixProto.MetaState));
                                if (regionAffixProto.AvatarPower != 0)
                                    SetProperty<bool>(true, new (PropertyEnum.RegionAvatarPower, regionAffixProto.AvatarPower));
                            }
                        }
                    }

                    EvalContextData context = new ();
                    context.SetReadOnlyVar_PropertyCollectionPtr(0, this);
                    int affixTier = Eval.Run<int>(affixTableProto.EvalTier, context);

                    RegionAffixTableTierEntryPrototype tierEntryProto = affixTableProto.GetByTier(affixTier);
                    if (tierEntryProto != null)
                    {
                        int value = 0;
                        ulong valueAsset = Property.PropertyEnumToAsset(PropertyEnum.LootSourceTableOverride, 1, value);
                        SetProperty<PrototypeDataRef>(tierEntryProto.LootTable, new (PropertyEnum.LootSourceTableOverride, affixTableProto.LootSource, valueAsset));
                    }
                } else 
                Logger.Warn($"Region created with affixes, but no RegionAffixTable. REGION={this} AFFIXES={Settings.Affixes}")
            }

            if (regionProto.AvatarPowers.IsNullOrEmpty() == false)
                foreach (var avatarPower in regionProto.AvatarPowers)
                    SetProperty<bool>(true, new (PropertyEnum.RegionAvatarPower, avatarPower));

            if (0 != regionProto.UITopPanel)
                SetProperty(regionProto.UITopPanel, PropertyEnum.RegionUITopPanel);

            */

            Min ??= Vector3.Zero;
            Max ??= Vector3.Zero;
            Bound ??= new Aabb(Min, Max);

            ArchiveData = new byte[] { }; // TODO: Gen ArchiveData

            return true;
        }

        private bool InitDividedStartLocations(DividedStartLocationPrototype[] dividedStartLocations)
        {
            ClearDividedStartLocations();
            if (dividedStartLocations == null) return false;

            foreach (var location in dividedStartLocations)
                DividedStartLocations.Add(new(location));

            return true;
        }

        private void ClearDividedStartLocations()
        {
            DividedStartLocations.Clear();
        }

        private void SetRegionLevel()
        {
            if (RegionLevel == 0) return;
            var regionProto = RegionPrototype;
            if (regionProto == null) return;

            if (Settings.DebugLevel == true) RegionLevel = Settings.Level;
            else if (regionProto.Level > 0) RegionLevel = regionProto.Level;
            else Logger.Error("RegionLevel <= 0");
        }

        public Aabb CalculateBound()
        {
            Aabb bound = Aabb.InvertedLimit;

            foreach (var area in IterateAreas())
                bound += area.RegionBounds;

            return bound;
        }

        public void SetBound(Aabb boundingBox)
        {
            if (boundingBox.Volume <= 0 || (boundingBox.Min == Bound.Min && boundingBox.Max == Bound.Max)) return;

            Bound = boundingBox;
            Min = Bound.Min; // OLD property
            Max = Bound.Max; // OLD property

            // NaviMesh.Initialize(Bound, 1000.0f, this);
            InitializeSpacialPartition(Bound);
        }

        private bool InitializeSpacialPartition(Aabb bound)
        {
            if (EntitySpatialPartition != null || CellSpatialPartition != null) return false;

            EntitySpatialPartition = new(bound);
            CellSpatialPartition = new(bound);

            foreach (var area in IterateAreas())
                foreach (var cell in area.CellList)
                    PartitionCell(cell, PartitionContext.Insert);

            SpawnMarkerRegistry.InitializeSpacialPartition(bound);

            return true;
        }

        public enum PartitionContext
        {
            Insert,
            Remove
        }

        public object PartitionCell(Cell cell, PartitionContext context)
        {
            if (CellSpatialPartition != null)
                switch (context)
                {
                    case PartitionContext.Insert:
                        return CellSpatialPartition.Insert(cell);
                    case PartitionContext.Remove:
                        return CellSpatialPartition.Remove(cell);
                }
            return null;
        }

        public bool GenerateAreas()
        {
            RegionGenerator regionGenerator = DRAGSystem.LinkRegionGenerator(RegionPrototype.RegionGenerator);

            regionGenerator.GenerateRegion(RandomSeed, this);

            StartArea = regionGenerator.StartArea;
            SetBound(CalculateBound());

            bool success = GenerateHelper(regionGenerator, GenerateFlag.Background)
                        && GenerateHelper(regionGenerator, GenerateFlag.PostInitialize)
            // GenerateHelper(regionGenerator, GenerateFlag.Navi);
            // GenerateNaviMesh()
                        && GenerateHelper(regionGenerator, GenerateFlag.PathCollection);
            // BuildObjectiveGraph()
            // GenerateMissionPopulation()
            success &= GenerateHelper(regionGenerator, GenerateFlag.Population)
                    && GenerateHelper(regionGenerator, GenerateFlag.PostGenerate);
            return success;
        }

        public bool GenerateHelper(RegionGenerator regionGenerator, GenerateFlag flag)
        {
            bool success = true;
            foreach (Area area in IterateAreas())
            {
                if (area == null)
                    success = false;
                else
                {
                    List<PrototypeId> areas = new() { area.GetPrototypeDataRef() };
                    success &= area.Generate(regionGenerator, areas, flag);
                    if (!area.TestStatus(GenerateFlag.Background)) Logger.Error($"{area} Not generated");
                }
            }
            return success;
        }

        public Area CreateArea(PrototypeId areaRef, Vector3 origin)
        {
            RegionManager regionManager = Game.RegionManager;
            if (regionManager == null) return null;

            AreaSettings settings = new()
            {
                Id = regionManager.AllocateAreaId(),
                AreaDataRef = areaRef,
                Origin = origin,
                RegionSettings = Settings
            };

            return AddArea(settings);
        }

        public Area AddArea(AreaSettings settings)
        {
            if (settings.AreaDataRef == 0 || settings.Id == 0 || settings.RegionSettings == null) return null;
            Area area = new(Game, this);
            if (!area.Initialize(settings))
            {
                DeallocateArea(area);
                return null;
            }
            AreaList.Add(area); // AreaMap[area.Id]
            Logger.Debug($"Adding area {area.GetPrototypeName()}, id={area.Id}, areapos = {area.Origin.ToStringFloat()}, seed = {RandomSeed}");
            return area;
        }

        public MetaStateChallengeTierEnum RegionAffixGetMissionTier()
        {
            foreach (var affix in Settings.Affixes)
            {
                var affixProto = GameDatabase.GetPrototype<RegionAffixPrototype>(affix);
                if (affixProto != null && affixProto.ChallengeTier != MetaStateChallengeTierEnum.None)
                    return affixProto.ChallengeTier;
            }
            return MetaStateChallengeTierEnum.None;
        }

        public Area GetAreaById(uint id)
        {
            foreach (Area area in AreaList)
                if (area.Id == id) return area;
            return null;
        }

        public void DestroyArea(uint id)
        {
            Area areaToRemove = GetAreaById(id);
            if (areaToRemove != null)
            {
                DeallocateArea(areaToRemove);
                AreaList.Remove(areaToRemove);
            }
        }

        private void DeallocateArea(Area area)
        {
            if (area == null) return;

            Logger.Trace($"{Game} - Deallocating area id {area.Id}, {area}");

            area.Shutdown();
        }

        public Area GetArea(PrototypeId prototypeId)
        {
            foreach (var area in AreaList)
                if ((PrototypeId)area.PrototypeId == prototypeId) return area;

            return null;
        }

        public float GetDistanceToClosestAreaBounds(Vector3 position)
        {
            float minDistance = float.MaxValue;
            foreach (var area in IterateAreas())
            {
                float distance = area.RegionBounds.DistanceToPoint2D(position);
                minDistance = Math.Min(distance, minDistance);
            }

            if (minDistance == float.MaxValue)
                Logger.Error("GetDistanceToClosestAreaBounds");
            return minDistance;
        }

        public IEnumerable<Cell> IterateCellsInVolume(Aabb bound)
        {
            if (CellSpatialPartition != null)
                return CellSpatialPartition.IterateElementsInVolume(bound);
            else
                return new CellSpatialPartition.ElementIterator();
        }

        public PrototypeId GetPrototypeDataRef()
        {
            return (PrototypeId)PrototypeId;
        }

        public override string ToString()
        {
            return $"{GameDatabase.GetPrototypeName(GetPrototypeDataRef())}, ID=0x{Id:X} ({Id}), DIFF={GameDatabase.GetFormattedPrototypeName(Settings.DifficultyTierRef)}, SEED={RandomSeed}, GAMEID={Game}";
        }

        private string GetPrototypeName()
        {
            return GameDatabase.GetPrototypeName(GetPrototypeDataRef());
        }

        public void Shutdown()
        {
            // SetStatus(2, true);
            /* TODO: When the entities will work
            int tries = 100;
            bool found;
            do
            {
                found = false;
                foreach (var entity in Entities)
                {
                    var worldEntity = entity as WorldEntity;
                    if (worldEntity != null)
                    {
                        var owner = worldEntity.GetRootOwner() as Player;
                        if (owner == null)
                        {
                            if (!worldEntity.TestStatus(1))
                            {
                                worldEntity.Destroy();
                                found = true;
                            }
                        }
                        else
                        {
                            if (worldEntity.IsInWorld())
                            {
                                worldEntity.ExitWorld();
                                found = true;
                            }
                        }
                    }
                }
            } while (found && (tries-- > 0)); 

            if (Game != null && MissionManager != null)
                MissionManager.Shutdown(this);

            while (MetaGames.Any())
            {
                var metaGameId = MetaGames.First();
                var metaGame = Game.EntityManager.GetEntityByPrototypeId(metaGameId);
                if (metaGame != null) metaGame.Destroy();

                MetaGames.Remove(metaGameId);
            }*/

            while (AreaList.Any())
            {
                var area = AreaList.First();
                DestroyArea(area.Id);
            }

            ClearDividedStartLocations();

            /* var scheduler = Game?.GameEventScheduler;
             if (scheduler != null)
             {
                 scheduler.CancelAllEvents(_events);
             }

             foreach (var entity in Game.EntityManager.GetEntities())
             {
                 if (entity is WorldEntity worldEntity)
                     worldEntity.EmergencyRegionCleanup(this);
             }
            */

            //NaviMesh.Release();
        }

        public ulong GetMatchNumber() => Settings.MatchNumber;

        public Cell GetCellAtPosition(Vector3 position)
        {
            foreach (var cell in Cells)
                if (cell.IntersectsXY(position)) return cell;
            return null;
        }

        public bool CheckMarkerFilter(PrototypeId filterRef)
        {
            if (filterRef == 0) return true;
            PrototypeId markerFilter = RegionPrototype.MarkerFilter;
            if (markerFilter == 0) return true;
            return markerFilter == filterRef;
        }

        public IEnumerable<Area> IterateAreas(Aabb bound = null)
        {
            for (int i = 0; i < AreaList.Count; i++)
            {
                Area area = AreaList[i];
                if (bound == null || area.RegionBounds.Intersects(bound))
                    yield return area;
            }
        }

        private bool FindAreaByTarget(out Area startArea, RegionConnectionTargetPrototype target)
        {
            startArea = null;
            if (target.Entity == 0) return false;
            // fix for AvengerTower
            if (StartArea.PrototypeId == AreaPrototypeId.AvengersTowerHubArea)
            {
                startArea = StartArea;
                return true; 
            }
            // fast search
            if (target.Area != 0)
            {
                foreach (Area area in AreaList)
                {
                    if (target.Area == (PrototypeId)area.PrototypeId)
                    {
                        startArea = area;
                        return true;
                    }
                }
            }
            // slow search
            foreach (Area area in AreaList)
            {
                foreach (Cell cell in area.CellList)
                {
                    if (cell.CellProto != null && cell.CellProto.InitializeSet.Markers.IsNullOrEmpty() == false)
                    {
                        foreach (var marker in cell.CellProto.InitializeSet.Markers)
                        {
                            if (marker is EntityMarkerPrototype entityMarker)
                            {
                                PrototypeId dataRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                                if (dataRef == target.Entity)
                                {
                                    startArea = area;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public bool FindWaypointMarker(PrototypeId waypointDataRef, out Vector3 waypointPosition, out Vector3 waypointOrientation)
        {
            waypointPosition = Bound.Center; // default
            waypointOrientation = new();

            if (RegionTransition.GetDestination(waypointDataRef, out RegionConnectionTargetPrototype target) == false) return false;

            if (target == null || target.Entity == 0) return false;

            if (FindAreaByTarget(out Area area, target))
            {
                foreach (Cell cell in area.CellList)
                {
                    // if (target.Cell != 0 && target.Cell != cell.CellProto.GetDataRef()) continue;
                    if (cell.CellProto != null && cell.CellProto.InitializeSet.Markers.IsNullOrEmpty() == false)
                    {
                        foreach (var marker in cell.CellProto.InitializeSet.Markers)
                        {
                            if (marker is EntityMarkerPrototype entityMarker)
                            {
                                PrototypeId dataRef = GameDatabase.GetDataRefByPrototypeGuid(entityMarker.EntityGuid);
                                if (dataRef == target.Entity)
                                {
                                    waypointPosition = cell.RegionBounds.Center + entityMarker.Position - cell.CellProto.BoundingBox.Center;
                                    waypointOrientation = entityMarker.Rotation;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public void LoadMessagesForArea(Area area, List<GameMessage> messageList, bool isStartArea)
        {
            messageList.Add(new((byte)GameServerToClientMessage.NetMessageAddArea, NetMessageAddArea.CreateBuilder()
                .SetAreaId(area.Id)
                .SetAreaPrototypeId((ulong)area.PrototypeId)
                .SetAreaOrigin(area.Origin.ToNetStructPoint3())
                .SetIsStartArea(isStartArea)
                .Build().ToByteArray()));

            foreach (Cell cell in area.CellList)
            {
                var builder = NetMessageCellCreate.CreateBuilder()
                    .SetAreaId(area.Id)
                    .SetCellId(cell.Id)
                    .SetCellPrototypeId((ulong)cell.PrototypeId)
                    .SetPositionInArea(cell.AreaPosition.ToNetStructPoint3())
                    .SetCellRandomSeed(RandomSeed)
                    .SetBufferwidth(0)
                    .SetOverrideLocationName(0);

                foreach (ReservedSpawn reservedSpawn in cell.EncounterList)
                    builder.AddEncounters(reservedSpawn.ToNetStruct());

                messageList.Add(new(builder.Build()));
                CellsInRegion++;
            }
        }

        public void LoadMessagesForConnectedAreas(Area startArea, List<GameMessage> messageList)
        {
            HashSet<Area> visitedAreas = new ();
            Queue<Area> queue = new ();

            visitedAreas.Add(startArea);
            queue.Enqueue(startArea);

            while (queue.Count > 0)
            {
                Area currentArea = queue.Dequeue();
                LoadMessagesForArea(currentArea, messageList, currentArea == startArea);
                foreach (uint subAreaId in currentArea.SubAreas)
                {
                    Area area = GetAreaById(subAreaId);
                    if (area != null) LoadMessagesForArea(area, messageList, false);
                }

                foreach (var connection in currentArea.AreaConnections)
                {
                    if (connection.ConnectedArea != null)
                    {
                        Area connectedArea = connection.ConnectedArea;
                        if (!visitedAreas.Contains(connectedArea))
                        {
                            visitedAreas.Add(connectedArea);
                            queue.Enqueue(connectedArea);
                        }
                    }                
                }
            }
                    
        }

        public GameMessage[] GetLoadingMessages(ulong serverGameId, PrototypeId waypointDataRef)
        {
            List<GameMessage> messageList = new();

            // Before changing to the actual destination region the game seems to first change into a transitional region
            messageList.Add(new(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build()));

            messageList.Add(new(NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)PrototypeId)
                .Build()));

            var regionChangeBuilder = NetMessageRegionChange.CreateBuilder()
                .SetRegionId(Id)
                .SetServerGameId(serverGameId)
                .SetClearingAllInterest(false)
                .SetRegionPrototypeId((ulong)PrototypeId)
                .SetRegionRandomSeed(RandomSeed)
                .SetRegionMin(Min.ToNetStructPoint3())
                .SetRegionMax(Max.ToNetStructPoint3())
                .SetCreateRegionParams(CreateParams.ToNetStruct());

            // can add EntitiesToDestroy here

            // empty archive data seems to cause region loading to hang for some time
            if (ArchiveData.Length > 0) regionChangeBuilder.SetArchiveData(ByteString.CopyFrom(ArchiveData));

            messageList.Add(new(regionChangeBuilder.Build()));

            // mission updates and entity creation happens here

            // why is there a second NetMessageQueueLoadingScreen?
            messageList.Add(new(NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId((ulong)PrototypeId).Build()));

            // TODO: prefetch other regions

            CellsInRegion = 0;
            // Get starArea to load by Waypoint
            if (StartArea != null)
            {
                if (RegionTransition.GetDestination(waypointDataRef, out RegionConnectionTargetPrototype target)
                        && FindAreaByTarget(out Area startArea, target))
                    LoadMessagesForConnectedAreas(startArea, messageList);
                else
                    LoadMessagesForConnectedAreas(StartArea, messageList);
            }

            messageList.Add(new(NetMessageEnvironmentUpdate.CreateBuilder().SetFlags(1).Build()));

            // Mini map
            MiniMapArchive miniMap = new(RegionManager.RegionIsHub(PrototypeId)); // Reveal map by default for hubs
            if (miniMap.IsRevealAll == false) miniMap.Map = Array.Empty<byte>();

            messageList.Add(new(NetMessageUpdateMiniMap.CreateBuilder()
                .SetArchiveData(miniMap.Serialize())
                .Build()));

            return messageList.ToArray();
        }

    }

    public class DividedStartLocation
    {
        public DividedStartLocationPrototype Location { get; }

        public DividedStartLocation(DividedStartLocationPrototype location)
        {
            Location = location;
        }
    }
 
    #region ProgressionGraph

    public class RegionProgressionGraph
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        private RegionProgressionNode _root;
        private List<RegionProgressionNode> _nodes;

        public RegionProgressionGraph() { _nodes = new(); _root = null; }

        public void SetRoot(Area area)
        {
            if (area == null) return;
            DestroyGraph();
            _root = CreateNode(null, area);
        }

        public Area GetRoot()
        {
            if (_root != null) return _root.Area;
            return null;
        }

        public RegionProgressionNode CreateNode(RegionProgressionNode parent, Area area)
        {
            if (area == null) return null;
            RegionProgressionNode node = new(parent, area);
            _nodes.Add(node);
            return node;
        }

        public void AddLink(Area parent, Area child)
        {
            if (parent == null || child == null) return;

            RegionProgressionNode foundParent = FindNode(parent);
            if (foundParent == null) return;

            RegionProgressionNode childNode = _root.FindChildNode(child, true);
            if (childNode == null)
            {
                childNode = CreateNode(foundParent, child);
                if (childNode == null) return;
            }
            else
            {
                Logger.Error($"Attempt to do a double link between a parent and child:\n parent: {foundParent.Area}\n child: {child}");
                return;
            }

            foundParent.AddChild(childNode);
        }

        public void RemoveLink(Area parent, Area child)
        {
            if (parent == null || child == null) return;

            RegionProgressionNode foundParent = FindNode(parent);
            if (foundParent == null) return;

            RegionProgressionNode childNode = _root.FindChildNode(child, true);
            if (childNode == null) return;

            foundParent.RemoveChild(childNode);
            RemoveNode(childNode);
        }

        public void RemoveNode(RegionProgressionNode deleteNode)
        {
            if (deleteNode != null) _nodes.Remove(deleteNode);
        }

        public RegionProgressionNode FindNode(Area area)
        {
            if (_root == null) return null;
            if (_root.Area == area) return _root;

            return _root.FindChildNode(area, true);
        }

        public Area GetPreviousArea(Area area)
        {
            RegionProgressionNode node = FindNode(area);
            if (node != null)
            {
                RegionProgressionNode prev = node.ParentNode;
                if (prev != null) return prev.Area;
            }
            return null;
        }

        private void DestroyGraph()
        {
            if (_root == null) return;
            _nodes.Clear();
            _root = null;
        }
    }

    public class RegionProgressionNode
    {
        public RegionProgressionNode ParentNode { get; }
        public Area Area { get; }

        private readonly List<RegionProgressionNode> _childs;

        public RegionProgressionNode(RegionProgressionNode parent, Area area)
        {
            ParentNode = parent;
            Area = area;
            _childs = new();
        }

        public void AddChild(RegionProgressionNode node) { _childs.Add(node); }

        public void RemoveChild(RegionProgressionNode node) { _childs.Remove(node); }

        public RegionProgressionNode FindChildNode(Area area, bool recurse = false)
        {
            foreach (var child in _childs)
            {
                if (child.Area == area)
                    return child;
                else if (recurse)
                {
                    RegionProgressionNode foundNode = child.FindChildNode(area, true);
                    if (foundNode != null)
                        return foundNode;
                }
            }
            return null;
        }

    }
    #endregion
}
