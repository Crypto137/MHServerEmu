using Gazillion;
using MHServerEmu.Core.Logging;
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
        private readonly Game _game;

        public PowerMessageHandler(Game game)
        {
            _game = game;
        }

        public void ReceiveMessage(PlayerConnection playerConnection, MailboxMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageTryActivatePower:              OnTryActivatePower(playerConnection, message); break;
                case ClientToGameServerMessage.NetMessagePowerRelease:                  OnPowerRelease(playerConnection, message); break;
                case ClientToGameServerMessage.NetMessageTryCancelPower:                OnTryCancelPower(playerConnection, message); break;
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:          OnTryCancelActivePower(playerConnection, message); break;
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer: OnContinuousPowerUpdate(playerConnection, message); break;
                case ClientToGameServerMessage.NetMessageAssignStolenPower:             OnAssignStolenPower(playerConnection, message); break;

                default: Logger.Warn($"ReceiveMessage(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private bool PowerHasKeyword(PrototypeId powerId, PrototypeId keyword)
        {
            var power = GameDatabase.GetPrototype<PowerPrototype>(powerId);
            if (power == null) return false;

            for (int i = 0; i < power.Keywords.Length; i++)
                if (power.Keywords[i] == keyword) return true;

            return false;
        }

        private void HandleTravelPower(PlayerConnection playerConnection, PrototypeId powerId)
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
                    _game.EventManager.AddEvent(playerConnection, EventEnum.StartTravel, 100 - delta, powerId);
                    break;
                case (PrototypeId)PowerPrototypes.Travel.AntmanFlight:
                    _game.EventManager.AddEvent(playerConnection, EventEnum.StartTravel, 210 - delta, powerId);
                    break;
                case (PrototypeId)PowerPrototypes.Travel.ThingFlight:
                    _game.EventManager.AddEvent(playerConnection, EventEnum.StartTravel, 235 - delta, powerId);
                    break;
            }
        }

        #region Message Handling

        private bool OnTryActivatePower(PlayerConnection playerConnection, MailboxMessage message)
        {
            var tryActivatePower = message.As<NetMessageTryActivatePower>();
            if (tryActivatePower == null) return Logger.WarnReturn(false, $"OnTryActivatePower(): Failed to retrieve message");

            /* ActivatePower using TryActivatePower data
            ActivatePowerArchive activatePowerArchive = new(tryActivatePowerMessage, client.LastPosition);
            client.SendMessage(muxId, new(NetMessageActivatePower.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(activatePowerArchive.Encode()))
                .Build()));
            */

            var powerPrototypeId = (PrototypeId)tryActivatePower.PowerPrototypeId;
            string powerPrototypePath = GameDatabase.GetPrototypeName(powerPrototypeId);
            Logger.Trace($"Received TryActivatePower for {powerPrototypePath}");

            if (powerPrototypePath.Contains("ThrowablePowers/"))
            {
                Logger.Trace($"AddEvent EndThrowing for {tryActivatePower.PowerPrototypeId}");
                var power = GameDatabase.GetPrototype<PowerPrototype>(powerPrototypeId);
                _game.EventManager.AddEvent(playerConnection, EventEnum.EndThrowing, power.AnimationTimeMS, tryActivatePower.PowerPrototypeId);
                return true;
            }
            else if (powerPrototypePath.Contains("EmmaFrost/"))
            {
                if (PowerHasKeyword(powerPrototypeId, (PrototypeId)HardcodedBlueprints.DiamondFormActivatePower))
                    _game.EventManager.AddEvent(playerConnection, EventEnum.DiamondFormActivate, 0, tryActivatePower.PowerPrototypeId);
                else if (PowerHasKeyword(powerPrototypeId, (PrototypeId)HardcodedBlueprints.Mental))
                    _game.EventManager.AddEvent(playerConnection, EventEnum.DiamondFormDeactivate, 0, tryActivatePower.PowerPrototypeId);
            }
            else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Magik.Ultimate)
            {
                _game.EventManager.AddEvent(playerConnection, EventEnum.StartMagikUltimate, 0, tryActivatePower.TargetPosition);
                _game.EventManager.AddEvent(playerConnection, EventEnum.EndMagikUltimate, 20000, 0u);
            }
            else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Items.BowlingBallItemPower)
            {
                Item bowlingBall = (Item)playerConnection.Game.EntityManager.GetEntityByPrototypeId((PrototypeId)7835010736274089329); // BowlingBallItem
                if (bowlingBall != null)
                {
                    playerConnection.SendMessage(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(bowlingBall.Id).Build());
                    playerConnection.Game.EntityManager.DestroyEntity(bowlingBall);
                }
            }

            // if (powerPrototypePath.Contains("TravelPower/")) 
            //    TrawerPower(client, tryActivatePower.PowerPrototypeId);

            //Logger.Trace(tryActivatePower.ToString());

            PowerResultArchive archive = new(tryActivatePower);
            if (archive.TargetEntityId > 0)
            {                
                playerConnection.SendMessage(NetMessagePowerResult.CreateBuilder()
                    .SetArchiveData(archive.Serialize())
                    .Build());

                TestHit(playerConnection, archive.TargetEntityId, (int)archive.DamagePhysical);
            }

            return true;
        }

        private void TestHit(PlayerConnection playerConnection, ulong entityId, int damage)
        {
            if (damage > 0)
            {
                WorldEntity entity = (WorldEntity)playerConnection.Game.EntityManager.GetEntityById(entityId);
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
                        playerConnection.SendMessage(
                         Property.ToNetMessageSetProperty(repId, new(PropertyEnum.IsDead), true)
                         );
                    } else if (proto is AgentPrototype agent && agent.Locomotion.Immobile == false)
                    {
                        LocomotionStateUpdateArchive locomotion = new()
                        {
                            ReplicationPolicy = AOINetworkPolicyValues.AOIChannelProximity,
                            EntityId = entityId,
                            FieldFlags = LocomotionMessageFlags.NoLocomotionState,
                            Position = new(entity.RegionLocation.GetPosition()),
                            Orientation = new(),
                            LocomotionState = new(0)
                        };
                        locomotion.Orientation.Yaw = Vector3.Angle(locomotion.Position, playerConnection.LastPosition);
                        playerConnection.SendMessage(NetMessageLocomotionStateUpdate.CreateBuilder()
                            .SetArchiveData(locomotion.Serialize())
                            .Build());
                    }
                    if (entity.ConditionCollection.Count > 0 && health == entity.Properties[PropertyEnum.HealthMaxOther])
                    {
                        playerConnection.SendMessage(NetMessageDeleteCondition.CreateBuilder()
                            .SetIdEntity(entityId)
                            .SetKey(1)
                            .Build());
                    }
                    entity.Properties[PropertyEnum.Health] = newHealth;
                    playerConnection.SendMessage(
                        Property.ToNetMessageSetProperty(repId, new(PropertyEnum.Health), newHealth)
                        );
                    if (newHealth == 0)
                    {
                        playerConnection.SendMessage(NetMessageEntityKill.CreateBuilder()
                            .SetIdEntity(entityId)
                            .SetIdKillerEntity(playerConnection.Player.CurrentAvatar.Id)
                            .SetKillFlags(0).Build());

                        playerConnection.SendMessage(
                            Property.ToNetMessageSetProperty(repId, new(PropertyEnum.NoEntityCollide), true)
                        );
                    }
                }
            }
        }

        private bool OnPowerRelease(PlayerConnection playerConnection, MailboxMessage message)
        {
            var powerRelease = message.As<NetMessagePowerRelease>();
            if (powerRelease == null) return Logger.WarnReturn(false, $"OnPowerRelease(): Failed to retrieve message");

            Logger.Trace($"Received PowerRelease for {GameDatabase.GetPrototypeName((PrototypeId)powerRelease.PowerPrototypeId)}");
            return true;
        }

        private bool OnTryCancelPower(PlayerConnection playerConnection, MailboxMessage message)
        {
            var tryCancelPower = message.As<NetMessageTryCancelPower>();
            if (tryCancelPower == null) return Logger.WarnReturn(false, $"OnTryCancelPower(): Failed to retrieve message");

            string powerPrototypePath = GameDatabase.GetPrototypeName((PrototypeId)tryCancelPower.PowerPrototypeId);
            Logger.Trace($"Received TryCancelPower for {powerPrototypePath}");

            if (powerPrototypePath.Contains("TravelPower/"))
                _game.EventManager.AddEvent(playerConnection, EventEnum.EndTravel, 0, tryCancelPower.PowerPrototypeId);

            return true;
        }

        private bool OnTryCancelActivePower(PlayerConnection playerConnection, MailboxMessage message)
        {
            Logger.Trace("Received TryCancelActivePower");
            return true;
        }

        private bool OnContinuousPowerUpdate(PlayerConnection playerConnection, MailboxMessage message)
        {
            var continuousPowerUpdate = message.As<NetMessageContinuousPowerUpdateToServer>();
            if (continuousPowerUpdate == null) return Logger.WarnReturn(false, $"OnContinuousPowerUpdate(): Failed to retrieve message");

            var powerPrototypeId = (PrototypeId)continuousPowerUpdate.PowerPrototypeId;
            string powerPrototypePath = GameDatabase.GetPrototypeName(powerPrototypeId);
            Logger.Trace($"Received ContinuousPowerUpdate for {powerPrototypePath}");

            if (powerPrototypePath.Contains("TravelPower/"))
                HandleTravelPower(playerConnection, powerPrototypeId);
            // Logger.Trace(continuousPowerUpdate.ToString());

            return true;
        }

        private bool OnAssignStolenPower(PlayerConnection playerConnection, MailboxMessage message)
        {
            var assignStolenPower = message.As<NetMessageAssignStolenPower>();
            if (assignStolenPower == null) return Logger.WarnReturn(false, $"OnAssignStolenPower(): Failed to retrieve message");

            PropertyParam param = Property.ToParam(PropertyEnum.AvatarMappedPower, 0, (PrototypeId)assignStolenPower.StealingPowerProtoId);
            playerConnection.SendMessage(Property.ToNetMessageSetProperty((ulong)HardcodedAvatarPropertyCollectionReplicationId.Rogue,
                new(PropertyEnum.AvatarMappedPower, param), (PrototypeId)assignStolenPower.StolenPowerProtoId));

            return true;
        }

        #endregion
    }
}
