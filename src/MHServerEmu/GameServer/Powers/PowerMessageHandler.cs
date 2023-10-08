using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.GameData.Gpak.FileFormats;
using MHServerEmu.GameServer.GameData.Gpak;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Games;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Powers
{
    public class PowerMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private readonly EventManager _eventManager;

        public PowerMessageHandler(EventManager eventManager)
        {
            _eventManager = eventManager;
        }

        public List<QueuedGameMessage> HandleMessage(FrontendClient client, GameMessage message)
        {
            List<QueuedGameMessage> messageList = new();
            string powerPrototypePath;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageTryActivatePower:
                    /* ActivatePower using TryActivatePower data
                    var tryActivatePower = NetMessageTryActivatePower.ParseFrom(message.Content);
                    ActivatePowerArchive activatePowerArchive = new(tryActivatePowerMessage, client.LastPosition);
                    client.SendMessage(muxId, new(NetMessageActivatePower.CreateBuilder()
                        .SetArchiveData(ByteString.CopyFrom(activatePowerArchive.Encode()))
                        .Build()));
                    */

                    var tryActivatePower = NetMessageTryActivatePower.ParseFrom(message.Payload);

                    if (GameDatabase.TryGetPrototypePath(tryActivatePower.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received TryActivatePower for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received TryActivatePower for invalid prototype id {tryActivatePower.PowerPrototypeId}");

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
                        break;
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
                            .SetArchiveData(ByteString.CopyFrom(archive.Encode()))
                            .Build())));

                    break;

                case ClientToGameServerMessage.NetMessagePowerRelease:
                    var powerRelease = NetMessagePowerRelease.ParseFrom(message.Payload);

                    if (GameDatabase.TryGetPrototypePath(powerRelease.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received PowerRelease for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received PowerRelease for invalid prototype id {powerRelease.PowerPrototypeId}");

                    break;

                case ClientToGameServerMessage.NetMessageTryCancelPower:
                    var tryCancelPower = NetMessageTryCancelPower.ParseFrom(message.Payload);

                    if (GameDatabase.TryGetPrototypePath(tryCancelPower.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received TryCancelPower for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received TryCancelPower for invalid prototype id {tryCancelPower.PowerPrototypeId}");

                    if (powerPrototypePath.Contains("TravelPower/"))
                    {
                        _eventManager.AddEvent(client, EventEnum.EndTravel, 0, tryCancelPower.PowerPrototypeId);
                    }

                    break;

                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                    var tryCancelActivePower = NetMessageTryCancelActivePower.ParseFrom(message.Payload);
                    Logger.Trace("Received TryCancelActivePower");
                    break;

                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                    var continuousPowerUpdate = NetMessageContinuousPowerUpdateToServer.ParseFrom(message.Payload);

                    if (GameDatabase.TryGetPrototypePath(continuousPowerUpdate.PowerPrototypeId, out powerPrototypePath))
                        Logger.Trace($"Received ContinuousPowerUpdate for {powerPrototypePath}");
                    else
                        Logger.Trace($"Received ContinuousPowerUpdate for invalid prototype id {continuousPowerUpdate.PowerPrototypeId}");

                    if (powerPrototypePath.Contains("TravelPower/"))
                        HandleTravelPower(client, continuousPowerUpdate.PowerPrototypeId);
                    // Logger.Trace(continuousPowerUpdate.ToString());

                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }

            return messageList;
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
    }
}
