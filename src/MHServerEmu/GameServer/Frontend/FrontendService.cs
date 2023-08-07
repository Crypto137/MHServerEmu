using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Frontend
{
    public class FrontendService : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private GameServerManager _gameServerManager;

        public FrontendService(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials:
                    Logger.Info($"Received ClientCredentials message on muxId {muxId}:");
                    ClientCredentials clientCredentials = ClientCredentials.ParseFrom(message.Content);
                    Logger.Trace(clientCredentials.ToString());
                    Cryptography.SetIV(clientCredentials.Iv.ToByteArray());
                    byte[] decryptedToken = Cryptography.DecryptSessionToken(clientCredentials);
                    Logger.Trace($"Decrypted token: {decryptedToken.ToHexString()}");

                    // Generate response
                    if (ConfigManager.Frontend.SimulateQueue)
                    {
                        Logger.Info("Responding with LoginQueueStatus message");

                        byte[] response = LoginQueueStatus.CreateBuilder()
                            .SetPlaceInLine(ConfigManager.Frontend.QueuePlaceInLine)
                            .SetNumberOfPlayersInLine(ConfigManager.Frontend.QueueNumberOfPlayersInLine)
                            .Build().ToByteArray();

                        client.SendMessage(muxId, new(FrontendProtocolMessage.LoginQueueStatus, response));
                    }
                    else
                    {
                        Logger.Info("Responding with SessionEncryptionChanged message");

                        byte[] response = SessionEncryptionChanged.CreateBuilder()
                            .SetRandomNumberIndex(1)
                            .SetEncryptedRandomNumber(ByteString.Empty)
                            .Build().ToByteArray();

                        client.SendMessage(muxId, new(FrontendProtocolMessage.SessionEncryptionChanged, response));
                    }

                    break;

                case FrontendProtocolMessage.InitialClientHandshake:
                    Logger.Info($"Received InitialClientHandshake message on muxId {muxId}:");
                    InitialClientHandshake initialClientHandshake = InitialClientHandshake.ParseFrom(message.Content);
                    Logger.Trace(initialClientHandshake.ToString());

                    if (initialClientHandshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND)
                    {
                        client.FinishedPlayerMgrServerFrontendHandshake = true;
                    }
                    else if (initialClientHandshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND)
                    {
                        client.FinishedGroupingManagerFrontendHandshake = true;

                        client.SendMessage(muxId, new(GameServerToClientMessage.NetMessageQueueLoadingScreen,
                            NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId(0).Build().ToByteArray()));

                        client.SendMultipleMessages(1, PacketHelper.LoadMessagesFromPacketFile("NetMessageAchievementDatabaseDump.bin"));
                        //client.SendMultipleMessages(1, PacketHelper.LoadMessagesFromPacketFile("NetMessageEntityEnterGameWorld.bin"));

                        var chatBroadcastMessage = ChatBroadcastMessage.CreateBuilder()
                            .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                            //.SetFromPlayerName("System")
                            //.SetTheMessage(ChatMessage.CreateBuilder().SetBody("Operation Omega is now active. Will you fight to defend S.H.I.E.L.D.?  Or will you support the evil HYDRA?"))
                            .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                            .SetTheMessage(ChatMessage.CreateBuilder().SetBody(ConfigManager.GroupingManager.MotdText))
                            .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                            .Build().ToByteArray();

                        client.SendMessage(2, new(GroupingManagerMessage.ChatBroadcastMessage, chatBroadcastMessage));

                        // Send hardcoded region loading data after initial handshakes finish
                        client.SendMultipleMessages(1, RegionLoader.GetBeginLoadingMessages(client.CurrentRegion, client.CurrentAvatar));
                        client.IsLoading = true;
                    }

                    break;

                default:
                    Logger.Warn($"Received unhandled message {(FrontendProtocolMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }
    }
}
