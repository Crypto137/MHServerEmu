using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Core.System;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Achievements;
using MHServerEmu.PlayerManagement.Configs;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// An <see cref="IGameService"/> that manages connected players and routes messages to relevant <see cref="Game"/> instances.
    /// </summary>
    public class PlayerManagerService : IGameService, IFrontendService
    {
        private const ushort MuxChannel = 1;   // All messages come to and from PlayerManager over mux channel 1

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly SessionManager _sessionManager;
        private readonly GameManager _gameManager;
        private readonly object _playerLock = new();
        private readonly Dictionary<ulong, FrontendClient> _playerDict = new();

        private readonly string _frontendAddress;
        private readonly string _frontendPort;

        public PlayerManagerConfig Config { get; }

        /// <summary>
        /// Constructs a new <see cref="PlayerManagerService"/> instance.
        /// </summary>
        public PlayerManagerService()
        {
            _sessionManager = new(this);
            _gameManager = new();

            // Get frontend information for AuthTickets
            var frontendConfig = ConfigManager.Instance.GetConfig<FrontendConfig>();
            _frontendAddress = frontendConfig.PublicAddress;
            _frontendPort = frontendConfig.Port;

            Config = ConfigManager.Instance.GetConfig<PlayerManagerConfig>();
        }

        #region IGameService Implementation

        public void Run()
        {
            _gameManager.CreateGame();
        }

        public void Shutdown()
        {
            // TODO: Shut down all games
        }

        public void Handle(ITcpClient tcpClient, MessagePackage message)
        {
            var client = (FrontendClient)tcpClient;
            message.Protocol = typeof(ClientToGameServerMessage);

            // Timestamp sync messages
            if (message.Id == (uint)ClientToGameServerMessage.NetMessageSyncTimeRequest || message.Id == (uint)ClientToGameServerMessage.NetMessagePing)
            {
                message.GameTimeReceived = Clock.GameTime;
                message.DateTimeReceived = Clock.UnixTime;
            }

            // Self-handle or route messages
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageReadyForGameJoin:  OnReadyForGameJoin(client, message); break;
                case ClientToGameServerMessage.NetMessageSyncTimeRequest:   OnSyncTimeRequest(client, message); break;
                case ClientToGameServerMessage.NetMessagePing:              OnPing(client, message); break;
                case ClientToGameServerMessage.NetMessageFPS:               OnFps(client, message); break;

                default:
                    // Route the rest of messages to the game the player is currently in
                    Game game = GetGameByPlayer(client);

                    if (game == null)
                    {
                        Logger.Warn($"Handle(): Cannot route {(ClientToGameServerMessage)message.Id}, the player {client.Session.Account} is not in a game");
                        return;
                    }

                    game.Handle(client, message);
                    break;
            }
        }

        public void Handle(ITcpClient client, IEnumerable<MessagePackage> messages)
        {
            foreach (MessagePackage message in messages)
                Handle(client, message);
        }

        public void Handle(ITcpClient client, MailboxMessage message)
        {
            Logger.Warn($"Handle(): Unhandled MailboxMessage");
        }

        public string GetStatus()
        {
            return $"Sessions: {_sessionManager.SessionCount} | Games: {_gameManager.GameCount}";
        }

        #endregion

        #region IFrontendService Implementation

        public void ReceiveFrontendMessage(FrontendClient client, IMessage message)
        {
            switch (message)
            {
                case InitialClientHandshake handshake: OnInitialClientHandshake(client, handshake); break;
                case ClientCredentials credentials: OnClientCredentials(client, credentials); break;
                default: Logger.Warn($"ReceiveFrontendMessage(): Unhandled message {message.DescriptorForType.Name}"); break;
            }
        }

        public bool AddFrontendClient(FrontendClient client)
        {
            lock (_playerLock)
            {
                if (client.Session == null || client.Session.Account == null)
                    return Logger.WarnReturn(false, "AddFrontendClient(): The client has no valid session assigned");

                ulong playerId = client.Session.Account.Id;

                // Handle duplicate login by disconnecting the existing player
                if (_playerDict.TryGetValue(playerId, out FrontendClient existingClient))
                {
                    Logger.Info($"Duplicate login for {client.Session.Account}, terminating existing session {existingClient.Session.Id}");
                    existingClient.Disconnect();
                    ((ClientSession)client.Session).RefreshAccount();   // Replace outdated data retrieved before the existing session was terminated
                }

                _playerDict.Add(client.Session.Account.Id, client);
                _gameManager.GetAvailableGame().AddPlayer(client);
            }

            return true;
        }

        public bool RemoveFrontendClient(FrontendClient client)
        {
            lock (_playerLock)
            {
                if (client.Session == null || client.Session.Account == null)
                    return Logger.WarnReturn(false, "RemoveFrontendClient(): The client has no valid session assigned");

                ulong playerId = client.Session.Account.Id;

                if (_playerDict.ContainsKey(client.Session.Account.Id) == false)
                    return Logger.WarnReturn(false, $"RemoveFrontendClient(): Player {client.Session.Account} not found");

                GetGameByPlayer(client)?.RemovePlayer(client);

                _playerDict.Remove(playerId);
                _sessionManager.RemoveSession(client.Session.Id);
            }

            if (Config.BypassAuth == false)
                DBManager.UpdateAccountData(client.Session.Account);
            else
                AccountManager.SaveDefaultAccount();

            return true;
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Retrieves the <see cref="ClientSession"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetSession(ulong sessionId, out ClientSession session) => _sessionManager.TryGetSession(sessionId, out session);

        /// <summary>
        /// Retrieves the <see cref="FrontendClient"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetClient(ulong sessionId, out FrontendClient client) => _sessionManager.TryGetClient(sessionId, out client);

        /// <summary>
        /// Retrieves the <see cref="Game"/> instance that the provided <see cref="FrontendClient"/> is in. Returns <see langword="null"/> if not found.
        /// </summary>
        public Game GetGameByPlayer(FrontendClient client)
        {
            // TODO: Keep track of this inside PlayerManagerService rather than relying on a client property
            return _gameManager.GetGameById(client.GameId);
        }

        /// <summary>
        /// Sends an <see cref="IMessage"/> to all connected <see cref="FrontendClient"/> instances.
        /// </summary>
        public void BroadcastMessage(IMessage message)
        {
            lock (_playerLock)
            {
                foreach (FrontendClient player in _playerDict.Values)
                    player.SendMessage(MuxChannel, message);
            }
        }

        #endregion

        #region Message Handling

        /// <summary>
        /// Handles <see cref="LoginDataPB"/>.
        /// </summary>
        public AuthStatusCode OnLoginDataPB(LoginDataPB loginDataPB, out AuthTicket authTicket)
        {
            authTicket = AuthTicket.DefaultInstance;

            var statusCode = _sessionManager.TryCreateSessionFromLoginDataPB(loginDataPB, out ClientSession session);

            if (statusCode == AuthStatusCode.Success)
            {
                authTicket = AuthTicket.CreateBuilder()
                    .SetSessionKey(ByteString.CopyFrom(session.Key))
                    .SetSessionToken(ByteString.CopyFrom(session.Token))
                    .SetSessionId(session.Id)
                    .SetFrontendServer(_frontendAddress)
                    .SetFrontendPort(_frontendPort)
                    .SetPlatformTicket("")
                    .SetHasnews(Config.ShowNewsOnLogin)
                    .SetNewsurl(Config.NewsUrl)
                    .SetSuccess(true)
                    .Build();
            }

            return statusCode;
        }

        /// <summary>
        /// Handles <see cref="InitialClientHandshake"/>.
        /// </summary>
        private void OnInitialClientHandshake(FrontendClient client, InitialClientHandshake handshake)
        {
            client.FinishedPlayerManagerHandshake = true;

            // Queue loading
            client.SendMessage(MuxChannel, NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId(0).Build());

            // Send achievement database
            client.SendMessage(MuxChannel, AchievementDatabase.Instance.GetDump());
            // NetMessageQueryIsRegionAvailable regionPrototype: 9833127629697912670 should go in the same packet as AchievementDatabaseDump
        }

        /// <summary>
        /// Handles <see cref="ClientCredentials"/>.
        /// </summary>
        private void OnClientCredentials(FrontendClient client, ClientCredentials credentials)
        {
            Logger.Info($"Received ClientCredentials");

            if (_sessionManager.VerifyClientCredentials(client, credentials) == false)
            {
                Logger.Warn($"Failed to verify client credentials, disconnecting client on {client.Connection}");
                client.Disconnect();
                return;
            }

            // Respond on successful auth
            if (Config.SimulateQueue)
            {
                Logger.Info("Responding with LoginQueueStatus message");
                client.SendMessage(MuxChannel, LoginQueueStatus.CreateBuilder()
                    .SetPlaceInLine(Config.QueuePlaceInLine)
                    .SetNumberOfPlayersInLine(Config.QueueNumberOfPlayersInLine)
                    .Build());
            }
            else
            {
                Logger.Info("Responding with SessionEncryptionChanged message");
                client.SendMessage(MuxChannel, SessionEncryptionChanged.CreateBuilder()
                    .SetRandomNumberIndex(0)
                    .SetEncryptedRandomNumber(ByteString.Empty)
                    .Build());
            }
        }

        /// <summary>
        /// Handles <see cref="NetMessageReadyForGameJoin"/>.
        /// </summary>
        private bool OnReadyForGameJoin(FrontendClient client, MessagePackage message)
        {
            // NetMessageReadyForGameJoin contains a bug where wipesDataIfMismatchedInDb is marked as required but the client
            // doesn't include it. To avoid an exception we build a partial message from the data we receive.
            NetMessageReadyForGameJoin readyForGameJoin;
            try
            {
                readyForGameJoin = NetMessageReadyForGameJoin.CreateBuilder().MergeFrom(message.Payload).BuildPartial();
            }
            catch
            {
                return Logger.ErrorReturn(false, "OnReadyForGameJoin(): Failed to deserialize");
            }

            Logger.Info($"Received NetMessageReadyForGameJoin from {client.Session.Account}");
            Logger.Trace(readyForGameJoin.ToString());

            // Log the player in
            Logger.Info($"Logging in player {client.Session.Account}");
            client.SendMessage(MuxChannel, NetMessageReadyAndLoggedIn.DefaultInstance); // add report defect (bug) config here

            // Sync time
            client.SendMessage(MuxChannel, NetMessageInitialTimeSync.CreateBuilder()
                .SetGameTimeServerSent(Clock.GameTime.Ticks / 10)
                .SetDateTimeServerSent(Clock.UnixTime.Ticks / 10)
                .Build());

            return true;
        }

        /// <summary>
        /// Handles <see cref="NetMessageSyncTimeRequest"/>.
        /// </summary>
        private bool OnSyncTimeRequest(FrontendClient client, MessagePackage message)
        {
            var request = message.Deserialize() as NetMessageSyncTimeRequest;
            if (request == null) return Logger.WarnReturn(false, $"OnSyncTimeRequest(): Failed to retrieve message");

            //Logger.Debug($"NetMessageSyncTimeRequest:\n{request}");

            var reply = NetMessageSyncTimeReply.CreateBuilder()
                .SetGameTimeClientSent(request.GameTimeClientSent)
                .SetGameTimeServerReceived(message.GameTimeReceived.Ticks / 10)
                .SetGameTimeServerSent(Clock.GameTime.Ticks / 10)
                .SetDateTimeClientSent(request.DateTimeClientSent)
                .SetDateTimeServerReceived(message.DateTimeReceived.Ticks / 10)
                .SetDateTimeServerSent(Clock.UnixTime.Ticks / 10)
                .SetDialation(1.0f)
                .SetGametimeDialationStarted(0)
                .SetDatetimeDialationStarted(0)
                .Build();

            //Logger.Debug($"NetMessageSyncTimeReply:\n{reply}");

            client.SendMessage(MuxChannel, reply);
            return true;
        }

        /// <summary>
        /// Handles <see cref="NetMessagePing"/>.
        /// </summary>
        private bool OnPing(FrontendClient client, MessagePackage message)
        {
            var ping = message.Deserialize() as NetMessagePing;
            if (ping == null) return Logger.WarnReturn(false, $"OnPing(): Failed to retrieve message");

            //Logger.Debug($"NetMessagePing:\n{ping}");

            var response = NetMessagePingResponse.CreateBuilder()
                .SetDisplayOutput(ping.DisplayOutput)
                .SetRequestSentClientTime(ping.SendClientTime)
                .SetRequestSentGameTime(ping.SendGameTime)
                .SetRequestNetReceivedGameTime((ulong)message.GameTimeReceived.TotalMilliseconds)
                .SetResponseSendTime((ulong)Clock.GameTime.TotalMilliseconds)
                .SetServerTickforecast(0)   // server tick time ms
                .SetGameservername("BOPR-MHVGIS2")
                .SetFrontendname("bopr-mhfes2")
                .Build();

            //Logger.Debug($"NetMessagePingResponse:\n{response}");

            client.SendMessage(MuxChannel, response);
            return true;
        }

        /// <summary>
        /// Handles <see cref="NetMessageFPS"/>.
        /// </summary>
        private void OnFps(FrontendClient client, MessagePackage message)
        {
            //Logger.Debug($"NetMessageFPS:\n{fps}");
        }

        #endregion
    }
}
