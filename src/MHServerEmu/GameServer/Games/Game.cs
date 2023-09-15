using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Powers;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Games
{
    public partial class Game : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public const int TickRate = 20;                 // Ticks per second based on client behavior
        public const long TickTime = 1000 / TickRate;   // ms per tick

        private readonly object _gameLock = new();
        private readonly Queue<QueuedGameMessage> _messageQueue = new();
        private readonly Dictionary<FrontendClient, Queue<QueuedGameMessage>> _responseQueueDict = new();
        private readonly Stopwatch _tickWatch = new();

        private readonly GameServerManager _gameServerManager;

        private int _tickCount;

        public ulong Id { get; }
        public RegionManager RegionManager { get; } = new();
        public ConcurrentDictionary<FrontendClient, Player> PlayerDict { get; } = new();

        public Game(GameServerManager gameServerManager, ulong id)
        {
            _gameServerManager = gameServerManager;
            Id = id;

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
                    ProcessMessageQueue();
                }

                _tickWatch.Stop();

                if (_tickWatch.ElapsedMilliseconds > TickTime)
                    Logger.Warn($"Game update took longer ({_tickWatch.ElapsedMilliseconds} ms) than target tick time ({TickTime} ms)");
                else
                    Thread.Sleep((int)(TickTime - _tickWatch.ElapsedMilliseconds));
            }
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            lock (_gameLock)
            {
                _messageQueue.Enqueue(new(client, muxId, message));
            }
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }

        public void AddPlayer(FrontendClient client)
        {
            lock (_gameLock)
            {
                client.GameId = Id;

                EnqueueResponse(client, 1, new(NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId(0).Build()));
                EnqueueResponse(client, 1, new(_gameServerManager.AchievementDatabase.ToNetMessageAchievementDatabaseDump()));
                // NetMessageQueryIsRegionAvailable regionPrototype: 9833127629697912670 should go in the same packet as AchievementDatabaseDump

                // Send MOTD - this should be in the GroupingManagerService
                var chatBroadcastMessage = ChatBroadcastMessage.CreateBuilder()
                    .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                    .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                    .SetTheMessage(ChatMessage.CreateBuilder().SetBody(ConfigManager.GroupingManager.MotdText))
                    .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                    .Build();

                EnqueueResponse(client, 2, new(chatBroadcastMessage));

                EnqueueResponses(client, 1, GetBeginLoadingMessages(client.Session.Account.PlayerData));
                client.IsLoading = true;
            }
        }

        public void MovePlayerToRegion(FrontendClient client, RegionPrototype region)
        {
            lock (_gameLock)
            {
                client.Session.Account.PlayerData.Region = region;
                EnqueueResponses(client, 1, GetBeginLoadingMessages(client.Session.Account.PlayerData, false));
                client.IsLoading = true;
            }
        }

        #region Message Queue

        private void ProcessMessageQueue()
        {
            // Handle all queued messages
            while (_messageQueue.Count > 0)
                HandleQueuedMessage(_messageQueue.Dequeue());

            // Send responses to all clients
            foreach (var kvp in _responseQueueDict)
            {
                List<GameMessage> playerManagerResponseList = new();
                List<GameMessage> groupingManagerResponseList = new();

                while (_responseQueueDict[kvp.Key].Count > 0)
                {
                    QueuedGameMessage queuedGameMessage = _responseQueueDict[kvp.Key].Dequeue();
                    if (queuedGameMessage.MuxId == 1)
                        playerManagerResponseList.Add(queuedGameMessage.Message);
                    else if (queuedGameMessage.MuxId == 2)
                        groupingManagerResponseList.Add(queuedGameMessage.Message); // this should be handled by the GroupingManagerService
                }

                // Only send update if there are messages to send
                if (playerManagerResponseList.Count > 0) kvp.Key.SendMultipleMessages(1, playerManagerResponseList);
                if (groupingManagerResponseList.Count > 0) kvp.Key.SendMultipleMessages(2, groupingManagerResponseList);
            }

            // Clear response queue dict
            _responseQueueDict.Clear();
        }

        private void EnqueueResponse(FrontendClient client, ushort muxId, GameMessage message)
        {
            if (_responseQueueDict.TryGetValue(client, out _) == false) _responseQueueDict.Add(client, new());
            _responseQueueDict[client].Enqueue(new(client, muxId, message));
        }

        private void EnqueueResponses(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages)
        {
            if (_responseQueueDict.TryGetValue(client, out _) == false) _responseQueueDict.Add(client, new());
            foreach (GameMessage message in messages)
                _responseQueueDict[client].Enqueue(new(client, muxId, message));
        }

        private void HandleQueuedMessage(QueuedGameMessage queuedMessage)
        {
            FrontendClient client = queuedMessage.Client;
            ushort muxId = queuedMessage.MuxId;
            GameMessage message = queuedMessage.Message;

            string powerPrototypePath;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                    var updateAvatarStateMessage = NetMessageUpdateAvatarState.ParseFrom(message.Content);
                    UpdateAvatarStateArchive avatarState = new(updateAvatarStateMessage.ArchiveData.ToByteArray());
                    client.LastPosition = avatarState.Position;

                    /* Logger spam
                    //Logger.Trace(avatarState.ToString());
                    Logger.Trace(avatarState.Position.ToString());
                    */

                    break;

                case ClientToGameServerMessage.NetMessageCellLoaded:
                    Logger.Info($"Received NetMessageCellLoaded");
                    if (client.IsLoading)
                    {
                        EnqueueResponses(client, muxId, GetFinishLoadingMessages(client.Session.Account.PlayerData));
                        client.IsLoading = false;
                    }

                    break;

                case ClientToGameServerMessage.NetMessageTryActivatePower:
                    /* ActivatePower using TryActivatePower data
                    var tryActivatePower = NetMessageTryActivatePower.ParseFrom(message.Content);
                    ActivatePowerArchive activatePowerArchive = new(tryActivatePowerMessage, client.LastPosition);
                    client.SendMessage(muxId, new(NetMessageActivatePower.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(activatePowerArchive.Encode()))
                        .Build()));
                    */

                    var tryActivatePower = NetMessageTryActivatePower.ParseFrom(message.Content);

                    if (GameDatabase.TryGetPrototypePath(tryActivatePower.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received TryActivatePower for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received TryActivatePower for invalid prototype id {tryActivatePower.PowerPrototypeId}");

                    //Logger.Trace(tryActivatePower.ToString());

                    PowerResultArchive archive = new(tryActivatePower);
                    EnqueueResponse(client, muxId, new(NetMessagePowerResult.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(archive.Encode()))
                        .Build()));

                    break;

                case ClientToGameServerMessage.NetMessagePowerRelease:
                    var powerRelease = NetMessagePowerRelease.ParseFrom(message.Content);

                    if (GameDatabase.TryGetPrototypePath(powerRelease.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received PowerRelease for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received PowerRelease for invalid prototype id {powerRelease.PowerPrototypeId}");

                    break;

                case ClientToGameServerMessage.NetMessageTryCancelPower:
                    var tryCancelPower = NetMessageTryCancelPower.ParseFrom(message.Content);

                    if (GameDatabase.TryGetPrototypePath(tryCancelPower.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received TryCancelPower for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received TryCancelPower for invalid prototype id {tryCancelPower.PowerPrototypeId}");

                    break;

                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                    var tryCancelActivePower = NetMessageTryCancelActivePower.ParseFrom(message.Content);
                    Logger.Trace("Received TryCancelActivePower");
                    break;

                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                    var continuousPowerUpdate = NetMessageContinuousPowerUpdateToServer.ParseFrom(message.Content);

                    if (GameDatabase.TryGetPrototypePath(continuousPowerUpdate.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received ContinuousPowerUpdate for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received ContinuousPowerUpdate for invalid prototype id {continuousPowerUpdate.PowerPrototypeId}");

                    //Logger.Trace(continuousPowerUpdate.ToString());

                    break;

                case ClientToGameServerMessage.NetMessageTryInventoryMove:
                    Logger.Info($"Received NetMessageTryInventoryMove");
                    var tryInventoryMoveMessage = NetMessageTryInventoryMove.ParseFrom(message.Content);

                    EnqueueResponse(client, muxId, new(NetMessageInventoryMove.CreateBuilder()
                        .SetEntityId(tryInventoryMoveMessage.ItemId)
                        .SetInvLocContainerEntityId(tryInventoryMoveMessage.ToInventoryOwnerId)
                        .SetInvLocInventoryPrototypeId(tryInventoryMoveMessage.ToInventoryPrototype)
                        .SetInvLocSlot(tryInventoryMoveMessage.ToSlot)
                        .Build()));
                    break;

                case ClientToGameServerMessage.NetMessageSwitchAvatar:
                    Logger.Info($"Received NetMessageSwitchAvatar");
                    var switchAvatarMessage = NetMessageSwitchAvatar.ParseFrom(message.Content);
                    Logger.Trace(switchAvatarMessage.ToString());

                    // A hack for changing starting avatar without using chat commands
                    if (ConfigManager.Frontend.BypassAuth == false)
                    {
                        string avatarName = Enum.GetName(typeof(AvatarPrototype), switchAvatarMessage.AvatarPrototypeId);

                        if (Enum.TryParse(typeof(HardcodedAvatarEntity), avatarName, true, out object avatar))
                        {
                            client.Session.Account.PlayerData.Avatar = (HardcodedAvatarEntity)avatar;
                            EnqueueResponse(client, 2, new(ChatNormalMessage.CreateBuilder()
                                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_METAGAME)
                                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                                .SetTheMessage(ChatMessage.CreateBuilder().SetBody($"Changing avatar to {client.Session.Account.PlayerData.Avatar}. Relog for changes to take effect."))
                                .SetPrestigeLevel(6)
                                .Build()));
                        }
                    }

                    /* Old experimental code
                    // WIP - Hardcoded Black Cat -> Thor -> requires triggering an avatar swap back to Black Cat to move Thor again  
                    List<GameMessage> messageList = new();
                    messageList.Add(new(GameServerToClientMessage.NetMessageInventoryMove, NetMessageInventoryMove.CreateBuilder()
                        .SetEntityId((ulong)HardcodedAvatarEntity.Thor)
                        .SetDestOwnerDataId((ulong)HardcodedAvatarEntity.Thor)
                        .SetInvLocContainerEntityId(14646212)
                        .SetInvLocInventoryPrototypeId(9555311166682372646)
                        .SetInvLocSlot(0)
                        .Build().ToByteArray()));

                    // Put player avatar entity in the game world
                    byte[] avatarEntityEnterGameWorldArchiveData = {
                        0x01, 0xB2, 0xF8, 0xFD, 0x06, 0xA0, 0x21, 0xF0, 0xA3, 0x01, 0xBC, 0x40,
                        0x90, 0x2E, 0x91, 0x03, 0xBC, 0x05, 0x00, 0x00, 0x01
                    };

                    EntityEnterGameWorldArchiveData avatarEnterArchiveData = new(avatarEntityEnterGameWorldArchiveData);
                    avatarEnterArchiveData.EntityId = (ulong)HardcodedAvatarEntity.Thor;

                    messageList.Add(new(GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                        NetMessageEntityEnterGameWorld.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(avatarEnterArchiveData.Encode()))
                        .Build().ToByteArray()));

                    client.SendMultipleMessages(1, messageList.ToArray());*/

                    break;

                case ClientToGameServerMessage.NetMessageUseWaypoint:
                    Logger.Info($"Received NetMessageUseWaypoint message");
                    var useWaypointMessage = NetMessageUseWaypoint.ParseFrom(message.Content);

                    Logger.Trace(useWaypointMessage.ToString());

                    RegionPrototype destinationRegion = (RegionPrototype)useWaypointMessage.RegionProtoId;

                    if (RegionManager.IsRegionAvailable(destinationRegion))
                        MovePlayerToRegion(client, destinationRegion);
                    else
                        Logger.Warn($"Region {destinationRegion} is not available");

                    break;

                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:
                    Logger.Info($"Received NetMessageRequestInterestInAvatarEquipment");
                    var requestInterestInAvatarEquipment = NetMessageRequestInterestInAvatarEquipment.ParseFrom(message.Content);
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        #endregion
    }
}
