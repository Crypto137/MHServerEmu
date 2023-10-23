using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Networking;

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
                    return OnTryActivatePower(client, message.Deserialize<NetMessageTryActivatePower>());
                
                case ClientToGameServerMessage.NetMessagePowerRelease:
                    return OnPowerRelease(client, message.Deserialize<NetMessagePowerRelease>());

                case ClientToGameServerMessage.NetMessageTryCancelPower:
                    return OnTryCancelPower(client, message.Deserialize<NetMessageTryCancelPower>());

                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                    return OnTryCancelActivePower(client, message.Deserialize<NetMessageTryCancelActivePower>());

                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                    return OnContinuousPowerUpdate(client, message.Deserialize<NetMessageContinuousPowerUpdateToServer>());

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    return Array.Empty<QueuedGameMessage>();
            }
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
                    _eventManager.AddEvent(client, EventEnum.StartTravel, 100 - delta, PowerId);
                    break;
                case (ulong)PowerPrototypes.AntMan.AntmanFlight:
                    _eventManager.AddEvent(client, EventEnum.StartTravel, 210 - delta, PowerId);
                    break;
                case (ulong)PowerPrototypes.Thing.ThingFlight:
                    _eventManager.AddEvent(client, EventEnum.StartTravel, 235 - delta, PowerId);
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
            string powerPrototypePath = GameDatabase.GetPrototypeName(tryActivatePower.PowerPrototypeId);
            Logger.Trace($"Received TryActivatePower for {powerPrototypePath}");

            if (powerPrototypePath.Contains("ThrowablePowers/"))
            {
                Logger.Trace($"AddEvent EndThrowing for {tryActivatePower.PowerPrototypeId}");
                PrototypeEntry Power = tryActivatePower.PowerPrototypeId.GetPrototype().GetEntry(BlueprintId.Power);
                long animationTimeMS = 1100;
                if (Power != null)
                {
                    PrototypeEntryElement AnimationTimeMS = Power.GetField(FieldId.AnimationTimeMS);
                    if (AnimationTimeMS != null) animationTimeMS = (long)AnimationTimeMS.Value;
                }
                _eventManager.AddEvent(client, EventEnum.EndThrowing, animationTimeMS, tryActivatePower.PowerPrototypeId);
                return messageList;
            }
            else if (powerPrototypePath.Contains("EmmaFrost/"))
            {
                if (PowerHasKeyword(tryActivatePower.PowerPrototypeId, BlueprintId.DiamondFormActivatePower))
                    _eventManager.AddEvent(client, EventEnum.DiamondFormActivate, 0, tryActivatePower.PowerPrototypeId);
                else if (PowerHasKeyword(tryActivatePower.PowerPrototypeId, BlueprintId.Mental))
                    _eventManager.AddEvent(client, EventEnum.DiamondFormDeactivate, 0, tryActivatePower.PowerPrototypeId);
            }
            else if (tryActivatePower.PowerPrototypeId == (ulong)PowerPrototypes.Magik.Ultimate)
            {
                _eventManager.AddEvent(client, EventEnum.StartMagikUltimate, 0, tryActivatePower.TargetPosition);
                _eventManager.AddEvent(client, EventEnum.EndMagikUltimate, 20000, 0u);
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
            Logger.Trace($"Received PowerRelease for {GameDatabase.GetPrototypeName(powerRelease.PowerPrototypeId)}");
            return Array.Empty<QueuedGameMessage>();
        }

        private IEnumerable<QueuedGameMessage> OnTryCancelPower(FrontendClient client, NetMessageTryCancelPower tryCancelPower)
        {
            string powerPrototypePath = GameDatabase.GetPrototypeName(tryCancelPower.PowerPrototypeId);
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
            string powerPrototypePath = GameDatabase.GetPrototypeName(continuousPowerUpdate.PowerPrototypeId);
            Logger.Trace($"Received ContinuousPowerUpdate for {powerPrototypePath}");

            if (powerPrototypePath.Contains("TravelPower/"))
                HandleTravelPower(client, continuousPowerUpdate.PowerPrototypeId);
            // Logger.Trace(continuousPowerUpdate.ToString());

            return Array.Empty<QueuedGameMessage>();
        }

        #endregion
    }
}
