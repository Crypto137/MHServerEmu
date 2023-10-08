using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Powers;
using MHServerEmu.GameServer.Properties;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;

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
        public EntityManager EntityManager { get; }
        public RegionManager RegionManager { get; }
        public ConcurrentDictionary<FrontendClient, Player> PlayerDict { get; } = new();

        public Game(GameServerManager gameServerManager, ulong id)
        {
            EntityManager = new();
            RegionManager = new(EntityManager);

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
                        if (GameDatabase.TryGetPrototypePath(useObject.MissionPrototypeRef, out string missionPrototype))
                            Logger.Debug($"NetMessageUseInteractableObject contains missionPrototypeRef:\n{missionPrototype}");
                        else
                            Logger.Debug($"NetMessageUseInteractableObject contains unknown missionPrototypeRef");

                        EnqueueResponse(client, new(NetMessageMissionInteractRelease.DefaultInstance));
                    }

                    if (EntityManager.TryGetEntityById(useObject.IdTarget, out Entity interactableObject) && interactableObject is Transition)
                    {                  
                        Transition teleport = interactableObject as Transition;
                        if (teleport.Destinations.Length == 0) break;
                        Logger.Trace($"Destination entity {teleport.Destinations[0].Entity}");
                        Entity target = EntityManager.FindEntityByDestination(teleport.Destinations[0]);
                        if (target == null) break;
                        Common.Vector3 targetRot = target.BaseData.Orientation;
                        float offset = 150f;
                        Common.Vector3 targetPos =  new(
                            target.BaseData.Position.X + offset * (float)Math.Cos(targetRot.X),
                            target.BaseData.Position.Y + offset * (float)Math.Sin(targetRot.X), 
                            target.BaseData.Position.Z);   

                        Logger.Trace($"Teleport to {targetPos}");                        

                        Property property = target.PropertyCollection.GetPropertyByEnum(PropertyEnum.MapCellId);
                        uint cellid = (uint)(long)property.Value.Get(); 
                        property = target.PropertyCollection.GetPropertyByEnum(PropertyEnum.MapAreaId);
                        uint areaid = (uint)(long)property.Value.Get();
                        Logger.Trace($"Teleport to areaid {areaid} cellid {cellid}");

                        EnqueueResponse(client, new(NetMessageEntityPosition.CreateBuilder()
                            .SetIdEntity((ulong)client.Session.Account.Player.Avatar.ToEntityId())
                            .SetFlags(64)
                            .SetPosition(targetPos.ToNetStructPoint3())
                            .SetOrientation(targetRot.ToNetStructPoint3())
                            .SetCellId(cellid)
                            .SetAreaId(areaid)
                            .SetEntityPrototypeId((ulong)client.Session.Account.Player.Avatar)
                            .Build()));
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
                        Logger.Trace($"AddEvent EndThrowing for {tryActivatePower.PowerPrototypeId}");
                        PrototypeEntry Power = tryActivatePower.PowerPrototypeId.GetPrototype().GetEntry(BlueprintId.Power);
                        long animationTimeMS = 1100;
                        if (Power != null) { 
                            PrototypeEntryElement AnimationTimeMS = Power.GetField(FieldId.AnimationTimeMS);
                            if (AnimationTimeMS != null) animationTimeMS = (long)AnimationTimeMS.Value; 
                        }
                        AddEvent(client, EventEnum.EndThrowing, animationTimeMS, tryActivatePower.PowerPrototypeId);                        
                        break;  
                    } 
                    else if (powerPrototypePath.Contains("EmmaFrost/"))
                    {
                        if (PowerHasKeyword(tryActivatePower.PowerPrototypeId, BlueprintId.DiamondFormActivatePower))
                            AddEvent(client, EventEnum.DiamondFormActivate, 0, tryActivatePower.PowerPrototypeId);
                        else if (PowerHasKeyword(tryActivatePower.PowerPrototypeId, BlueprintId.Mental))
                            AddEvent(client, EventEnum.DiamondFormDeactivate, 0, tryActivatePower.PowerPrototypeId);
                    } 
                    else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Magik.Ultimate)
                    {
                        AddEvent(client, EventEnum.StartMagikUltimate, 0, tryActivatePower.TargetPosition);
                        AddEvent(client, EventEnum.EndMagikUltimate, 20000, 0u);
                    }

                    // if (powerPrototypePath.Contains("TravelPower/")) 
                    //    TrawerPower(client, tryActivatePower.PowerPrototypeId);

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

                    if (powerPrototypePath.Contains("TravelPower/"))
                        HandleTravelPower(client, continuousPowerUpdate.PowerPrototypeId);
                    // Logger.Trace(continuousPowerUpdate.ToString());

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

        #region Events

        public void AddEvent(FrontendClient client, EventEnum eventId, long timeMs, object data)
        {
            _eventList.Add(new(client, eventId, timeMs, data));
        }

        private void HandleEvent(GameEvent queuedEvent)
        {
            FrontendClient client = queuedEvent.Client;
            EventEnum eventId = queuedEvent.Event;
            ulong powerId;

            if (!queuedEvent.IsExpired())
                return;

            AddConditionArchive conditionArchive;
            ulong avatarEntityId = (ulong)client.Session.Account.Player.Avatar.ToEntityId();

            switch (eventId)
            {
                case EventEnum.StartTravel:

                    powerId = (ulong)queuedEvent.Data;
                    switch (powerId)
                    {
                        case (ulong)PowerPrototypes.GhostRider.GhostRiderRide:
                            Logger.Trace($"EventStart GhostRiderRide");
                            // Player.Avatar.EvalOnCreate.AssignProp.ProcProp.Param1 
                            conditionArchive = new(avatarEntityId, 666, 55, powerId, 0);   // TODO: generate and save Condition.Id                        

                            EnqueueResponse(client, new(NetMessageAddCondition.CreateBuilder()
                                .SetArchiveData(ByteString.CopyFrom(conditionArchive.Encode()))
                                .Build()));
                            
                            EnqueueResponse(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                                .SetEntityId(avatarEntityId)
                                .SetPowerProtoId((ulong)PowerPrototypes.GhostRider.RideBikeHotspotsEnd)
                                .SetPowerRank(0)
                                .SetCharacterLevel(60)
                                .SetCombatLevel(60)
                                .SetItemLevel(1)
                                .SetItemVariation(1)
                                .Build()));

                            break;

                        case (ulong)PowerPrototypes.Wolverine.WolverineRide:
                        case (ulong)PowerPrototypes.Deadpool.DeadpoolRide:
                        case (ulong)PowerPrototypes.NickFury.NickFuryRide:
                        case (ulong)PowerPrototypes.Cyclops.CyclopsRide:
                        case (ulong)PowerPrototypes.BlackWidow.BlackWidowRide:
                        case (ulong)PowerPrototypes.Blade.BladeRide:
                        case (ulong)PowerPrototypes.AntMan.AntmanFlight:
                        case (ulong)PowerPrototypes.Thing.ThingFlight:
                            Logger.Trace($"EventStart Ride");
                            conditionArchive = new(avatarEntityId, 667, 55, powerId, 0);
                            EnqueueResponse(client, new(NetMessageAddCondition.CreateBuilder()
                                .SetArchiveData(ByteString.CopyFrom(conditionArchive.Encode()))
                                .Build()));
                            break;

                    }

                    break;

                case EventEnum.EndTravel:
                    powerId = (ulong)queuedEvent.Data;
                    switch (powerId)
                    {
                        case (ulong)PowerPrototypes.GhostRider.GhostRiderRide:
                            Logger.Trace($"EventEnd GhostRiderRide");

                            EnqueueResponse(client, new(NetMessageDeleteCondition.CreateBuilder()
                                .SetIdEntity(avatarEntityId)
                                .SetKey(666)
                                .Build()));

                            EnqueueResponse(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                                .SetEntityId(avatarEntityId)
                                .SetPowerProtoId((ulong)PowerPrototypes.GhostRider.RideBikeHotspotsEnd) 
                                .Build()));

                            break;

                        case (ulong)PowerPrototypes.Wolverine.WolverineRide:
                        case (ulong)PowerPrototypes.Deadpool.DeadpoolRide:
                        case (ulong)PowerPrototypes.NickFury.NickFuryRide:
                        case (ulong)PowerPrototypes.Cyclops.CyclopsRide:
                        case (ulong)PowerPrototypes.BlackWidow.BlackWidowRide:
                        case (ulong)PowerPrototypes.Blade.BladeRide:
                        case (ulong)PowerPrototypes.AntMan.AntmanFlight:
                        case (ulong)PowerPrototypes.Thing.ThingFlight:
                            Logger.Trace($"EventEnd Ride");
                            EnqueueResponse(client, new(NetMessageDeleteCondition.CreateBuilder()
                                .SetIdEntity(avatarEntityId)
                                .SetKey(667)
                                .Build()));

                            break;
                    }

                    break;

                case EventEnum.StartThrowing:

                    ulong idTarget = (ulong)queuedEvent.Data;

                    client.ThrowingObject = EntityManager.GetEntityById(idTarget);
                    if (client.ThrowingObject == null) break;

                    // TODO: avatarRepId = Player.EntityManager.GetEntity(avatarEntityId).RepId
                    ulong avatarRepId = (ulong)client.Session.Account.Player.Avatar.ToPropertyCollectionReplicationId();

                    Property property = new(PropertyEnum.ThrowableOriginatorEntity, idTarget);
                    EnqueueResponse(client, new(property.ToNetMessageSetProperty(avatarRepId)));
                    Logger.Warn($"{GameDatabase.GetPrototypePath(client.ThrowingObject.BaseData.PrototypeId)}");
                    // ThrowObject.Prototype.WorldEntity.UnrealClass
                    Prototype throwPrototype = client.ThrowingObject.BaseData.PrototypeId.GetPrototype();
                    PrototypeEntry worldEntity = throwPrototype.GetEntry(BlueprintId.WorldEntity);
                    if (worldEntity == null) break;
                    ulong unrealClass = (ulong)worldEntity.GetField(FieldId.UnrealClass).Value;
                    client.IsThrowing = true;
                    if (throwPrototype.ParentId != 14997899060839977779) // ThrowableProp
                        throwPrototype = throwPrototype.ParentId.GetPrototype();
                    property = new(PropertyEnum.ThrowableOriginatorAssetRef, unrealClass); // MarvelDestructible_Throwable_PoliceCar
                    EnqueueResponse(client, new(property.ToNetMessageSetProperty(avatarRepId)));

                    // ThrowObject.Prototype.ThrowableRestorePowerProp.Value
                    client.ThrowingCancelPower = (ulong)throwPrototype.GetEntry(BlueprintId.ThrowableRestorePowerProp).Elements[0].Value;
                    EnqueueResponse(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingCancelPower) // Powers/Environment/ThrowablePowers/Vehicles/ThrownPoliceCarCancelPower.prototype
                        .SetPowerRank(0)
                        .SetCharacterLevel(60) // TODO: Player.Avatar.GetProperty(PropertyEnum.CharacterLevel)
                        .SetCombatLevel(60) // TODO: Player.Avatar.GetProperty(PropertyEnum.CombatLevel)
                        .SetItemLevel(1)
                        .SetItemVariation(1)
                        .Build()));

                    // ThrowObject.Prototype.ThrowablePowerProp.Value
                    client.ThrowingPower = (ulong)throwPrototype.GetEntry(BlueprintId.ThrowablePowerProp).Elements[0].Value;
                    EnqueueResponse(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingPower) // Powers/Environment/ThrowablePowers/Vehicles/ThrownPoliceCarPower.prototype
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
                    powerId = (ulong)queuedEvent.Data;
                    avatarRepId = (ulong)client.Session.Account.Player.Avatar.ToPropertyCollectionReplicationId();
                    // TODO: avatarRepId = Player.EntityManager.GetEntity(AvatarEntityId).RepId

                    property = new(PropertyEnum.ThrowableOriginatorEntity, 0ul);
                    EnqueueResponse(client, new(property.ToNetMessageRemoveProperty(avatarRepId)));
                    property = new(PropertyEnum.ThrowableOriginatorAssetRef, 0ul);
                    EnqueueResponse(client, new(property.ToNetMessageRemoveProperty(avatarRepId)));
                    
                    // ThrowObject.Prototype.ThrowablePowerProp.Value
                    EnqueueResponse(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingPower) // ThrownPoliceCarPower
                        .Build()));

                    // ThrowObject.Prototype.ThrowableRestorePowerProp.Value
                    EnqueueResponse(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingCancelPower) // ThrownPoliceCarCancelPower
                        .Build()));

                    Logger.Trace("Event EndThrowing");

                    if (GameDatabase.GetPrototypePath(powerId).Contains("CancelPower")) // ThrownPoliceCarCancelPower
                    {     
                        EnqueueResponse(client, new(client.ThrowingObject.ToNetMessageEntityCreate()));
                        Logger.Trace("Event ThrownPoliceCarCancelPower");
                    }
                    client.ThrowingObject = null;
                    client.IsThrowing = false;
                    break;

                case EventEnum.DiamondFormActivate:

                    ulong diamondFormCondition = (ulong)PowerPrototypes.EmmaFrost.DiamondFormCondition;
                    conditionArchive = new((ulong)client.Session.Account.Player.Avatar.ToEntityId(), 111, 567, diamondFormCondition, 0); 

                    Logger.Trace($"Event Start EmmaDiamondForm");

                    ulong emmaCostume = client.Session.Account.CurrentAvatar.Costume;

                    // 0 is the same as the default costume, but it's not a valid prototype id
                    if (emmaCostume == 0) emmaCostume = GameDatabase.GetPrototypeId("Entity/Items/Costumes/Prototypes/EmmaFrost/Modern.prototype");
                    
                    ulong asset = (ulong)emmaCostume.GetPrototype().GetEntry(BlueprintId.Costume).GetField(FieldId.CostumeUnrealClass).Value;
                    conditionArchive.Condition.EngineAssetGuid = asset;  // MarvelPlayer_EmmaFrost_Modern

                    EnqueueResponse(client, new(NetMessageAddCondition.CreateBuilder()
                         .SetArchiveData(ByteString.CopyFrom(conditionArchive.Encode()))
                         .Build()));

                    break;

                case EventEnum.DiamondFormDeactivate:
                    // TODO: get DiamondFormCondition Condition Key
                    EnqueueResponse(client, new(NetMessageDeleteCondition.CreateBuilder()
                      .SetKey(111)
                      .SetIdEntity((ulong)client.Session.Account.Player.Avatar.ToEntityId())
                      .Build()));

                    Logger.Trace($"EventEnd EmmaDiamondForm");  

                    break;

                case EventEnum.StartMagikUltimate:
                    NetStructPoint3 position = (NetStructPoint3)queuedEvent.Data;

                    Logger.Trace($"EventStart Magik Ultimate");

                    conditionArchive = new(avatarEntityId, 777, 183, (ulong)PowerPrototypes.Magik.Ultimate, 0);
                    conditionArchive.Condition.Duration = 20000;

                    EnqueueResponse(client, new(NetMessageAddCondition.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(conditionArchive.Encode()))
                        .Build()));

                    WorldEntity arenaEntity = EntityManager.CreateWorldEntityEmpty(
                        RegionManager.GetRegion(client.Session.Account.Player.Region).Id,
                        (ulong)PowerPrototypes.Magik.UltimateArea,
                        new(position.X, position.Y, position.Z), new());

                    // we need to store this state in the avatar entity instead
                    client.MagikUltimateEntityId = arenaEntity.BaseData.EntityId;

                    EnqueueResponse(client, new(arenaEntity.ToNetMessageEntityCreate()));

                    EnqueueResponse(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(arenaEntity.BaseData.EntityId)
                        .SetPowerProtoId((ulong)PowerPrototypes.Magik.UltimateHotspotEffect) 
                        .SetPowerRank(0)
                        .SetCharacterLevel(60)
                        .SetCombatLevel(60)
                        .SetItemLevel(1)
                        .SetItemVariation(1)
                        .Build()));

                    property = new(PropertyEnum.AttachedToEntityId, avatarEntityId);
                    EnqueueResponse(client, new(property.ToNetMessageSetProperty(arenaEntity.PropertyCollection.ReplicationId)));

                    break;

                case EventEnum.EndMagikUltimate:
                    Logger.Trace($"EventEnd Magik Ultimate");

                    EnqueueResponse(client, new(NetMessageDeleteCondition.CreateBuilder()
                        .SetIdEntity(avatarEntityId)
                        .SetKey(777)
                        .Build()));

                    ulong arenaEntityId = client.MagikUltimateEntityId;

                    EnqueueResponse(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(arenaEntityId)
                        .SetPowerProtoId((ulong)PowerPrototypes.Magik.UltimateHotspotEffect) 
                        .Build()));

                    EntityManager.DestroyEntity(arenaEntityId);

                    EnqueueResponse(client, new(NetMessageEntityDestroy.CreateBuilder()
                        .SetIdEntity(arenaEntityId)
                        .Build()));

                    break;
            }

            queuedEvent.IsRunning = false;
        }

        private bool PowerHasKeyword(ulong PowerId, BlueprintId Keyword)
        {
            PrototypeEntry Power = PowerId.GetPrototype().GetEntry(BlueprintId.Power);
            if (Power == null) return false;
            PrototypeEntryListElement Keywords = Power.GetListField(FieldId.Keywords);
            if (Keywords == null) return false;

            for (int i = 0; i < Keywords.Values.Length; i++)
                if ((ulong)Keywords.Values[i] == (ulong)Keyword) return true;
            
            return false;
        }

        private void HandleTravelPower(FrontendClient client, ulong PowerId)
        {
            uint delta = 65; // TODO: Sync server-client
            switch (PowerId)
            {   // Power.AnimationContactTimePercent
                case (ulong)PowerPrototypes.GhostRider.GhostRiderRide:
                case (ulong)PowerPrototypes.Wolverine.WolverineRide:
                case (ulong)PowerPrototypes.Deadpool.DeadpoolRide:
                case (ulong)PowerPrototypes.NickFury.NickFuryRide:
                case (ulong)PowerPrototypes.Cyclops.CyclopsRide:
                case (ulong)PowerPrototypes.BlackWidow.BlackWidowRide:
                case (ulong)PowerPrototypes.Blade.BladeRide:
                    AddEvent(client, EventEnum.StartTravel, 100 - delta, PowerId);
                    break;
                case (ulong)PowerPrototypes.AntMan.AntmanFlight:
                    AddEvent(client, EventEnum.StartTravel, 210 - delta, PowerId);
                    break;
                case (ulong)PowerPrototypes.Thing.ThingFlight:
                    AddEvent(client, EventEnum.StartTravel, 235 - delta, PowerId);
                    break;
            }
        }

        #endregion
    }
}
