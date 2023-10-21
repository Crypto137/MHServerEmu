using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Grouping;
using MHServerEmu.Networking;

namespace MHServerEmu.Games.Events
{
    public partial class Game : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public const int TickRate = 20;                 // Ticks per second based on client behavior
        public const long TickTime = 1000 / TickRate;   // ms per tick

        private readonly object _gameLock = new();
        private readonly Queue<QueuedGameMessage> _messageQueue = new();
        private readonly Dictionary<FrontendClient, List<GameMessage>> _responseListDict = new();
        private readonly Stopwatch _tickWatch = new();

        private readonly ServerManager _gameServerManager;

        private readonly PowerMessageHandler _powerMessageHandler;

        private int _tickCount;

        public ulong Id { get; }
        public EventManager EventManager { get; }
        public EntityManager EntityManager { get; }
        public RegionManager RegionManager { get; }
        public ConcurrentDictionary<FrontendClient, Player> PlayerDict { get; } = new();

        public Game(ServerManager gameServerManager, ulong id)
        {
            EventManager = new(this);
            EntityManager = new();
            RegionManager = new(EntityManager);

            _powerMessageHandler = new(EventManager);

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
                    // Handle all queued messages
                    while (_messageQueue.Count > 0)
                        HandleQueuedMessage(_messageQueue.Dequeue());

                    // Update event manager
                    EnqueueResponses(EventManager.Update());

                    // Send responses to all clients
                    foreach (var kvp in _responseListDict)
                        kvp.Key.SendMessages(1, kvp.Value);     // no GroupingManager messages should be here, so we can assume that muxId for all messages is 1

                    // Clear response list dict
                    _responseListDict.Clear();
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
                _messageQueue.Enqueue(new(client, message));
            }
        }

        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }

        public void AddPlayer(FrontendClient client)
        {
            lock (_gameLock)
            {
                client.GameId = Id;
                EnqueueResponses(client, GetBeginLoadingMessages(client.Session.Account));
            }
        }

        public void MovePlayerToRegion(FrontendClient client, RegionPrototype region)
        {
            lock (_gameLock)
            {
                EnqueueResponses(client, GetExitGameMessages());
                client.Session.Account.Player.Region = region;
                EnqueueResponses(client, GetBeginLoadingMessages(client.Session.Account));
                client.IsLoading = true;
            }
        }

        #region Message Queue

        private void EnqueueResponse(FrontendClient client, GameMessage message)
        {
            if (_responseListDict.TryGetValue(client, out _) == false) _responseListDict.Add(client, new());
            _responseListDict[client].Add(message);
        }

        private void EnqueueResponses(FrontendClient client, IEnumerable<GameMessage> messages)
        {
            if (_responseListDict.TryGetValue(client, out _) == false) _responseListDict.Add(client, new());
            _responseListDict[client].AddRange(messages);                
        }

        private void EnqueueResponses(IEnumerable<QueuedGameMessage> queuedMessages)
        {
            foreach (QueuedGameMessage message in queuedMessages)
                EnqueueResponse(message.Client, message.Message);
        }

        private void HandleQueuedMessage(QueuedGameMessage queuedMessage)
        {
            FrontendClient client = queuedMessage.Client;
            GameMessage message = queuedMessage.Message;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                    var updateAvatarStateMessage = NetMessageUpdateAvatarState.ParseFrom(message.Payload);
                    UpdateAvatarStateArchive avatarState = new(updateAvatarStateMessage.ArchiveData);
                    client.LastPosition = avatarState.Position;

                    /* Logger spam
                    Logger.Trace(avatarState.ToString())
                    Logger.Trace(avatarState.Position.ToString());
                    */

                    break;

                case ClientToGameServerMessage.NetMessageCellLoaded:
                    Logger.Info($"Received NetMessageCellLoaded");
                    if (client.IsLoading)
                    {
                        EnqueueResponses(client, GetFinishLoadingMessages(client.Session.Account));
                        client.IsLoading = false;
                    }

                    break;

                case ClientToGameServerMessage.NetMessageUseInteractableObject:
                    var useObject = NetMessageUseInteractableObject.ParseFrom(message.Payload);
                    Logger.Info($"Received NetMessageUseInteractableObject");

                    if (useObject.MissionPrototypeRef != 0)
                    {
                        Logger.Debug($"NetMessageUseInteractableObject contains missionPrototypeRef:\n{GameDatabase.GetPrototypeName(useObject.MissionPrototypeRef)}");
                        EnqueueResponse(client, new(NetMessageMissionInteractRelease.DefaultInstance));
                    }

                    if (EntityManager.TryGetEntityById(useObject.IdTarget, out Entity interactableObject) && interactableObject is Transition)
                    {                  
                        Transition teleport = interactableObject as Transition;
                        if (teleport.Destinations.Length == 0) break;
                        Logger.Trace($"Destination entity {teleport.Destinations[0].Entity}");
                        Entity target = EntityManager.FindEntityByDestination(teleport.Destinations[0]);
                        if (target == null) break;
                        Vector3 targetRot = target.BaseData.Orientation;
                        float offset = 150f;
                        Vector3 targetPos = new(
                            target.BaseData.Position.X + offset * (float)Math.Cos(targetRot.X),
                            target.BaseData.Position.Y + offset * (float)Math.Sin(targetRot.X), 
                            target.BaseData.Position.Z);   

                        Logger.Trace($"Teleporting to {targetPos}");                        

                        Property property = target.PropertyCollection.GetPropertyByEnum(PropertyEnum.MapCellId);
                        uint cellid = (uint)(long)property.Value.Get(); 
                        property = target.PropertyCollection.GetPropertyByEnum(PropertyEnum.MapAreaId);
                        uint areaid = (uint)(long)property.Value.Get();
                        Logger.Trace($"Teleporting to areaid {areaid} cellid {cellid}");

                        EnqueueResponse(client, new(NetMessageEntityPosition.CreateBuilder()
                            .SetIdEntity((ulong)client.Session.Account.Player.Avatar.ToEntityId())
                            .SetFlags(64)
                            .SetPosition(targetPos.ToNetStructPoint3())
                            .SetOrientation(targetRot.ToNetStructPoint3())
                            .SetCellId(cellid)
                            .SetAreaId(areaid)
                            .SetEntityPrototypeId((ulong)client.Session.Account.Player.Avatar)
                            .Build()));

                        client.LastPosition = targetPos;
                    }

                    break;

                case ClientToGameServerMessage.NetMessageTryActivatePower:
                case ClientToGameServerMessage.NetMessagePowerRelease:
                case ClientToGameServerMessage.NetMessageTryCancelPower:
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                    EnqueueResponses(_powerMessageHandler.HandleMessage(client, message));

                    break;

                case ClientToGameServerMessage.NetMessageTryInventoryMove:
                    Logger.Info($"Received NetMessageTryInventoryMove");
                    var tryInventoryMoveMessage = NetMessageTryInventoryMove.ParseFrom(message.Payload);

                    EnqueueResponse(client, new(NetMessageInventoryMove.CreateBuilder()
                        .SetEntityId(tryInventoryMoveMessage.ItemId)
                        .SetInvLocContainerEntityId(tryInventoryMoveMessage.ToInventoryOwnerId)
                        .SetInvLocInventoryPrototypeId(tryInventoryMoveMessage.ToInventoryPrototype)
                        .SetInvLocSlot(tryInventoryMoveMessage.ToSlot)
                        .Build()));
                    break;

                case ClientToGameServerMessage.NetMessageThrowInteraction:
                    var throwInteraction = NetMessageThrowInteraction.ParseFrom(message.Payload);
                    ulong idTarget = throwInteraction.IdTarget;
                    int avatarIndex = throwInteraction.AvatarIndex;
                    Logger.Trace($"Received NetMessageThrowInteraction Avatar[{avatarIndex}] Target[{idTarget}]");

                    EventManager.AddEvent(client, EventEnum.StartThrowing, 0, idTarget);

                    break;

                case ClientToGameServerMessage.NetMessageSwitchAvatar:
                    Logger.Info($"Received NetMessageSwitchAvatar");
                    var switchAvatarMessage = NetMessageSwitchAvatar.ParseFrom(message.Payload);
                    Logger.Trace(switchAvatarMessage.ToString());

                    // A hack for changing avatar in-game
                    //client.Session.Account.CurrentAvatar.Costume = 0;  // reset costume on avatar switch
                    client.Session.Account.Player.Avatar = (AvatarPrototype)switchAvatarMessage.AvatarPrototypeId;
                    GroupingManagerService.SendMetagameChatMessage(client, $"Changing avatar to {client.Session.Account.Player.Avatar}.");
                    MovePlayerToRegion(client, client.Session.Account.Player.Region);

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
                    var useWaypointMessage = NetMessageUseWaypoint.ParseFrom(message.Payload);

                    Logger.Trace(useWaypointMessage.ToString());

                    RegionPrototype destinationRegion = (RegionPrototype)useWaypointMessage.RegionProtoId;

                    if (RegionManager.IsRegionAvailable(destinationRegion))
                        MovePlayerToRegion(client, destinationRegion);
                    else
                        Logger.Warn($"Region {destinationRegion} is not available");

                    break;

                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:
                    Logger.Info($"Received NetMessageRequestInterestInAvatarEquipment");
                    var requestInterestInAvatarEquipment = NetMessageRequestInterestInAvatarEquipment.ParseFrom(message.Payload);
                    break;

                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:
                    var omegaCommit = NetMessageOmegaBonusAllocationCommit.ParseFrom(message.Payload);
                    Logger.Debug(omegaCommit.ToString());
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        #endregion
    }
}
