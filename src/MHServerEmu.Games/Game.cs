using System.Diagnostics;
using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Metrics;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Network.InstanceManagement;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social;
using MHServerEmu.Games.Social.Parties;
using MHServerEmu.Games.UI;

namespace MHServerEmu.Games
{
    public enum GameState
    {
        Created,
        Running,
        ShuttingDown,
        Shutdown
    }

    public enum GameShutdownReason
    {
        ShutdownRequested,
        GameInstanceCrash
    }

    public class Game
    {
        public const string Version = "1.52.0.1700";

        [ThreadStatic]
        internal static Game Current;

        private const int TargetFrameRate = 20;
        public static readonly TimeSpan StartTime = TimeSpan.FromMilliseconds(1);
        public readonly NetStructGameOptions GameOptions;
        public readonly CustomGameOptionsConfig CustomGameOptions;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Stopwatch _gameTimer = new();
        private FixedQuantumGameTime _realGameTime = new(TimeSpan.FromMilliseconds(1));
        private TimeSpan _currentGameTime = TimeSpan.FromMilliseconds(1);   // Current time in the game simulation
        private TimeSpan _lastFixedTimeUpdateProcessTime;                   // How long the last fixed update took
        private TimeSpan _fixedTimeUpdateProcessTimeLogThreshold;
        private long _frameCount;

        private int _liveTuningChangeNum = -1;

        private ulong _currentRepId;

        private GameShutdownReason? _shutdownReason;

        public ulong Id { get; }
        public GameManager GameManager { get; }

        public GameState State { get; private set; } = GameState.Created;

        public GRandom Random { get; } = new();
        public PlayerConnectionManager NetworkManager { get; }
        public GameServiceMailbox ServiceMailbox { get; }
        public EventScheduler GameEventScheduler { get; private set; }
        public EntityManager EntityManager { get; }
        public RegionManager RegionManager { get; }
        public AdminCommandManager AdminCommandManager { get; }
        public LootManager LootManager { get; }
        public GameDialogManager GameDialogManager { get; }
        public ChatManager ChatManager { get; }
        public PartyManager PartyManager { get; }
        public LiveTuningData LiveTuningData { get => LiveTuningData.Current; }

        public ConditionPool ConditionPool { get; } = new();

        public TimeSpan FixedTimeBetweenUpdates { get; } = TimeSpan.FromMilliseconds(1000f / TargetFrameRate);
        public TimeSpan RealGameTime { get => (TimeSpan)_realGameTime; }
        public TimeSpan CurrentTime { get => GameEventScheduler != null ? GameEventScheduler.CurrentTime : _currentGameTime; }
        public TimeSpan NextUpdateTime { get; private set; } = Clock.GameTime;
        public ulong NumQuantumFixedTimeUpdates { get => (ulong)CurrentTime.CalcNumTimeQuantums(FixedTimeBetweenUpdates); }

        public ulong CurrentRepId { get => ++_currentRepId; }
        public Dictionary<ulong, IArchiveMessageHandler> MessageHandlerDict { get; } = new();
        public bool OmegaMissionsEnabled { get; set; }
        public bool AchievementsEnabled { get; set; }
        public bool LeaderboardsEnabled { get; set; }
        public bool InfinitySystemEnabled { get => GameOptions.InfinitySystemEnabled; }

        public override string ToString() => $"serverGameId=0x{Id:X}";

        public Game(ulong id, GameManager gameManager)
        {
            Id = id;
            GameManager = gameManager;

            // Small lags are fine, and logging all of them creates too much noise
            _fixedTimeUpdateProcessTimeLogThreshold = FixedTimeBetweenUpdates * 5;

            // Initialize game options
            var config = ConfigManager.Instance.GetConfig<GameOptionsConfig>();
            AchievementsEnabled = config.AchievementsEnabled;
            LeaderboardsEnabled = config.LeaderboardsEnabled;
            GameOptions = config.ToProtobuf();

            CustomGameOptions = ConfigManager.Instance.GetConfig<CustomGameOptionsConfig>();

            // The game uses 16 bits of the current UTC time in seconds as the initial replication id
            _currentRepId = (ulong)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) & 0xFFFF;

            AdminCommandManager = new(this);
            NetworkManager = new(this);
            ServiceMailbox = new(this);
            RegionManager = new();
            EntityManager = new(this);
            LootManager = new(this);
            GameDialogManager = new(this);
            ChatManager = new(this);
            PartyManager = new(this);
            Random = new();

            Initialize();
        }

        public bool Initialize()
        {
            bool success = true;

            _gameTimer.Start();

            _realGameTime.SetQuantumSize(FixedTimeBetweenUpdates);
            _realGameTime.UpdateToNow();
            _currentGameTime = RealGameTime;

            GameEventScheduler = new(RealGameTime, FixedTimeBetweenUpdates);

            success &= RegionManager.Initialize(this);
            success &= EntityManager.Initialize();

            OmegaMissionsEnabled = true;

            State = GameState.Running;
            Logger.Info($"Game 0x{Id:X} started, initial replication id: {_currentRepId}");

            return success;
        }

        public void Shutdown(GameShutdownReason reason)
        {
            if (State != GameState.Running)
                return;

            Logger.Info($"Game [{this}] received shutdown request, reason={reason}");

            _shutdownReason = reason;
            State = GameState.ShuttingDown;
        }

        public void Update()
        {
            if (State == GameState.ShuttingDown)
            {
                DoShutdown();
                return;
            }

            using GameProfileTimer timer = new(Id, GamePerformanceMetricEnum.UpdateTime);

            TimeSpan startTime = Clock.GameTime;

            // NOTE: We process input in NetworkManager.ReceiveAllPendingMessages() outside of UpdateFixedTime(), same as the client.

            NetworkManager.Update();                            // Add / remove clients
            NetworkManager.ReceiveAllPendingMessages();         // Process input

            UpdateLiveTuning();                                 // Check if live tuning data is out of date

            UpdateFixedTime();                                  // Update simulation state

            // Schedule the next update
            TimeSpan endTime = Clock.GameTime;

            if ((endTime - startTime) > FixedTimeBetweenUpdates)
                NextUpdateTime = endTime + FixedTimeBetweenUpdates;
            else
                NextUpdateTime = startTime + FixedTimeBetweenUpdates;
        }

        public void AddClient(IFrontendClient client)
        {
            NetworkManager.AsyncAddClient(client);
        }

        public void RemoveClient(IFrontendClient client)
        {
            NetworkManager.AsyncRemoveClient(client);
        }

        public void ReceiveMessageBuffer(IFrontendClient client, in MessageBuffer messageBuffer)
        {
            NetworkManager.AsyncReceiveMessageBuffer(client, messageBuffer);
        }

        public void ReceiveServiceMessage<T>(in T message) where T: struct, IGameServiceMessage
        {
            ServiceMailbox.PostMessage(message);
        }

        public Entity AllocateEntity(PrototypeId entityRef)
        {
            var proto = GameDatabase.GetPrototype<EntityPrototype>(entityRef);

            Entity entity;
            if (proto is SpawnerPrototype)
                entity = new Spawner(this);
            else if (proto is TransitionPrototype)
                entity = new Transition(this);
            else if (proto is AvatarPrototype)
                entity = new Avatar(this);
            else if (proto is MissilePrototype)
                entity = new Missile(this);
            else if (proto is PropPrototype) // DestructiblePropPrototype
                entity = new WorldEntity(this);
            else if (proto is AgentPrototype) // AgentTeamUpPrototype OrbPrototype SmartPropPrototype
                entity = new Agent(this);
            else if (proto is ItemPrototype) // CharacterTokenPrototype BagItemPrototype CostumePrototype CraftingIngredientPrototype
                                             // CostumeCorePrototype CraftingRecipePrototype ArmorPrototype ArtifactPrototype
                                             // LegendaryPrototype MedalPrototype RelicPrototype TeamUpGearPrototype
                                             // InventoryStashTokenPrototype EmoteTokenPrototype
                entity = new Item(this);
            else if (proto is KismetSequenceEntityPrototype)
                entity = new KismetSequenceEntity(this);
            else if (proto is HotspotPrototype)
                entity = new Hotspot(this);
            else if (proto is WorldEntityPrototype)
                entity = new WorldEntity(this);
            else if (proto is MissionMetaGamePrototype)
                entity = new MissionMetaGame(this);
            else if (proto is PvPPrototype)
                entity = new PvP(this);
            else if (proto is MetaGamePrototype) // MatchMetaGamePrototype
                entity = new MetaGame(this);
            else if (proto is PlayerPrototype)
                entity = new Player(this);
            else
                entity = new Entity(this);

            return entity;
        }

        public Power AllocatePower(PrototypeId powerProtoRef)
        {
            Type powerClassType = GameDatabase.DataDirectory.GetPrototypeClassType(powerProtoRef);

            if (powerClassType == typeof(MissilePowerPrototype))
                return new MissilePower(this, powerProtoRef);
            else if (powerClassType == typeof(SummonPowerPrototype))
                return new SummonPower(this, powerProtoRef);
            else
                return new Power(this, powerProtoRef);
        }

        // StartTime is always a TimeSpan of 1 ms, so we can make both Game::GetTimeFromStart() and Game::GetTimeFromDelta() static

        public static long GetTimeFromStart(TimeSpan gameTime) => (long)(gameTime - StartTime).TotalMilliseconds;
        public static TimeSpan GetTimeFromDelta(long delta) => StartTime.Add(TimeSpan.FromMilliseconds(delta));

        private void UpdateFixedTime()
        {
            // First we make sure enough time has passed to do another fixed time update
            _realGameTime.UpdateToNow();

            int timesUpdated = 0;

            TimeSpan updateStartTime = _gameTimer.Elapsed;
            while (_currentGameTime + FixedTimeBetweenUpdates <= RealGameTime)
            {
                _currentGameTime += FixedTimeBetweenUpdates;

                TimeSpan stepStartTime = _gameTimer.Elapsed;

                DoFixedTimeUpdate();
                _frameCount++;
                timesUpdated++;

                _lastFixedTimeUpdateProcessTime = _gameTimer.Elapsed - stepStartTime;

                if (_lastFixedTimeUpdateProcessTime > _fixedTimeUpdateProcessTimeLogThreshold)
                    Logger.Trace($"UpdateFixedTime(): Frame took longer ({_lastFixedTimeUpdateProcessTime.TotalMilliseconds:0.00} ms) than _fixedTimeUpdateWarningThreshold ({_fixedTimeUpdateProcessTimeLogThreshold.TotalMilliseconds:0.00} ms)");

                // Record additional metrics
                MetricsManager.Instance.RecordGamePerformanceMetric(Id, GamePerformanceMetricEnum.EntityCount, EntityManager.EntityCount);
                MetricsManager.Instance.RecordGamePerformanceMetric(Id, GamePerformanceMetricEnum.PlayerCount, EntityManager.PlayerCount);

                // Bail out if we have fallen behind more exceeded frame budget
                if (_gameTimer.Elapsed - updateStartTime > FixedTimeBetweenUpdates)
                    break;
            }

            _currentGameTime = RealGameTime;
        }

        private void DoFixedTimeUpdate()
        {
            using GameProfileTimer timer = new(Id, GamePerformanceMetricEnum.FrameTime);

            ServiceMailbox.ProcessMessages();

            GameEventScheduler.TriggerEvents(_currentGameTime);

            EntityManager.LocomoteEntities();

            EntityManager.PhysicsResolveEntities();

            EntityManager.ProcessDeferredLists();

            NetworkManager.SendAllPendingMessages();
        }

        private void DoShutdown()
        {
            if (State != GameState.ShuttingDown)
                return;

            Logger.Info($"Game shutdown requested. Game={this}, Reason={_shutdownReason}");
            NetworkManager.SendAllPendingMessages();
            GameManager.OnGameShutdown(this);   // This will notify the PlayerManager and disconnect all players

            // Wait for all players to leave the game (TODO?: let the thread do other work while we wait?)
            while (NetworkManager.Count > 0)
            {
                NetworkManager.Update();
                Thread.Sleep(1);
            }

            // Clean up entities
            EntityManager.DestroyAllEntities();
            EntityManager.ProcessDeferredLists();

            // Clean up regions
            RegionManager.DestroyAllRegions();

            // Mark this game as shut down
            State = GameState.Shutdown;

            Logger.Info($"Game [{this}] finished shutting down");
            MetricsManager.Instance.RemoveGameInstance(Id);
        }

        private void UpdateLiveTuning()
        {
            LiveTuningData liveTuningData = LiveTuningData;

            LiveTuningManager.Instance.CopyLiveTuningData(liveTuningData);  

            if (_liveTuningChangeNum != liveTuningData.ChangeNum)
            {
                NetworkManager.BroadcastMessage(liveTuningData.GetLiveTuningUpdate());
                _liveTuningChangeNum = liveTuningData.ChangeNum;
            }
        }
    }
}
