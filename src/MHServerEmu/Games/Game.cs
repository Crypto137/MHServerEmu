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
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Regions;
using MHServerEmu.PlayerManagement.Accounts.DBModels;

namespace MHServerEmu.Games
{
    public partial class Game
    {
        public const string Version = "1.52.0.1700";

        private static readonly Logger Logger = LogManager.CreateLogger();

        public const int TickRate = 20;                 // Ticks per second based on client behavior
        public const long TickTime = 1000 / TickRate;   // ms per tick

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

            // The game uses 16 bits of the current UTC time in seconds as the initial replication id
            _currentRepId = (ulong)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond) & 0xFFFF;
            Logger.Debug($"Initial repId: {_currentRepId}");

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

        public void Handle(FrontendClient client, GameMessage message)
        {
            lock (_gameLock)
            {
                _mailbox.Post(client, message);
            }
        }

        public void Handle(FrontendClient client, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages) Handle(client, message);
        }

        public void AddPlayer(FrontendClient client)
        {
            lock (_gameLock)
            {
                client.GameId = Id;
                PlayerConnection connection = NetworkManager.AddPlayer(client);
                foreach (IMessage message in GetBeginLoadingMessages(connection))
                    SendMessage(connection, message);

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

        public void MovePlayerToRegion(PlayerConnection connection, RegionPrototypeId region, PrototypeId waypointDataRef)
        {
            lock (_gameLock)
            {
                foreach (IMessage message in GetExitGameMessages())
                    SendMessage(connection, message);

                connection.FrontendClient.Session.Account.Player.Region = region;
                connection.FrontendClient.Session.Account.Player.Waypoint = waypointDataRef;

                foreach (IMessage message in GetBeginLoadingMessages(connection))
                    SendMessage(connection, message);
            }
        }

        public void MovePlayerToEntity(PlayerConnection connection, ulong entityId)
        {   
            // TODO change Reload without exit of region
            lock (_gameLock)
            {
                var entityManager = connection.Game.EntityManager;
                var targetEntity = entityManager.GetEntityById(entityId);
                if (targetEntity is not WorldEntity worldEntity) return;

                foreach (IMessage message in GetExitGameMessages())
                    SendMessage(connection, message);

                connection.FrontendClient.Session.Account.Player.Region = worldEntity.Region.PrototypeId;
                connection.FrontendClient.EntityToTeleport = worldEntity;

                foreach (IMessage message in GetBeginLoadingMessages(connection))
                    SendMessage(connection, message);
            }
        }

        public void FinishLoading(PlayerConnection playerConnection)
        {
            foreach (IMessage message in GetFinishLoadingMessages(playerConnection))
                SendMessage(playerConnection, message);

            playerConnection.FrontendClient.IsLoading = false;
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

        private List<IMessage> GetBeginLoadingMessages(PlayerConnection connection)
        {
            FrontendClient client = connection.FrontendClient;

            DBAccount account = client.Session.Account;
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
            messageList.AddRange(LoadPlayerEntityMessages(account));
            messageList.Add(NetMessageReadyAndLoadedOnGameServer.DefaultInstance);

            // Before changing to the actual destination region the game seems to first change into a transitional region
            messageList.Add(NetMessageRegionChange.CreateBuilder()
                .SetRegionId(0)
                .SetServerGameId(0)
                .SetClearingAllInterest(false)
                .Build());

            messageList.Add(NetMessageQueueLoadingScreen.CreateBuilder()
                .SetRegionPrototypeId((ulong)account.Player.Region)
                .Build());

            // Run region generation as a task
            Task.Run(() => GetRegionAsync(connection, account.Player.Region));
            client.AOI.LoadedCellCount = 0;
            client.IsLoading = true;
            return messageList;
        }

        private void GetRegionAsync(PlayerConnection connection, RegionPrototypeId regionPrototypeId)
        {
            Region region = RegionManager.GetRegion(regionPrototypeId);
            EventManager.AddEvent(connection, EventEnum.GetRegion, 0, region);
        }

        private List<IMessage> GetFinishLoadingMessages(PlayerConnection playerConnection)
        {
            FrontendClient client = playerConnection.FrontendClient;

            DBAccount account = client.Session.Account;
            List<IMessage> messageList = new();

            Vector3 entrancePosition = new(client.StartPositon);
            Orientation entranceOrientation = new(client.StartOrientation);
            entrancePosition.Z += 42; // TODO project to floor

            EnterGameWorldArchive avatarEnterGameWorldArchive = new((ulong)account.Player.Avatar.ToEntityId(), entrancePosition, entranceOrientation.Yaw, 350f);
            messageList.Add(NetMessageEntityEnterGameWorld.CreateBuilder()
                .SetArchiveData(avatarEnterGameWorldArchive.Serialize())
                .Build());

            client.AOI.Update(entrancePosition);
            messageList.AddRange(client.AOI.Messages);

            // Load power collection
            messageList.AddRange(PowerLoader.LoadAvatarPowerCollection(account.Player.Avatar.ToEntityId()));

            // Dequeue loading screen
            messageList.Add(NetMessageDequeueLoadingScreen.DefaultInstance);

            return messageList;
        }

        private List<IMessage> LoadPlayerEntityMessages(DBAccount account)
        {
            List<IMessage> messageList = new();

            // NetMessageLocalPlayer (set local player entity id and game options)
            messageList.Add(NetMessageLocalPlayer.CreateBuilder()
                .SetLocalPlayerEntityId(14646212)
                .SetGameOptions(NetStructGameOptions.CreateBuilder()
                    .SetTeamUpSystemEnabled(ConfigManager.GameOptions.TeamUpSystemEnabled)
                    .SetAchievementsEnabled(ConfigManager.GameOptions.AchievementsEnabled)
                    .SetOmegaMissionsEnabled(ConfigManager.GameOptions.OmegaMissionsEnabled)
                    .SetVeteranRewardsEnabled(ConfigManager.GameOptions.VeteranRewardsEnabled)
                    .SetMultiSpecRewardsEnabled(ConfigManager.GameOptions.MultiSpecRewardsEnabled)
                    .SetGiftingEnabled(ConfigManager.GameOptions.GiftingEnabled)
                    .SetCharacterSelectV2Enabled(ConfigManager.GameOptions.CharacterSelectV2Enabled)
                    .SetCommunityNewsV2Enabled(ConfigManager.GameOptions.CommunityNewsV2Enabled)
                    .SetLeaderboardsEnabled(ConfigManager.GameOptions.LeaderboardsEnabled)
                    .SetNewPlayerExperienceEnabled(ConfigManager.GameOptions.NewPlayerExperienceEnabled)
                    .SetServerTimeOffsetUTC(-7)
                    .SetUseServerTimeOffset(true)  // Although originally this was set to false, it needs to be true because auto offset doesn't work past 2019
                    .SetMissionTrackerV2Enabled(ConfigManager.GameOptions.MissionTrackerV2Enabled)
                    .SetGiftingAccountAgeInDaysRequired(ConfigManager.GameOptions.GiftingAccountAgeInDaysRequired)
                    .SetGiftingAvatarLevelRequired(ConfigManager.GameOptions.GiftingAvatarLevelRequired)
                    .SetGiftingLoginCountRequired(ConfigManager.GameOptions.GiftingLoginCountRequired)
                    .SetInfinitySystemEnabled(ConfigManager.GameOptions.InfinitySystemEnabled)
                    .SetChatBanVoteAccountAgeInDaysRequired(ConfigManager.GameOptions.ChatBanVoteAccountAgeInDaysRequired)
                    .SetChatBanVoteAvatarLevelRequired(ConfigManager.GameOptions.ChatBanVoteAvatarLevelRequired)
                    .SetChatBanVoteLoginCountRequired(ConfigManager.GameOptions.ChatBanVoteLoginCountRequired)
                    .SetIsDifficultySliderEnabled(ConfigManager.GameOptions.IsDifficultySliderEnabled)
                    .SetOrbisTrophiesEnabled(ConfigManager.GameOptions.OrbisTrophiesEnabled)
                    .SetPlatformType((int)Platforms.PC))
                .Build());

            // Create and initialize player entity
            Player player = new(new EntityBaseData());
            player.InitializeFromDBAccount(account);
            messageList.Add(player.ToNetMessageEntityCreate());

            // Avatars
            PrototypeId currentAvatarId = (PrototypeId)account.CurrentAvatar.Prototype;
            ulong avatarEntityId = player.BaseData.EntityId + 1;
            ulong avatarRepId = player.PartyId.ReplicationId + 1;

            List<Avatar> avatarList = new();
            uint librarySlot = 0;
            foreach (PrototypeId avatarId in GameDatabase.DataDirectory.IteratePrototypesInHierarchy(typeof(AvatarPrototype),
                PrototypeIterateFlags.NoAbstract | PrototypeIterateFlags.ApprovedOnly))
            {
                if (avatarId == (PrototypeId)6044485448390219466) continue;   //zzzBrevikOLD.prototype

                Avatar avatar = new(avatarEntityId, avatarRepId);
                avatarEntityId++;
                avatarRepId += 2;

                avatar.InitializeFromDBAccount(avatarId, account);

                avatar.BaseData.InvLoc = (avatarId == currentAvatarId)
                    ? new(player.BaseData.EntityId, (PrototypeId)9555311166682372646, 0)                // Entity/Inventory/PlayerInventories/PlayerAvatarInPlay.prototype
                    : new(player.BaseData.EntityId, (PrototypeId)5235960671767829134, librarySlot++);   // Entity/Inventory/PlayerInventories/PlayerAvatarLibrary.prototype

                avatarList.Add(avatar);
            }

            messageList.AddRange(avatarList.Select(avatar => avatar.ToNetMessageEntityCreate()));

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
