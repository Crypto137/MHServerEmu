using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Generators.Regions;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions.ObjectiveGraphs;

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

        public bool GenerateEntities;
        public bool GenerateLog;
    }

    public class Region : IMissionManagerOwner
    {
        // Old
        private static readonly IdGenerator IdGenerator = new(IdType.Region, 0);

        public RegionPrototypeId PrototypeId { get; private set; }   
        public byte[] ArchiveData { get; set; }
        public CreateRegionParams CreateParams { get; private set; }

        // New
        public readonly object Lock = new();
        public ulong Id { get; private set; } // InstanceAddress
        public int RandomSeed { get; private set; }
        public Dictionary<uint, Area> Areas { get; } = new();  

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
                if (_startArea == null && Areas.Any()) _startArea = IterateAreas().First();
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
        public NaviSystem NaviSystem { get; private set; }
        public NaviMesh NaviMesh { get; private set; }
        public List<DividedStartLocation> DividedStartLocations { get; } = new();
        public int RegionLevel { get; private set; }
        public IEnumerable<Cell> Cells { get => IterateCellsInVolume(Bound); }
        public IEnumerable<Entity> Entities { get => Game.EntityManager.GetEntities(this); }
        public List<ulong> MetaGames { get; private set; } = new();
        public ConnectionNodeList Targets { get; private set; }
        public PopulationManager PopulationManager { get; private set; }

        public Region(RegionPrototypeId prototype, int randomSeed, byte[] archiveData, CreateRegionParams createParams) // Old
        {
            Id = IdGenerator.Generate();

            PrototypeId = prototype;
            RandomSeed = randomSeed;
            ArchiveData = archiveData;
            CreateParams = createParams;

            NaviSystem = new();
            NaviMesh = new(NaviSystem);
        }

        public Region(Game game)
        {
            Game = game;
            SpawnMarkerRegistry = new(this);
            Settings = new();
            PathCache = new();

            NaviSystem = new();
            NaviMesh = new(NaviSystem);
        }

        public bool Initialize(RegionSettings settings)
        {
            // "Region_Initialize" ProfileTimer
            if (Game == null) return false;

            MissionManager = new MissionManager(Game, this);
            // CreateUIDataProvider(Game);
            PopulationManager = new(Game, this);

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
            RestrictedRosterEnabled = (RegionPrototype.RestrictedRoster.HasValue());

            SetRegionLevel();

            //FlattenCopyFrom(settings.PropertyCollection, false); 
            // unk1 = settings.unk1;
            // SetProperty(settings.EndlessLevel, PropertyEnum.EndlessLevel);
            // SequenceRegionGeneratorPrototype sequenceRegionGenerator = regionProto.RegionGenerator as SequenceRegionGeneratorPrototype;
            // SetProperty(sequenceRegionGenerator != null ? sequenceRegionGenerator.EndlessLevelsPerTheme : 0, PropertyEnum.EndlessLevelsTotal);

            EntityTracker = new(this);
            //LowResMapResolution = GetLowResMapResolution();

            GlobalsPrototype globals = GameDatabase.GlobalsPrototype;
            if (globals == null)
                return Logger.ErrorReturn(false, "Unable to get globals prototype for region initialize");

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

            if (regionProto.DividedStartLocations.HasValue())
                InitDividedStartLocations(regionProto.DividedStartLocations);

            if (NaviSystem.Initialize(this) == false) return false;
            if (Bound.IsZero() == false) {
                if (settings.GenerateAreas) Logger.Warn("Bound is not Zero with GenerateAreas On");                
                InitializeSpacialPartition(Bound);
                NaviMesh.Initialize(Bound, 1000.0f, this);
            }

            SpawnMarkerRegistry.Initialize();
            ProgressionGraph = new();
            ObjectiveGraph = new(Game, this);

            if (MissionManager != null && MissionManager.InitializeForRegion(this) == false) return false;

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

            Targets = RegionTransition.BuildConnectionEdges(settings.RegionDataRef); // For Teleport system

            if (regionProto.MetaGames.HasValue())
                foreach (var metaGameRef in regionProto.MetaGames)
                {
                    EntitySettings metaSettings = new();
                    metaSettings.RegionId = Id;
                    metaSettings.EntityRef = metaGameRef;
                    MetaGame metagame = Game.EntityManager.CreateEntity(metaSettings) as MetaGame;                    
                }

            if (settings.GenerateAreas)
            {
                if (GenerateAreas(settings.GenerateLog) == false)
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

            if (regionProto.AvatarPowers.HasValue())
                foreach (var avatarPower in regionProto.AvatarPowers)
                    SetProperty<bool>(true, new (PropertyEnum.RegionAvatarPower, avatarPower));

            if (0 != regionProto.UITopPanel)
                SetProperty(regionProto.UITopPanel, PropertyEnum.RegionUITopPanel);

            */

            Bound ??= new Aabb(Vector3.Zero, Vector3.Zero);

            ArchiveData = new byte[] { }; // TODO: Gen ArchiveData

            return true;
        }

        public void RegisterMetaGame(MetaGame metaGame)
        {
            if (metaGame != null) MetaGames.Add(metaGame.Id);
        }

        public void UnRegisterMetaGame(MetaGame metaGame)
        {
            if (metaGame != null) MetaGames.Remove(metaGame.Id);
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

            NaviMesh.Initialize(Bound, 1000.0f, this);
            InitializeSpacialPartition(Bound);
        }

        private bool InitializeSpacialPartition(Aabb bound)
        {
            if (EntitySpatialPartition != null || CellSpatialPartition != null) return false;

            EntitySpatialPartition = new(bound);
            CellSpatialPartition = new(bound);

            foreach (var area in IterateAreas())
                foreach (var cellItr in area.Cells)
                    PartitionCell(cellItr.Value, PartitionContext.Insert);

            SpawnMarkerRegistry.InitializeSpacialPartition(bound);
            PopulationManager.InitializeSpacialPartition(bound);

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

        public bool GenerateAreas(bool log)
        {
            RegionGenerator regionGenerator = DRAGSystem.LinkRegionGenerator(RegionPrototype.RegionGenerator);

            regionGenerator.GenerateRegion(log, RandomSeed, this);

            StartArea = regionGenerator.StartArea;
            SetBound(CalculateBound());

            bool success = GenerateHelper(regionGenerator, GenerateFlag.Background)
                        && GenerateHelper(regionGenerator, GenerateFlag.PostInitialize)
                        && GenerateHelper(regionGenerator, GenerateFlag.Navi)
                        && GenerateNaviMesh()
                        && GenerateHelper(regionGenerator, GenerateFlag.PathCollection);
            // BuildObjectiveGraph()
            if (success) success &= GenerateMissionPopulation()
                        && GenerateHelper(regionGenerator, GenerateFlag.Population)
                        && GenerateHelper(regionGenerator, GenerateFlag.PostGenerate);
            return success;
        }

        public bool GenerateNaviMesh()
        {
            NaviSystem.ClearErrorLog();
            return NaviMesh.GenerateMesh();
        }

        public bool GenerateMissionPopulation()
        {
            foreach(var metaGameId in MetaGames)
            {
                MetaGame metaGame = Game.EntityManager.GetEntityById(metaGameId) as MetaGame;                
                metaGame?.RegistyStates();
            }
            return MissionManager.GenerateMissionPopulation();            
        }

        public bool GenerateHelper(RegionGenerator regionGenerator, GenerateFlag flag)
        {
            bool success = Areas.Count > 0;
            foreach (Area area in IterateAreas())
            {
                if (area == null)
                    success = false;
                else
                {
                    List<PrototypeId> areas = new() { area.PrototypeDataRef };
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
            Areas[area.Id] = area;
            if (settings.RegionSettings.GenerateLog) Logger.Debug($"Adding area {area.PrototypeName}, id={area.Id}, areapos = {area.Origin}, seed = {RandomSeed}");
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
            if (Areas.TryGetValue(id, out Area area)) return area;
            return null;
        }

        public void DestroyArea(uint id)
        {
            if (Areas.TryGetValue(id, out Area areaToRemove))
            {
                DeallocateArea(areaToRemove);
                Areas.Remove(id);
            }
        }

        private void DeallocateArea(Area area)
        {
            if (area == null) return;
            if (Settings.GenerateLog) Logger.Trace($"{Game} - Deallocating area id {area.Id}, {area}");
            area.Shutdown();
        }

        public Area GetArea(PrototypeId prototypeId)
        {
            foreach (var area in Areas)
                if (area.Value.PrototypeDataRef == prototypeId) return area.Value;

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

        public IEnumerable<Cell> IterateCellsInVolume<B>(B bound) where B: IBounds
        {
            if (CellSpatialPartition != null)
                return CellSpatialPartition.IterateElementsInVolume(bound);
            else
                return Enumerable.Empty<Cell>(); //new CellSpatialPartition.ElementIterator();
        }

        public bool InsertEntityInSpatialPartition(WorldEntity entity) => EntitySpatialPartition.Insert(entity);
        public void UpdateEntityInSpatialPartition(WorldEntity entity) {
            if (EntitySpatialPartition.Update(entity) == false)
                Logger.Error($"Update EntitySpatialPartition fail {entity.WorldEntityPrototype}");
        }
        public bool RemoveEntityFromSpatialPartition(WorldEntity entity) => EntitySpatialPartition.Remove(entity);

        public IEnumerable<WorldEntity> IterateEntitiesInRegion(EntityRegionSPContext context)
        {
            return IterateEntitiesInVolume(Bound, context);
        }

        public IEnumerable<WorldEntity> IterateEntitiesInVolume<B>(B bound, EntityRegionSPContext context) where B : IBounds
        {
            if (EntitySpatialPartition != null)
                return EntitySpatialPartition.IterateElementsInVolume(bound, context);
            else
                return Enumerable.Empty<WorldEntity>();
        }

        public PrototypeId PrototypeDataRef => RegionPrototype.DataRef;

        public DateTime CreatedTime { get; set; }
        public DateTime VisitedTime { get; private set; }
        public string PrototypeName => GameDatabase.GetFormattedPrototypeName(PrototypeDataRef);

        public override string ToString()
        {
            return $"{GameDatabase.GetPrototypeName(PrototypeDataRef)}, ID=0x{Id:X} ({Id}), DIFF={GameDatabase.GetFormattedPrototypeName(Settings.DifficultyTierRef)}, SEED={RandomSeed}, GAMEID={Game}";
        }

        private string GetPrototypeName()
        {
            return GameDatabase.GetPrototypeName(PrototypeDataRef);
        }

        public void Shutdown()
        {
            // SetStatus(2, true);
            
           /* int tries = 100;
            bool found;
            do
            {
                found = false;*/
                foreach (var entity in Entities)
                {
                    if (entity is WorldEntity worldEntity)
                    {
                        /*if (worldEntity.GetRootOwner() is not Player owner)
                        {
                            if (worldEntity.IsDestroyed() == false)
                            {
                                worldEntity.Destroy();
                                found = true;
                            }
                        }
                        else*/
                        {
                            if (worldEntity.IsInWorld())
                            {
                                worldEntity.ExitWorld();
                                // found = true;
                            }
                        }
                    }
                }
            // } while (found && (tries-- > 0)); // TODO: For what 100 tries?

            /*
            if (Game != null && MissionManager != null)
                MissionManager.Shutdown(this);
            */
            while (MetaGames.Any())
            {
                var metaGameId = MetaGames.First();
                var metaGame = Game.EntityManager.GetEntityById(metaGameId);
                if (metaGame != null) metaGame.Destroy();
                MetaGames.Remove(metaGameId);
            }

            while (Areas.Any())
            {
                var areaId = Areas.First().Key;
                DestroyArea(areaId);
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

        public Area GetAreaAtPosition(Vector3 position)
        {
            foreach (var itr in Areas)
            {
                Area area = itr.Value;
                if (area.IntersectsXY(position)) return area;
            }
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
            List<Area> areasList = Areas.Values.ToList();
            foreach (Area area in areasList)
            {
                //Area area = enumerator.Current.Value;
                if (bound == null || area.RegionBounds.Intersects(bound))
                    yield return area;
            }
        }

        public bool FindTargetPosition(Vector3 markerPos, Orientation markerRot, RegionConnectionTargetPrototype target)
        {
            Area targetArea;

            // fix for AvengerTower
            if (StartArea.PrototypeId == AreaPrototypeId.AvengersTowerHubArea)
            {
                markerPos.Set(1589.0f, -2.0f, 180.0f);
                markerRot.Set(3.1415f, 0.0f, 0.0f);
                return true;
            }

            var areaRef = target.Area;

            bool found = false;

            // Has areaRef
            if (areaRef != 0)
            {
                targetArea = GetArea(areaRef);
                if (targetArea != null) 
                    found = targetArea.FindTargetPosition(markerPos, markerRot, target);
            }

            // Has the wrong areaRef
            if (found == false)
                foreach (Area area in IterateAreas())
                {
                    targetArea = area;
                    if (targetArea.FindTargetPosition(markerPos, markerRot, target))
                        return true;
                }

            // Has the wrong cellRef // Fix for Upper Eastside
            if (found == false)
                foreach (Cell cell in Cells)
                {
                    if (cell.FindTargetPosition(markerPos, markerRot, target))
                        return true;
                }

            return found;
        }

        public List<IMessage> GetLoadingMessages(ulong serverGameId, PrototypeId targetRef, PlayerConnection playerConnection)
        {
            List<IMessage> messageList = new();

            var regionChangeBuilder = NetMessageRegionChange.CreateBuilder()
                .SetRegionId(Id)
                .SetServerGameId(serverGameId)
                .SetClearingAllInterest(false)
                .SetRegionPrototypeId((ulong)PrototypeId)
                .SetRegionRandomSeed(RandomSeed)
                .SetRegionMin(Bound.Min.ToNetStructPoint3())
                .SetRegionMax(Bound.Max.ToNetStructPoint3())
                .SetCreateRegionParams(CreateParams.ToNetStruct());

            // can add EntitiesToDestroy here

            // empty archive data seems to cause region loading to hang for some time
            if (ArchiveData.Length > 0) regionChangeBuilder.SetArchiveData(ByteString.CopyFrom(ArchiveData));

            messageList.Add(regionChangeBuilder.Build());

            // mission updates and entity creation happens here

            // why is there a second NetMessageQueueLoadingScreen?
            messageList.Add(NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId((ulong)PrototypeId).Build());

            // TODO: prefetch other regions
            
            // Get starArea to load by Waypoint
            if (StartArea != null)
            {
                if (playerConnection.EntityToTeleport != null) // TODO change teleport without reload Region
                {
                    Vector3 position = new(playerConnection.EntityToTeleport.RegionLocation.GetPosition());
                    Orientation orientation = new(playerConnection.EntityToTeleport.RegionLocation.GetOrientation());
                    if (playerConnection.EntityToTeleport.EntityPrototype is TransitionPrototype teleportEntity
                        && teleportEntity.SpawnOffset > 0) teleportEntity.CalcSpawnOffset(orientation, position);
                    playerConnection.StartPositon = position;
                    playerConnection.StartOrientation = orientation;
                    playerConnection.EntityToTeleport = null;
                }
                else if (RegionTransition.FindStartPosition(this, targetRef, out Vector3 position, out Orientation orientation))
                {
                    playerConnection.StartPositon = position;
                    playerConnection.StartOrientation = orientation;
                }
                else
                {
                    playerConnection.StartPositon = StartArea.Cells.First().Value.RegionBounds.Center;
                    playerConnection.StartOrientation = Orientation.Zero;
                }

                playerConnection.AOI.Reset(this);
                playerConnection.AOI.Update(playerConnection.StartPositon, true);
                messageList.AddRange(playerConnection.AOI.Messages);
            }


            return messageList;
        }

        public Cell GetCellbyId(uint cellId)
        {
            foreach (var cell in Cells)
                if (cell.Id == cellId) return cell;
            return default;
        }

        internal void ApplyRegionAffixesEnemyBoosts(PrototypeId rankRef, HashSet<PrototypeId> overrides)
        {
            throw new NotImplementedException();
        }

        internal PrototypeId GetDifficultyTierRef()
        {
            throw new NotImplementedException();
        }

        public void Visited()
        {
            lock (Lock)
            {
                VisitedTime = DateTime.Now;
            }
        }

        public static bool IsBoundsBlockedByEntity(Bounds bounds, WorldEntity entity, BlockingCheckFlags blockFlags)
        {
            if (entity != null)
            {
                if (entity.NoCollide) return false;

                bool selfBlocking = false;
                bool otherBlocking = false;

                if (blockFlags != 0)
                {
                    var entityProto = entity.WorldEntityPrototype;
                    if (entityProto == null) return false;

                    var boundsProto = entityProto.Bounds;
                    if (boundsProto == null) return false;

                    selfBlocking |= blockFlags.HasFlag(BlockingCheckFlags.CheckSelf);
                    otherBlocking |= blockFlags.HasFlag(BlockingCheckFlags.CheckSpawns) && boundsProto.BlocksSpawns;
                    otherBlocking |= blockFlags.HasFlag(BlockingCheckFlags.CheckGroundMovementPowers) && (boundsProto.BlocksMovementPowers == BoundsMovementPowerBlockType.Ground || boundsProto.BlocksMovementPowers == BoundsMovementPowerBlockType.All);
                    otherBlocking |= blockFlags.HasFlag(BlockingCheckFlags.CheckAllMovementPowers) && boundsProto.BlocksMovementPowers == BoundsMovementPowerBlockType.All;
                    otherBlocking |= blockFlags.HasFlag(BlockingCheckFlags.CheckLanding) && boundsProto.BlocksLanding;

                    if (otherBlocking == false) return false;
                }

                Bounds entityBounds = entity.Bounds;
                if (bounds.CanBeBlockedBy(entityBounds, selfBlocking, otherBlocking) && bounds.Intersects(entityBounds)) return true;
            }

            return false;
        }

        public int GetAreaLevel(Area area)
        {
            if (RegionPrototype.LevelUseAreaOffset) return area.GetAreaLevel();
            return RegionLevel;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {            
            return keywordProto != null && RegionPrototype.HasKeyword(keywordProto);
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
