using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Frontend
{
    public class FrontendService : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private GameServerManager _gameServerManager;
        private Dictionary<ulong, ClientSession> _sessionDict = new();      // todo: session management (invalidate old sessions, remove session when a client disconnects, etc.)

        public FrontendService(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials:
                    Logger.Info($"Received ClientCredentials on muxId {muxId}");
                    ClientCredentials clientCredentials = ClientCredentials.ParseFrom(message.Content);

                    if (_sessionDict.ContainsKey(clientCredentials.Sessionid))
                    {
                        ClientSession session = _sessionDict[clientCredentials.Sessionid];
                        byte[] decryptedToken = null;

                        try
                        {
                            decryptedToken = Cryptography.DecryptToken(clientCredentials.EncryptedToken.ToByteArray(), session.Key, clientCredentials.Iv.ToByteArray());
                        }
                        catch
                        {
                            Logger.Warn($"Failed to decrypt token for sessionId {session.Id}");
                        }

                        if (decryptedToken != null && Cryptography.VerifyToken(decryptedToken, session.Token))
                        {
                            Logger.Info($"Verified client for sessionId {session.Id}");
                            client.Account = session.Account;   // assign account to the client if the token is valid
                            _sessionDict.Remove(session.Id);

                            // Generate response
                            if (ConfigManager.Frontend.SimulateQueue)
                            {
                                Logger.Info("Responding with LoginQueueStatus message");

                                var response = LoginQueueStatus.CreateBuilder()
                                    .SetPlaceInLine(ConfigManager.Frontend.QueuePlaceInLine)
                                    .SetNumberOfPlayersInLine(ConfigManager.Frontend.QueueNumberOfPlayersInLine)
                                    .Build();

                                client.SendMessage(muxId, new(response));
                            }
                            else
                            {
                                Logger.Info("Responding with SessionEncryptionChanged message");

                                var response = SessionEncryptionChanged.CreateBuilder()
                                    .SetRandomNumberIndex(0)
                                    .SetEncryptedRandomNumber(ByteString.Empty)
                                    .Build();

                                client.SendMessage(muxId, new(response));
                            }
                        }
                        else
                        {
                            Logger.Warn($"Failed to verify token for sessionId {session.Id}, disconnecting client");
                            client.Disconnect();
                        }

                    }
                    else
                    {
                        Logger.Warn($"SessionId {clientCredentials.Sessionid} not found, disconnecting client");
                        client.Disconnect();
                        return;
                    }

                    break;

                case FrontendProtocolMessage.InitialClientHandshake:
                    InitialClientHandshake initialClientHandshake = InitialClientHandshake.ParseFrom(message.Content);
                    Logger.Info($"Received InitialClientHandshake for {initialClientHandshake.ServerType} on muxId {muxId}");

                    if (initialClientHandshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND)
                    {
                        client.FinishedPlayerMgrServerFrontendHandshake = true;
                    }
                    else if (initialClientHandshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND)
                    {
                        client.FinishedGroupingManagerFrontendHandshake = true;
                        _gameServerManager.GameManager.GetAvailableGame().AddPlayer(client);
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

        public ClientSession CreateSessionFromLoginDataPB(LoginDataPB loginDataPB, out AuthServer.ErrorCode? errorCode)
        {
            Account account;
            if (ConfigManager.Frontend.BypassAuth)
            {
                account = AccountManager.DefaultAccount;
                errorCode = null;
            }
            else
            {
                account = AccountManager.GetAccountByLoginDataPB(loginDataPB, out errorCode);
            }

            if (account != null)
            {
                lock (this)     // lock session creation to prevent async weirdness
                {
                    ClientSession session = new(HashHelper.GenerateUniqueRandomId(_sessionDict), account);
                    _sessionDict.Add(session.Id, session);
                    return session;
                }
            }
            else
            {
                return null;
            }   
        }
    }
}
