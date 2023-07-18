using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Networking;

namespace MHServerEmu.Services.Implementations
{
    public class PlayerMgrServerFrontendService : GameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private long _startTime;    // Used for calculating game time

        public PlayerMgrServerFrontendService()
        {
            ServerType = Gazillion.PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND;
            _startTime = GetDateTime();
        }

        public override void Handle(FrontendClient client, byte messageId, byte[] message)
        {
            if (messageId == (byte)ClientToGameServerMessage.NetMessageReadyForGameJoin)
            {
                Logger.Info($"Received NetMessageReadyForGameJoin message");
                try
                {
                    var parsedMessage = NetMessageReadyForGameJoin.ParseFrom(message);
                    Logger.Trace(parsedMessage.ToString());
                }
                catch (InvalidProtocolBufferException e)
                {
                    Logger.Warn($"Failed to parse NetMessageReadyForGameJoin message: {e.Message}");
                }

                /*
                Logger.Info("Responding with NetMessageReadyAndLoggedIn message");
                byte[] response = NetMessageReadyAndLoggedIn.CreateBuilder()
                    .Build().ToByteArray();
                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessageReadyAndLoggedIn, response);
                */

                Logger.Info("Responding with NetMessageReadyForTimeSync message");
                byte[] response = NetMessageReadyForTimeSync.CreateBuilder()
                    .Build().ToByteArray();

                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessageReadyForTimeSync, response);

                /*
                Logger.Info("Responding with NetMessageSelectStartingAvatarForNewPlayer message");
                byte[] response = NetMessageSelectStartingAvatarForNewPlayer.CreateBuilder()
                    .Build().ToByteArray();
                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessageSelectStartingAvatarForNewPlayer, response, GetGameTime());
                */

            }
            else if (messageId == (byte)ClientToGameServerMessage.NetMessageSyncTimeRequest)
            {
                Logger.Info($"Received NetMessageSyncTimeRequest message");
                var parsedMessage = NetMessageSyncTimeRequest.ParseFrom(message);
                Logger.Trace(parsedMessage.ToString());

                Logger.Info("Responding with NetMessageSyncTimeReply");

                byte[] response = NetMessageSyncTimeReply.CreateBuilder()
                    .SetGameTimeClientSent(parsedMessage.GameTimeClientSent)
                    .SetGameTimeServerReceived(GetGameTime())
                    .SetGameTimeServerSent(GetGameTime())

                    .SetDateTimeClientSent(parsedMessage.DateTimeClientSent)
                    .SetDateTimeServerReceived(GetDateTime())
                    .SetDateTimeServerSent(GetDateTime())

                    .SetDialation(0.0f)
                    .SetGametimeDialationStarted(GetGameTime())
                    .SetDatetimeDialationStarted(GetDateTime())
                    .Build().ToByteArray();

                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessageSyncTimeReply, response);
            }
            else if (messageId == (byte)ClientToGameServerMessage.NetMessagePing)
            {
                Logger.Info($"Received NetMessagePing message");

                var parsedMessage = NetMessagePing.ParseFrom(message);
                Logger.Trace(parsedMessage.ToString());

                /*
                byte[] response = NetMessagePingResponse.CreateBuilder()
                    .SetDisplayOutput(false)
                    .SetRequestSentClientTime(parsedMessage.SendClientTime)
                    .SetRequestSentGameTime(parsedMessage.SendGameTime)
                    .Build().ToByteArray();

                client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessagePingResponse, response);
                */
            }
            else
            {
                Logger.Warn($"Received unknown message id {messageId}");
            }
        }

        private long GetDateTime()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() * 1000;
        }

        private long GetGameTime()
        {
            return GetDateTime() - _startTime;
        }
    }
}
