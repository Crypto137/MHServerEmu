using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Google.ProtocolBuffers;
using Google.ProtocolBuffers.Descriptors;
using System.Reflection.PortableExecutable;
using Gazillion;
using MHServerEmu.Services;
using MHServerEmu.Services.Implementations;

namespace MHServerEmu
{
    public class FrontendClient
    {
        private readonly Socket socket;
        private readonly NetworkStream stream;

        private readonly bool SimulateQueue = false;

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
            { PubSubServerTypes.PLAYERMGR_SERVER_MATCH, new PlayerMgrServerMatchService() },
            { PubSubServerTypes.GROUPING_MANAGER_FRONTEND, new GroupingManagerFrontendService() },
            { PubSubServerTypes.FAKE_CHAT_LOAD_TESTER, new FakeChatLoadTesterService() }
        };

        private Dictionary<ushort, PubSubServerTypes> MuxIdServerDict = new();
        private Dictionary<PubSubServerTypes, ushort> ServerMuxIdDict = new();

        public FrontendClient(Socket socket)
        {
            this.socket = socket;
            this.stream = new NetworkStream(socket);
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
                Console.WriteLine("[Frontend] Client disconnected");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Disconnect()
        {
            socket.Disconnect(false);
        }

        public void SendGameServiceMessage(Gazillion.PubSubServerTypes serverType, byte messageId, byte[] message, long timestamp = 0)
        {
            ServerPacket packet = new(ServerMuxIdDict[serverType], MuxCommand.Message);
            packet.WriteMessage(messageId, message, timestamp);
            Send(packet);
        }

        private void Handle(CodedInputStream stream)
        {
            ClientPacket packet = new(stream);

            // Respond
            ServerPacket response = null;
            byte[] responseMessage = Array.Empty<byte>();

            switch (packet.Command)
            {
                case MuxCommand.Connect:
                    Console.WriteLine($"[Frontend] Received connect for MuxId {packet.MuxId}");
                    packet.RawData.PrintHex();
                    Console.WriteLine($"[Frontend] Sending accept for MuxId {packet.MuxId}");
                    response = new(packet.MuxId, MuxCommand.Accept);
                    Send(response);
                    break;

                case MuxCommand.Accept:
                    Console.WriteLine($"[Frontend] Received accept for MuxId {packet.MuxId}");
                    packet.RawData.PrintHex();
                    break;

                case MuxCommand.Disconnect:
                    Console.WriteLine($"[Frontend] Received disconnect for MuxId {packet.MuxId}");
                    packet.RawData.PrintHex();
                    break;

                case MuxCommand.Insert:
                    Console.WriteLine($"[Frontend] Received insert for MuxId {packet.MuxId}");
                    packet.RawData.PrintHex();
                    break;

                case MuxCommand.Message:
                    Console.WriteLine($"[Frontend] Received message on MuxId {packet.MuxId} ({packet.BodyLength} bytes)");
                    packet.RawData.PrintHex();

                    // First byte is message id, second byte is protobuf size as uint8
                    byte[] message = new byte[packet.Body[1]];
                    for (int i = 0; i < message.Length; i++)
                    {
                        message[i] = packet.Body[i + 2];
                    }

                    if (MuxIdServerDict.ContainsKey(packet.MuxId))
                    {
                        Console.WriteLine($"[Frontend] Routing message to {MuxIdServerDict[packet.MuxId]}");
                        ServiceDict[MuxIdServerDict[packet.MuxId]].Handle(this, packet.Body[0], message);
                    }
                    else
                    {
                        //Console.WriteLine($"[Frontend] MuxId is not assigned to a server, handling on frontend");
                        switch ((FrontendProtocolMessage)packet.Body[0])
                        {
                            case FrontendProtocolMessage.ClientCredentials:
                                Console.WriteLine($"[Frontend] Received ClientCredentials message:");
                                Gazillion.ClientCredentials clientCredentials = Gazillion.ClientCredentials.ParseFrom(message);
                                Console.Write(clientCredentials.ToString());
                                Console.Write($"[Frontend] Decrypted token: ");
                                Cryptography.SetIV(clientCredentials.Iv.ToByteArray());
                                byte[] decryptedToken = Cryptography.DecryptSessionToken(clientCredentials);
                                decryptedToken.PrintHex();

                                // Generate response
                                if (SimulateQueue)
                                {
                                    Console.WriteLine("[Frontend] Responding with LoginQueueStatus message");

                                    responseMessage = Gazillion.LoginQueueStatus.CreateBuilder()
                                        .SetPlaceInLine(1337)
                                        .SetNumberOfPlayersInLine(9001)
                                        .Build().ToByteArray();

                                    response = new(packet.MuxId, MuxCommand.Message);
                                    response.WriteMessage((byte)FrontendProtocolMessage.LoginQueueStatus, responseMessage);
                                    Send(response);
                                }
                                else
                                {
                                    Console.WriteLine("[Frontend] Responding with SessionEncryptionChanged message");

                                    responseMessage = Gazillion.SessionEncryptionChanged.CreateBuilder()
                                        .SetRandomNumberIndex(1)
                                        .SetEncryptedRandomNumber(ByteString.Empty)
                                        .Build().ToByteArray();

                                    response = new(packet.MuxId, MuxCommand.Message);
                                    response.WriteMessage((byte)FrontendProtocolMessage.SessionEncryptionChanged, responseMessage);
                                    Send(response);
                                }

                                break;

                            case FrontendProtocolMessage.InitialClientHandshake:
                                Console.WriteLine($"[Frontend] Received InitialClientHandshake message:");
                                Gazillion.InitialClientHandshake initialClientHandshake = Gazillion.InitialClientHandshake.ParseFrom(message);
                                Console.Write(initialClientHandshake.ToString());

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
                                Console.WriteLine($"[Frontend] Received unknown message id {packet.Body[0]}");
                                break;
                        }
                    }

                    break;

            }
        }

        private void Send(ServerPacket packet)
        {
            byte[] data = packet.Data;

            Console.WriteLine("[Frontend] Sending server response:");
            data.PrintHex();

            this.stream.Write(data, 0, data.Length);
        }
    }
}
