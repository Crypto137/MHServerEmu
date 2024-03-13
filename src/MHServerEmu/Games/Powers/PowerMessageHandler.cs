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

        public void ReceiveMessage(PlayerConnection playerConnection, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageTryActivatePower:
                    if (message.TryDeserialize<NetMessageTryActivatePower>(out var tryActivatePower))
                        OnTryActivatePower(playerConnection, tryActivatePower);
                    break;

                case ClientToGameServerMessage.NetMessagePowerRelease:
                    if (message.TryDeserialize<NetMessagePowerRelease>(out var powerRelease))
                        OnPowerRelease(playerConnection, powerRelease);
                    break;

                case ClientToGameServerMessage.NetMessageTryCancelPower:
                    if (message.TryDeserialize<NetMessageTryCancelPower>(out var tryCancelPower))
                        OnTryCancelPower(playerConnection, tryCancelPower);
                    break;

                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                    if (message.TryDeserialize<NetMessageTryCancelActivePower>(out var tryCancelActivePower))
                        OnTryCancelActivePower(playerConnection, tryCancelActivePower);
                    break;

                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                    if (message.TryDeserialize<NetMessageContinuousPowerUpdateToServer>(out var continuousPowerUpdate))
                        OnContinuousPowerUpdate(playerConnection, continuousPowerUpdate);
                    break;

                case ClientToGameServerMessage.NetMessageAssignStolenPower:
                    if (message.TryDeserialize<NetMessageAssignStolenPower>(out var assignStolenPower))
                        OnAssignStolenPower(playerConnection, assignStolenPower);
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
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

        private void OnTryActivatePower(PlayerConnection playerConnection, NetMessageTryActivatePower tryActivatePower)
        {
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
                return;
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
                            Position = new(entity.Location.GetPosition()),
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

        private void OnPowerRelease(PlayerConnection playerConnection, NetMessagePowerRelease powerRelease)
        {
            Logger.Trace($"Received PowerRelease for {GameDatabase.GetPrototypeName((PrototypeId)powerRelease.PowerPrototypeId)}");
        }

        private void OnTryCancelPower(PlayerConnection playerConnection, NetMessageTryCancelPower tryCancelPower)
        {
            string powerPrototypePath = GameDatabase.GetPrototypeName((PrototypeId)tryCancelPower.PowerPrototypeId);
            Logger.Trace($"Received TryCancelPower for {powerPrototypePath}");

            if (powerPrototypePath.Contains("TravelPower/"))
                _game.EventManager.AddEvent(playerConnection, EventEnum.EndTravel, 0, tryCancelPower.PowerPrototypeId);
        }

        private void OnTryCancelActivePower(PlayerConnection playerConnection, NetMessageTryCancelActivePower tryCancelActivePower)
        {
            Logger.Trace("Received TryCancelActivePower");
        }

        private void OnContinuousPowerUpdate(PlayerConnection playerConnection, NetMessageContinuousPowerUpdateToServer continuousPowerUpdate)
        {
            var powerPrototypeId = (PrototypeId)continuousPowerUpdate.PowerPrototypeId;
            string powerPrototypePath = GameDatabase.GetPrototypeName(powerPrototypeId);
            Logger.Trace($"Received ContinuousPowerUpdate for {powerPrototypePath}");

            if (powerPrototypePath.Contains("TravelPower/"))
                HandleTravelPower(playerConnection, powerPrototypeId);
            // Logger.Trace(continuousPowerUpdate.ToString());
        }

        private void OnAssignStolenPower(PlayerConnection playerConnection, NetMessageAssignStolenPower assignStolenPower)
        {
            PropertyParam param = Property.ToParam(PropertyEnum.AvatarMappedPower, 0, (PrototypeId)assignStolenPower.StealingPowerProtoId);
            playerConnection.SendMessage(Property.ToNetMessageSetProperty((ulong)HardcodedAvatarPropertyCollectionReplicationId.Rogue,
                new(PropertyEnum.AvatarMappedPower, param), (PrototypeId)assignStolenPower.StolenPowerProtoId));
        }

        #endregion
    }
}
