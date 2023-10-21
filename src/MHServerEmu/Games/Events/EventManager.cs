using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Networking;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.Events
{
    public class EventManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly List<GameEvent> _eventList = new();
        private readonly Game _game;

        public EventManager(Game game)
        {
            _game = game;
        }

        public List<QueuedGameMessage> Update()
        {
            List<QueuedGameMessage> messageList = new();

            // Handle Events
            foreach (GameEvent @event in _eventList)
                messageList.AddRange(HandleEvent(@event));

            if (_eventList.Count > 0)
                _eventList.RemoveAll(@event => @event.IsRunning == false);

            return messageList;
        }

        public void AddEvent(FrontendClient client, EventEnum eventId, long timeMs, object data)
        {
            _eventList.Add(new(client, eventId, timeMs, data));
        }

        private List<QueuedGameMessage> HandleEvent(GameEvent queuedEvent)
        {
            List<QueuedGameMessage> messageList = new();
            FrontendClient client = queuedEvent.Client;
            EventEnum eventId = queuedEvent.Event;
            ulong powerId;

            if (queuedEvent.IsExpired() == false)
                return messageList;

            AddConditionArchive conditionArchive;
            ulong avatarEntityId = (ulong)client.Session.Account.Player.Avatar.ToEntityId();

            switch (eventId)
            {
                case EventEnum.EmoteDance:

                    AvatarPrototype avatar = (AvatarPrototype)queuedEvent.Data;
                    avatarEntityId = (ulong)avatar.ToEntityId();
                    ActivatePowerArchive archive = new()
                    {
                        ReplicationPolicy = 1,
                        Flags = 202u.ToBoolArray(8),
                        IdUserEntity = avatarEntityId,
                        IdTargetEntity = avatarEntityId,
                        PowerPrototypeId = (ulong)PowerPrototypes.Emotes.EmoteDance,
                        UserPosition = client.LastPosition,
                        PowerRandomSeed = 1111,
                        FXRandomSeed = 1111

                    };
                    messageList.Add(new(client,new(NetMessageActivatePower.CreateBuilder()
                         .SetArchiveData(archive.Serialize())
                         .Build())));
                    break;

                case EventEnum.ToTeleport:

                    Vector3 targetPos = (Vector3)queuedEvent.Data;
                    Vector3 targetRot = new();

                    uint cellid = 1;
                    uint areaid = 1;

                    messageList.Add(new(client, new(NetMessageEntityPosition.CreateBuilder()
                        .SetIdEntity((ulong)client.Session.Account.Player.Avatar.ToEntityId())
                        .SetFlags(64)
                        .SetPosition(targetPos.ToNetStructPoint3())
                        .SetOrientation(targetRot.ToNetStructPoint3())
                        .SetCellId(cellid)
                        .SetAreaId(areaid)
                        .SetEntityPrototypeId((ulong)client.Session.Account.Player.Avatar)
                        .Build())));

                    client.LastPosition = targetPos;
                    Logger.Trace($"Teleporting to {targetPos}");

                    break;

                case EventEnum.StartTravel:

                    powerId = (ulong)queuedEvent.Data;
                    switch (powerId)
                    {
                        case (ulong)PowerPrototypes.GhostRider.GhostRiderRide:
                            Logger.Trace($"EventStart GhostRiderRide");
                            // Player.Avatar.EvalOnCreate.AssignProp.ProcProp.Param1 
                            conditionArchive = new(avatarEntityId, 666, 55, powerId, 0);   // TODO: generate and save Condition.Id                        

                            messageList.Add(new(client, new(NetMessageAddCondition.CreateBuilder()
                                .SetArchiveData(conditionArchive.Serialize())
                                .Build())));

                            messageList.Add(new(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                                .SetEntityId(avatarEntityId)
                                .SetPowerProtoId((ulong)PowerPrototypes.GhostRider.RideBikeHotspotsEnd)
                                .SetPowerRank(0)
                                .SetCharacterLevel(60)
                                .SetCombatLevel(60)
                                .SetItemLevel(1)
                                .SetItemVariation(1)
                                .Build())));

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
                            messageList.Add(new(client, new(NetMessageAddCondition.CreateBuilder()
                                .SetArchiveData(conditionArchive.Serialize())
                                .Build())));
                            break;

                    }

                    break;

                case EventEnum.EndTravel:
                    powerId = (ulong)queuedEvent.Data;
                    switch (powerId)
                    {
                        case (ulong)PowerPrototypes.GhostRider.GhostRiderRide:
                            Logger.Trace($"EventEnd GhostRiderRide");

                            messageList.Add(new(client, new(NetMessageDeleteCondition.CreateBuilder()
                                .SetIdEntity(avatarEntityId)
                                .SetKey(666)
                                .Build())));

                            messageList.Add(new(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                                .SetEntityId(avatarEntityId)
                                .SetPowerProtoId((ulong)PowerPrototypes.GhostRider.RideBikeHotspotsEnd)
                                .Build())));

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
                            messageList.Add(new(client, new(NetMessageDeleteCondition.CreateBuilder()
                                .SetIdEntity(avatarEntityId)
                                .SetKey(667)
                                .Build())));

                            break;
                    }

                    break;

                case EventEnum.StartThrowing:

                    ulong idTarget = (ulong)queuedEvent.Data;

                    client.ThrowingObject = _game.EntityManager.GetEntityById(idTarget);
                    if (client.ThrowingObject == null) break;

                    // TODO: avatarRepId = Player.EntityManager.GetEntity(avatarEntityId).RepId
                    ulong avatarRepId = (ulong)client.Session.Account.Player.Avatar.ToPropertyCollectionReplicationId();

                    Property property = new(PropertyEnum.ThrowableOriginatorEntity, idTarget);
                    messageList.Add(new(client, new(property.ToNetMessageSetProperty(avatarRepId))));
                    Logger.Warn($"{GameDatabase.GetPrototypeName(client.ThrowingObject.BaseData.PrototypeId)}");
                    // ThrowObject.Prototype.WorldEntity.UnrealClass
                    Prototype throwPrototype = client.ThrowingObject.BaseData.PrototypeId.GetPrototype();
                    PrototypeEntry worldEntity = throwPrototype.GetEntry(BlueprintId.WorldEntity);
                    if (worldEntity == null) break;
                    ulong unrealClass = (ulong)worldEntity.GetField(FieldId.UnrealClass).Value;
                    client.IsThrowing = true;
                    if (throwPrototype.ParentId != 14997899060839977779) // ThrowableProp
                        throwPrototype = throwPrototype.ParentId.GetPrototype();
                    property = new(PropertyEnum.ThrowableOriginatorAssetRef, unrealClass); // MarvelDestructible_Throwable_PoliceCar
                    messageList.Add(new(client, new(property.ToNetMessageSetProperty(avatarRepId))));

                    // ThrowObject.Prototype.ThrowableRestorePowerProp.Value
                    client.ThrowingCancelPower = (ulong)throwPrototype.GetEntry(BlueprintId.ThrowableRestorePowerProp).Elements[0].Value;
                    messageList.Add(new(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingCancelPower) // Powers/Environment/ThrowablePowers/Vehicles/ThrownPoliceCarCancelPower.prototype
                        .SetPowerRank(0)
                        .SetCharacterLevel(60) // TODO: Player.Avatar.GetProperty(PropertyEnum.CharacterLevel)
                        .SetCombatLevel(60) // TODO: Player.Avatar.GetProperty(PropertyEnum.CombatLevel)
                        .SetItemLevel(1)
                        .SetItemVariation(1)
                        .Build())));

                    // ThrowObject.Prototype.ThrowablePowerProp.Value
                    client.ThrowingPower = (ulong)throwPrototype.GetEntry(BlueprintId.ThrowablePowerProp).Elements[0].Value;
                    messageList.Add(new(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingPower) // Powers/Environment/ThrowablePowers/Vehicles/ThrownPoliceCarPower.prototype
                        .SetPowerRank(0)
                        .SetCharacterLevel(60)
                        .SetCombatLevel(60)
                        .SetItemLevel(1)
                        .SetItemVariation(1)
                        .Build())));

                    messageList.Add(new(client, new(NetMessageEntityDestroy.CreateBuilder()
                        .SetIdEntity(idTarget)
                        .Build())));

                    Logger.Trace($"Event StartThrowing");

                    break;

                case EventEnum.EndThrowing:
                    powerId = (ulong)queuedEvent.Data;
                    avatarRepId = (ulong)client.Session.Account.Player.Avatar.ToPropertyCollectionReplicationId();
                    // TODO: avatarRepId = Player.EntityManager.GetEntity(AvatarEntityId).RepId

                    property = new(PropertyEnum.ThrowableOriginatorEntity, 0ul);
                    messageList.Add(new(client, new(property.ToNetMessageRemoveProperty(avatarRepId))));
                    property = new(PropertyEnum.ThrowableOriginatorAssetRef, 0ul);
                    messageList.Add(new(client, new(property.ToNetMessageRemoveProperty(avatarRepId))));

                    // ThrowObject.Prototype.ThrowablePowerProp.Value
                    messageList.Add(new(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingPower) // ThrownPoliceCarPower
                        .Build())));

                    // ThrowObject.Prototype.ThrowableRestorePowerProp.Value
                    messageList.Add(new(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingCancelPower) // ThrownPoliceCarCancelPower
                        .Build())));

                    Logger.Trace("Event EndThrowing");

                    if (GameDatabase.GetPrototypeName(powerId).Contains("CancelPower")) // ThrownPoliceCarCancelPower
                    {
                        if (client.ThrowingObject != null)
                            messageList.Add(new(client, new(client.ThrowingObject.ToNetMessageEntityCreate())));
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
                    if (emmaCostume == 0) emmaCostume = GameDatabase.GetPrototypeRefByName("Entity/Items/Costumes/Prototypes/EmmaFrost/Modern.prototype");

                    ulong asset = (ulong)emmaCostume.GetPrototype().GetEntry(BlueprintId.Costume).GetField(FieldId.CostumeUnrealClass).Value;
                    conditionArchive.Condition.EngineAssetGuid = asset;  // MarvelPlayer_EmmaFrost_Modern

                    messageList.Add(new(client, new(NetMessageAddCondition.CreateBuilder()
                         .SetArchiveData(conditionArchive.Serialize())
                         .Build())));

                    break;

                case EventEnum.DiamondFormDeactivate:
                    // TODO: get DiamondFormCondition Condition Key
                    messageList.Add(new(client, new(NetMessageDeleteCondition.CreateBuilder()
                      .SetKey(111)
                      .SetIdEntity((ulong)client.Session.Account.Player.Avatar.ToEntityId())
                      .Build())));

                    Logger.Trace($"EventEnd EmmaDiamondForm");

                    break;

                case EventEnum.StartMagikUltimate:
                    NetStructPoint3 position = (NetStructPoint3)queuedEvent.Data;

                    Logger.Trace($"EventStart Magik Ultimate");

                    conditionArchive = new(avatarEntityId, 777, 183, (ulong)PowerPrototypes.Magik.Ultimate, 0);
                    conditionArchive.Condition.Duration = 20000;

                    messageList.Add(new(client, new(NetMessageAddCondition.CreateBuilder()
                        .SetArchiveData(conditionArchive.Serialize())
                        .Build())));

                    WorldEntity arenaEntity = _game.EntityManager.CreateWorldEntityEmpty(
                        _game.RegionManager.GetRegion(client.Session.Account.Player.Region).Id,
                        (ulong)PowerPrototypes.Magik.UltimateArea,
                        new(position.X, position.Y, position.Z), new());

                    // we need to store this state in the avatar entity instead
                    client.MagikUltimateEntityId = arenaEntity.BaseData.EntityId;

                    messageList.Add(new(client, new(arenaEntity.ToNetMessageEntityCreate())));

                    messageList.Add(new(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(arenaEntity.BaseData.EntityId)
                        .SetPowerProtoId((ulong)PowerPrototypes.Magik.UltimateHotspotEffect)
                        .SetPowerRank(0)
                        .SetCharacterLevel(60)
                        .SetCombatLevel(60)
                        .SetItemLevel(1)
                        .SetItemVariation(1)
                        .Build())));

                    property = new(PropertyEnum.AttachedToEntityId, avatarEntityId);
                    messageList.Add(new(client, new(property.ToNetMessageSetProperty(arenaEntity.PropertyCollection.ReplicationId))));

                    break;

                case EventEnum.EndMagikUltimate:
                    Logger.Trace($"EventEnd Magik Ultimate");

                    messageList.Add(new(client, new(NetMessageDeleteCondition.CreateBuilder()
                        .SetIdEntity(avatarEntityId)
                        .SetKey(777)
                        .Build())));

                    ulong arenaEntityId = client.MagikUltimateEntityId;

                    messageList.Add(new(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(arenaEntityId)
                        .SetPowerProtoId((ulong)PowerPrototypes.Magik.UltimateHotspotEffect)
                        .Build())));

                    _game.EntityManager.DestroyEntity(arenaEntityId);

                    messageList.Add(new(client, new(NetMessageEntityDestroy.CreateBuilder()
                        .SetIdEntity(arenaEntityId)
                        .Build())));

                    break;
            }

            queuedEvent.IsRunning = false;

            return messageList;
        }
    }
}
