using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Services.Implementations
{
    public class GameInstanceService : GameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public GameInstanceService(GameServerManager gameServerManager) : base(gameServerManager)
        {
        }

        public override void Handle(FrontendClient client, ushort muxId, byte messageId, byte[] message)
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

                client.SendGameMessage(muxId, (byte)GameServerToClientMessage.NetMessageReadyForTimeSync, response);

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
                    .SetGameTimeServerReceived(_gameServerManager.GetGameTime())
                    .SetGameTimeServerSent(_gameServerManager.GetGameTime())

                    .SetDateTimeClientSent(parsedMessage.DateTimeClientSent)
                    .SetDateTimeServerReceived(_gameServerManager.GetDateTime())
                    .SetDateTimeServerSent(_gameServerManager.GetDateTime())

                    .SetDialation(0.0f)
                    .SetGametimeDialationStarted(_gameServerManager.GetGameTime())
                    .SetDatetimeDialationStarted(_gameServerManager.GetDateTime())
                    .Build().ToByteArray();

                client.SendGameMessage(muxId, (byte)GameServerToClientMessage.NetMessageSyncTimeReply, response);
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
                Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)messageId} (id {messageId})");
            }
        }
    }
}
