using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Network;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Powers
{
    public class PowerMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly EventManager _eventManager;

        public PowerMessageHandler(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        public IEnumerable<(FrontendClient, GameMessage)> HandleMessage(FrontendClient client, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageTryActivatePower:
                    if (message.TryDeserialize<NetMessageTryActivatePower>(out var tryActivatePower))
                        return OnTryActivatePower(client, tryActivatePower);
                    break;

                case ClientToGameServerMessage.NetMessagePowerRelease:
                    if (message.TryDeserialize<NetMessagePowerRelease>(out var powerRelease))
                        return OnPowerRelease(client, powerRelease);
                    break;

                case ClientToGameServerMessage.NetMessageTryCancelPower:
                    if (message.TryDeserialize<NetMessageTryCancelPower>(out var tryCancelPower))
                        return OnTryCancelPower(client, tryCancelPower);
                    break;

                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                    if (message.TryDeserialize<NetMessageTryCancelActivePower>(out var tryCancelActivePower))
                        return OnTryCancelActivePower(client, tryCancelActivePower);
                    break;

                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                    if (message.TryDeserialize<NetMessageContinuousPowerUpdateToServer>(out var continuousPowerUpdate))
                        return OnContinuousPowerUpdate(client, continuousPowerUpdate);
                    break;

                case ClientToGameServerMessage.NetMessageAbilitySlotToAbilityBar:
                    if (message.TryDeserialize<NetMessageAbilitySlotToAbilityBar>(out var slotToAbilityBar))
                        return OnAbilitySlotToAbilityBar(client, slotToAbilityBar);
                    break;

                case ClientToGameServerMessage.NetMessageAbilityUnslotFromAbilityBar:
                    if (message.TryDeserialize<NetMessageAbilityUnslotFromAbilityBar>(out var unslotFromAbilityBar))
                        return OnAbilityUnslotFromAbilityBar(client, unslotFromAbilityBar);
                    break;

                case ClientToGameServerMessage.NetMessageAbilitySwapInAbilityBar:
                    if (message.TryDeserialize<NetMessageAbilitySwapInAbilityBar>(out var swapInAbilityBar))
                        return OnAbilitySwapInAbilityBar(client, swapInAbilityBar);
                    break;

                case ClientToGameServerMessage.NetMessageAssignStolenPower:
                    if (message.TryDeserialize<NetMessageAssignStolenPower>(out var assignStolenPower))
                        return OnAssignStolenPower(client, assignStolenPower);
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }

            return Array.Empty<(FrontendClient, GameMessage)>();
        }

        private bool PowerHasKeyword(PrototypeId powerId, PrototypeId keyword)
        {
            var power = GameDatabase.GetPrototype<PowerPrototype>(powerId);
            if (power == null) return false;

            for (int i = 0; i < power.Keywords.Length; i++)
                if (power.Keywords[i] == keyword) return true;

            return false;
        }

        private void HandleTravelPower(FrontendClient client, PrototypeId powerId)
        {
            uint delta = 65; // TODO: Sync server-client
            switch (powerId)
            {   // Power.AnimationContactTimePercent
                case (PrototypeId)PowerPrototypes.Travel.GhostRiderRide:
                case (PrototypeId)PowerPrototypes.Travel.WolverineRide:
                case (PrototypeId)PowerPrototypes.Travel.DeadpoolRide:
                case (PrototypeId)PowerPrototypes.Travel.NickFuryRide:
                case (PrototypeId)PowerPrototypes.Travel.CyclopsRide:
                case (PrototypeId)PowerPrototypes.Travel.BlackWidowRide:
                case (PrototypeId)PowerPrototypes.Travel.BladeRide:
                    _eventManager.AddEvent(client, EventEnum.StartTravel, 100 - delta, powerId);
                    break;
                case (PrototypeId)PowerPrototypes.Travel.AntmanFlight:
                    _eventManager.AddEvent(client, EventEnum.StartTravel, 210 - delta, powerId);
                    break;
                case (PrototypeId)PowerPrototypes.Travel.ThingFlight:
                    _eventManager.AddEvent(client, EventEnum.StartTravel, 235 - delta, powerId);
                    break;
            }
        }

        #region Message Handling

        private IEnumerable<(FrontendClient, GameMessage)> OnTryActivatePower(FrontendClient client, NetMessageTryActivatePower tryActivatePower)
        {
            /* ActivatePower using TryActivatePower data
            ActivatePowerArchive activatePowerArchive = new(tryActivatePowerMessage, client.LastPosition);
            client.SendMessage(muxId, new(NetMessageActivatePower.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(activatePowerArchive.Encode()))
                .Build()));
            */

            List<(FrontendClient, GameMessage)> messageList = new();
            var powerPrototypeId = (PrototypeId)tryActivatePower.PowerPrototypeId;
            string powerPrototypePath = GameDatabase.GetPrototypeName(powerPrototypeId);
            Logger.Trace($"Received TryActivatePower for {powerPrototypePath}");

            if (powerPrototypePath.Contains("ThrowablePowers/"))
            {
                Logger.Trace($"AddEvent EndThrowing for {tryActivatePower.PowerPrototypeId}");
                var power = GameDatabase.GetPrototype<PowerPrototype>(powerPrototypeId);
                _eventManager.AddEvent(client, EventEnum.EndThrowing, power.AnimationTimeMS, tryActivatePower.PowerPrototypeId);
                return messageList;
            }
            else if (powerPrototypePath.Contains("EmmaFrost/"))
            {
                if (PowerHasKeyword(powerPrototypeId, (PrototypeId)HardcodedBlueprints.DiamondFormActivatePower))
                    _eventManager.AddEvent(client, EventEnum.DiamondFormActivate, 0, tryActivatePower.PowerPrototypeId);
                else if (PowerHasKeyword(powerPrototypeId, (PrototypeId)HardcodedBlueprints.Mental))
                    _eventManager.AddEvent(client, EventEnum.DiamondFormDeactivate, 0, tryActivatePower.PowerPrototypeId);
            }
            else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Magik.Ultimate)
            {
                _eventManager.AddEvent(client, EventEnum.StartMagikUltimate, 0, tryActivatePower.TargetPosition);
                _eventManager.AddEvent(client, EventEnum.EndMagikUltimate, 20000, 0u);
            }
            else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Items.BowlingBallItemPower)
            {
                Item bowlingBall = (Item)client.CurrentGame.EntityManager.GetEntityByPrototypeId((PrototypeId)7835010736274089329); // BowlingBallItem
                if (bowlingBall != null)
                {
                    messageList.Add((client, new(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(bowlingBall.BaseData.EntityId).Build())));
                    client.CurrentGame.EntityManager.DestroyEntity(bowlingBall.BaseData.EntityId);
                }
            }

            // if (powerPrototypePath.Contains("TravelPower/")) 
            //    TrawerPower(client, tryActivatePower.PowerPrototypeId);

            //Logger.Trace(tryActivatePower.ToString());

            PowerResultArchive archive = new(tryActivatePower);
            if (archive.TargetEntityId > 0)
            {                
                messageList.Add((client, new(NetMessagePowerResult.CreateBuilder()
                    .SetArchiveData(archive.Serialize())
                    .Build())));
                messageList.AddRange(TestHit(client, archive.TargetEntityId, (int)archive.DamagePhysical));
            }

            return messageList;
        }

        private List<(FrontendClient, GameMessage)> TestHit(FrontendClient client, ulong entityId, int damage)
        {
            List<(FrontendClient, GameMessage)> messageList = new();
            if (damage > 0)
            {
                WorldEntity entity = (WorldEntity)client.CurrentGame.EntityManager.GetEntityById(entityId);
                if (entity != null)
                {
                    var proto = entity.WorldEntityPrototype;
                    var repId = entity.Properties.ReplicationId;
                    int health = entity.Properties[PropertyEnum.Health];
                    int newHealth = health - damage;
                    if (newHealth <= 0)
                    {
                        entity.ToDead();
                        newHealth = 0;
                        entity.Properties[PropertyEnum.IsDead] = true;
                        messageList.Add((client,
                         new(Property.ToNetMessageSetProperty(repId, new(PropertyEnum.IsDead), true))
                         ));
                    } else if (proto is AgentPrototype agent && agent.Locomotion.Immobile == false)
                    {
                        LocomotionStateUpdateArchive locomotion = new()
                        {
                            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity,
                            EntityId = entityId,
                            FieldFlags = LocomotionMessageFlags.NoLocomotionState,
                            Position = new(entity.Location.GetPosition()),
                            Orientation = new(),
                            LocomotionState = new(0)
                        };
                        locomotion.Orientation.Yaw = Vector3.Angle(locomotion.Position, client.LastPosition);
                        messageList.Add((client, new(NetMessageLocomotionStateUpdate.CreateBuilder()
                            .SetArchiveData(locomotion.Serialize())
                            .Build())));
                    }
                    if (entity.ConditionCollection.Count > 0 && health == entity.Properties[PropertyEnum.HealthMaxOther])
                    {
                        messageList.Add((client, new(NetMessageDeleteCondition.CreateBuilder()
                            .SetIdEntity(entityId)
                            .SetKey(1)
                            .Build())));
                    }
                    entity.Properties[PropertyEnum.Health] = newHealth;
                    messageList.Add((client,
                        new(Property.ToNetMessageSetProperty(repId, new(PropertyEnum.Health), newHealth))
                        ));
                    if (newHealth == 0)
                    {
                        messageList.Add((client,
                        new(NetMessageEntityKill.CreateBuilder()
                        .SetIdEntity(entityId)
                        .SetIdKillerEntity((ulong)client.Session.Account.Player.Avatar.ToEntityId())
                        .SetKillFlags(0).Build())));
                        messageList.Add((client,
                        new(Property.ToNetMessageSetProperty(repId, new(PropertyEnum.NoEntityCollide), true))
                        ));
                    }
                }
            }
            return messageList;
        }

        private IEnumerable<(FrontendClient, GameMessage)> OnPowerRelease(FrontendClient client, NetMessagePowerRelease powerRelease)
        {
            Logger.Trace($"Received PowerRelease for {GameDatabase.GetPrototypeName((PrototypeId)powerRelease.PowerPrototypeId)}");
            return Array.Empty<(FrontendClient, GameMessage)>();
        }

        private IEnumerable<(FrontendClient, GameMessage)> OnTryCancelPower(FrontendClient client, NetMessageTryCancelPower tryCancelPower)
        {
            string powerPrototypePath = GameDatabase.GetPrototypeName((PrototypeId)tryCancelPower.PowerPrototypeId);
            Logger.Trace($"Received TryCancelPower for {powerPrototypePath}");

            if (powerPrototypePath.Contains("TravelPower/"))
                _eventManager.AddEvent(client, EventEnum.EndTravel, 0, tryCancelPower.PowerPrototypeId);

            return Array.Empty<(FrontendClient, GameMessage)>();
        }

        private IEnumerable<(FrontendClient, GameMessage)> OnTryCancelActivePower(FrontendClient client, NetMessageTryCancelActivePower tryCancelActivePower)
        {
            Logger.Trace("Received TryCancelActivePower");
            return Array.Empty<(FrontendClient, GameMessage)>();
        }

        private IEnumerable<(FrontendClient, GameMessage)> OnContinuousPowerUpdate(FrontendClient client, NetMessageContinuousPowerUpdateToServer continuousPowerUpdate)
        {
            var powerPrototypeId = (PrototypeId)continuousPowerUpdate.PowerPrototypeId;
            string powerPrototypePath = GameDatabase.GetPrototypeName(powerPrototypeId);
            Logger.Trace($"Received ContinuousPowerUpdate for {powerPrototypePath}");

            if (powerPrototypePath.Contains("TravelPower/"))
                HandleTravelPower(client, powerPrototypeId);
            // Logger.Trace(continuousPowerUpdate.ToString());

            return Array.Empty<(FrontendClient, GameMessage)>();
        }

        // Ability bar management (TODO: Move this to avatar entity)

        private IEnumerable<(FrontendClient, GameMessage)> OnAbilitySlotToAbilityBar(FrontendClient client, NetMessageAbilitySlotToAbilityBar slotToAbilityBar)
        {
            var abilityKeyMapping = client.Session.Account.CurrentAvatar.AbilityKeyMapping;
            PrototypeId prototypeRefId = (PrototypeId)slotToAbilityBar.PrototypeRefId;
            AbilitySlot slotNumber = (AbilitySlot)slotToAbilityBar.SlotNumber;
            Logger.Trace($"NetMessageAbilitySlotToAbilityBar: {GameDatabase.GetFormattedPrototypeName(prototypeRefId)} to {slotNumber}");

            // Set
            abilityKeyMapping.SetAbilityInAbilitySlot(prototypeRefId, slotNumber);
            
            return Array.Empty<(FrontendClient, GameMessage)>();
        }

        private IEnumerable<(FrontendClient, GameMessage)> OnAbilityUnslotFromAbilityBar(FrontendClient client, NetMessageAbilityUnslotFromAbilityBar unslotFromAbilityBar)
        {
            var abilityKeyMapping = client.Session.Account.CurrentAvatar.AbilityKeyMapping;
            AbilitySlot slotNumber = (AbilitySlot)unslotFromAbilityBar.SlotNumber;
            Logger.Trace($"NetMessageAbilityUnslotFromAbilityBar: from {slotNumber}");

            // Remove by assigning invalid id
            abilityKeyMapping.SetAbilityInAbilitySlot(PrototypeId.Invalid, slotNumber);

            return Array.Empty<(FrontendClient, GameMessage)>();
        }

        private IEnumerable<(FrontendClient, GameMessage)> OnAbilitySwapInAbilityBar(FrontendClient client, NetMessageAbilitySwapInAbilityBar swapInAbilityBar)
        {
            var abilityKeyMapping = client.Session.Account.CurrentAvatar.AbilityKeyMapping;
            AbilitySlot slotA = (AbilitySlot)swapInAbilityBar.SlotNumberA;
            AbilitySlot slotB = (AbilitySlot)swapInAbilityBar.SlotNumberB;
            Logger.Trace($"NetMessageAbilitySwapInAbilityBar: {slotA} and {slotB}");

            // Swap
            PrototypeId prototypeA = abilityKeyMapping.GetAbilityInAbilitySlot(slotA);
            PrototypeId prototypeB = abilityKeyMapping.GetAbilityInAbilitySlot(slotB);
            abilityKeyMapping.SetAbilityInAbilitySlot(prototypeB, slotA);
            abilityKeyMapping.SetAbilityInAbilitySlot(prototypeA, slotB);

            return Array.Empty<(FrontendClient, GameMessage)>();
        }

        private IEnumerable<(FrontendClient, GameMessage)> OnAssignStolenPower(FrontendClient client, NetMessageAssignStolenPower assignStolenPower)
        {
            PropertyParam param = Property.ToParam(PropertyEnum.AvatarMappedPower, 0, (PrototypeId)assignStolenPower.StealingPowerProtoId);
            yield return (client, new(Property.ToNetMessageSetProperty((ulong)HardcodedAvatarPropertyCollectionReplicationId.Rogue,
                new(PropertyEnum.AvatarMappedPower, param), (PrototypeId)assignStolenPower.StolenPowerProtoId)));
        }

        #endregion
    }
}
