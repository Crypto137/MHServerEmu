using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.Events.LegacyImplementations;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Network;
using MHServerEmu.Games.Properties;

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
            int delta = 65; // TODO: Sync server-client
            int timeOffsetMS;

            switch (powerId)
            {   // Power.AnimationContactTimePercent
                case (PrototypeId)PowerPrototypes.Travel.AntmanFlight:
                    timeOffsetMS = 210 - delta;
                    break;
                case (PrototypeId)PowerPrototypes.Travel.ThingFlight:
                    timeOffsetMS = 235 - delta;
                    break;
                default:
                    timeOffsetMS = 100 - delta;
                    break;
            }

            EventPointer<OLD_StartTravelEvent> eventPointer = new();
            _game.GameEventScheduler.ScheduleEvent(eventPointer, TimeSpan.FromMilliseconds(timeOffsetMS));
            eventPointer.Get().Initialize(playerConnection, powerId);
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

            Avatar avatar = playerConnection.Player.CurrentAvatar;

            // Send this activation to other players
            ActivatePowerArchive activatePowerArchive = new();
            activatePowerArchive.Initialize(tryActivatePower, avatar.RegionLocation.Position);
            _game.NetworkManager.SendMessageToInterested(activatePowerArchive.ToProtobuf(), avatar, AOINetworkPolicyValues.AOIChannelProximity, true);

            if (powerPrototypePath.Contains("ThrowablePowers/"))
            {
                Logger.Trace($"AddEvent EndThrowing for {GameDatabase.GetPrototypeName(powerPrototypeId)}");
                var power = GameDatabase.GetPrototype<PowerPrototype>(powerPrototypeId);

                EventPointer<OLD_EndThrowingEvent> endThrowingPointer = new();
                _game.GameEventScheduler.ScheduleEvent(endThrowingPointer, TimeSpan.FromMilliseconds(power.AnimationTimeMS));
                endThrowingPointer.Get().Initialize(playerConnection, (PrototypeId)tryActivatePower.PowerPrototypeId);

                return true;
            }
            else if (powerPrototypePath.Contains("EmmaFrost/"))
            {
                if (PowerHasKeyword(powerPrototypeId, (PrototypeId)HardcodedBlueprints.DiamondFormActivatePower))
                {
                    EventPointer<OLD_DiamondFormActivateEvent> activateEventPointer = new();
                    _game.GameEventScheduler.ScheduleEvent(activateEventPointer, TimeSpan.Zero);
                    activateEventPointer.Get().PlayerConnection = playerConnection;
                }
                else if (PowerHasKeyword(powerPrototypeId, (PrototypeId)HardcodedBlueprints.Mental))
                {
                    EventPointer<OLD_DiamondFormDeactivateEvent> deactivateEventPointer = new();
                    _game.GameEventScheduler.ScheduleEvent(deactivateEventPointer, TimeSpan.Zero);
                    deactivateEventPointer.Get().PlayerConnection = playerConnection;
                }
            }
            else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Magik.Ultimate)
            {
                EventPointer<OLD_StartMagikUltimate> startEventPointer = new();
                _game.GameEventScheduler.ScheduleEvent(startEventPointer, TimeSpan.Zero);
                startEventPointer.Get().Initialize(playerConnection);

                EventPointer<OLD_EndMagikUltimateEvent> endEventPointer = new();
                _game.GameEventScheduler.ScheduleEvent(endEventPointer, TimeSpan.FromSeconds(20));
                endEventPointer.Get().PlayerConnection = playerConnection;
            }
            else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Items.BowlingBallItemPower)
            {
                Inventory inventory = playerConnection.Player.GetInventory(InventoryConvenienceLabel.General);
                
                Entity bowlingBall = inventory.GetMatchingEntity((PrototypeId)7835010736274089329); // BowlingBallItem
                if (bowlingBall == null) return false;

                if (bowlingBall.Properties[PropertyEnum.InventoryStackCount] > 1)
                    bowlingBall.Properties.AdjustProperty(-1, PropertyEnum.InventoryStackCount);
                else
                    bowlingBall.Destroy();
            }

            // if (powerPrototypePath.Contains("TravelPower/")) 
            //    TrawerPower(client, tryActivatePower.PowerPrototypeId);

            //Logger.Trace(tryActivatePower.ToString());

            PowerResults results = new();
            results.Init(tryActivatePower);
            if (results.TargetEntityId > 0)
            {
                _game.NetworkManager.SendMessageToInterested(results.ToProtobuf(), avatar, AOINetworkPolicyValues.AOIChannelProximity);
                TestHit(playerConnection, results.TargetEntityId, (int)results.DamagePhysical);
            }

            return true;
        }

        private void TestHit(PlayerConnection playerConnection, ulong entityId, int damage)
        {
            if (damage == 0) return;
            
            var entity = playerConnection.Game.EntityManager.GetEntity<WorldEntity>(entityId);
            if (entity == null) return;

            int health = entity.Properties[PropertyEnum.Health];

            if (entity.ConditionCollection.Count > 0 && health == entity.Properties[PropertyEnum.HealthMaxOther])
            {
                // TODO: Clean this up
                playerConnection.SendMessage(NetMessageDeleteCondition.CreateBuilder()
                    .SetIdEntity(entityId)
                    .SetKey(1)
                    .Build());
            }

            int newHealth = Math.Max(health - damage, 0);
            entity.Properties[PropertyEnum.Health] = newHealth;

            if (newHealth == 0)
            {
                entity.Kill(playerConnection.Player.CurrentAvatar.Id);
            }
            else if (entity.WorldEntityPrototype is AgentPrototype agentProto && agentProto.Locomotion.Immobile == false)
            {
                entity.ChangeRegionPosition(null, new(Vector3.AngleYaw(entity.RegionLocation.Position, playerConnection.LastPosition), 0f, 0f));
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

            Logger.Trace(tryCancelPower.ToString());

            // Broadcast to other interested clients
            Avatar avatar = playerConnection.Player.CurrentAvatar;

            // NOTE: Although NetMessageCancelPower is not an archive, it uses power prototype enums
            ulong powerPrototypeEnum = (ulong)DataDirectory.Instance.GetPrototypeEnumValue<PowerPrototype>((PrototypeId)tryCancelPower.PowerPrototypeId);
            var clientMessage = NetMessageCancelPower.CreateBuilder()
                .SetIdAgent(tryCancelPower.IdUserEntity)
                .SetPowerPrototypeId(powerPrototypeEnum)
                .SetEndPowerFlags(tryCancelPower.EndPowerFlags)
                .Build();

            _game.NetworkManager.SendMessageToInterested(clientMessage, avatar, AOINetworkPolicyValues.AOIChannelProximity, true);

            if (powerPrototypePath.Contains("TravelPower/"))
            {
                EventPointer<OLD_EndTravelEvent> eventPointer = new();
                _game.GameEventScheduler.ScheduleEvent(eventPointer, TimeSpan.Zero);
                eventPointer.Get().Initialize(playerConnection, (PrototypeId)tryCancelPower.PowerPrototypeId);
            }

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
            // Logger.Trace(continuousPowerUpdate.ToString());

            Avatar avatar = playerConnection.Player.CurrentAvatar;

            // Broadcast to other interested clients
            var clientMessageBuilder = NetMessageContinuousPowerUpdateToClient.CreateBuilder()
                .SetIdAvatar(avatar.Id)
                .SetPowerPrototypeId(continuousPowerUpdate.PowerPrototypeId);

            if (continuousPowerUpdate.HasIdTargetEntity)
                clientMessageBuilder.SetIdTargetEntity(continuousPowerUpdate.IdTargetEntity);

            if (continuousPowerUpdate.HasTargetPosition)
                clientMessageBuilder.SetTargetPosition(continuousPowerUpdate.TargetPosition);

            if (continuousPowerUpdate.HasRandomSeed)
                clientMessageBuilder.SetRandomSeed(continuousPowerUpdate.RandomSeed);

            _game.NetworkManager.SendMessageToInterested(clientMessageBuilder.Build(), avatar, AOINetworkPolicyValues.AOIChannelProximity, true);

            // Handle travel
            if (powerPrototypePath.Contains("TravelPower/"))
                HandleTravelPower(playerConnection, powerPrototypeId);

            return true;
        }

        private bool OnAssignStolenPower(PlayerConnection playerConnection, MailboxMessage message)
        {
            var assignStolenPower = message.As<NetMessageAssignStolenPower>();
            if (assignStolenPower == null) return Logger.WarnReturn(false, $"OnAssignStolenPower(): Failed to retrieve message");

            PrototypeId stealingPowerRef = (PrototypeId)assignStolenPower.StealingPowerProtoId;
            PrototypeId stolenPowerRef = (PrototypeId)assignStolenPower.StolenPowerProtoId;

            Avatar avatar = playerConnection.Player.CurrentAvatar;
            avatar.Properties[PropertyEnum.AvatarMappedPower, stealingPowerRef] = stolenPowerRef;

            return true;
        }

        #endregion
    }
}
