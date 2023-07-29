using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Data.Enums;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Services.Implementations
{
    public class FrontendService : GameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private bool _simulateQueue = false;

        public FrontendService(GameServerManager gameServerManager) : base(gameServerManager)
        {
        }

        public override void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages)
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
                        if (_simulateQueue)
                        {
                            Logger.Info("Responding with LoginQueueStatus message");

                            byte[] response = LoginQueueStatus.CreateBuilder()
                                .SetPlaceInLine(1337)
                                .SetNumberOfPlayersInLine(9001)
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
                                .SetFromPlayerName("MHServerEmu")
                                .SetTheMessage(ChatMessage.CreateBuilder().SetBody("Hello world 2023"))
                                .SetPrestigeLevel(6)
                                .Build().ToByteArray();

                            client.SendMessage(2, new(GroupingManagerMessage.ChatBroadcastMessage, chatBroadcastMessage));

                            // Send hardcoded region loading data after initial handshakes finish
                            if (client.StartingRegion != RegionPrototype.AvengersTower &&
                                client.StartingRegion != RegionPrototype.DangerRoom &&
                                client.StartingRegion != RegionPrototype.MidtownPatrolCosmic)
                            {
                                Logger.Error($"Trying to load region {client.StartingRegion} that has no data, falling back to AvengersTower");
                                client.StartingRegion = RegionPrototype.AvengersTower;
                            }

                            client.SendMultipleMessages(1, RegionLoader.GetBeginLoadingMessages(client.StartingRegion, client.StartingAvatar));
                        }

                        break;

                    default:
                        Logger.Warn($"Received unhandled message {(FrontendProtocolMessage)message.Id} (id {message.Id})");
                        break;
                }
            }
        }
    }
}
