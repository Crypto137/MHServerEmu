using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Auth;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Frontend.Accounts;
using MHServerEmu.GameServer.Frontend.Accounts.DBModels;
using MHServerEmu.Networking;
using MHServerEmu.Networking.Base;

namespace MHServerEmu.GameServer.Frontend
{
    public class FrontendService : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private GameServerManager _gameServerManager;

        // Use a lock object instead of this to prevent deadlocks
        // More info: https://learn.microsoft.com/en-us/archive/msdn-magazine/2003/january/net-column-safe-thread-synchronization
        private readonly object _sessionLock = new();     
        private readonly Dictionary<ulong, ClientSession> _sessionDict = new();
        private readonly Dictionary<ulong, FrontendClient> _clientDict = new();

        public int SessionCount { get => _sessionDict.Count; }

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
                    ClientCredentials clientCredentials = ClientCredentials.ParseFrom(message.Payload);

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
                            lock (_sessionLock) _sessionDict.Remove(session.Id);    // invalidate session after a failed login attempt
                            Logger.Warn($"Failed to decrypt token for sessionId {session.Id}");
                        }

                        if (decryptedToken != null && Cryptography.VerifyToken(decryptedToken, session.Token))
                        {
                            Logger.Info($"Verified client for sessionId {session.Id}");

                            // assign account to the client if the token is valid
                            lock (_sessionLock)
                            {
                                client.AssignSession(session);
                                _clientDict.Add(session.Id, client);
                            }

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
                            client.Connection.Disconnect();
                        }

                    }
                    else
                    {
                        Logger.Warn($"SessionId {clientCredentials.Sessionid} not found, disconnecting client");
                        client.Connection.Disconnect();
                        return;
                    }

                    break;

                case FrontendProtocolMessage.InitialClientHandshake:
                    InitialClientHandshake initialClientHandshake = InitialClientHandshake.ParseFrom(message.Payload);
                    Logger.Info($"Received InitialClientHandshake for {initialClientHandshake.ServerType} on muxId {muxId}");

                    // These handshakes should probably be handled by PlayerManagerService and GroupingManagerService. They should probably also track clients on their own.
                    if (initialClientHandshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND && client.FinishedPlayerManagerHandshake == false)
                    {
                        client.FinishedPlayerManagerHandshake = true;
                        
                        // Queue loading
                        client.IsLoading = true;
                        client.SendMessage(1, new(NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId(0).Build()));

                        // Send achievement database
                        client.SendMessage(1, new(_gameServerManager.AchievementDatabase.ToNetMessageAchievementDatabaseDump()));
                        // NetMessageQueryIsRegionAvailable regionPrototype: 9833127629697912670 should go in the same packet as AchievementDatabaseDump
                    }
                    else if (initialClientHandshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND && client.FinishedGroupingManagerHandshake == false)
                    {
                        client.FinishedGroupingManagerHandshake = true;
                    }

                    // Add the player to a game when both handshakes are finished
                    // Adding the player early can cause GroupingManager handshake to not finish properly, which leads to the chat not working
                    if (client.FinishedPlayerManagerHandshake && client.FinishedGroupingManagerHandshake)
                    {
                        _gameServerManager.GroupingManagerService.SendMotd(client);
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

        public AuthStatusCode TryCreateSessionFromLoginDataPB(LoginDataPB loginDataPB, out ClientSession session)
        {
            session = null;
            
            // Check client version
            if (loginDataPB.Version != GameServerManager.GameVersion)
            {
                Logger.Warn($"Client version mismatch ({loginDataPB.Version} instead of {GameServerManager.GameVersion})");

                // Fail auth if version mismatch is not allowed
                if (ConfigManager.Frontend.AllowClientVersionMismatch == false)
                    return AuthStatusCode.PatchRequired;
            }

            // Verify credentials
            DBAccount account;
            AuthStatusCode statusCode;

            if (ConfigManager.Frontend.BypassAuth)  // Auth always succeeds when BypassAuth is set to true
            {
                account = AccountManager.DefaultAccount;
                statusCode = AuthStatusCode.Success;
            }
            else                                    // Check credentials with AccountManager
            {
                statusCode = AccountManager.TryGetAccountByLoginDataPB(loginDataPB, out account);
            }

            // Create a new session if login data is valid
            if (statusCode == AuthStatusCode.Success)
            {
                lock (_sessionLock)
                {
                    session = new(IdGenerator.Generate(IdType.Session), account, loginDataPB.ClientDownloader, loginDataPB.Locale);
                    _sessionDict.Add(session.Id, session);
                }
            }

            return statusCode;
        }

        public bool TryGetSession(ulong sessionId, out ClientSession session) => _sessionDict.TryGetValue(sessionId, out session);
        public bool TryGetClient(ulong sessionId, out FrontendClient client) => _clientDict.TryGetValue(sessionId, out client);

        public void OnClientDisconnect(object sender, ConnectionEventArgs e)
        {
            FrontendClient client = e.Connection.Client as FrontendClient;

            if (client.Session == null)
            {
                Logger.Info("Client disconnected");
            }
            else
            {
                lock (_sessionLock)
                {
                    _sessionDict.Remove(client.Session.Id);
                    _clientDict.Remove(client.Session.Id);
                }

                if (ConfigManager.Frontend.BypassAuth == false) DBManager.UpdateAccountData(client.Session.Account);

                Logger.Info($"Client disconnected (sessionId {client.Session.Id})");
            }
        }

        public void BroadcastMessage(ushort muxId, GameMessage message)
        {
            lock (_sessionLock)
            {
                foreach (var kvp in _clientDict)
                    kvp.Value.SendMessage(muxId, message);
            }
        }
    }
}
