using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Networking;
using MHServerEmu.Games.Entities.Items;

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

        public IEnumerable<QueuedGameMessage> HandleMessage(FrontendClient client, GameMessage message)
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

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }

            return Array.Empty<QueuedGameMessage>();
        }

        private bool PowerHasKeyword(PrototypeId powerId, DefaultPrototypeId Keyword)
        {
            PrototypeFieldGroup Power = powerId.GetPrototype().GetFieldGroup(DefaultPrototypeId.Power);
            if (Power == null) return false;
            PrototypeListField Keywords = Power.GetListField(FieldId.Keywords);
            if (Keywords == null) return false;

            for (int i = 0; i < Keywords.Values.Length; i++)
                if ((ulong)Keywords.Values[i] == (ulong)Keyword) return true;

            return false;
        }

        private void HandleTravelPower(FrontendClient client, PrototypeId powerId)
        {
            uint delta = 65; // TODO: Sync server-client
            switch (powerId)
            {   // Power.AnimationContactTimePercent
                case (PrototypeId)PowerPrototypes.GhostRider.GhostRiderRide:
                case (PrototypeId)PowerPrototypes.Wolverine.WolverineRide:
                case (PrototypeId)PowerPrototypes.Deadpool.DeadpoolRide:
                case (PrototypeId)PowerPrototypes.NickFury.NickFuryRide:
                case (PrototypeId)PowerPrototypes.Cyclops.CyclopsRide:
                case (PrototypeId)PowerPrototypes.BlackWidow.BlackWidowRide:
                case (PrototypeId)PowerPrototypes.Blade.BladeRide:
                    _eventManager.AddEvent(client, EventEnum.StartTravel, 100 - delta, powerId);
                    break;
                case (PrototypeId)PowerPrototypes.AntMan.AntmanFlight:
                    _eventManager.AddEvent(client, EventEnum.StartTravel, 210 - delta, powerId);
                    break;
                case (PrototypeId)PowerPrototypes.Thing.ThingFlight:
                    _eventManager.AddEvent(client, EventEnum.StartTravel, 235 - delta, powerId);
                    break;
            }
        }

        #region Message Handling

        private IEnumerable<QueuedGameMessage> OnTryActivatePower(FrontendClient client, NetMessageTryActivatePower tryActivatePower)
        {
            /* ActivatePower using TryActivatePower data
            ActivatePowerArchive activatePowerArchive = new(tryActivatePowerMessage, client.LastPosition);
            client.SendMessage(muxId, new(NetMessageActivatePower.CreateBuilder()
                .SetArchiveData(ByteString.CopyFrom(activatePowerArchive.Encode()))
                .Build()));
            */

            List<QueuedGameMessage> messageList = new();
            var powerPrototypeId = (PrototypeId)tryActivatePower.PowerPrototypeId;
            string powerPrototypePath = GameDatabase.GetPrototypeName(powerPrototypeId);
            Logger.Trace($"Received TryActivatePower for {powerPrototypePath}");

            if (powerPrototypePath.Contains("ThrowablePowers/"))
            {
                Logger.Trace($"AddEvent EndThrowing for {tryActivatePower.PowerPrototypeId}");
                PrototypeFieldGroup Power = powerPrototypeId.GetPrototype().GetFieldGroup(DefaultPrototypeId.Power);
                long animationTimeMS = 1100;
                if (Power != null)
                {
                    PrototypeSimpleField AnimationTimeMS = Power.GetField(FieldId.AnimationTimeMS);
                    if (AnimationTimeMS != null) animationTimeMS = (long)AnimationTimeMS.Value;
                }
                _eventManager.AddEvent(client, EventEnum.EndThrowing, animationTimeMS, tryActivatePower.PowerPrototypeId);
                return messageList;
            }
            else if (powerPrototypePath.Contains("EmmaFrost/"))
            {
                if (PowerHasKeyword(powerPrototypeId, DefaultPrototypeId.DiamondFormActivatePower))
                    _eventManager.AddEvent(client, EventEnum.DiamondFormActivate, 0, tryActivatePower.PowerPrototypeId);
                else if (PowerHasKeyword(powerPrototypeId, DefaultPrototypeId.Mental))
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
                    messageList.Add(new(client, new(NetMessageEntityDestroy.CreateBuilder().SetIdEntity(bowlingBall.BaseData.EntityId).Build())));
                    client.CurrentGame.EntityManager.DestroyEntity(bowlingBall.BaseData.EntityId);
                }
            }

            // if (powerPrototypePath.Contains("TravelPower/")) 
            //    TrawerPower(client, tryActivatePower.PowerPrototypeId);

            //Logger.Trace(tryActivatePower.ToString());

            PowerResultArchive archive = new(tryActivatePower);
            if (archive.TargetId > 0)
                messageList.Add(new(client, new(NetMessagePowerResult.CreateBuilder()
                    .SetArchiveData(archive.Serialize())
                    .Build())));

            return messageList;
        }

        private IEnumerable<QueuedGameMessage> OnPowerRelease(FrontendClient client, NetMessagePowerRelease powerRelease)
        {
            Logger.Trace($"Received PowerRelease for {GameDatabase.GetPrototypeName((PrototypeId)powerRelease.PowerPrototypeId)}");
            return Array.Empty<QueuedGameMessage>();
        }

        private IEnumerable<QueuedGameMessage> OnTryCancelPower(FrontendClient client, NetMessageTryCancelPower tryCancelPower)
        {
            string powerPrototypePath = GameDatabase.GetPrototypeName((PrototypeId)tryCancelPower.PowerPrototypeId);
            Logger.Trace($"Received TryCancelPower for {powerPrototypePath}");

            if (powerPrototypePath.Contains("TravelPower/"))
                _eventManager.AddEvent(client, EventEnum.EndTravel, 0, tryCancelPower.PowerPrototypeId);

            return Array.Empty<QueuedGameMessage>();
        }

        private IEnumerable<QueuedGameMessage> OnTryCancelActivePower(FrontendClient client, NetMessageTryCancelActivePower tryCancelActivePower)
        {
            Logger.Trace("Received TryCancelActivePower");
            return Array.Empty<QueuedGameMessage>();
        }

        private IEnumerable<QueuedGameMessage> OnContinuousPowerUpdate(FrontendClient client, NetMessageContinuousPowerUpdateToServer continuousPowerUpdate)
        {
            var powerPrototypeId = (PrototypeId)continuousPowerUpdate.PowerPrototypeId;
            string powerPrototypePath = GameDatabase.GetPrototypeName(powerPrototypeId);
            Logger.Trace($"Received ContinuousPowerUpdate for {powerPrototypePath}");

            if (powerPrototypePath.Contains("TravelPower/"))
                HandleTravelPower(client, powerPrototypeId);
            // Logger.Trace(continuousPowerUpdate.ToString());

            return Array.Empty<QueuedGameMessage>();
        }

        #endregion
    }
}
