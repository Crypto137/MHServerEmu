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
            byte[] response;
            switch ((ClientToGameServerMessage)messageId)
            {
                case ClientToGameServerMessage.NetMessageReadyForGameJoin:

                    Logger.Info($"Received NetMessageReadyForGameJoin message");
                    try
                    {
                        var parsedReadyForGameJoin = NetMessageReadyForGameJoin.ParseFrom(message);
                        Logger.Trace(parsedReadyForGameJoin.ToString());
                    }
                    catch (InvalidProtocolBufferException e)
                    {
                        Logger.Warn($"Failed to parse NetMessageReadyForGameJoin message: {e.Message}");
                    }

                    Logger.Info("Responding with NetMessageReadyAndLoggedIn message");
                    response = NetMessageReadyAndLoggedIn.CreateBuilder()
                        .Build().ToByteArray();
                    client.SendGameMessage(muxId, (byte)GameServerToClientMessage.NetMessageReadyAndLoggedIn, response);

                    Logger.Info("Responding with NetMessageInitialTimeSync message");
                    response = NetMessageInitialTimeSync.CreateBuilder()
                        .SetGameTimeServerSent(161351679299542)     // dumped
                        .SetDateTimeServerSent(1509657957345525)    // dumped
                        .Build().ToByteArray();
                    client.SendGameMessage(muxId, (byte)GameServerToClientMessage.NetMessageInitialTimeSync, response, true);

                    break;

                case ClientToGameServerMessage.NetMessageSyncTimeRequest:
                    Logger.Info($"Received NetMessageSyncTimeRequest message");
                    var parsedSyncTimeRequestMessage = NetMessageSyncTimeRequest.ParseFrom(message);
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
                    var parsedPingMessage = NetMessagePing.ParseFrom(message);
                    //Logger.Trace(parsedPingMessage.ToString());
                    break;

                case ClientToGameServerMessage.NetMessagePlayerTradeCancel:
                    Logger.Info($"Received NetMessagePlayerTradeCancel message");

                    if (client.InitReceivedFirstNetMessagePlayerTradeCancel == false)
                    {
                        client.SendPacketFromFile("NetMessagePlayerTradeStatus.bin");
                        client.InitReceivedFirstNetMessagePlayerTradeCancel = true;
                    }
                    else if (client.InitReceivedSecondNetMessagePlayerTradeCancel == false)
                    {
                        client.SendPacketFromFile("NetMessagePlayerTradeStatus2.bin");
                        client.SendPacketFromFile("NetMessageEntityCreate.bin");
                        client.InitReceivedSecondNetMessagePlayerTradeCancel = true;
                    }
                    break;

                case ClientToGameServerMessage.NetMessageVanityTitleSelect:
                    Logger.Info($"Received NetMessagePlayerTradeCancel message");

                    if (client.InitReceivedFirstNetMessageVanityTitleSelect == false)
                    {
                        client.SendPacketFromFile("NetMessageAddCondition.bin");
                        client.InitReceivedFirstNetMessageVanityTitleSelect = true;
                    }
                    break;

                case ClientToGameServerMessage.NetMessageRequestInterestInInventory:
                    Logger.Info($"Received NetMessageRequestInterestInInventory message");
                    if (client.InitReceivedFirstNetMessageRequestInterestInInventory == false)
                    {
                        client.SendPacketFromFile("NetMessageEntityCreate2.bin");
                        client.InitReceivedFirstNetMessageRequestInterestInInventory = true;
                    }
                    break;

                case ClientToGameServerMessage.NetMessageCellLoaded:
                    Logger.Info($"Received NetMessageCellLoaded message");

                    if (client.InitReceivedFirstNetMessageCellLoaded == false)
                    {
                        client.SendPacketFromFile("NetMessageEntityCreate3.bin");
                        client.InitReceivedFirstNetMessageCellLoaded = true;
                    }
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)messageId} (id {messageId})");
                    break;
            }
        }
    }
}
