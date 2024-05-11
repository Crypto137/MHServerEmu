using System.Diagnostics;
using System.Globalization;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games
{
    public partial class Game
    {
        public const string Version = "1.52.0.1700";

        [ThreadStatic]
        internal static Game Current;

        private const int TargetFrameRate = 20;
        public static readonly TimeSpan StartTime = TimeSpan.FromMilliseconds(1);
        public readonly TimeSpan FixedTimeBetweenUpdates = TimeSpan.FromMilliseconds(1000f / TargetFrameRate);
        public readonly NetStructGameOptions GameOptions;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly object _gameLock = new();
        private readonly CoreNetworkMailbox<FrontendClient> _mailbox = new();

        private readonly Stopwatch _gameTimer = new();
        private TimeSpan _accumulatedFixedTimeUpdateTime;   // How much has passed since the last fixed time update
        private TimeSpan _lastFixedTimeUpdateStartTime;     // When was the last time we tried to do a fixed time update
        private TimeSpan _lastFixedTimeUpdateProcessTime;   // How long the last fixed time took
        private int _frameCount;

        private bool _isRunning;
        private Thread _gameThread;
        private Task _regionCleanupTask;

        private ulong _currentRepId;

        // Dumped ids: 0xF9E00000FA2B3EA (Lobby), 0xFF800000FA23AE9 (Tower), 0xF4A00000FA2B47D (Danger Room), 0xFCC00000FA29FE7 (Midtown)
        public ulong Id { get; }
        public GRandom Random { get; } = new();
        public PlayerConnectionManager NetworkManager { get; }
        public EventManager EventManager { get; }
        public EntityManager EntityManager { get; }
        public RegionManager RegionManager { get; }
        public AdminCommandManager AdminCommandManager { get; }

        public ulong CurrentRepId { get => ++_currentRepId; }
        // We use a dictionary property instead of AccessMessageHandlerHash(), which is essentially just a getter
        public Dictionary<ulong, IArchiveMessageHandler> MessageHandlerDict { get; } = new();
        public ulong NumQuantumFixedTimeUpdates { get => throw new NotImplementedException(); }

        public override string ToString() => $"serverGameId=0x{Id:X}";

        public Game(ulong id)
        {
            Id = id;

            // Initialize game options
            var config = ConfigManager.Instance.GetConfig<GameOptionsConfig>();
            GameOptions = config.ToProtobuf();

            // The game uses 16 bits of the current UTC time in seconds as the initial replication id
            _currentRepId = (ulong)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) & 0xFFFF;

            AdminCommandManager = new(this);
            NetworkManager = new(this);
            EventManager = new(this);
            EntityManager = new(this);
            RegionManager = new(EntityManager);
            RegionManager.Initialize(this);

            Random = new();
        }

        public void Run()
        {
            // NOTE: This is now separate from the constructor so that we can have
            // a dummy game with no simulation running that we use to parse messages.
            if (_isRunning) throw new InvalidOperationException();
            _isRunning = true;

            // Run a task that cleans up unused regions periodically
            _regionCleanupTask = Task.Run(async () => await RegionManager.CleanUpRegionsAsync());

            // Initialize and start game thread
            _gameThread = new(Update) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            _gameThread.Start();

            Logger.Info($"Game 0x{Id:X} started, initial replication id: {_currentRepId}");
        }

        public void HandleClientMessage(FrontendClient client, MessagePackage message)
        {
            lock (_gameLock)
            {
                _mailbox.Post(client, message);
            }
        }

        public void AddClient(FrontendClient client)
        {
            NetworkManager.AsyncAddClient(client);
        }

        public void RemoveClient(FrontendClient client)
        {
            NetworkManager.AsyncRemoveClient(client);
        }

        /// <summary>
        /// Sends an <see cref="IMessage"/> over the specified <see cref="PlayerConnection"/>.
        /// </summary>
        public void SendMessage(PlayerConnection connection, IMessage message)
        {
            NetworkManager.SendMessage(connection, message);
        }

        /// <summary>
        /// Sends an <see cref="IMessage"/> to all connected players.
        /// </summary>
        public void BroadcastMessage(IMessage message)
        {
            NetworkManager.BroadcastMessage(message);
        }

        public void MovePlayerToRegion(PlayerConnection playerConnection, PrototypeId regionDataRef, PrototypeId waypointDataRef)
        {
            // This lock is still here because it is still technically possible to call MovePlayerToRegion() asynchronously from commands.
            // This should not happen in practice since the only way to invoke those commands is from the game.
            lock (_gameLock)
            {
                playerConnection.ExitGame();

                playerConnection.RegionDataRef = regionDataRef;
                playerConnection.WaypointDataRef = waypointDataRef;

                NetworkManager.SetPlayerConnectionPending(playerConnection);
            }
        }

        public void MovePlayerToEntity(PlayerConnection playerConnection, ulong entityId)
        {
            // TODO change Reload without exit of region
            var entityManager = playerConnection.Game.EntityManager;
            var worldEntity = entityManager.GetEntity<WorldEntity>(entityId);
            if (worldEntity == null) return;

            playerConnection.ExitGame();

            playerConnection.RegionDataRef = (PrototypeId)worldEntity.Region.PrototypeId;
            playerConnection.EntityToTeleport = worldEntity;

            NetworkManager.SetPlayerConnectionPending(playerConnection);
        }

        public void GetRegionAsync(PlayerConnection playerConnection)
        {
            Region region = RegionManager.GetRegion((RegionPrototypeId)playerConnection.RegionDataRef);
            if (region != null) EventManager.AddEvent(playerConnection, EventEnum.GetRegion, 0, region);
            else EventManager.AddEvent(playerConnection, EventEnum.ErrorInRegion, 0, playerConnection.RegionDataRef);
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

        public IEnumerable<Region> RegionIterator()
        {            
            foreach (Region region in RegionManager.AllRegions) 
                yield return region;
        }

        // StartTime is always a TimeSpan of 1 ms, so we can make both Game::GetTimeFromStart() and Game::GetTimeFromDelta() static

        public static long GetTimeFromStart(TimeSpan gameTime) => (long)(gameTime - StartTime).TotalMilliseconds;
        public static TimeSpan GetTimeFromDelta(long delta) => StartTime.Add(TimeSpan.FromMilliseconds(delta));

        public TimeSpan GetCurrentTime()
        {
            // TODO check EventScheduler
            return Clock.GameTime;
        }

        private void Update()
        {
            Current = this;
            _gameTimer.Start();

            while (true)
            {
                NetworkManager.Update();                            // Add / remove clients
                NetworkManager.ProcessPendingPlayerConnections();   // Load pending players
                UpdateFixedTime();                                  // Update simulation state
            }
        }

        private void UpdateFixedTime()
        {
            // First we make sure enough time has passed to do another fixed time update
            TimeSpan currentTime = _gameTimer.Elapsed;
            _accumulatedFixedTimeUpdateTime += currentTime - _lastFixedTimeUpdateStartTime;
            _lastFixedTimeUpdateStartTime = currentTime;

            if (_accumulatedFixedTimeUpdateTime < FixedTimeBetweenUpdates)
            {
                // Thread.Sleep() can sleep for longer than specified, so rather than sleeping
                // for the entire time window between fixed updates, we do it in 1 ms intervals.
                // For reference see MonoGame implementation here:
                // https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Game.cs#L518
                if ((FixedTimeBetweenUpdates - _accumulatedFixedTimeUpdateTime).TotalMilliseconds >= 2.0)
                    Thread.Sleep(1);
                return;
            }

            lock (_gameLock)    // Lock to prevent state from being modified mid-update
            {
                int timesUpdated = 0;

                while (_accumulatedFixedTimeUpdateTime >= FixedTimeBetweenUpdates)
                {
                    _accumulatedFixedTimeUpdateTime -= FixedTimeBetweenUpdates;

                    TimeSpan fixedUpdateStartTime = _gameTimer.Elapsed;

                    DoFixedTimeUpdate();
                    _frameCount++;
                    timesUpdated++;

                    _lastFixedTimeUpdateProcessTime = _gameTimer.Elapsed - fixedUpdateStartTime;

                    if (_lastFixedTimeUpdateProcessTime > FixedTimeBetweenUpdates)
                        Logger.Warn($"UpdateFixedTime(): Frame took longer ({_lastFixedTimeUpdateProcessTime.TotalMilliseconds:0.00} ms) than FixedTimeBetweenUpdates ({FixedTimeBetweenUpdates.TotalMilliseconds:0.00} ms)");
                }

                if (timesUpdated > 1)
                    Logger.Warn($"UpdateFixedTime(): Simulated {timesUpdated} frames in a single update to catch up");
            }
        }

        private void DoFixedTimeUpdate()
        {
            // Handle all queued messages
            while (_mailbox.HasMessages)
            {
                var message = _mailbox.PopNextMessage();
                PlayerConnection connection = NetworkManager.GetPlayerConnection(message.Item1);
                connection.ReceiveMessage(message.Item2);
            }

            // Update event manager
            EventManager.Update();
            // Update locomote
            EntityManager.LocomoteEntities();
            // Update physics manager
            EntityManager.PhysicsResolveEntities();

            // Send responses to all clients
            NetworkManager.SendAllPendingMessages();
        }
    }
}
