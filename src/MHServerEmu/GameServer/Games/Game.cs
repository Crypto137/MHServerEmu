using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Powers;
using MHServerEmu.GameServer.Properties;
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
        private readonly List<GameEvent> _eventList = new();
        private readonly Dictionary<FrontendClient, List<GameMessage>> _responseListDict = new();
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
                    // Handle all queued messages
                    while (_messageQueue.Count > 0)
                        HandleQueuedMessage(_messageQueue.Dequeue());

                    // Handle Events
                    foreach (GameEvent @event in _eventList)
                        HandleEvent(@event);
                    if (_eventList.Count > 0)
                        _eventList.RemoveAll(@event => @event.IsRunning == false);

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
                EnqueueResponses(client, GetBeginLoadingMessages(client.Session.Account.PlayerData));
            }
        }

        public void MovePlayerToRegion(FrontendClient client, RegionPrototype region)
        {
            lock (_gameLock)
            {
                client.Session.Account.PlayerData.Region = region;
                EnqueueResponses(client, GetBeginLoadingMessages(client.Session.Account.PlayerData, false));
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

        private void HandleQueuedMessage(QueuedGameMessage queuedMessage)
        {
            FrontendClient client = queuedMessage.Client;
            GameMessage message = queuedMessage.Message;

            string powerPrototypePath;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                    var updateAvatarStateMessage = NetMessageUpdateAvatarState.ParseFrom(message.Payload);
                    UpdateAvatarStateArchive avatarState = new(updateAvatarStateMessage.ArchiveData.ToByteArray());
                    client.LastPosition = avatarState.Position;

                    /* Logger spam
                    //Logger.Trace(avatarState.ToString())
                    Logger.Trace(avatarState.Position.ToString());
                    ;*/

                    break;

                case ClientToGameServerMessage.NetMessageCellLoaded:
                    Logger.Info($"Received NetMessageCellLoaded");
                    if (client.IsLoading)
                    {
                        EnqueueResponses(client, GetFinishLoadingMessages(client.Session.Account.PlayerData));
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

                    var tryActivatePower = NetMessageTryActivatePower.ParseFrom(message.Payload);

                    if (GameDatabase.TryGetPrototypePath(tryActivatePower.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received TryActivatePower for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received TryActivatePower for invalid prototype id {tryActivatePower.PowerPrototypeId}");

                    if (powerPrototypePath.Contains("ThrowablePowers/"))
                    {
                        // TODO: GetPrototype(tryActivatePower.PowerPrototypeId).Power.AnimationTimeMS
                        long AnimationTimeMS = 1033; 
                        AddEvent(client, EventEnum.EndThrowing, AnimationTimeMS, tryActivatePower.PowerPrototypeId);
                        Logger.Trace($"AddEvent EndThrowing for {tryActivatePower.PowerPrototypeId}");
                        break;  
                    }

                    if (powerPrototypePath.Contains("TravelPower/")) 
                    {
                        AddEvent(client, EventEnum.StartTravel, 0, tryActivatePower.PowerPrototypeId);
                    }

                    //Logger.Trace(tryActivatePower.ToString());

                    PowerResultArchive archive = new(tryActivatePower);
                    if (archive.TargetId > 0)
                    EnqueueResponse(client, new(NetMessagePowerResult.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(archive.Encode()))
                        .Build()));

                    break;

                case ClientToGameServerMessage.NetMessagePowerRelease:
                    var powerRelease = NetMessagePowerRelease.ParseFrom(message.Payload);

                    if (GameDatabase.TryGetPrototypePath(powerRelease.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received PowerRelease for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received PowerRelease for invalid prototype id {powerRelease.PowerPrototypeId}");

                    break;

                case ClientToGameServerMessage.NetMessageTryCancelPower:
                    var tryCancelPower = NetMessageTryCancelPower.ParseFrom(message.Payload);

                    if (GameDatabase.TryGetPrototypePath(tryCancelPower.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received TryCancelPower for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received TryCancelPower for invalid prototype id {tryCancelPower.PowerPrototypeId}");

                    if (powerPrototypePath.Contains("TravelPower/"))
                    {
                        AddEvent(client, EventEnum.EndTravel, 0, tryCancelPower.PowerPrototypeId);
                    }

                    break;

                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                    var tryCancelActivePower = NetMessageTryCancelActivePower.ParseFrom(message.Payload);
                    Logger.Trace("Received TryCancelActivePower");
                    break;

                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                    var continuousPowerUpdate = NetMessageContinuousPowerUpdateToServer.ParseFrom(message.Payload);

                    if (GameDatabase.TryGetPrototypePath(continuousPowerUpdate.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received ContinuousPowerUpdate for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received ContinuousPowerUpdate for invalid prototype id {continuousPowerUpdate.PowerPrototypeId}");

                    //Logger.Trace(continuousPowerUpdate.ToString());

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

                    AddEvent(client, EventEnum.StartThrowing, 0, idTarget);

                    break;

                case ClientToGameServerMessage.NetMessageSwitchAvatar:
                    Logger.Info($"Received NetMessageSwitchAvatar");
                    var switchAvatarMessage = NetMessageSwitchAvatar.ParseFrom(message.Payload);
                    Logger.Trace(switchAvatarMessage.ToString());

                    // A hack for changing starting avatar without using chat commands
                    if (ConfigManager.Frontend.BypassAuth == false)
                    {
                        string avatarName = Enum.GetName(typeof(AvatarPrototype), switchAvatarMessage.AvatarPrototypeId);

                        if (Enum.TryParse(typeof(HardcodedAvatarEntity), avatarName, true, out object avatar))
                        {
                            client.Session.Account.PlayerData.Avatar = (HardcodedAvatarEntity)avatar;
                            GroupingManagerService.SendMetagameChatMessage(client, $"Changing avatar to {client.Session.Account.PlayerData.Avatar}. Relog for changes to take effect.");
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

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        #endregion

        #region Events

        public void AddEvent(FrontendClient client, EventEnum eventId, long timeMs, ulong data)
        {
            _eventList.Add(new(client, eventId, timeMs, data));
        }

        private void HandleEvent(GameEvent queuedEvent)
        {
            FrontendClient client = queuedEvent.Client;
            EventEnum eventId = queuedEvent.Event;
            ulong data = queuedEvent.Data;

            if (!queuedEvent.IsExpired())
                return;

            switch (eventId)
            {
                case EventEnum.StartTravel:

                    ulong avatarEntityId = (ulong)client.Session.Account.PlayerData.Avatar;

                    switch (data)
                    {              
                        case 534644109020894342: // GhostRiderRide
                            Logger.Trace($"EventStart GhostRiderRide");

                            // Player.Avatar.EvalOnCreate.AssignProp.ProcProp.Param1 
                            AddConditionArchive conditionArchive = new(avatarEntityId, 666, 55, data, 0);   // TODO: generate and save Condition.Id                        

                            EnqueueResponse(client, new(NetMessageAddCondition.CreateBuilder()
                                .SetArchiveData(ByteString.CopyFrom(conditionArchive.Encode()))
                                .Build()));
                            
                            EnqueueResponse(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                                .SetEntityId(avatarEntityId)
                                .SetPowerProtoId(1023796862002271777) // Powers/Player/GhostRider/RideBikeHotspotsEnd.prototype
                                .SetPowerRank(0)
                                .SetCharacterLevel(60)
                                .SetCombatLevel(60)
                                .SetItemLevel(1)
                                .SetItemVariation(1)
                                .Build()));

                            break;

                        case 12091550505432716326: // WolverineRide
                        case 13293849182765716371: // DeadpoolRide
                        case 873351779127923638: // NickFuryRide
                        case 5296410749208826696: // CyclopsRide
                        case 767029628138689650: // BlackWidowRide
                        case 9306725620939166275: // BladeRide
                            Logger.Trace($"EventStart Ride");
                            conditionArchive = new(avatarEntityId, 667, 55, data, 0);
                            EnqueueResponse(client, new(NetMessageAddCondition.CreateBuilder()
                                .SetArchiveData(ByteString.CopyFrom(conditionArchive.Encode()))
                                .Build()));
                            break;

                    }

                    break;

                case EventEnum.EndTravel:

                    avatarEntityId = (ulong)client.Session.Account.PlayerData.Avatar;

                    switch (data)
                    {
                        case 534644109020894342: // GhostRiderRide
                            Logger.Trace($"EventEnd GhostRiderRide");

                            EnqueueResponse(client, new(NetMessageDeleteCondition.CreateBuilder()
                                .SetIdEntity(avatarEntityId)
                                .SetKey(666)
                                .Build()));

                            EnqueueResponse(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                                .SetEntityId(avatarEntityId)
                                .SetPowerProtoId(1023796862002271777) // RideBikeHotspotsEnd
                                .Build()));

                            break;

                        case 13293849182765716371: // DeadpoolRide
                        case 12091550505432716326: // WolverineRide
                        case 873351779127923638: // NickFuryRide
                        case 5296410749208826696: // CyclopsRide
                        case 767029628138689650: // BlackWidowRide
                        case 9306725620939166275: // BladeRide
                            Logger.Trace($"EventEnd Ride");
                            EnqueueResponse(client, new(NetMessageDeleteCondition.CreateBuilder()
                                .SetIdEntity(avatarEntityId)
                                .SetKey(667)
                                .Build()));

                            break;
                    }

                    break;
                case EventEnum.StartThrowing:

                    ulong idTarget = data;
                    // TODO: Player.Avatar.SetThrowObject(idTarget)
                    // TODO: ThrowObject = Player.EntityManager.GetEntity(idTarget)

                    avatarEntityId = (ulong)client.Session.Account.PlayerData.Avatar;
                    // TODO: avatarRepId = Player.EntityManager.GetEntity(avatarEntityId).RepId
                    ulong avatarRepId = (ulong)Enum.Parse(typeof(HardcodedAvatarReplicationId), Enum.GetName(typeof(HardcodedAvatarEntity), client.Session.Account.PlayerData.Avatar));

                    Property property = new(PropertyEnum.ThrowableOriginatorEntity, idTarget);
                    EnqueueResponse(client, new(property.ToNetMessageSetProperty(avatarRepId)));

                    // ThrowObject.Prototype.WorldEntity.UnrealClass
                    property = new(PropertyEnum.ThrowableOriginatorAssetRef, 9953069070637601478); // MarvelDestructible_Throwable_PoliceCar
                    EnqueueResponse(client, new(property.ToNetMessageSetProperty(avatarRepId)));

                    // ThrowObject.Prototype.ThrowableRestorePowerProp.Value
                    EnqueueResponse(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(5387918100020273380) // Powers/Environment/ThrowablePowers/Vehicles/ThrownPoliceCarCancelPower.prototype
                        .SetPowerRank(0)
                        .SetCharacterLevel(60) // TODO: Player.Avatar.GetProperty(PropertyEnum.CharacterLevel)
                        .SetCombatLevel(60) // TODO: Player.Avatar.GetProperty(PropertyEnum.CombatLevel)
                        .SetItemLevel(1)
                        .SetItemVariation(1)
                        .Build()));

                    // ThrowObject.Prototype.ThrowablePowerProp.Value
                    EnqueueResponse(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(13813867126636027518) // Powers/Environment/ThrowablePowers/Vehicles/ThrownPoliceCarPower.prototype
                        .SetPowerRank(0)
                        .SetCharacterLevel(60)
                        .SetCombatLevel(60)
                        .SetItemLevel(1)
                        .SetItemVariation(1)
                        .Build()));

                    EnqueueResponse(client, new(NetMessageEntityDestroy.CreateBuilder()
                        .SetIdEntity(idTarget)
                        .Build()));

                    Logger.Trace($"Event StartThrowing");

                    break;

                case EventEnum.EndThrowing:

                    avatarEntityId = (ulong)client.Session.Account.PlayerData.Avatar;

                    avatarRepId = (ulong)Enum.Parse(typeof(HardcodedAvatarReplicationId), Enum.GetName(typeof(HardcodedAvatarEntity), client.Session.Account.PlayerData.Avatar));
                    // TODO: avatarRepId = Player.EntityManager.GetEntity(AvatarEntityId).RepId

                    property = new(PropertyEnum.ThrowableOriginatorEntity, 0ul);
                    EnqueueResponse(client, new(property.ToNetMessageRemoveProperty(avatarRepId)));
                    property = new(PropertyEnum.ThrowableOriginatorAssetRef, 0ul);
                    EnqueueResponse(client, new(property.ToNetMessageRemoveProperty(avatarRepId)));

                    // TODO: ThrowObject = Player.Avatar.GetThrowObject

                    // ThrowObject.Prototype.ThrowablePowerProp.Value
                    EnqueueResponse(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(13813867126636027518) // ThrownPoliceCarPower
                        .Build()));

                    // ThrowObject.Prototype.ThrowableRestorePowerProp.Value
                    EnqueueResponse(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(5387918100020273380) // ThrownPoliceCarCancelPower
                        .Build()));

                    Logger.Trace("Event EndThrowing");

                    if (GameDatabase.GetPrototypePath(data).Contains("CancelPower")) // ThrownPoliceCarCancelPower
                    {
                        // TODO: CreateEntity for ThrowObject
                        Logger.Trace("Event ThrownPoliceCarCancelPower");
                    }

                    break;

            }

            queuedEvent.IsRunning = false;
        }

        #endregion
    }
}
