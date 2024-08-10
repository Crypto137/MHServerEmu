using System.Diagnostics;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.DRAG;
using MHServerEmu.Games.DRAG.Generators.Regions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions.ObjectiveGraphs;
using MHServerEmu.Games.UI;

namespace MHServerEmu.Games.Regions
{
    [Flags]
    public enum PositionCheckFlags
    {
        None = 0,
        CanBeBlockedEntity = 1 << 0,
        CanBeBlockedAvatar = 1 << 1,
        CanPathTo          = 1 << 2,
        CanSweepTo         = 1 << 3,
        CanSweepRadius     = 1 << 4,
        CanPathToEntities  = 1 << 5,
        InRadius           = 1 << 6,
        PreferNoEntity     = 1 << 7,
    }

    [Flags]
    public enum RegionStatus
    {
        None = 0,
        GenerateAreas = 1 << 0,
        Shutdown = 1 << 2,
    }

    public enum RegionPartitionContext
    {
        Insert,
        Remove
    }

    public class Region : IMissionManagerOwner, ISerialize, IUIDataProviderOwner
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly BitList _collisionIds = new();
        private readonly BitList _collisionBits = new();
        private readonly List<BitList> _collisionBitList = new();

        private Area _startArea;
        private RegionStatus _statusFlag;
        private int _playerDeaths;

        public Game Game { get; private set; }
        public ulong Id { get; private set; } // InstanceAddress
        public RegionSettings Settings { get; private set; }
        public int RandomSeed { get; private set; }
        public ulong MatchNumber { get => Settings.MatchNumber; }
        public int RegionLevel { get; private set; }
        public PrototypeId DifficultyTierRef { get => Properties[PropertyEnum.DifficultyTier]; }

        public RegionPrototype Prototype { get; private set; }
        public PrototypeId PrototypeDataRef { get => Prototype.DataRef; }
        public string PrototypeName { get => GameDatabase.GetFormattedPrototypeName(PrototypeDataRef); }

        public bool IsPublic { get => Prototype != null && Prototype.IsPublic; }
        public bool IsPrivate { get => Prototype != null && Prototype.IsPrivate; }

        public Aabb Aabb { get; private set; }
        public Aabb2 Aabb2 { get => new(Aabb); }
        public int MaxCollisionId { get => _collisionIds.Size; }

        public bool IsGenerated { get; private set; }
        public bool AvatarSwapEnabled { get; private set; }
        public bool RestrictedRosterEnabled { get; private set; }

        public TimeSpan CreatedTime { get; private set; }
        public TimeSpan LastVisitedTime { get; private set; }

        public Dictionary<uint, Area> Areas { get; } = new();
        public IEnumerable<Cell> Cells { get => IterateCellsInVolume(Aabb); }
        public IEnumerable<Entity> Entities { get => Game.EntityManager.IterateEntities(this); }

        // ArchiveData
        public ReplicatedPropertyCollection Properties { get; private set; }
        public MissionManager MissionManager { get; private set; }
        public UIDataProvider UIDataProvider { get; private set; }
        public ObjectiveGraph ObjectiveGraph { get; private set; }

        public List<DividedStartLocation> DividedStartLocations { get; } = new();
        public ConnectionNodeList Targets { get; private set; }
        public RegionProgressionGraph ProgressionGraph { get; set; }
        public EntityRegionSpatialPartition EntitySpatialPartition { get; private set; }
        public CellSpatialPartition CellSpatialPartition { get; private set; }
        public NaviSystem NaviSystem { get; private set; }
        public NaviMesh NaviMesh { get; private set; }
        public PathCache PathCache { get; private set; }
        public List<ulong> MetaGames { get; private set; } = new();

        public PopulationManager PopulationManager { get; private set; }
        public SpawnMarkerRegistry SpawnMarkerRegistry { get; private set; }
        public EntityTracker EntityTracker { get; private set; }
        public TuningTable TuningTable { get; private set; }    // Difficulty table

        #region Events

        public Event<EntityDeadGameEvent> EntityDeadEvent = new();
        public Event<AIBroadcastBlackboardGameEvent> AIBroadcastBlackboardEvent = new();
        public Event<PlayerInteractGameEvent> PlayerInteractEvent = new();
        public Event<EntityAggroedGameEvent> EntityAggroedEvent = new();
        public Event<AdjustHealthGameEvent> AdjustHealthEvent = new();
        public Event<EntityEnteredMissionHotspotGameEvent> EntityEnteredMissionHotspotEvent = new();
        public Event<EntityLeftMissionHotspotGameEvent> EntityLeftMissionHotspotEvent = new();
        public Event<EntityLeaveDormantGameEvent> EntityLeaveDormantEvent = new();
        public Event<EntityEnteredAreaGameEvent> EntityEnteredAreaEvent = new();
        public Event<EntityLeftAreaGameEvent> EntityLeftAreaEvent = new();
        public Event<AreaCreatedGameEvent> AreaCreatedEvent = new();
        public Event<CellCreatedGameEvent> CellCreatedEvent = new();
        public Event<PlayerEnteredCellGameEvent> PlayerEnteredCellEvent = new();
        public Event<PlayerLeftCellGameEvent> PlayerLeftCellEvent = new();
        public Event<AvatarEnteredRegionGameEvent> AvatarEnteredRegionEvent = new();
        public Event<PlayerEnteredRegionGameEvent> PlayerEnteredRegionEvent = new();
        public Event<PlayerLeftRegionGameEvent> PlayerLeftRegionEvent = new();
        public Event<PlayerCompletedMissionGameEvent> PlayerCompletedMissionEvent = new();
        public Event<PlayerCompletedMissionObjectiveGameEvent> PlayerCompletedMissionObjectiveEvent = new();
        public Event<MissionObjectiveUpdatedGameEvent> MissionObjectiveUpdatedEvent = new();
        public Event<OpenMissionCompleteGameEvent> OpenMissionCompleteEvent = new();
        public Event<OpenMissionFailedGameEvent> OpenMissionFailedEvent = new();
        public Event<PlayerFailedMissionGameEvent> PlayerFailedMissionEvent = new();
        public Event<EntitySetSimulatedGameEvent> EntitySetSimulatedEvent = new();
        public Event<EntitySetUnSimulatedGameEvent> EntitySetUnSimulatedEvent = new();
        public Event<ActiveChapterChangedGameEvent> ActiveChapterChangedEvent = new();
        public Event<PlayerBeginTravelToAreaGameEvent> PlayerBeginTravelToAreaEvent = new();
        public Event<PlayerEnteredAreaGameEvent> PlayerEnteredAreaEvent = new();
        public Event<PlayerLeftAreaGameEvent> PlayerLeftAreaEvent = new();
        public Event<PartySizeChangedGameEvent> PartySizeChangedEvent = new();
        public Event<PlayerSwitchedToAvatarGameEvent> PlayerSwitchedToAvatarEvent = new();
        public Event<AvatarLeveledUpGameEvent> AvatarLeveledUpEvent = new();
        public Event<CurrencyCollectedGameEvent> CurrencyCollectedEvent = new();
        public Event<EmotePerformedGameEvent> EmotePerformedEvent = new();
        public Event<PlayerUnlockedAvatarGameEvent> PlayerUnlockedAvatarEvent = new();
        public Event<EntityEnteredWorldGameEvent> EntityEnteredWorldEvent = new();
        public Event<EntityExitedWorldGameEvent> EntityExitedWorldEvent = new();

        #endregion

        public Region(Game game)
        {
            Game = game;
            SpawnMarkerRegistry = new(this);
            Settings = new();
            PathCache = new();

            NaviSystem = new();
            NaviMesh = new(NaviSystem);

            _collisionIds = new();
            _collisionBits = new();
            _collisionBitList = new();
            _collisionIds.Resize(256);
        }

        public override string ToString()
        {
            return $"{GameDatabase.GetPrototypeName(PrototypeDataRef)}, ID=0x{Id:X} ({Id}), DIFF={GameDatabase.GetFormattedPrototypeName(Settings.DifficultyTierRef)}, SEED={RandomSeed}, GAMEID={Game}";
        }

        public bool Initialize(RegionSettings settings)
        {
            if (Game == null) return false;

            MissionManager = new MissionManager(Game, this);
            UIDataProvider = new(Game, this);     // CreateUIDataProvider(Game);
            PopulationManager = new(Game, this);

            Settings = settings;
            Properties = new(Game.CurrentRepId); // TODO: Bind(this, 0xEF);

            Id = settings.InstanceAddress; // Region Id
            if (Id == 0) return Logger.WarnReturn(false, "Initialize(): settings.InstanceAddress == 0");

            Prototype = GameDatabase.GetPrototype<RegionPrototype>(settings.RegionDataRef);
            if (Prototype == null) return Logger.WarnReturn(false, "Initialize(): Prototype == null");

            RegionPrototype regionProto = Prototype;
            RandomSeed = settings.Seed;
            Aabb = settings.Bounds;
            AvatarSwapEnabled = Prototype.EnableAvatarSwap;
            RestrictedRosterEnabled = (Prototype.RestrictedRoster.HasValue());

            SetRegionLevel();

            if (settings.Properties != null)
                Properties.FlattenCopyFrom(settings.Properties, false);

            _playerDeaths = settings.PlayerDeaths;
            Properties[PropertyEnum.EndlessLevel] = settings.EndlessLevel;

            var sequenceRegionGenerator = regionProto.RegionGenerator as SequenceRegionGeneratorPrototype;
            Properties[PropertyEnum.EndlessLevelsTotal] = sequenceRegionGenerator != null ? sequenceRegionGenerator.EndlessLevelsPerTheme : 0;

            EntityTracker = new(this);
            //LowResMapResolution = GetLowResMapResolution();

            GlobalsPrototype globals = GameDatabase.GlobalsPrototype;
            if (globals == null)
                return Logger.ErrorReturn(false, "Unable to get globals prototype for region initialize");

            TuningTable = new(this);

            RegionDifficultySettingsPrototype difficultySettings = regionProto.GetDifficultySettings();
            if (difficultySettings != null)
            {
                TuningTable.SetTuningTable(difficultySettings.TuningTable);

                /* if (HasProperty(PropertyEnum.DifficultyIndex))
                       TuningTable.SetDifficultyIndex(GetProperty<int>(PropertyEnum.DifficultyIndex), false);
                */
            }

            if (regionProto.DividedStartLocations.HasValue())
                InitDividedStartLocations(regionProto.DividedStartLocations);

            if (NaviSystem.Initialize(this) == false) return false;
            if (Aabb.IsZero() == false)
            {
                if (settings.GenerateAreas)
                    Logger.Warn("Bound is not Zero with GenerateAreas On");             
                
                InitializeSpacialPartition(Aabb);
                NaviMesh.Initialize(Aabb, 1000.0f, this);
            }

            SpawnMarkerRegistry.Initialize();
            ProgressionGraph = new();
            ObjectiveGraph = new(Game, this);

            if (MissionManager != null && MissionManager.InitializeForRegion(this) == false) return false;

            if (settings.Affixes != null && settings.Affixes.Any())
            {
                RegionAffixTablePrototype affixTableP = GameDatabase.GetPrototype<RegionAffixTablePrototype>(regionProto.AffixTable);
                if (affixTableP != null)
                {
                    foreach (PrototypeId regionAffixProtoRef in settings.Affixes)
                    {
                        RegionAffixPrototype regionAffixProto = GameDatabase.GetPrototype<RegionAffixPrototype>(regionAffixProtoRef);
                        if (regionAffixProto != null)
                        {
                            Properties.AdjustProperty(regionAffixProto.AdditionalLevels, PropertyEnum.EndlessLevelsTotal);

                            if (regionAffixProto.Eval != null)
                            {
                                EvalContextData contextData = new(Game);
                                contextData.SetVar_PropertyCollectionPtr(EvalContext.Default, Properties);
                                Eval.RunBool(regionAffixProto.Eval, contextData);
                            }
                        }
                    }
                }
            }

            if (settings.DifficultyTierRef != PrototypeId.Invalid)
                Properties[PropertyEnum.DifficultyTier] = settings.DifficultyTierRef;
            else
                Logger.Warn("Initialize(): settings.DifficultyTierRef == PrototypeId.Invalid");

            Targets = RegionTransition.BuildConnectionEdges(settings.RegionDataRef); // For Teleport system

            // Does this need to be initialized before we generate areas? Is this supposed to be happening later?
            if (regionProto.MetaGames.HasValue())
            {
                foreach (var metaGameRef in regionProto.MetaGames)
                {
                    EntitySettings metaSettings = new();
                    metaSettings.RegionId = Id;
                    metaSettings.EntityRef = metaGameRef;
                    MetaGame metagame = Game.EntityManager.CreateEntity(metaSettings) as MetaGame;
                }
            }

            if (settings.GenerateAreas)
            {
                if (GenerateAreas(settings.GenerateLog) == false)
                    return Logger.WarnReturn(false, $"Initialize(): Failed to generate areas for\n  region: {this}\n    seed: {RandomSeed}");
            }

            if (settings.Affixes != null && settings.Affixes.Any())
            {
                var affixTableProto = GameDatabase.GetPrototype<RegionAffixTablePrototype>(regionProto.AffixTable);
                if (affixTableProto != null)
                {
                    foreach (PrototypeId regionAffixProtoRef in Settings.Affixes)
                    {
                        var regionAffixProto = GameDatabase.GetPrototype<RegionAffixPrototype>(regionAffixProtoRef);
                        if (regionAffixProto != null)
                        {
                            Properties[PropertyEnum.RegionAffix, regionAffixProtoRef] = true;
                            Properties.AdjustProperty(regionAffixProto.Difficulty, PropertyEnum.RegionAffixDifficulty);

                            if (regionAffixProto.CanApplyToRegion(this))
                            {
                                if (regionAffixProto.MetaState != PrototypeId.Invalid)
                                    Properties[PropertyEnum.MetaStateApplyOnInit, regionAffixProto.MetaState] = true;

                                if (regionAffixProto.AvatarPower != PrototypeId.Invalid)
                                    Properties[PropertyEnum.RegionAvatarPower, regionAffixProto.AvatarPower] = true;
                            }
                        }
                    }

                    EvalContextData contextData = new();
                    contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, Properties);
                    int affixTier = Eval.RunInt(affixTableProto.EvalTier, contextData);

                    RegionAffixTableTierEntryPrototype tierEntryProto = affixTableProto.GetByTier(affixTier);
                    if (tierEntryProto != null)
                    {
                        int enumValue = (int)LootDropEventType.None;
                        AssetId valueAsset = Property.PropertyEnumToAsset(PropertyEnum.LootSourceTableOverride, 1, enumValue);
                        Properties[PropertyEnum.LootSourceTableOverride, affixTableProto.LootSource, valueAsset] = tierEntryProto.LootTable;
                    }
                }
                else
                {
                    Logger.Warn($"Initialize(): Region created with affixes, but no RegionAffixTable. REGION={this} AFFIXES={Settings.Affixes}");
                }
            }
            
            if (regionProto.AvatarPowers.HasValue())
            {
                foreach (PrototypeId avatarPowerRef in regionProto.AvatarPowers)
                    Properties[PropertyEnum.RegionAvatarPower, avatarPowerRef] = true;
            }

            // NOTE: The only region prototype that uses UITopPanel is Regions/ZZZDevelopment/DevRooms/TestingRoom/TestRegionA.prototype
            // that has UI/Panels/SurturRaidTopPanel.prototype assigned to it that doesn't seem to do anything.
            //
            // It's useless for 1.52, but I'm leaving this here in case it has a larger role in older versions of the game.
            // If we ever need this, we also need to add OnPropertyChange() handling for the RegionUITopPanel property.
            //
            // if (regionProto.UITopPanel != PrototypeId.Invalid)
            //     Properties[PropertyEnum.RegionUITopPanel] = regionProto.UITopPanel;

            IsGenerated = true;
            CreatedTime = Clock.UnixTime;
            return true;
        }

        public bool TestStatus(RegionStatus status)
        {
            return _statusFlag.HasFlag(status);
        }

        private void SetStatus(RegionStatus status, bool enable)
        {
            if (enable) _statusFlag |= status;
            else _statusFlag ^= status;
        }

        public void Shutdown()
        {
            SetStatus(RegionStatus.Shutdown, true);

            /* int tries = 100;
             bool found;
             do
             {
                 found = false;*/
            foreach (var entity in Entities)
            {
                if (entity is WorldEntity worldEntity)
                {
                    if (worldEntity.GetRootOwner() is not Player)
                    {
                        if (worldEntity.IsDestroyed == false)
                        {
                            worldEntity.Destroy();
                            //found = true;
                        }
                    }
                    else
                    {
                        if (worldEntity.IsInWorld)
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
                var metaGame = Game.EntityManager.GetEntity<Entity>(metaGameId);
                metaGame?.Destroy();
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

            NaviMesh.Release();
            PopulationManager.Deallocate();
        }

        public bool Serialize(Archive archive)
        {
            bool success = Properties.Serialize(archive);
            success &= MissionManager.Serialize(archive);
            success &= UIDataProvider.Serialize(archive);
            success &= ObjectiveGraph.Serialize(archive);
            return success;
        }

        public List<IMessage> OLD_GetLoadingMessages(ulong serverGameId, PrototypeId targetRef, PlayerConnection playerConnection)
        {
            // TODO: Move this to AOI

            List<IMessage> messageList = new();

            var regionChangeBuilder = NetMessageRegionChange.CreateBuilder()
                .SetRegionId(Id)
                .SetServerGameId(serverGameId)
                .SetClearingAllInterest(false)
                .SetRegionPrototypeId((ulong)PrototypeDataRef)
                .SetRegionRandomSeed(RandomSeed)
                .SetRegionMin(Aabb.Min.ToNetStructPoint3())
                .SetRegionMax(Aabb.Max.ToNetStructPoint3())
                .SetCreateRegionParams(NetStructCreateRegionParams.CreateBuilder()
                    .SetLevel((uint)RegionLevel)
                    .SetDifficultyTierProtoId((ulong)DifficultyTierRef));

            // can add EntitiesToDestroy here

            using (Archive archive = new(ArchiveSerializeType.Replication, (ulong)AOINetworkPolicyValues.DefaultPolicy))
            {
                Serialize(archive);
                regionChangeBuilder.SetArchiveData(archive.ToByteString());
            }

            messageList.Add(regionChangeBuilder.Build());

            // mission updates and entity creation happens here

            // why is there a second NetMessageQueueLoadingScreen?
            messageList.Add(NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId((ulong)PrototypeDataRef).Build());

            // TODO: prefetch other regions

            // Get startArea to load by Waypoint
            Area startArea = GetStartArea();
            if (startArea != null)
            {
                if (playerConnection.EntityToTeleport != null) // TODO change teleport without reload Region
                {
                    Vector3 position = playerConnection.EntityToTeleport.RegionLocation.Position;
                    Orientation orientation = playerConnection.EntityToTeleport.RegionLocation.Orientation;
                    if (playerConnection.EntityToTeleport.Prototype is TransitionPrototype teleportEntity
                        && teleportEntity.SpawnOffset > 0) teleportEntity.CalcSpawnOffset(ref orientation, ref position);
                    playerConnection.StartPosition = position;
                    playerConnection.StartOrientation = orientation;
                    playerConnection.EntityToTeleport = null;
                }
                else if (RegionTransition.FindStartPosition(this, targetRef, out Vector3 position, out Orientation orientation))
                {
                    playerConnection.StartPosition = position;
                    playerConnection.StartOrientation = orientation;
                }
                else
                {
                    playerConnection.StartPosition = _startArea.Cells.First().Value.RegionBounds.Center;
                    playerConnection.StartOrientation = Orientation.Zero;
                }
            }

            return messageList;
        }

        #region Area Management

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

            if (area.Initialize(settings) == false)
            {
                DeallocateArea(area);
                return null;
            }

            Areas[area.Id] = area;

            if (settings.RegionSettings.GenerateLog)
                Logger.Debug($"Adding area {area.PrototypeName}, id={area.Id}, areapos = {area.Origin}, seed = {RandomSeed}");

            return area;
        }

        public Area GetArea(PrototypeId prototypeId)
        {
            foreach (var area in Areas)
            {
                if (area.Value.PrototypeDataRef == prototypeId)
                    return area.Value;
            }

            return null;
        }

        public Area GetAreaById(uint id)
        {
            if (Areas.TryGetValue(id, out Area area))
                return area;

            return null;
        }

        public Area GetAreaAtPosition(Vector3 position)
        {
            foreach (var itr in Areas)
            {
                Area area = itr.Value;
                if (area.IntersectsXY(position))
                    return area;
            }

            return null;
        }

        public Area GetStartArea()
        {
            if (_startArea == null && Areas.Any())
                _startArea = IterateAreas().First();

            return _startArea;
        }

        public IEnumerable<Area> IterateAreas(Aabb? bound = null)
        {
            List<Area> areasList = Areas.Values.ToList();   // TODO: Change this to ToArray()
            foreach (Area area in areasList)
            {
                //Area area = enumerator.Current.Value;
                if (bound == null || area.RegionBounds.Intersects(bound.Value))
                    yield return area;
            }
        }

        public int GetAreaLevel(Area area)
        {
            if (Prototype.LevelUseAreaOffset)
                return area.GetAreaLevel();

            return RegionLevel;
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

            if (Settings.GenerateLog)
                Logger.Trace($"{Game} - Deallocating area id {area.Id}, {area}");

            area.Shutdown();
        }

        #endregion

        #region Cell Management

        public Cell GetCellbyId(uint cellId)
        {
            foreach (Cell cell in Cells)
            {
                if (cell.Id == cellId)
                    return cell;
            }

            return default;
        }

        public Cell GetCellAtPosition(Vector3 position)
        {
            foreach (Cell cell in Cells)
            {
                if (cell.IntersectsXY(position))
                    return cell;
            }

            return null;
        }

        public IEnumerable<Cell> IterateCellsInVolume<B>(B bounds) where B : IBounds
        {
            if (CellSpatialPartition != null)
                return CellSpatialPartition.IterateElementsInVolume(bounds);
            else
                return Enumerable.Empty<Cell>(); //new CellSpatialPartition.ElementIterator();
        }

        #endregion

        #region Entity Management

        public bool InsertEntityInSpatialPartition(WorldEntity entity) => EntitySpatialPartition.Insert(entity);
        public void UpdateEntityInSpatialPartition(WorldEntity entity) => EntitySpatialPartition.Update(entity);
        public bool RemoveEntityFromSpatialPartition(WorldEntity entity) => EntitySpatialPartition.Remove(entity);

        public IEnumerable<WorldEntity> IterateEntitiesInRegion(EntityRegionSPContext context)
        {
            return IterateEntitiesInVolume(Aabb, context);
        }

        public IEnumerable<WorldEntity> IterateEntitiesInVolume<B>(B bound, EntityRegionSPContext context) where B : IBounds
        {
            if (EntitySpatialPartition != null)
                return EntitySpatialPartition.IterateElementsInVolume(bound, context);
            else
                return Enumerable.Empty<WorldEntity>();
        }

        public IEnumerable<Avatar> IterateAvatarsInVolume(in Sphere bound)
        {
            if (EntitySpatialPartition != null)
                return EntitySpatialPartition.IterateAvatarsInVolume(bound);
            else
                return Enumerable.Empty<Avatar>();
        }

        public void GetEntitiesInVolume<B>(List<WorldEntity> entities, B volume, EntityRegionSPContext context) where B : IBounds
        {
            EntitySpatialPartition?.GetElementsInVolume(entities, volume, context);
        }

        #endregion

        #region Generation

        public bool GenerateAreas(bool log)
        {
            if (TestStatus(RegionStatus.GenerateAreas) == false) return false;

            RegionGenerator regionGenerator = DRAGSystem.LinkRegionGenerator(Prototype.RegionGenerator);

            regionGenerator.GenerateRegion(log, RandomSeed, this);

            _startArea = regionGenerator.StartArea;
            SetStatus(RegionStatus.GenerateAreas, true);
            SetAabb(CalculateAabbFromAreas());

            bool success = GenerateHelper(regionGenerator, GenerateFlag.Background)
                        && GenerateHelper(regionGenerator, GenerateFlag.PostInitialize)
                        && GenerateHelper(regionGenerator, GenerateFlag.Navi)
                        && GenerateNaviMesh()
                        && GenerateHelper(regionGenerator, GenerateFlag.PathCollection);
            // BuildObjectiveGraph()

            if (success)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                success &= GenerateMissionPopulation()
                        && GenerateHelper(regionGenerator, GenerateFlag.Population)
                        && GenerateHelper(regionGenerator, GenerateFlag.PostGenerate);

                Logger.Info($"Generated population in {stopwatch.ElapsedMilliseconds} ms");
            }

            return success;
        }

        public bool GenerateNaviMesh()
        {
            NaviSystem.ClearErrorLog();
            return NaviMesh.GenerateMesh();
        }

        public bool GenerateMissionPopulation()
        {
            foreach (var metaGameId in MetaGames)
            {
                var metaGame = Game.EntityManager.GetEntity<MetaGame>(metaGameId);
                metaGame?.RegisterStates();
            }
            return MissionManager.GenerateMissionPopulation();
        }

        public bool GenerateHelper(RegionGenerator regionGenerator, GenerateFlag flag)
        {
            bool success = Areas.Count > 0;

            foreach (Area area in IterateAreas())
            {
                if (area == null)
                {
                    success = false;
                }
                else
                {
                    List<PrototypeId> areas = new() { area.PrototypeDataRef };
                    success &= area.Generate(regionGenerator, areas, flag);
                    if (area.TestStatus(GenerateFlag.Background) == false)
                        Logger.Error($"{area} Not generated");
                }
            }

            return success;
        }

        #endregion

        #region Difficulty & Affixes & MetaGame

        public void RegisterMetaGame(MetaGame metaGame)
        {
            if (metaGame != null) MetaGames.Add(metaGame.Id);
        }

        public void UnRegisterMetaGame(MetaGame metaGame)
        {
            if (metaGame != null) MetaGames.Remove(metaGame.Id);
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

        public void ApplyRegionAffixesEnemyBoosts(PrototypeId rankRef, HashSet<PrototypeId> overrides)
        {
            throw new NotImplementedException();
        }

        private void SetRegionLevel()
        {
            if (RegionLevel == 0) return;
            RegionPrototype regionProto = Prototype;
            if (regionProto == null) return;

            if (Settings.DebugLevel == true)
                RegionLevel = Settings.Level;
            else if (regionProto.Level > 0)
                RegionLevel = regionProto.Level;
            else
                Logger.Error("RegionLevel <= 0");
        }

        #endregion

        #region Space & Physics

        public Aabb CalculateAabbFromAreas()
        {
            Aabb bounds = Aabb.InvertedLimit;

            foreach (var area in IterateAreas())
                bounds += area.RegionBounds;

            return bounds;
        }

        public void SetAabb(in Aabb boundingBox)
        {
            if (boundingBox.Volume <= 0 || (boundingBox.Min == Aabb.Min && boundingBox.Max == Aabb.Max)) return;

            Aabb = boundingBox;

            NaviMesh.Initialize(Aabb, 1000.0f, this);
            InitializeSpacialPartition(Aabb);
        }

        private bool InitializeSpacialPartition(in Aabb bound)
        {
            if (EntitySpatialPartition != null || CellSpatialPartition != null) return false;

            EntitySpatialPartition = new(bound);
            CellSpatialPartition = new(bound);

            foreach (Area area in IterateAreas())
            {
                foreach (var cellItr in area.Cells)
                    PartitionCell(cellItr.Value, RegionPartitionContext.Insert);
            }

            SpawnMarkerRegistry.InitializeSpacialPartition(bound);
            PopulationManager.InitializeSpacialPartition(bound);

            return true;
        }

        public bool? PartitionCell(Cell cell, RegionPartitionContext context)
        {
            if (CellSpatialPartition != null)
            {
                switch (context)
                {
                    case RegionPartitionContext.Insert:
                        return CellSpatialPartition.Insert(cell);
                    case RegionPartitionContext.Remove:
                        return CellSpatialPartition.Remove(cell);
                }
            }

            return null;
        }

        public float GetDistanceToClosestAreaBounds(Vector3 position)
        {
            float minDistance = float.MaxValue;
            foreach (Area area in IterateAreas())
            {
                float distance = area.RegionBounds.DistanceToPoint2D(position);
                minDistance = Math.Min(distance, minDistance);
            }

            if (minDistance == float.MaxValue)
                Logger.Error("GetDistanceToClosestAreaBounds");

            return minDistance;
        }

        public bool CheckMarkerFilter(PrototypeId filterRef)
        {
            if (filterRef == 0) return true;
            PrototypeId markerFilter = Prototype.MarkerFilter;
            if (markerFilter == 0) return true;
            return markerFilter == filterRef;
        }

        public bool FindTargetPosition(ref Vector3 markerPos, ref Orientation markerRot, RegionConnectionTargetPrototype target)
        {
            Area targetArea;

            // Fix for the old Avengers Tower
            if ((AreaPrototypeId)_startArea?.PrototypeDataRef == AreaPrototypeId.AvengersTowerHubArea)
            {
                markerPos = new(1589.0f, -2.0f, 180.0f);
                markerRot = new(3.1415f, 0.0f, 0.0f);
                return true;
            }

            var areaRef = target.Area;

            bool found = false;

            // Has areaRef
            if (areaRef != 0)
            {
                targetArea = GetArea(areaRef);
                if (targetArea != null)
                    found = targetArea.FindTargetPosition(ref markerPos, ref markerRot, target);
            }

            // Has the wrong areaRef
            if (found == false)
            {
                foreach (Area area in IterateAreas())
                {
                    targetArea = area;
                    if (targetArea.FindTargetPosition(ref markerPos, ref markerRot, target))
                        return true;
                }
            }

            // Has the wrong cellRef // Fix for Upper Eastside
            if (found == false)
            {
                foreach (Cell cell in Cells)
                {
                    if (cell.FindTargetPosition(ref markerPos, ref markerRot, target))
                        return true;
                }
            }

            return found;
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

        public int AcquireCollisionId()
        {
            int index = _collisionIds.FirstUnset();
            if (index == -1) index = _collisionIds.Size;
            _collisionIds.Set(index, true);
            return index;
        }

        public bool CollideEntities(int collisionId, int otherCollisionId)
        {
            int maxCollisionId = _collisionBitList.Count;
            if (collisionId >= maxCollisionId)
            {
                maxCollisionId = MaxCollisionId + 64;
                while (_collisionBitList.Count < maxCollisionId)
                    _collisionBitList.Add(new());
            }

            var collisionBits = _collisionBitList[collisionId];

            if (_collisionBits[collisionId] == false)
            {
                _collisionBits.Set(collisionId);
                collisionBits.Clear();
            }

            if (otherCollisionId >= collisionBits.Size)
                collisionBits.Resize(maxCollisionId);

            bool collide = collisionBits[otherCollisionId];
            collisionBits.Set(otherCollisionId);
            return !collide;
        }

        public void ReleaseCollisionId(int collisionId)
        {
            if (collisionId >= 0)
                _collisionIds.Reset(collisionId);
        }

        public void ClearCollidedEntities()
        {
            if (MaxCollisionId < _collisionBitList.Count / 2)
            {
                _collisionBitList.Clear();
                _collisionBits.Resize(0);
            }
            else
            {
                _collisionBits.Clear();
            }
        }

        public static PathFlags GetPathFlagsForEntity(WorldEntityPrototype entityProto)
        {
            return entityProto != null ? Locomotor.GetPathFlags(entityProto.NaviMethod) : PathFlags.None;
        }

        public bool LineOfSightTo(Vector3 startPosition, WorldEntity owner, Vector3 targetPosition, ulong targetEntityId,
            float radius = 0.0f, float padding = 0.0f, float height = 0.0f, PathFlags pathFlags = PathFlags.Sight)
        {
            float maxHeight = Math.Max(startPosition.Z, targetPosition.Z);
            maxHeight += height;
            Vector3? resultPosition = Vector3.Zero;
            Vector3? resultNormal = null;

            SweepResult sweepResult = NaviMesh.Sweep(startPosition, targetPosition, radius, pathFlags, ref resultPosition, ref resultNormal,
                padding, HeightSweepType.Constraint, (int)maxHeight, short.MinValue, owner);

            if (sweepResult == SweepResult.Success)
            {
                Vector3? resultHitPosition = null;
                return SweepToFirstHitEntity(startPosition, targetPosition, owner, targetEntityId, true, 0.0f, ref resultHitPosition) == null;
            }

            return false;
        }

        public WorldEntity SweepToFirstHitEntity<T>(Bounds sweepBounds, Vector3 sweepVelocity, ref Vector3? resultHitPosition, T canBlock) where T : ICanBlock
        {
            bool CanBlockFunc(WorldEntity otherEntity) => canBlock.CanBlock(otherEntity);
            return SweepToFirstHitEntity(sweepBounds, sweepVelocity, ref resultHitPosition, CanBlockFunc);
        }

        public WorldEntity SweepToFirstHitEntity(Vector3 startPosition, Vector3 targetPosition, WorldEntity owner,
            ulong targetEntityId, bool blocksLOS, float radiusOverride, ref Vector3? resultHitPosition)
        {
            Bounds sweepBounds = new();

            if (owner != null)
                sweepBounds = new(owner.Bounds);

            if (blocksLOS || owner == null)
                sweepBounds.InitializeSphere(1.0f, sweepBounds.CollisionType);

            if (radiusOverride > 0.0f)
                sweepBounds.Radius = radiusOverride;

            sweepBounds.Center = startPosition;
            Vector3 sweepVector = targetPosition - startPosition;

            bool CanBlockFunc(WorldEntity otherEntity) => CanBlockEntitySweep(otherEntity, owner, targetEntityId, blocksLOS);
            return SweepToFirstHitEntity(sweepBounds, sweepVector, ref resultHitPosition, CanBlockFunc);
        }

        private WorldEntity SweepToFirstHitEntity(Bounds sweepBounds, Vector3 sweepVelocity, ref Vector3? resultHitPosition, Func<WorldEntity, bool> canBlockFunc)
        {
            Vector3 sweepStart = sweepBounds.Center;
            Vector3 sweepEnd = sweepStart + sweepVelocity;
            float sweepRadius = sweepBounds.Radius;
            Aabb sweepBox = new Aabb(sweepStart, sweepRadius) + new Aabb(sweepEnd, sweepRadius);

            var sweepVector2D = sweepVelocity.To2D();
            if (Vector3.IsNearZero(sweepVector2D)) return null;
            Vector3.SafeNormalAndLength2D(sweepVector2D, out Vector3 sweepNormal2D, out float sweepLength);

            float minTime = 1.0f;
            float minDot = -1f;
            WorldEntity hitEntity = null;
            var spContext = new EntityRegionSPContext(EntityRegionSPContextFlags.All);

            foreach (var otherEntity in IterateEntitiesInVolume(sweepBox, spContext))
            {
                if (canBlockFunc(otherEntity))
                {
                    float resultTime = 1.0f;
                    Vector3? resultNormal = null;
                    if (sweepBounds.Sweep(otherEntity.Bounds, Vector3.Zero, sweepVelocity, ref resultTime, ref resultNormal))
                    {
                        if (hitEntity != null)
                        {
                            float epsilon = 0.25f / sweepLength;
                            if (Segment.EpsilonTest(resultTime, minTime, epsilon))
                            {
                                float dot = Vector3.Dot(sweepNormal2D, Vector3.Normalize2D(otherEntity.RegionLocation.Position - sweepStart));
                                if (dot > minDot)
                                {
                                    hitEntity = otherEntity;
                                    minTime = resultTime;
                                    minDot = dot;
                                    resultHitPosition = sweepStart + sweepVelocity * minTime;
                                }
                            }
                        }

                        if (resultTime < minTime)
                        {
                            float dot = Vector3.Dot(sweepNormal2D, Vector3.Normalize2D(otherEntity.RegionLocation.Position - sweepStart));
                            hitEntity = otherEntity;
                            minTime = resultTime;
                            minDot = dot;
                            resultHitPosition = sweepStart + sweepVelocity * minTime;
                        }
                    }
                }
            }

            return hitEntity;
        }

        private static bool CanBlockEntitySweep(WorldEntity testEntity, WorldEntity owner, ulong targetEntityId, bool blocksLOS)
        {
            if (testEntity == null) return false;

            if (owner != null)
            {
                if (testEntity.Id == owner.Id)
                    return false;

                if (blocksLOS == false && owner.CanBeBlockedBy(testEntity))
                    return true;
            }

            if (targetEntityId != Entity.InvalidId && testEntity.Id == targetEntityId)
                return false;

            if (blocksLOS)
            {
                var proto = testEntity.WorldEntityPrototype;
                if (proto == null) return false;

                if (proto.Bounds.BlocksLineOfSight)
                    return true;
            }

            return false;
        }

        public bool ChoosePositionAtOrNearPoint(Bounds bounds, PathFlags pathFlags, PositionCheckFlags posFlags, BlockingCheckFlags blockFlags,
            float maxDistance, out Vector3 resultPosition, RandomPositionPredicate positionPredicate = null,
            EntityCheckPredicate checkPredicate = null, int maxPositionTests = 400)
        {
            if (IsLocationClear(bounds, pathFlags, posFlags, blockFlags)
                && (positionPredicate == null || positionPredicate.Test(bounds.Center)))
            {
                resultPosition = bounds.Center;
                return true;
            }
            else
            {
                return ChooseRandomPositionNearPoint(bounds, pathFlags, posFlags, blockFlags, 0, maxDistance, out resultPosition,
                    positionPredicate, checkPredicate, maxPositionTests);
            }
        }

        public bool ChooseRandomPositionNearPoint(Bounds bounds, PathFlags pathFlags, PositionCheckFlags posFlags, BlockingCheckFlags blockFlags,
            float minDistanceFromPoint, float maxDistanceFromPoint, out Vector3 resultPosition, RandomPositionPredicate positionPredicate = null,
            EntityCheckPredicate checkPredicate = null, int maxPositionTests = 400, HeightSweepType heightSweep = HeightSweepType.None,
            int maxSweepHeight = 0)
        {
            resultPosition = Vector3.Zero;
            if (maxDistanceFromPoint < minDistanceFromPoint) return false;

            if (posFlags.HasFlag(PositionCheckFlags.CanPathTo) && posFlags.HasFlag(PositionCheckFlags.CanSweepTo))
            {
                Logger.Warn("Do not use CheckCanSweepTo with CheckCanPathTo, it is a worthless CheckPath after the CheckSweep passes. " +
                            "If the CheckSweep fails, the point is dropped and CheckPath never happens. " +
                            "If you must CheckPath, you want EXCLUSIVELY CheckCanPathTo.");
                return false;
            }

            if (maxPositionTests <= 0)
            {
                Logger.Warn("maxPositionTests must be greater than zero or you will not test any positions!");
                return false;
            }

            Vector3 point = bounds.Center;
            resultPosition.Z = point.Z;
            var random = Game.Random;

            List<WorldEntity> entitiesInRadius = new();
            if (posFlags.HasFlag(PositionCheckFlags.CanBeBlockedEntity) || posFlags.HasFlag(PositionCheckFlags.CanPathToEntities))
            {
                entitiesInRadius.Capacity = 256;
                GetEntitiesInVolume(entitiesInRadius, new Sphere(point, maxDistanceFromPoint), new EntityRegionSPContext(EntityRegionSPContextFlags.ActivePartition));

                if (posFlags.HasFlag(PositionCheckFlags.CanBeBlockedEntity) && checkPredicate != null)
                {
                    for (int i = entitiesInRadius.Count - 1; i >= 0; i--)
                    {
                        if (checkPredicate.Test(entitiesInRadius[i]) == false)
                            entitiesInRadius.RemoveAt(i);
                    }
                }
            }

            Bounds checkBounds = new(bounds);
            if (blockFlags.HasFlag(BlockingCheckFlags.CheckSpawns))
                checkBounds.CollisionType = BoundsCollisionType.Blocking;

            float minDistanceSq = minDistanceFromPoint * minDistanceFromPoint;
            float maxDistanceSq = maxDistanceFromPoint * maxDistanceFromPoint;

            bool foundBlockedEntity = false;
            Vector3 blockedPosition = Vector3.Zero;

            List<WorldEntity> influenceEntities = new();

            if (posFlags.HasFlag(PositionCheckFlags.CanPathToEntities))
            {
                foreach (WorldEntity entity in entitiesInRadius)
                {
                    if (entity.HasNavigationInfluence)
                    {
                        entity.DisableNavigationInfluence();
                        influenceEntities.Add(entity);
                    }
                }
            }

            PathFlags checkPathFlags = pathFlags;
            if (blockFlags.HasFlag(BlockingCheckFlags.CheckLanding))
                checkPathFlags = PathFlags.Walk;

            float angle = 0f;
            float checkRadius = Math.Max(bounds.Radius, 5.0f);
            checkRadius = Math.Max(checkRadius, minDistanceFromPoint);
            float circumference = checkRadius / 1.5f;

            int tries = maxPositionTests; // 400!
            while (tries-- > 0)
            {
                Vector3 offset = Vector3.Zero;
                if (posFlags.HasFlag(PositionCheckFlags.InRadius))
                {
                    offset.X = checkRadius;
                    offset = Vector3.AxisAngleRotate(offset, Vector3.ZAxis, angle);
                    angle += circumference / checkRadius;
                    if (angle >= MathHelper.TwoPi)
                    {
                        checkRadius += Math.Max(bounds.Radius, 5.0f);
                        angle = 0f;
                        if (checkRadius > maxDistanceFromPoint) break;
                    }
                }
                else
                {
                    offset.X = random.NextFloat(-maxDistanceFromPoint, maxDistanceFromPoint);
                    offset.Y = random.NextFloat(-maxDistanceFromPoint, maxDistanceFromPoint);
                    float lengthSq = Vector3.LengthSquared(offset);
                    if (lengthSq < minDistanceSq || lengthSq > maxDistanceSq)
                        continue;
                }

                resultPosition.X = point.X + offset.X;
                resultPosition.Y = point.Y + offset.Y;
                checkBounds.Center = resultPosition;

                var naviMesh = NaviMesh;
                if (naviMesh.Contains(checkBounds.Center, checkBounds.Radius, new DefaultContainsPathFlagsCheck(checkPathFlags)))
                {
                    if (posFlags.HasFlag(PositionCheckFlags.CanSweepTo) || posFlags.HasFlag(PositionCheckFlags.CanSweepRadius))
                    {
                        Vector3? resultSweepPosition = new();
                        Vector3? resultNorm = null;
                        float radius = posFlags.HasFlag(PositionCheckFlags.CanSweepRadius) ? 0f : bounds.Radius;
                        SweepResult sweepResult = naviMesh.Sweep(point, resultPosition, radius, pathFlags,
                            ref resultSweepPosition, ref resultNorm, 0f, heightSweep, maxSweepHeight);
                        if (sweepResult != SweepResult.Success) continue;
                    }

                    if (posFlags.HasFlag(PositionCheckFlags.CanPathTo) || posFlags.HasFlag(PositionCheckFlags.CanPathToEntities))
                        if (NaviPath.CheckCanPathTo(naviMesh, bounds.Center, resultPosition, bounds.Radius, pathFlags) != NaviPathResult.Success)
                            continue;

                    if (positionPredicate != null && positionPredicate.Test(resultPosition) == false)
                        continue;

                    if (posFlags.HasFlag(PositionCheckFlags.CanBeBlockedEntity))
                    {
                        if (IsLocationClearOfEntities(checkBounds, entitiesInRadius, blockFlags) == false)
                        {
                            if (posFlags.HasFlag(PositionCheckFlags.PreferNoEntity) && foundBlockedEntity == false)
                            {
                                foundBlockedEntity = true;
                                blockedPosition = checkBounds.Center;
                            }

                            continue;
                        }
                    }

                    return true;
                }
            }

            foreach (WorldEntity entity in influenceEntities)
                entity.EnableNavigationInfluence();

            if (posFlags.HasFlag(PositionCheckFlags.CanBeBlockedEntity) && posFlags.HasFlag(PositionCheckFlags.PreferNoEntity) && foundBlockedEntity)
            {
                resultPosition = blockedPosition;
                return true;
            }

            resultPosition = point;
            return false;
        }

        private static bool IsLocationClearOfEntities(Bounds bounds, List<WorldEntity> entities, BlockingCheckFlags blockFlags = BlockingCheckFlags.None)
        {
            foreach (WorldEntity entity in entities)
            {
                if (IsBoundsBlockedByEntity(bounds, entity, blockFlags))
                    return false;
            }

            return true;
        }

        public bool IsLocationClear(Bounds bounds, PathFlags pathFlags, PositionCheckFlags posFlags, BlockingCheckFlags blockFlags = BlockingCheckFlags.None)
        {
            if (NaviMesh.Contains(bounds.Center, bounds.Radius, new DefaultContainsPathFlagsCheck(pathFlags)) == false)
                return false;

            if (posFlags.HasFlag(PositionCheckFlags.CanBeBlockedEntity) || posFlags.HasFlag(PositionCheckFlags.CanBeBlockedAvatar))
            {
                var volume = new Sphere(bounds.Center, bounds.Radius);
                foreach (WorldEntity entity in IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.ActivePartition)))
                {
                    if (posFlags.HasFlag(PositionCheckFlags.CanBeBlockedAvatar) && entity is not Avatar) continue;
                    if (IsBoundsBlockedByEntity(bounds, entity, blockFlags))
                        return false;
                }
            }

            return true;
        }

        public bool ProjectBoundsIntoRegion(ref Bounds bounds, in Vector3 direction)
        {
            Point2[] points = Aabb2.Expand(-bounds.GetRadius()).GetPoints();

            float minDistance = float.MaxValue;
            Vector3 closestPoint = Vector3.Zero;

            for (int i = 0; i < 4; i++)
            {
                var point1 = new Vector3(points[i].X, points[i].Y, 0.0f);
                var point2 = new Vector3(points[(i + 1) % 4].X, points[(i + 1) % 4].Y, 0.0f);

                if (Segment.RaySegmentIntersect2D(bounds.Center, direction, point1, point2 - point1, out Vector3 intersectPoint))
                {
                    float distance = Vector3.Distance(intersectPoint, bounds.Center);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestPoint = intersectPoint;
                    }
                }
            }
            if (minDistance == float.MaxValue) return false;

            bounds.Center = new Vector3(closestPoint.X, closestPoint.Y, bounds.Center.Z);
            return true;
        }

        #endregion

        public void UpdateLastVisitedTime()
        {
            LastVisitedTime = Clock.UnixTime;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && Prototype.HasKeyword(keywordProto);
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

        public bool FilterRegion(PrototypeId filterRegionRef, bool includeChildren = false, PrototypeId[] regionsExclude = null)
        {
            if (Prototype == null) return false;
            return Prototype.FilterRegion(filterRegionRef, includeChildren, regionsExclude);
        }

        public void OnAddToAOI(Player player)
        {
            player.MissionManager.InitializeForPlayer(player, this);
        }
    }

    public class RandomPositionPredicate    // TODO: Change to interface / struct
    {
        public virtual bool Test(Vector3 center) => false;
    }

    public class EntityCheckPredicate       // TODO: Change to interface / struct
    {
        public virtual bool Test(WorldEntity worldEntity) => false;
    }

    public class DividedStartLocation
    {
        public DividedStartLocationPrototype Location { get; }

        public DividedStartLocation(DividedStartLocationPrototype location)
        {
            Location = location;
        }
    }
}
