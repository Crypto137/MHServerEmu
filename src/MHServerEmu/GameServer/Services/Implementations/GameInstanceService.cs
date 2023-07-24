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

        public override void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages)
            {
                byte[] response;
                switch ((ClientToGameServerMessage)message.Id)
                {
                    case ClientToGameServerMessage.NetMessageReadyForGameJoin:

                        Logger.Info($"Received NetMessageReadyForGameJoin message");
                        try
                        {
                            var parsedReadyForGameJoin = NetMessageReadyForGameJoin.ParseFrom(message.Content);
                            Logger.Trace(parsedReadyForGameJoin.ToString());
                        }
                        catch (InvalidProtocolBufferException e)
                        {
                            Logger.Warn($"Failed to parse NetMessageReadyForGameJoin message: {e.Message}");
                        }

                        Logger.Info("Responding with NetMessageReadyAndLoggedIn message");
                        response = NetMessageReadyAndLoggedIn.CreateBuilder()
                            .Build().ToByteArray();
                        client.SendMessage(muxId, new((byte)GameServerToClientMessage.NetMessageReadyAndLoggedIn, response));

                        Logger.Info("Responding with NetMessageInitialTimeSync message");
                        response = NetMessageInitialTimeSync.CreateBuilder()
                            .SetGameTimeServerSent(161351679299542)     // dumped
                            .SetDateTimeServerSent(1509657957345525)    // dumped
                            .Build().ToByteArray();
                        client.SendMessage(muxId, new((byte)GameServerToClientMessage.NetMessageInitialTimeSync, response));

                        break;

                    case ClientToGameServerMessage.NetMessageSyncTimeRequest:
                        Logger.Info($"Received NetMessageSyncTimeRequest message");
                        var parsedSyncTimeRequestMessage = NetMessageSyncTimeRequest.ParseFrom(message.Content);
                        Logger.Trace(parsedSyncTimeRequestMessage.ToString());

                        //Logger.Info("Responding with NetMessageSyncTimeReply");

                        response = NetMessageSyncTimeReply.CreateBuilder()
                            .SetGameTimeClientSent(parsedSyncTimeRequestMessage.GameTimeClientSent)
                            .SetGameTimeServerReceived(_gameServerManager.GetGameTime())
                            .SetGameTimeServerSent(_gameServerManager.GetGameTime())

                            .SetDateTimeClientSent(parsedSyncTimeRequestMessage.DateTimeClientSent)
                            .SetDateTimeServerReceived(_gameServerManager.GetDateTime())
                            .SetDateTimeServerSent(_gameServerManager.GetDateTime())

                            .SetDialation(0.0f)
                            .SetGametimeDialationStarted(_gameServerManager.GetGameTime())
                            .SetDatetimeDialationStarted(_gameServerManager.GetDateTime())
                            .Build().ToByteArray();

                        //client.SendGameServiceMessage(ServerType, (byte)GameServerToClientMessage.NetMessageSyncTimeReply, response);
                        break;

                    case ClientToGameServerMessage.NetMessagePing:
                        Logger.Info($"Received NetMessagePing message");
                        var parsedPingMessage = NetMessagePing.ParseFrom(message.Content);
                        //Logger.Trace(parsedPingMessage.ToString());
                        break;

                    case ClientToGameServerMessage.NetMessageCellLoaded:
                        Logger.Info($"Received NetMessageCellLoaded message");
                        client.SendMultipleMessages(1, RegionLoader.GetFinishLoadingMessages(client.StartingRegion));

                        break;

                    case ClientToGameServerMessage.NetMessageUseWaypoint:
                        Logger.Info($"Received NetMessageUseWaypoint message");
                        var useWaypointMessage = NetMessageUseWaypoint.ParseFrom(message.Content);
                        Logger.Trace(useWaypointMessage.ToString());

                        break;

                    default:
                        Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                        break;
                }
            }
        }
    }
}
