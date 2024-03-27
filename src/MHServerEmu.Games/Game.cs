using System.Diagnostics;
using System.Globalization;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games
{
    public partial class Game
    {
        public const string Version = "1.52.0.1700";

        private static readonly Logger Logger = LogManager.CreateLogger();

        public const int TickRate = 20;                 // Ticks per second based on client behavior
        public const long TickTime = 1000 / TickRate;   // ms per tick

        private readonly NetStructGameOptions _gameOptions;
        private readonly object _gameLock = new();
        private readonly CoreNetworkMailbox<FrontendClient> _mailbox = new();
        private readonly Stopwatch _tickWatch = new();

        private int _tickCount;
        private ulong _currentRepId;

        public ulong Id { get; }
        public GRandom Random { get; } = new();
        public PlayerConnectionManager NetworkManager { get; }
        public EventManager EventManager { get; }
        public EntityManager EntityManager { get; }
        public RegionManager RegionManager { get; }

        public ulong CurrentRepId { get => ++_currentRepId; }
        // We use a dictionary property instead of AccessMessageHandlerHash(), which is essentially just a getter
        public Dictionary<ulong, IArchiveMessageHandler> MessageHandlerDict { get; } = new();
        
        public override string ToString() => $"serverGameId=0x{Id:X}";

        public Game(ulong id)
        {
            Id = id;

            // Initialize game options
            var config = ConfigManager.Instance.GetConfig<GameOptionsConfig>();
            _gameOptions = config.ToProtobuf();

            // The game uses 16 bits of the current UTC time in seconds as the initial replication id
            _currentRepId = (ulong)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) & 0xFFFF;

            NetworkManager = new(this);
            EventManager = new(this);
            EntityManager = new(this);
            RegionManager = new(EntityManager);
            RegionManager.Initialize(this);

            Random = new();
            // Run a task that cleans up unused regions periodically
            Task.Run(async () => await RegionManager.CleanUpRegionsAsync());

            // Start main game loop
            Thread gameThread = new(Update) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            gameThread.Start();

            Logger.Info($"Game 0x{Id:X} created, initial replication id: {_currentRepId}");
        }

        public void Update()
        {
            while (true)
            {
                _tickWatch.Restart();
                Interlocked.Increment(ref _tickCount);

                lock (_gameLock)     // lock to prevent state from being modified mid-update
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

                    // Send responses to all clients
                    NetworkManager.SendAllPendingMessages();
                }

                _tickWatch.Stop();

                if (_tickWatch.ElapsedMilliseconds > TickTime)
                    Logger.Warn($"Game update took longer ({_tickWatch.ElapsedMilliseconds} ms) than target tick time ({TickTime} ms)");
                else
                    Thread.Sleep((int)(TickTime - _tickWatch.ElapsedMilliseconds));
            }
        }

        public void Handle(FrontendClient client, MessagePackage message)
        {
            lock (_gameLock)
            {
                _mailbox.Post(client, message);
            }
        }

        public void Handle(FrontendClient client, IEnumerable<MessagePackage> messages)
        {
            foreach (MessagePackage message in messages) Handle(client, message);
        }

        public void AddPlayer(FrontendClient client)
        {
            lock (_gameLock)
            {
                client.GameId = Id;
                PlayerConnection playerConnection = NetworkManager.AddPlayer(client);
                foreach (IMessage message in GetBeginLoadingMessages(playerConnection))
                    SendMessage(playerConnection, message);

                Logger.Trace($"Player {client.Session.Account} added to {this}");
            }
        }

        public void RemovePlayer(FrontendClient client)
        {
            lock (_gameLock)
            {
                NetworkManager.RemovePlayer(client);
                Logger.Trace($"Player {client.Session.Account} removed from {this}");
            }
        }

        public void MovePlayerToRegion(PlayerConnection playerConnetion, PrototypeId regionDataRef, PrototypeId waypointDataRef)
        {
            lock (_gameLock)
            {
                foreach (IMessage message in GetExitGameMessages())
                    SendMessage(playerConnetion, message);

                playerConnetion.RegionDataRef = regionDataRef;
                playerConnetion.WaypointDataRef = waypointDataRef;

                foreach (IMessage message in GetBeginLoadingMessages(playerConnetion))
                    SendMessage(playerConnetion, message);
            }
        }

        public void MovePlayerToEntity(PlayerConnection playerConnection, ulong entityId)
        {   
            // TODO change Reload without exit of region
            lock (_gameLock)
            {
                var entityManager = playerConnection.Game.EntityManager;
                var targetEntity = entityManager.GetEntityById(entityId);
                if (targetEntity is not WorldEntity worldEntity) return;

                foreach (IMessage message in GetExitGameMessages())
                    SendMessage(playerConnection, message);

                playerConnection.RegionDataRef = (PrototypeId)worldEntity.Region.PrototypeId;
                playerConnection.EntityToTeleport = worldEntity;

                foreach (IMessage message in GetBeginLoadingMessages(playerConnection))
                    SendMessage(playerConnection, message);
            }
        }

        public void FinishLoading(PlayerConnection playerConnection)
        {
            foreach (IMessage message in GetFinishLoadingMessages(playerConnection))
                SendMessage(playerConnection, message);

            playerConnection.IsLoading = false;
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

        private List<IMessage> GetBeginLoadingMessages(PlayerConnection playerConnection)
        {
            List<IMessage> messageList = new();

            // Add server info messages
            messageList.Add(NetMessageMarkFirstGameFrame.CreateBuilder()
                .SetCurrentservergametime((ulong)Clock.GameTime.TotalMilliseconds)
                .SetCurrentservergameid(1150669705055451881)
                .SetGamestarttime(1)
                .Build());

            messageList.Add(NetMessageServerVersion.CreateBuilder().SetVersion(Version).Build());
            messageList.Add(LiveTuningManager.LiveTuningData.ToNetMessageLiveTuningUpdate());
            messageList.Add(NetMessageReadyForTimeSync.DefaultInstance);

            // Load local player data
            messageList.Add(NetMessageLocalPlayer.CreateBuilder()
                .SetLocalPlayerEntityId(playerConnection.Player.BaseData.EntityId)
                .SetGameOptions(_gameOptions)
                .Build());

            messageList.Add(playerConnection.Player.ToNetMessageEntityCreate());

            messageList.AddRange(playerConnection.Player.AvatarList.Select(avatar => avatar.ToNetMessageEntityCreate()));

            messageList.Add(NetMessageReadyAndLoadedOnGameServer.DefaultInstance);

            // Before changing to the actual destination region the game seems to first change into a transitional region
            messageList.Add(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build());

            messageList.Add(NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)playerConnection.RegionDataRef)
                .Build());

            // Run region generation as a task
            Task.Run(() => GetRegionAsync(playerConnection));
            playerConnection.AOI.LoadedCellCount = 0;
            playerConnection.IsLoading = true;
            return messageList;
        }

        private void GetRegionAsync(PlayerConnection playerConnection)
        {
            Region region = RegionManager.GetRegion((RegionPrototypeId)playerConnection.RegionDataRef);
            EventManager.AddEvent(playerConnection, EventEnum.GetRegion, 0, region);
        }

        private List<IMessage> GetFinishLoadingMessages(PlayerConnection playerConnection)
        {
            List<IMessage> messageList = new();

            Vector3 entrancePosition = new(playerConnection.StartPositon);
            Orientation entranceOrientation = new(playerConnection.StartOrientation);
            entrancePosition.Z += 42; // TODO project to floor

            EnterGameWorldArchive avatarEnterGameWorldArchive = new((ulong)playerConnection.Player.CurrentAvatar.BaseData.EntityId, entrancePosition, entranceOrientation.Yaw, 350f);
            messageList.Add(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(avatarEnterGameWorldArchive.Serialize())
                .Build());

            playerConnection.AOI.Update(entrancePosition);
            messageList.AddRange(playerConnection.AOI.Messages);

            // Load power collection
            messageList.AddRange(PowerLoader.LoadAvatarPowerCollection(playerConnection));

            // Dequeue loading screen
            messageList.Add(NetMessageDequeueLoadingScreen.DefaultInstance);

            return messageList;
        }

        public IMessage[] GetExitGameMessages()
        {
            return new IMessage[]
            {
                NetMessageBeginExitGame.DefaultInstance,
                NetMessageRegionChange.CreateBuilder().SetRegionId(0).SetServerGameId(0).SetClearingAllInterest(true).Build()
            };
        }
    }
}
