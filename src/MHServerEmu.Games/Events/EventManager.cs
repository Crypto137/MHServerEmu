using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Events
{
    public class Event
    {        
        // TODO write worked Event and replace Action
        private readonly List<Action> _actionList = new();

        public void AddActionBack(Action handler)
        {
            _actionList.Add(handler);
        }

        public void Invoke()
        {
            foreach(var action in _actionList)
                action();
        }

        public void UnregisterCallbacks()
        {
            _actionList.Clear();
        }

    }

    public class EventManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly List<GameEvent> _eventList = new();
        private readonly Game _game;
        private readonly object _eventLock = new();

        public EventManager(Game game)
        {
            _game = game;
        }

        public void Update()
        {
            lock (_eventLock)
            {
                // Handle Events
                foreach (GameEvent @event in _eventList)
                    HandleEvent(@event);

                if (_eventList.Count > 0)
                    _eventList.RemoveAll(@event => @event.IsRunning == false);
            }
        }

        public void AddEvent(PlayerConnection playerConnection, EventEnum eventId, long timeMs, object data)
        {
            lock (_eventLock)
            {
                _eventList.Add(new(playerConnection, eventId, timeMs, data));
            }
        }

        public bool HasEvent(PlayerConnection playerConnection, EventEnum eventId)
        {
            lock (_eventLock)
            {
                return _eventList.Exists(@event => @event.PlayerConnection == playerConnection && @event.Event == eventId);
            }
        }
        
        public void KillEvent(PlayerConnection playerConnection, EventEnum eventId)
        {
            lock (_eventLock)
            {
                if (_eventList.Count > 0)
                    _eventList.RemoveAll(@event => (@event.PlayerConnection == playerConnection) && (@event.Event == eventId));
            }
        }

        private void HandleEvent(GameEvent queuedEvent)
        {
            PlayerConnection playerConnection = queuedEvent.PlayerConnection;
            EventEnum eventId = queuedEvent.Event;

            if (queuedEvent.IsExpired() == false)
                return;

            switch (eventId)
            {
                case EventEnum.UseInteractableObject:   OnUseInteractableObject(playerConnection, (Entity)queuedEvent.Data); break;
                case EventEnum.PreInteractPower:        OnPreInteractPower(playerConnection, (Entity)queuedEvent.Data); break;
                case EventEnum.PreInteractPowerEnd:     OnPreInteractPowerEnd(playerConnection, (Entity)queuedEvent.Data); break;
                case EventEnum.FinishCellLoading:       OnFinishCellLoading(playerConnection, (int)queuedEvent.Data); break;
                case EventEnum.EmoteDance:              OnEmoteDance(playerConnection, (AvatarPrototypeId)queuedEvent.Data); break;
                case EventEnum.ToTeleport:              OnToTeleport(playerConnection, (Vector3)queuedEvent.Data); break;
                case EventEnum.StartTravel:             OnStartTravel(playerConnection, (PrototypeId)queuedEvent.Data); break;
                case EventEnum.EndTravel:               OnEndTravel(playerConnection, (PrototypeId)queuedEvent.Data); break;
                case EventEnum.StartThrowing:           OnStartThrowing(playerConnection, (ulong)queuedEvent.Data); break;
                case EventEnum.EndThrowing:             OnEndThrowing(playerConnection, (PrototypeId)queuedEvent.Data); break;
                case EventEnum.DiamondFormActivate:     OnDiamondFormActivate(playerConnection); break;
                case EventEnum.DiamondFormDeactivate:   OnDiamondFormDeactivate(playerConnection); break;
                case EventEnum.StartMagikUltimate:      OnStartMagikUltimate(playerConnection, (NetStructPoint3)queuedEvent.Data); break;
                case EventEnum.EndMagikUltimate:        OnEndMagikUltimate(playerConnection); break;
                case EventEnum.GetRegion:               OnGetRegion(playerConnection, (Region)queuedEvent.Data); break;
                case EventEnum.ErrorInRegion:               OnErrorInRegion(playerConnection, (PrototypeId)queuedEvent.Data); break;
            }

            queuedEvent.IsRunning = false;
        }

        private void OnUseInteractableObject(PlayerConnection playerConnection, Entity interactObject)
        {
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

                Item bowlingBall = (Item)playerConnection.Game.EntityManager.GetEntityByPrototypeId(bowlingBallItem);
 
                if (bowlingBall != null)
                { // TODO: test if ball already in Inventary
                    playerConnection.SendMessage(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(bowlingBall.Id).Build());
                    playerConnection.Game.EntityManager.DestroyEntity(bowlingBall);
                }

                AffixSpec[] affixSpec = { new AffixSpec((PrototypeId)4906559676663600947, 0, 1) }; // BindingInformation                        
                int seed = _game.Random.Next();
                float itemVariation = _game.Random.NextFloat();
                bowlingBall = playerConnection.Game.EntityManager.CreateInvItem(
                    bowlingBallItem,
                    new(playerConnection.Player.Id, (PrototypeId)6731158030400100344, 0), // PlayerGeneral
                    itemRarities, 1,
                    itemVariation, seed,
                    affixSpec,
                    true);

                // TODO: applyItemSpecProperties 
                bowlingBall.Properties[PropertyEnum.InventoryStackSizeMax] = 1000;          // Item.StackSettings
                bowlingBall.Properties[PropertyEnum.ItemIsTradable] = false;                // DefaultSettings.IsTradable
                bowlingBall.Properties[PropertyEnum.ItemBindsToCharacterOnEquip] = true;    // DefaultSettings.BindsToAccountOnPickup
                bowlingBall.Properties[PropertyEnum.ItemBindsToAccountOnPickup] = true;     // DefaultSettings.BindsToCharacterOnEquip 

                playerConnection.SendMessage(bowlingBall.ToNetMessageEntityCreate());

                //  if (assign) // TODO: check power assigned by player
                ulong avatarEntityId = playerConnection.Player.CurrentAvatar.Id;
                playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                    .SetEntityId(avatarEntityId)
                    .SetPowerProtoId((ulong)itemPower)
                    .Build());

                playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                    .SetEntityId(avatarEntityId)
                    .SetPowerProtoId((ulong)itemPower)
                    .SetPowerRank(0)
                    .SetCharacterLevel(60)
                    .SetCombatLevel(60)
                    .SetItemLevel(1)
                    .SetItemVariation(itemVariation)
                    .Build());
            }
        }

        private void OnPreInteractPower(PlayerConnection playerConnection, Entity interactObject)
        {
            ulong avatarEntityId = playerConnection.Player.CurrentAvatar.Id;
            PrototypeId proto = interactObject.BaseData.PrototypeId;
            var world = GameDatabase.GetPrototype<WorldEntityPrototype>(proto);
            if (world == null) return;
            var preIteractPower = world.PreInteractPower;
            if (preIteractPower == PrototypeId.Invalid) return;
            Logger.Trace($"OnPreInteractPower {GameDatabase.GetPrototypeName(preIteractPower)}");

            playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(avatarEntityId)
                .SetPowerProtoId((ulong)preIteractPower)
                .SetPowerRank(0)
                .SetCharacterLevel(60)
                .SetCombatLevel(60)
                .SetItemLevel(1)
                .SetItemVariation(1)
                .Build());

            ActivatePowerArchive activatePower = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity,
                Flags = ActivatePowerMessageFlags.HasTriggeringPowerPrototypeId | ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasPowerRandomSeed | ActivatePowerMessageFlags.HasFXRandomSeed,
                IdUserEntity = avatarEntityId,
                IdTargetEntity = 0,
                PowerPrototypeId = preIteractPower,
                UserPosition = playerConnection.LastPosition,
                PowerRandomSeed = 2222,
                FXRandomSeed = 2222
            };

            playerConnection.SendMessage(NetMessageActivatePower.CreateBuilder()
                 .SetArchiveData(activatePower.Serialize())
                 .Build());
        }

        private void OnPreInteractPowerEnd(PlayerConnection playerConnection, Entity interactObject)
        {
            ulong avatarEntityId = playerConnection.Player.CurrentAvatar.Id;
            PrototypeId proto = interactObject.BaseData.PrototypeId;
            var world = GameDatabase.GetPrototype<WorldEntityPrototype>(proto);
            if (world == null) return;
            PrototypeId preIteractPower = world.PreInteractPower;
            if (preIteractPower == 0) return;
            Logger.Trace($"OnPreInteractPowerEnd");

            playerConnection.SendMessage(NetMessageOnPreInteractPowerEnd.CreateBuilder()
                .SetIdTargetEntity(interactObject.Id)
                .SetAvatarIndex(0)
                .Build());

            playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                .SetEntityId(avatarEntityId)
                .SetPowerProtoId((ulong)preIteractPower)
                .Build());
        }

        private void OnFinishCellLoading(PlayerConnection playerConnection, int loadedCellCount)
        {
            Logger.Warn($"Forсed loading");
            playerConnection.AOI.LoadedCellCount = loadedCellCount;
            playerConnection.Game.FinishLoading(playerConnection);
        }

        private void OnEmoteDance(PlayerConnection playerConnection, AvatarPrototypeId avatar)
        {
            ulong avatarEntityId = playerConnection.Player.CurrentAvatar.Id;
            ActivatePowerArchive activatePower = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity,
                Flags = ActivatePowerMessageFlags.HasTriggeringPowerPrototypeId | ActivatePowerMessageFlags.TargetPositionIsUserPosition | ActivatePowerMessageFlags.HasPowerRandomSeed | ActivatePowerMessageFlags.HasFXRandomSeed,
                IdUserEntity = avatarEntityId,
                IdTargetEntity = avatarEntityId,
                PowerPrototypeId = (PrototypeId)PowerPrototypes.Emotes.EmoteDance,
                UserPosition = playerConnection.LastPosition,
                PowerRandomSeed = 1111,
                FXRandomSeed = 1111
            };

            playerConnection.SendMessage(NetMessageActivatePower.CreateBuilder()
                .SetArchiveData(activatePower.Serialize())
                .Build());
        }

        private void OnToTeleport(PlayerConnection playerConnection, Vector3 targetPos)
        {
            Vector3 targetRot = new();

            uint cellid = 1;
            uint areaid = 1;

            playerConnection.SendMessage(NetMessageEntityPosition.CreateBuilder()
                .SetIdEntity(playerConnection.Player.CurrentAvatar.Id)
                .SetFlags((uint)ChangePositionFlags.Teleport)
                .SetPosition(targetPos.ToNetStructPoint3())
                .SetOrientation(targetRot.ToNetStructPoint3())
                .SetCellId(cellid)
                .SetAreaId(areaid)
                .SetEntityPrototypeId((ulong)playerConnection.Player.CurrentAvatar.EntityPrototype.DataRef)
                .Build());

            playerConnection.LastPosition = targetPos;
            Logger.Trace($"Teleporting to {targetPos}");
        }

        private bool OnStartTravel(PlayerConnection playerConnection, PrototypeId powerId)
        {
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            switch (powerId)
            {
                case (PrototypeId)PowerPrototypes.Travel.GhostRiderRide:
                    Condition ghostRiderRideCondition = avatar.ConditionCollection.GetCondition(666);
                    if (ghostRiderRideCondition != null) return Logger.WarnReturn(false, "OnStartTravel(): ghostRiderRideCondition != null");

                    Logger.Trace($"EventStart GhostRiderRide");

                    // Player.Avatar.EvalOnCreate.AssignProp.ProcProp.Param1 
                    // Create and add a ride condition
                    ghostRiderRideCondition = avatar.ConditionCollection.AllocateCondition();
                    ghostRiderRideCondition.InitializeFromPowerMixinPrototype(666, powerId, 0, TimeSpan.Zero);
                    avatar.ConditionCollection.AddCondition(ghostRiderRideCondition);

                    // Assign the hotspot power to the avatar
                    PowerIndexProperties indexProps = new(0, avatar.CharacterLevel, avatar.CombatLevel);
                    avatar.AssignPower((PrototypeId)PowerPrototypes.GhostRider.RideBikeHotspotsEnd, indexProps);

                    // Notify the client
                    AddConditionArchive conditionArchive = new()
                    {
                        ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy,
                        EntityId = avatar.Id,
                        Condition = ghostRiderRideCondition
                    };

                    playerConnection.SendMessage(NetMessageAddCondition.CreateBuilder()
                        .SetArchiveData(conditionArchive.SerializeToByteString())
                        .Build());

                    playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                        .SetEntityId(avatar.Id)
                        .SetPowerProtoId((ulong)PowerPrototypes.GhostRider.RideBikeHotspotsEnd)
                        .SetPowerRank(indexProps.PowerRank)
                        .SetCharacterLevel(indexProps.CharacterLevel)
                        .SetCombatLevel(indexProps.CombatLevel)
                        .SetItemLevel(indexProps.ItemLevel)
                        .SetItemVariation(indexProps.ItemVariation)
                        .Build());

                    break;

                case (PrototypeId)PowerPrototypes.Travel.WolverineRide:
                case (PrototypeId)PowerPrototypes.Travel.DeadpoolRide:
                case (PrototypeId)PowerPrototypes.Travel.NickFuryRide:
                case (PrototypeId)PowerPrototypes.Travel.CyclopsRide:
                case (PrototypeId)PowerPrototypes.Travel.BlackWidowRide:
                case (PrototypeId)PowerPrototypes.Travel.BladeRide:
                case (PrototypeId)PowerPrototypes.Travel.AntmanFlight:
                case (PrototypeId)PowerPrototypes.Travel.ThingFlight:
                    Condition rideCondition = avatar.ConditionCollection.GetCondition(667);
                    if (rideCondition != null) return Logger.WarnReturn(false, "OnStartTravel(): rideCondition != null");

                    Logger.Trace($"EventStart Ride");

                    // Create and add a ride condition
                    rideCondition = avatar.ConditionCollection.AllocateCondition();
                    rideCondition.InitializeFromPowerMixinPrototype(667, powerId, 0, TimeSpan.Zero);
                    avatar.ConditionCollection.AddCondition(rideCondition);

                    // Notify the client
                    conditionArchive = new()
                    {
                        ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy,
                        EntityId = avatar.Id,
                        Condition = rideCondition
                    };

                    playerConnection.SendMessage(NetMessageAddCondition.CreateBuilder()
                        .SetArchiveData(conditionArchive.SerializeToByteString())
                        .Build());

                    break;
            }

            return true;
        }

        private void OnEndTravel(PlayerConnection playerConnection, PrototypeId powerId)
        {
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            switch (powerId)
            {
                case (PrototypeId)PowerPrototypes.Travel.GhostRiderRide:
                    if (avatar.ConditionCollection.GetCondition(666) == null) return;

                    Logger.Trace($"EventEnd GhostRiderRide");

                    // Remove the ride condition
                    avatar.ConditionCollection.RemoveCondition(666);
                    avatar.UnassignPower((PrototypeId)PowerPrototypes.GhostRider.RideBikeHotspotsEnd);

                    // Notify the client
                    playerConnection.SendMessage(NetMessageDeleteCondition.CreateBuilder()
                        .SetIdEntity(avatar.Id)
                        .SetKey(666)
                        .Build());

                    playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                        .SetEntityId(avatar.Id)
                        .SetPowerProtoId((ulong)PowerPrototypes.GhostRider.RideBikeHotspotsEnd)
                        .Build());

                    break;

                case (PrototypeId)PowerPrototypes.Travel.WolverineRide:
                case (PrototypeId)PowerPrototypes.Travel.DeadpoolRide:
                case (PrototypeId)PowerPrototypes.Travel.NickFuryRide:
                case (PrototypeId)PowerPrototypes.Travel.CyclopsRide:
                case (PrototypeId)PowerPrototypes.Travel.BlackWidowRide:
                case (PrototypeId)PowerPrototypes.Travel.BladeRide:
                case (PrototypeId)PowerPrototypes.Travel.AntmanFlight:
                case (PrototypeId)PowerPrototypes.Travel.ThingFlight:
                    if (avatar.ConditionCollection.GetCondition(667) == null) return;

                    Logger.Trace($"EventEnd Ride");

                    // Remove the ride condition
                    avatar.ConditionCollection.RemoveCondition(667);

                    // Notify the client
                    playerConnection.SendMessage(NetMessageDeleteCondition.CreateBuilder()
                        .SetIdEntity(avatar.Id)
                        .SetKey(667)
                        .Build());

                    break;
            }
        }

        private void OnStartThrowing(PlayerConnection playerConnection, ulong idTarget)
        {
            Logger.Trace($"Event StartThrowing");

            Avatar avatar = playerConnection.Player.CurrentAvatar;

            playerConnection.ThrowableEntity = _game.EntityManager.GetEntityById(idTarget);
            if (playerConnection.ThrowableEntity == null) return;

            Logger.Trace($"{GameDatabase.GetPrototypeName(playerConnection.ThrowableEntity.PrototypeDataRef)}");

            var throwableProto = playerConnection.ThrowableEntity.EntityPrototype as WorldEntityPrototype;
            if (throwableProto == null) return;

            // Set throwable properties on the avatar
            avatar.Properties[PropertyEnum.ThrowableOriginatorEntity] = idTarget;
            avatar.Properties[PropertyEnum.ThrowableOriginatorAssetRef] = throwableProto.UnrealClass;

            // Assign throwable and throwable can powers to the avatar's power collection
            PrototypeId throwablePowerRef = throwableProto.Properties[PropertyEnum.ThrowablePower];
            PrototypeId throwableCancelPowerRef = throwableProto.Properties[PropertyEnum.ThrowableRestorePower];

            PowerIndexProperties indexProps = new(0, avatar.CharacterLevel, avatar.CombatLevel);
            avatar.AssignPower(throwablePowerRef, indexProps);
            avatar.AssignPower(throwableCancelPowerRef, indexProps);

            // Notify the client
            playerConnection.SendMessage(Property.ToNetMessageSetProperty(avatar.Properties.ReplicationId, new(PropertyEnum.ThrowableOriginatorEntity), idTarget));

            playerConnection.SendMessage(Property.ToNetMessageSetProperty(avatar.Properties.ReplicationId, new(PropertyEnum.ThrowableOriginatorAssetRef), throwableProto.UnrealClass));

            playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwableCancelPowerRef)
                .SetPowerRank(indexProps.PowerRank)
                .SetCharacterLevel(indexProps.CharacterLevel)
                .SetCombatLevel(indexProps.CombatLevel)
                .SetItemLevel(indexProps.ItemLevel)
                .SetItemVariation(indexProps.ItemVariation)
                .Build());

            playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwablePowerRef)
                .SetPowerRank(indexProps.PowerRank)
                .SetCharacterLevel(indexProps.CharacterLevel)
                .SetCombatLevel(indexProps.CombatLevel)
                .SetItemLevel(indexProps.ItemLevel)
                .SetItemVariation(indexProps.ItemVariation)
                .Build());

            playerConnection.SendMessage(NetMessageEntityDestroy.CreateBuilder()
                .SetIdEntity(idTarget)
                .Build());
        }

        private void OnEndThrowing(PlayerConnection playerConnection, PrototypeId powerId)
        {
            Logger.Trace("Event EndThrowing");

            Avatar avatar = playerConnection.Player.CurrentAvatar;

            // Remove throwable properties
            avatar.Properties.RemoveProperty(new(PropertyEnum.ThrowableOriginatorEntity));
            avatar.Properties.RemoveProperty(new(PropertyEnum.ThrowableOriginatorAssetRef));

            // Unassign throwable and throwable cancel powers
            Power throwablePower = avatar.GetThrowablePower();
            Power throwableCancelPower = avatar.GetThrowableCancelPower();

            avatar.UnassignPower(throwablePower.PrototypeDataRef);
            avatar.UnassignPower(throwableCancelPower.PrototypeDataRef);

            if (GameDatabase.GetPrototypeName(powerId).Contains("CancelPower"))
            {
                if (playerConnection.ThrowableEntity != null)
                    playerConnection.SendMessage(playerConnection.ThrowableEntity.ToNetMessageEntityCreate());
                Logger.Trace("Event RestoreThrowable");
            }
            else
            {
                playerConnection.ThrowableEntity?.ToDead();
            }

            playerConnection.ThrowableEntity = null;

            // Notify the client
            playerConnection.SendMessage(Property.ToNetMessageRemoveProperty(avatar.Properties.ReplicationId, new(PropertyEnum.ThrowableOriginatorEntity)));
            playerConnection.SendMessage(Property.ToNetMessageRemoveProperty(avatar.Properties.ReplicationId, new(PropertyEnum.ThrowableOriginatorAssetRef)));

            playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwablePower.PrototypeDataRef)
                .Build());

            playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                .SetEntityId(avatar.Id)
                .SetPowerProtoId((ulong)throwableCancelPower.PrototypeDataRef)
                .Build());
        }

        private void OnDiamondFormActivate(PlayerConnection playerConnection)
        {
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            Condition diamondFormCondition = avatar.ConditionCollection.GetCondition(111);
            if (diamondFormCondition != null) return;

            Logger.Trace($"Event Start EmmaDiamondForm");

            // Get the asset id for the current costume to set the correct owner asset id override
            PrototypeId emmaCostume = avatar.Properties[PropertyEnum.CostumeCurrent];
            // Invalid prototype id is the same as the default costume
            if (emmaCostume == PrototypeId.Invalid)
                emmaCostume = GameDatabase.GetPrototypeRefByName("Entity/Items/Costumes/Prototypes/EmmaFrost/Modern.prototype");    // MarvelPlayer_EmmaFrost_Modern

            AssetId costumeAsset = emmaCostume.As<CostumePrototype>().CostumeUnrealClass;

            // Create and add a condition for the diamond form
            diamondFormCondition = avatar.ConditionCollection.AllocateCondition();
            diamondFormCondition.InitializeFromPowerMixinPrototype(111, (PrototypeId)PowerPrototypes.EmmaFrost.DiamondFormCondition, 0, TimeSpan.Zero, true, costumeAsset);
            avatar.ConditionCollection.AddCondition(diamondFormCondition);

            // Notify the client
            AddConditionArchive conditionArchive = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy,
                EntityId = avatar.Id,
                Condition = diamondFormCondition
            };

            playerConnection.SendMessage(NetMessageAddCondition.CreateBuilder()
                 .SetArchiveData(conditionArchive.SerializeToByteString())
                 .Build());
        }

        private void OnDiamondFormDeactivate(PlayerConnection playerConnection)
        {
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            // TODO: get DiamondFormCondition Condition Key
            if (avatar.ConditionCollection.GetCondition(111) == null) return;

            Logger.Trace($"EventEnd EmmaDiamondForm");

            // Remove the condition server-side
            avatar.ConditionCollection.RemoveCondition(111);

            // Notify the client
            playerConnection.SendMessage(NetMessageDeleteCondition.CreateBuilder()
              .SetKey(111)
              .SetIdEntity(avatar.Id)
              .Build());
        }

        private bool OnStartMagikUltimate(PlayerConnection playerConnection, NetStructPoint3 position)
        {
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            Condition magikUltimateCondition = avatar.ConditionCollection.GetCondition(777);
            if (magikUltimateCondition != null) return Logger.WarnReturn(false, "OnStartMagikUltimate(): magikUltimateCondition != null");

            Logger.Trace($"EventStart Magik Ultimate");

            // Create and add a condition for the ultimate
            magikUltimateCondition = avatar.ConditionCollection.AllocateCondition();
            magikUltimateCondition.InitializeFromPowerMixinPrototype(777, (PrototypeId)PowerPrototypes.Magik.Ultimate, 0, TimeSpan.FromMilliseconds(20000));
            avatar.ConditionCollection.AddCondition(magikUltimateCondition);

            /*
            // Create the arena entity
            WorldEntity arenaEntity = _game.EntityManager.CreateWorldEntityEmpty(
                playerConnection.AOI.Region.Id,
                (PrototypeId)PowerPrototypes.Magik.UltimateArea,
                new(position.X, position.Y, position.Z), new());


            // Save the entity id for the arena entity (we need to store this state in the avatar entity instead)
            playerConnection.MagikUltimateEntityId = arenaEntity.Id;
            */

            // Notify the client
            AddConditionArchive conditionArchive = new()
            {
                ReplicationPolicy = AOINetworkPolicyValues.DefaultPolicy,
                EntityId = avatar.Id,
                Condition = magikUltimateCondition
            };

            playerConnection.SendMessage(NetMessageAddCondition.CreateBuilder()
                .SetArchiveData(conditionArchive.SerializeToByteString())
                .Build());

            /*
            playerConnection.SendMessage(arenaEntity.ToNetMessageEntityCreate());

            playerConnection.SendMessage(NetMessagePowerCollectionAssignPower.CreateBuilder()
                .SetEntityId(arenaEntity.Id)
                .SetPowerProtoId((ulong)PowerPrototypes.Magik.UltimateHotspotEffect)
                .SetPowerRank(0)
                .SetCharacterLevel(60)
                .SetCombatLevel(60)
                .SetItemLevel(1)
                .SetItemVariation(1)
                .Build());

            playerConnection.SendMessage(Property.ToNetMessageSetProperty(arenaEntity.Properties.ReplicationId, new(PropertyEnum.AttachedToEntityId), avatar.Id));
            */

            return true;
        }

        private bool OnEndMagikUltimate(PlayerConnection playerConnection)
        {
            // Make sure we still get Magik in case the player switched to another avatar
            Avatar avatar = playerConnection.Player.AvatarList.FirstOrDefault(avatar => avatar.PrototypeDataRef == (PrototypeId)AvatarPrototypeId.Magik);
            if (avatar == null) return Logger.WarnReturn(false, "OnEndMagikUltimate(): avatar == null");

            Condition magikUltimateCondition = avatar.ConditionCollection.GetCondition(777);
            if (magikUltimateCondition == null) return Logger.WarnReturn(false, "OnEndMagikUltimate(): magikUltimateCondition == null");

            Logger.Trace($"EventEnd Magik Ultimate");

            // Remove the ultimate condition
            avatar.ConditionCollection.RemoveCondition(777);

            /*
            // TODO: Removed the hotspot effect power from the arena's power collection

            // Destroy the arena entity
            ulong arenaEntityId = playerConnection.MagikUltimateEntityId;
            var entity = _game.EntityManager.GetEntityById(arenaEntityId);
            entity?.Destroy();
            */

            // Notify the client
            playerConnection.SendMessage(NetMessageDeleteCondition.CreateBuilder()
                .SetIdEntity(avatar.Id)
                .SetKey(777)
                .Build());

            /*
            playerConnection.SendMessage(NetMessagePowerCollectionUnassignPower.CreateBuilder()
                .SetEntityId(arenaEntityId)
                .SetPowerProtoId((ulong)PowerPrototypes.Magik.UltimateHotspotEffect)
                .Build());

            playerConnection.SendMessage(NetMessageEntityDestroy.CreateBuilder()
                .SetIdEntity(arenaEntityId)
                .Build());
            */

            return true;
        }

        private void OnGetRegion(PlayerConnection playerConnection, Region region)
        {
            Logger.Trace($"Event GetRegion");
            var messages = region.GetLoadingMessages(playerConnection.Game.Id, playerConnection.WaypointDataRef, playerConnection);
            foreach (IMessage message in messages)
                playerConnection.SendMessage(message);
        }

        private void OnErrorInRegion(PlayerConnection playerConnection, PrototypeId regionProtoId)
        {
            Logger.Error($"Event ErrorInRegion {GameDatabase.GetFormattedPrototypeName(regionProtoId)}");
            playerConnection.Disconnect();
        }
    }
}
