using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
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

        public override void Handle(FrontendClient client, ushort muxId, byte messageId, byte[] message)
        {
            switch ((FrontendProtocolMessage)messageId)
            {
                case FrontendProtocolMessage.ClientCredentials:
                    Logger.Info($"Received ClientCredentials message on muxId {muxId}:");
                    ClientCredentials clientCredentials = ClientCredentials.ParseFrom(message);
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

                        client.SendGameMessage(muxId, (byte)FrontendProtocolMessage.LoginQueueStatus, response);
                    }
                    else
                    {
                        Logger.Info("Responding with SessionEncryptionChanged message");

                        byte[] response = SessionEncryptionChanged.CreateBuilder()
                            .SetRandomNumberIndex(1)
                            .SetEncryptedRandomNumber(ByteString.Empty)
                            .Build().ToByteArray();

                        client.SendGameMessage(muxId, (byte)FrontendProtocolMessage.SessionEncryptionChanged, response);
                    }

                    break;

                case FrontendProtocolMessage.InitialClientHandshake:
                    Logger.Info($"Received InitialClientHandshake message on muxId {muxId}:");
                    InitialClientHandshake initialClientHandshake = InitialClientHandshake.ParseFrom(message);
                    Logger.Trace(initialClientHandshake.ToString());

                    if (initialClientHandshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND)
                    {
                        client.FinishedPlayerMgrServerFrontendHandshake = true;
                    }
                    else if (initialClientHandshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND)
                    {
                        client.FinishedGroupingManagerFrontendHandshake = true;
                    }

                    break;

                default:
                    Logger.Warn($"Received unhandled message {(FrontendProtocolMessage)messageId} (id {messageId})");
                    break;
            }
        }
    }
}
