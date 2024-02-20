using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Networking;
using MHServerEmu.Games.Regions;

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
            PrototypeId powerId;
            ActivatePowerArchive activatePower;

            if (queuedEvent.IsExpired() == false)
                return messageList;

            AddConditionArchive conditionArchive;
            ulong avatarEntityId = (ulong)client.Session.Account.Player.Avatar.ToEntityId();

            switch (eventId)
            {
                case EventEnum.UseInteractableObject:

                    Entity interactObject = (Entity)queuedEvent.Data;
                    var proto = interactObject.BaseData.PrototypeId;
                    Logger.Trace($"UseInteractableObject {GameDatabase.GetPrototypeName(proto)}");

                    if (proto == (PrototypeId)16537916167475500124) // BowlingBallReturnDispenser
                    {                      
                        // bowlingBallItem = proto.LootTablePrototypeProp.Value->Table.Choices.Item.Item
                        var bowlingBallItem = (PrototypeId)7835010736274089329; // Entity/Items/Consumables/Prototypes/AchievementRewards/ItemRewards/BowlingBallItem
                        // itemPower = bowlingBallItem.Item.ActionsTriggeredOnItemEvent.ItemActionSet.Choices.ItemActionUsePower.Power
                        var itemPower = (PrototypeId)PowerPrototypes.Items.BowlingBallItemPower; // BowlingBallItemPower
                        // itemRarities = bowlingBallItem.Item.LootDropRestrictions.Rarity.AllowedRarities
                        var itemRarities = (PrototypeId)9254498193264414304; // R4Epic

                        Item bowlingBall = (Item)client.CurrentGame.EntityManager.GetEntityByPrototypeId(bowlingBallItem);
 
                       if (bowlingBall != null)
                        { // TODO: test if ball already in Inventary
                            messageList.Add(new(client, new(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(bowlingBall.BaseData.EntityId).Build())));
                            client.CurrentGame.EntityManager.DestroyEntity(bowlingBall.BaseData.EntityId);
                        }

                        AffixSpec[] affixSpec = { new AffixSpec((PrototypeId)4906559676663600947, 0, 1) }; // BindingInformation                        
                        int seed = _game.Random.Next();
                        float itemVariation = _game.Random.NextFloat(); 
                        bowlingBall = client.CurrentGame.EntityManager.CreateInvItem(
                            bowlingBallItem,
                            new(14646212, (PrototypeId)6731158030400100344, 0), // PlayerGeneral
                            itemRarities, 1, 
                            itemVariation, seed, 
                            affixSpec,
                            true );

                        // TODO: applyItemSpecProperties 
                        bowlingBall.Properties[PropertyEnum.InventoryStackSizeMax] = 1000;          // Item.StackSettings
                        bowlingBall.Properties[PropertyEnum.ItemIsTradable] = false;                // DefaultSettings.IsTradable
                        bowlingBall.Properties[PropertyEnum.ItemBindsToCharacterOnEquip] = true;    // DefaultSettings.BindsToAccountOnPickup
                        bowlingBall.Properties[PropertyEnum.ItemBindsToAccountOnPickup] = true;     // DefaultSettings.BindsToCharacterOnEquip 

                        messageList.Add(new(client, new(bowlingBall.ToNetMessageEntityCreate())));

                        //  if (assign) // TODO: check power assigned by player
                        messageList.Add(new(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                            .SetEntityId(avatarEntityId)
                            .SetPowerProtoId((ulong)itemPower)
                            .Build())));

                        messageList.Add(new(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                            .SetEntityId(avatarEntityId)
                            .SetPowerProtoId((ulong)itemPower)
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
                    var world = GameDatabase.GetPrototype<WorldEntityPrototype>(proto);
                    if (world == null) break;
                    var preIteractPower = world.PreInteractPower;
                    if (preIteractPower == PrototypeId.Invalid) break;
                    Logger.Trace($"OnPreInteractPower {GameDatabase.GetPrototypeName(preIteractPower)}");

                    messageList.Add(new(client, new(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatarEntityId)
                        .SetPowerProtoId((ulong)preIteractPower)
                        .SetPowerRank(0)
                        .SetCharacterLevel(60)
                        .SetCombatLevel(60)
                        .SetItemLevel(1)
                        .SetItemVariation(1)
                        .Build())));

                    activatePower = new()
                    {
                        ReplicationPolicy = AoiNetworkPolicyValues.AoiChannel0,
                        Flags = ActivatePowerMessageFlags.HasTriggeringPowerPrototypeId | ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasPowerRandomSeed | ActivatePowerMessageFlags.HasFXRandomSeed,
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
                    world = GameDatabase.GetPrototype<WorldEntityPrototype>(proto);
                    if (world == null) break;
                    preIteractPower = world.PreInteractPower;
                    if (preIteractPower == 0) break;
                    Logger.Trace($"OnPreInteractPowerEnd");

                    messageList.Add(new(client, new(NetMessageOnPreInteractPowerEnd.CreateBuilder()
                        .SetIdTargetEntity(interactObject.BaseData.EntityId)
                        .SetAvatarIndex(0)
                        .Build())));

                    messageList.Add(new(client, new(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                              .SetEntityId(avatarEntityId)
                              .SetPowerProtoId((ulong)preIteractPower)
                              .Build())));
                    break;

                case EventEnum.FinishCellLoading:
                    Logger.Warn($"Forсed loading");
                    client.AOI.LoadedCellCount = (int)queuedEvent.Data;
                    client.CurrentGame.FinishLoading(client);
                    break;

                case EventEnum.EmoteDance:

                    AvatarPrototypeId avatar = (AvatarPrototypeId)queuedEvent.Data;
                    avatarEntityId = (ulong)avatar.ToEntityId();
                    activatePower = new()
                    {
                        ReplicationPolicy = AoiNetworkPolicyValues.AoiChannel0,
                        Flags = ActivatePowerMessageFlags.HasTriggeringPowerPrototypeId | ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasPowerRandomSeed | ActivatePowerMessageFlags.HasFXRandomSeed,
                        IdUserEntity = avatarEntityId,
                        IdTargetEntity = avatarEntityId,
                        PowerPrototypeId = (PrototypeId)PowerPrototypes.Emotes.EmoteDance,
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

                    var conditionSerializationFlags = ConditionSerializationFlags.NoCreatorId | ConditionSerializationFlags.NoUltimateCreatorId | ConditionSerializationFlags.NoConditionPrototypeId
                        | ConditionSerializationFlags.HasIndex | ConditionSerializationFlags.HasAssetDataRef;

                    powerId = (PrototypeId)queuedEvent.Data;
                    switch (powerId)
                    {
                        case (PrototypeId)PowerPrototypes.Travel.GhostRiderRide:
                            Logger.Trace($"EventStart GhostRiderRide");
                            // Player.Avatar.EvalOnCreate.AssignProp.ProcProp.Param1 
                            conditionArchive = new(avatarEntityId, 666, conditionSerializationFlags, powerId, 0);   // TODO: generate and save Condition.Id                        

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

                        case (PrototypeId)PowerPrototypes.Travel.WolverineRide:
                        case (PrototypeId)PowerPrototypes.Travel.DeadpoolRide:
                        case (PrototypeId)PowerPrototypes.Travel.NickFuryRide:
                        case (PrototypeId)PowerPrototypes.Travel.CyclopsRide:
                        case (PrototypeId)PowerPrototypes.Travel.BlackWidowRide:
                        case (PrototypeId)PowerPrototypes.Travel.BladeRide:
                        case (PrototypeId)PowerPrototypes.Travel.AntmanFlight:
                        case (PrototypeId)PowerPrototypes.Travel.ThingFlight:
                            Logger.Trace($"EventStart Ride");
                            conditionArchive = new(avatarEntityId, 667, conditionSerializationFlags, powerId, 0);
                            messageList.Add(new(client, new(NetMessageAddCondition.CreateBuilder()
                                .SetArchiveData(conditionArchive.Serialize())
                                .Build())));
                            break;

                    }

                    break;

                case EventEnum.EndTravel:
                    powerId = (PrototypeId)queuedEvent.Data;
                    switch (powerId)
                    {
                        case (PrototypeId)PowerPrototypes.Travel.GhostRiderRide:
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

                        case (PrototypeId)PowerPrototypes.Travel.WolverineRide:
                        case (PrototypeId)PowerPrototypes.Travel.DeadpoolRide:
                        case (PrototypeId)PowerPrototypes.Travel.NickFuryRide:
                        case (PrototypeId)PowerPrototypes.Travel.CyclopsRide:
                        case (PrototypeId)PowerPrototypes.Travel.BlackWidowRide:
                        case (PrototypeId)PowerPrototypes.Travel.BladeRide:
                        case (PrototypeId)PowerPrototypes.Travel.AntmanFlight:
                        case (PrototypeId)PowerPrototypes.Travel.ThingFlight:
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

                    messageList.Add(new(client, new(
                        Property.ToNetMessageSetProperty(avatarRepId, new(PropertyEnum.ThrowableOriginatorEntity), idTarget)
                        )));
                    Logger.Warn($"{GameDatabase.GetPrototypeName(client.ThrowingObject.BaseData.PrototypeId)}");
                    // ThrowObject.Prototype.WorldEntity.UnrealClass

                    var throwPrototype = GameDatabase.GetPrototype<WorldEntityPrototype>(client.ThrowingObject.BaseData.PrototypeId);
                    if (throwPrototype == null) break;
                    client.IsThrowing = true;
                    //if (throwPrototype.Header.ReferenceType != (PrototypeId)HardcodedBlueprintId.ThrowableProp)
                    //    if (throwPrototype.Header.ReferenceType != (PrototypeId)HardcodedBlueprintId.ThrowableSmartProp)
                    //        throwPrototype = throwPrototype.Header.ReferenceType.GetPrototype();
                    messageList.Add(new(client, new(
                        Property.ToNetMessageSetProperty(avatarRepId, new(PropertyEnum.ThrowableOriginatorAssetRef), throwPrototype.UnrealClass)
                        )));

                    // ThrowObject.Prototype.ThrowableRestorePowerProp.Value
                    var propertyCollection = throwPrototype.Properties;
                    while (propertyCollection == null)
                    {
                        throwPrototype = GameDatabase.GetPrototype<WorldEntityPrototype>(throwPrototype.ParentDataRef);
                        propertyCollection = throwPrototype.Properties;
                    }
                    client.ThrowingCancelPower = throwPrototype.Properties.GetPropertyContainer((BlueprintId)HardcodedBlueprintId.ThrowableRestorePowerProp).Value;
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
                    client.ThrowingPower = throwPrototype.Properties.GetPropertyContainer((BlueprintId)HardcodedBlueprintId.ThrowablePowerProp).Value;
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
                    powerId = (PrototypeId)queuedEvent.Data;
                    avatarRepId = (ulong)client.Session.Account.Player.Avatar.ToPropertyCollectionReplicationId();
                    // TODO: avatarRepId = Player.EntityManager.GetEntity(AvatarEntityId).RepId

                    messageList.Add(new(client, new(
                        Property.ToNetMessageRemoveProperty(avatarRepId, new(PropertyEnum.ThrowableOriginatorEntity))
                        )));
                    messageList.Add(new(client, new(
                        Property.ToNetMessageRemoveProperty(avatarRepId, new(PropertyEnum.ThrowableOriginatorAssetRef))
                        )));

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
                    conditionSerializationFlags = ConditionSerializationFlags.NoCreatorId | ConditionSerializationFlags.NoUltimateCreatorId | ConditionSerializationFlags.NoConditionPrototypeId
                        | ConditionSerializationFlags.HasIndex | ConditionSerializationFlags.HasAssetDataRef | ConditionSerializationFlags.AssetDataRefIsNotFromOwner;

                    var diamondFormCondition = (PrototypeId)PowerPrototypes.EmmaFrost.DiamondFormCondition;
                    conditionArchive = new((ulong)client.Session.Account.Player.Avatar.ToEntityId(), 111, conditionSerializationFlags, diamondFormCondition, 0);

                    Logger.Trace($"Event Start EmmaDiamondForm");

                    var emmaCostume = (PrototypeId)client.Session.Account.CurrentAvatar.Costume;

                    // Invalid prototype id is the same as the default costume
                    if (emmaCostume == PrototypeId.Invalid)
                        emmaCostume = GameDatabase.GetPrototypeRefByName("Entity/Items/Costumes/Prototypes/EmmaFrost/Modern.prototype");

                    var asset = GameDatabase.GetPrototype<CostumePrototype>(emmaCostume).CostumeUnrealClass;
                    conditionArchive.Condition.AssetDataRef = asset;  // MarvelPlayer_EmmaFrost_Modern

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
                    conditionSerializationFlags = ConditionSerializationFlags.NoCreatorId | ConditionSerializationFlags.NoUltimateCreatorId | ConditionSerializationFlags.NoConditionPrototypeId
                        | ConditionSerializationFlags.HasIndex | ConditionSerializationFlags.HasAssetDataRef | ConditionSerializationFlags.HasDuration;

                    NetStructPoint3 position = (NetStructPoint3)queuedEvent.Data;

                    Logger.Trace($"EventStart Magik Ultimate");

                    conditionArchive = new(avatarEntityId, 777, conditionSerializationFlags, (PrototypeId)PowerPrototypes.Magik.Ultimate, 0);
                    conditionArchive.Condition.Duration = 20000;

                    messageList.Add(new(client, new(NetMessageAddCondition.CreateBuilder()
                        .SetArchiveData(conditionArchive.Serialize())
                        .Build())));

                    WorldEntity arenaEntity = _game.EntityManager.CreateWorldEntityEmpty(
                        client.AOI.Region.Id,
                        (PrototypeId)PowerPrototypes.Magik.UltimateArea,
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

                    messageList.Add(new(client, new(
                        Property.ToNetMessageSetProperty(arenaEntity.Properties.ReplicationId, new(PropertyEnum.AttachedToEntityId), avatarEntityId)
                        )));

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

                case EventEnum.GetRegion:
                    Logger.Trace($"Event GetRegion");
                    Region region = (Region)queuedEvent.Data;
                    var messages = region.GetLoadingMessages(client.GameId, client.Session.Account.Player.Waypoint, client);
                    foreach (var message in messages)
                        messageList.Add(new(client, message));

                    break;
            }

            queuedEvent.IsRunning = false;

            return messageList;
        }
    }
}
