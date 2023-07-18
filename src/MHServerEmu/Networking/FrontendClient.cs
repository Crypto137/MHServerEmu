using System.Net.Sockets;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Services;
using MHServerEmu.Services.Implementations;

namespace MHServerEmu.Networking
{
    public class FrontendClient
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Socket socket;
        private readonly NetworkStream stream;

        private readonly bool SimulateQueue = true;

        private static Dictionary<PubSubServerTypes, GameService> ServiceDict = new()
        {
            { PubSubServerTypes.FRONTEND_SERVER, new FrontendServerService() },
            { PubSubServerTypes.GAME_INSTANCE_SERVER_USER, new GameInstanceServerUserService() },
            { PubSubServerTypes.GAME_INSTANCE_SERVER_PLAYERMGR, new GameInstanceServerPlayerMgrService() },
            { PubSubServerTypes.GAME_INSTANCE_SERVER_GROUPING, new GameInstanceServerGroupingService() },
            { PubSubServerTypes.GAME_INSTANCE_SERVER_METRICS, new GameInstanceServerMetricsService() },
            { PubSubServerTypes.GAME_INSTANCE_SERVER_BILLING, new GameInstanceServerBillingService() },
            { PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND, new PlayerMgrServerFrontendService() },
            { PubSubServerTypes.PLAYERMGR_SERVER_SITEMGR_CONTROL, new PlayerMgrServerSiteMgrControlService() },
            { PubSubServerTypes.PLAYERMGR_SERVER_SOCIAL_COMMON, new PlayerMgrServerSocialCommonService() },
            //{ PubSubServerTypes.PLAYERMGR_SERVER_MATCH, new PlayerMgrServerMatchService() },
            { PubSubServerTypes.GROUPING_MANAGER_FRONTEND, new GroupingManagerFrontendService() },
            { PubSubServerTypes.FAKE_CHAT_LOAD_TESTER, new FakeChatLoadTesterService() }
        };

        private Dictionary<ushort, PubSubServerTypes> MuxIdServerDict = new();
        private Dictionary<PubSubServerTypes, ushort> ServerMuxIdDict = new();

        public FrontendClient(Socket socket)
        {
            this.socket = socket;
            stream = new NetworkStream(socket);
        }

        public void Run()
        {
            try
            {
                CodedInputStream stream = CodedInputStream.CreateInstance(this.stream);

                while (!stream.IsAtEnd)
                {
                    Handle(stream);
                }
                Logger.Info("Client disconnected");
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        public void Disconnect()
        {
            socket.Disconnect(false);
        }

        public void SendGameServiceMessage(PubSubServerTypes serverType, byte messageId, byte[] message, long timestamp = 0)
        {
            ServerPacket packet = new(ServerMuxIdDict[serverType], MuxCommand.Message);
            packet.WriteMessage(messageId, message, timestamp);
            Send(packet);
        }

        public void SendPacketFromFile(string fileName)
        {
            string path = $"{Directory.GetCurrentDirectory()}\\packets\\{fileName}";

            if (File.Exists(path))
            {
                Logger.Info($"Sending {fileName}");
                SendRaw(File.ReadAllBytes(path));
            }
            else
            {
                Logger.Warn($"{fileName} not found");
            }
        }

        private void Handle(CodedInputStream stream)
        {
            ClientPacket packet = new(stream);
            Logger.Trace($"IN: {packet.RawData.ToHexString()}");

            // Respond
            ServerPacket response = null;
            byte[] responseMessage = Array.Empty<byte>();

            switch (packet.Command)
            {
                case MuxCommand.Connect:
                    Logger.Info($"Received connect for MuxId {packet.MuxId}");
                    Logger.Info($"Sending accept for MuxId {packet.MuxId}");
                    response = new(packet.MuxId, MuxCommand.Accept);
                    Send(response);
                    break;

                case MuxCommand.Accept:
                    Logger.Info($"Received accept for MuxId {packet.MuxId}");
                    break;

                case MuxCommand.Disconnect:
                    Logger.Info($"Received disconnect for MuxId {packet.MuxId}");
                    break;

                case MuxCommand.Insert:
                    Logger.Info($"Received insert for MuxId {packet.MuxId}");
                    break;

                case MuxCommand.Message:
                    Logger.Info($"Received message on MuxId {packet.MuxId} ({packet.BodyLength} bytes)");

                    // First byte is message id, second byte is protobuf size as uint8
                    byte[] message = new byte[packet.Body[1]];
                    for (int i = 0; i < message.Length; i++)
                    {
                        message[i] = packet.Body[i + 2];
                    }

                    if (MuxIdServerDict.ContainsKey(packet.MuxId))
                    {
                        Logger.Info($"Routing message to {MuxIdServerDict[packet.MuxId]}");
                        ServiceDict[MuxIdServerDict[packet.MuxId]].Handle(this, packet.Body[0], message);
                    }
                    else
                    {
                        //Logger.Info($"[Frontend] MuxId is not assigned to a server, handling on frontend");
                        switch ((FrontendProtocolMessage)packet.Body[0])
                        {
                            case FrontendProtocolMessage.ClientCredentials:
                                Logger.Info($"Received ClientCredentials message:");
                                ClientCredentials clientCredentials = ClientCredentials.ParseFrom(message);
                                Logger.Trace(clientCredentials.ToString());
                                Cryptography.SetIV(clientCredentials.Iv.ToByteArray());
                                byte[] decryptedToken = Cryptography.DecryptSessionToken(clientCredentials);
                                Logger.Trace($"Decrypted token: {decryptedToken.ToHexString()}");

                                // Generate response
                                if (SimulateQueue)
                                {
                                    Logger.Info("Responding with LoginQueueStatus message");

                                    responseMessage = LoginQueueStatus.CreateBuilder()
                                        .SetPlaceInLine(1337)
                                        .SetNumberOfPlayersInLine(9001)
                                        .Build().ToByteArray();

                                    response = new(packet.MuxId, MuxCommand.Message);
                                    response.WriteMessage((byte)FrontendProtocolMessage.LoginQueueStatus, responseMessage);
                                    Send(response);
                                }
                                else
                                {
                                    Logger.Info("Responding with SessionEncryptionChanged message");

                                    responseMessage = SessionEncryptionChanged.CreateBuilder()
                                        .SetRandomNumberIndex(1)
                                        .SetEncryptedRandomNumber(ByteString.Empty)
                                        .Build().ToByteArray();

                                    response = new(packet.MuxId, MuxCommand.Message);
                                    response.WriteMessage((byte)FrontendProtocolMessage.SessionEncryptionChanged, responseMessage);
                                    Send(response);
                                }

                                break;

                            case FrontendProtocolMessage.InitialClientHandshake:
                                Logger.Info($"Received InitialClientHandshake message:");
                                InitialClientHandshake initialClientHandshake = InitialClientHandshake.ParseFrom(message);
                                Logger.Trace(initialClientHandshake.ToString());

                                MuxIdServerDict[packet.MuxId] = initialClientHandshake.ServerType;
                                ServerMuxIdDict[initialClientHandshake.ServerType] = packet.MuxId;

                                /*
                                if (initialClientHandshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND)
                                {
                                    responseMessage = NetMessageSelectStartingAvatarForNewPlayer.CreateBuilder()
                                        .Build().ToByteArray();

                                    response = new(1, MuxCommand.Message);
                                    response.WriteMessage((byte)GameServerToClientMessage.NetMessageSelectStartingAvatarForNewPlayer, responseMessage, true);
                                    Send(response);
                                }
                                */

                                break;

                            default:
                                Logger.Warn($"Received unknown message id {packet.Body[0]}");
                                break;
                        }
                    }

                    break;

            }
        }

        private void Send(ServerPacket packet)
        {
            byte[] data = packet.Data;
            Logger.Trace($"OUT: {data.ToHexString()}");
            stream.Write(data, 0, data.Length);
        }

        private void SendRaw(byte[] data)
        {
            Logger.Trace($"OUT: raw {data.Length} bytes");
            stream.Write(data, 0, data.Length);
        }
    }
}
