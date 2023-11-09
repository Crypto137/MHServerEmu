using Gazillion;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Networking;

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

        public bool HasEvent(FrontendClient client, EventEnum eventId)
        {
            return _eventList.Exists(@event => @event.Client == client && @event.Event == eventId);
        }
        
        public void KillEvent(FrontendClient client, EventEnum eventId)
        {
            if (_eventList.Count > 0)
                _eventList.RemoveAll(@event => (@event.Client == client) && (@event.Event == eventId));
        }

        private List<QueuedGameMessage> HandleEvent(GameEvent queuedEvent)
        {
            List<QueuedGameMessage> messageList = new();
            FrontendClient client = queuedEvent.Client;
            EventEnum eventId = queuedEvent.Event;
            ulong powerId;
            ActivatePowerArchive activatePower;

            if (queuedEvent.IsExpired() == false)
                return messageList;

            AddConditionArchive conditionArchive;
            ulong avatarEntityId = (ulong)client.Session.Account.Player.Avatar.ToEntityId();

            switch (eventId)
            {
                case EventEnum.UseInteractableObject:

                    Entity interactObject = (Entity)queuedEvent.Data;
                    ulong proto = interactObject.BaseData.PrototypeId;
                    Logger.Trace($"UseInteractableObject {GameDatabase.GetPrototypeName(proto)}");

                    if (proto == 16537916167475500124) // BowlingBallReturnDispenser
                    {                      
                        // bowlingBallItem = proto.LootTablePrototypeProp.Value->Table.Choices.Item.Item
                        ulong bowlingBallItem = 7835010736274089329; // Entity/Items/Consumables/Prototypes/AchievementRewards/ItemRewards/BowlingBallItem
                        // itemPower = bowlingBallItem.Item.ActionsTriggeredOnItemEvent.ItemActionSet.Choices.ItemActionUsePower.Power
                        ulong itemPower = (ulong)PowerPrototypes.Items.BowlingBallItemPower; // BowlingBallItemPower
                        // itemRarities = bowlingBallItem.Item.LootDropRestrictions.Rarity.AllowedRarities
                        ulong itemRarities = 9254498193264414304; // R4Epic

                        Item bowlingBall = (Item)client.CurrentGame.EntityManager.GetEntityByPrototypeId(bowlingBallItem);
 
                       if (bowlingBall != null)
                        { // TODO: test if ball already in Inventary
                            messageList.Add(new(client, new(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(bowlingBall.BaseData.EntityId).Build())));
                            client.CurrentGame.EntityManager.DestroyEntity(bowlingBall.BaseData.EntityId);
                        }

                        AffixSpec[] affixSpec = { new AffixSpec(4906559676663600947, 0, 1) }; // BindingInformation                        
                        int seed = _game.Random.Next();
                        float itemVariation = _game.Random.NextFloat(); 
                        bowlingBall = client.CurrentGame.EntityManager.CreateInvItem(
                            bowlingBallItem,
                            new(14646212, 6731158030400100344, 0), // PlayerGeneral
                            itemRarities, 1, 
                            itemVariation, seed, 
                            affixSpec,
                            true );

                        // TODO: applyItemSpecProperties 
                        bowlingBall.PropertyCollection.List.AddRange(
                            new Property[] {
                            new(PropertyEnum.InventoryStackSizeMax, 1000), // Item.StackSettings
                            new(PropertyEnum.ItemIsTradable, false), // DefaultSettings.IsTradable
                            new(PropertyEnum.ItemBindsToAccountOnPickup, true), // DefaultSettings.BindsToAccountOnPickup
                            new(PropertyEnum.ItemBindsToCharacterOnEquip, true) // // DefaultSettings.BindsToCharacterOnEquip
                            });

                        messageList.Add(new(client, new(bowlingBall.ToNetMessageEntityCreate())));

                        //  if (assign) // TODO: check power assigned by player
                        messageList.Add(new(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                            .SetEntityId(avatarEntityId)
                            .SetPowerProtoId(itemPower)
                            .Build())));

                        messageList.Add(new(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId(avatarEntityId)
                            .SetPowerProtoId(itemPower)
                            .SetPowerRank(0)
                            .SetCharacterLevel(60)
                            .SetCombatLevel(60)
                            .SetItemLevel(1)
                            .SetItemVariation(itemVariation)
                            .Build())));
                    }

                    break;

                case EventEnum.OnPreInteractPower:
                    interactObject = (Entity)queuedEvent.Data;
                    proto = interactObject.BaseData.PrototypeId;
                    PrototypeEntry world = proto.GetPrototype().GetEntry(BlueprintId.WorldEntity);
                    if (world == null) break;
                    ulong preIteractPower = world.GetFieldDef(FieldId.PreInteractPower);
                    if (preIteractPower == 0) break;
                    Logger.Trace($"OnPreInteractPower {GameDatabase.GetPrototypeName(preIteractPower)}");

                    messageList.Add(new(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(preIteractPower)
                        .SetPowerRank(0)
                        .SetCharacterLevel(60)
                        .SetCombatLevel(60)
                        .SetItemLevel(1)
                        .SetItemVariation(1)
                        .Build())));

                    activatePower = new()
                    {
                        ReplicationPolicy = 1,
                        Flags = 202u.ToBoolArray(8),
                        IdUserEntity = avatarEntityId,
                        IdTargetEntity = 0,
                        PowerPrototypeId = preIteractPower,
                        UserPosition = client.LastPosition,
                        PowerRandomSeed = 2222,
                        FXRandomSeed = 2222
                    };

                    messageList.Add(new(client, new(NetMessageActivatePower.CreateBuilder()
                         .SetArchiveData(activatePower.Serialize())
                         .Build())));

                    break;

                case EventEnum.OnPreInteractPowerEnd:

                    interactObject = (Entity)queuedEvent.Data;
                    proto = interactObject.BaseData.PrototypeId;
                    world = proto.GetPrototype().GetEntry(BlueprintId.WorldEntity);
                    if (world == null) break;
                    preIteractPower = world.GetFieldDef(FieldId.PreInteractPower);
                    if (preIteractPower == 0) break;
                    Logger.Trace($"OnPreInteractPowerEnd");

                    messageList.Add(new(client, new(NetMessageOnPreInteractPowerEnd.CreateBuilder()
                        .SetIdTargetEntity(interactObject.BaseData.EntityId)
                        .SetAvatarIndex(0)
                        .Build())));

                    messageList.Add(new(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                              .SetEntityId(avatarEntityId)
                              .SetPowerProtoId(preIteractPower)
                              .Build())));
                    break;

                case EventEnum.FinishCellLoading:
                    Logger.Warn($"Forсed loading");
                    client.LoadedCellCount = (int)queuedEvent.Data;
                    client.CurrentGame.FinishLoading(client);
                    break;

                case EventEnum.EmoteDance:

                    AvatarPrototypeId avatar = (AvatarPrototypeId)queuedEvent.Data;
                    avatarEntityId = (ulong)avatar.ToEntityId();
                    activatePower = new()
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
                         .SetArchiveData(activatePower.Serialize())
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
                    if (throwPrototype.Header.ReferenceType != (ulong)BlueprintId.ThrowableProp)
                        if (throwPrototype.Header.ReferenceType != (ulong)BlueprintId.ThrowableSmartProp)
                            throwPrototype = throwPrototype.Header.ReferenceType.GetPrototype();
                    property = new(PropertyEnum.ThrowableOriginatorAssetRef, unrealClass);
                    messageList.Add(new(client, new(property.ToNetMessageSetProperty(avatarRepId))));

                    // ThrowObject.Prototype.ThrowableRestorePowerProp.Value
                    client.ThrowingCancelPower = (ulong)throwPrototype.GetEntry(BlueprintId.ThrowableRestorePowerProp).Elements[0].Value;
                    messageList.Add(new(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingCancelPower)
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
                        .SetPowerProtoId(client.ThrowingPower)
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
                        .SetPowerProtoId(client.ThrowingPower)
                        .Build())));

                    // ThrowObject.Prototype.ThrowableRestorePowerProp.Value
                    messageList.Add(new(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId(client.ThrowingCancelPower)
                        .Build())));

                    Logger.Trace("Event EndThrowing");

                    if (GameDatabase.GetPrototypeName(powerId).Contains("CancelPower")) 
                    {
                        if (client.ThrowingObject != null)
                            messageList.Add(new(client, new(client.ThrowingObject.ToNetMessageEntityCreate())));
                        Logger.Trace("Event ThrownCancelPower");
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
