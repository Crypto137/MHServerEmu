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

                    /*
                    case ClientToGameServerMessage.NetMessagePlayerTradeCancel:
                        Logger.Info($"Received NetMessagePlayerTradeCancel message");

                        if (client.InitReceivedFirstNetMessagePlayerTradeCancel == false)
                        {
                            client.SendMultipleMessages(1, PacketHelper.LoadMessagesFromPacketFile("NetMessagePlayerTradeStatus.bin"));
                            client.InitReceivedFirstNetMessagePlayerTradeCancel = true;
                        }
                        else if (client.InitReceivedSecondNetMessagePlayerTradeCancel == false)
                        {
                            client.SendMultipleMessages(1, PacketHelper.LoadMessagesFromPacketFile("NetMessagePlayerTradeStatus2.bin"));
                            client.SendMultipleMessages(1, PacketHelper.LoadMessagesFromPacketFile("NetMessageEntityCreate.bin"));
                            client.InitReceivedSecondNetMessagePlayerTradeCancel = true;
                        }
                        break;

                    case ClientToGameServerMessage.NetMessageVanityTitleSelect:
                        Logger.Info($"Received NetMessagePlayerTradeCancel message");

                        if (client.InitReceivedFirstNetMessageVanityTitleSelect == false)
                        {
                            client.SendMultipleMessages(1, PacketHelper.LoadMessagesFromPacketFile("NetMessageAddCondition.bin"));
                            client.InitReceivedFirstNetMessageVanityTitleSelect = true;
                        }
                        break;

                    case ClientToGameServerMessage.NetMessageRequestInterestInInventory:
                        Logger.Info($"Received NetMessageRequestInterestInInventory message");
                        if (client.InitReceivedFirstNetMessageRequestInterestInInventory == false)
                        {
                            client.SendMultipleMessages(1, PacketHelper.LoadMessagesFromPacketFile("NetMessageEntityCreate2.bin"));
                            client.InitReceivedFirstNetMessageRequestInterestInInventory = true;
                        }
                        break;

                    */

                    case ClientToGameServerMessage.NetMessageCellLoaded:
                        Logger.Info($"Received NetMessageCellLoaded message");
                        
                        if (client.InitReceivedFirstNetMessageCellLoaded == false)
                        {
                            List<GameMessage> messageList = new();

                            byte[] blackCatEntityEnterGameWorld = {
                                0x01, 0xB2, 0xF8, 0xFD, 0x06, 0xA0, 0x21, 0xF0, 0xA3, 0x01, 0xBC, 0x40,
                                0x90, 0x2E, 0x91, 0x03, 0xBC, 0x05, 0x00, 0x00, 0x01
                            };

                            messageList.Add(new((byte)GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                                NetMessageEntityEnterGameWorld.CreateBuilder().SetArchiveData(ByteString.CopyFrom(blackCatEntityEnterGameWorld)).Build().ToByteArray()));

                            byte[] waypointEntityEnterGameWorld = {
                                0x01, 0x0C, 0x02, 0x80, 0x43, 0xE0, 0x6B, 0xD8, 0x2A, 0xC8, 0x01
                            };

                            messageList.Add(new((byte)GameServerToClientMessage.NetMessageEntityEnterGameWorld,
                                NetMessageEntityEnterGameWorld.CreateBuilder().SetArchiveData(ByteString.CopyFrom(waypointEntityEnterGameWorld)).Build().ToByteArray()));

                            messageList.Add(new((byte)GameServerToClientMessage.NetMessageDequeueLoadingScreen, NetMessageDequeueLoadingScreen.DefaultInstance.ToByteArray()));

                            client.SendMultipleMessages(1, messageList.ToArray());

                            //client.SendMultipleMessages(1, PacketHelper.LoadMessagesFromPacketFile("NetMessageEntityCreate3.bin"));
                            client.InitReceivedFirstNetMessageCellLoaded = true;
                        }

                        break;

                    default:
                        Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                        break;
                }
            }
        }
    }
}
