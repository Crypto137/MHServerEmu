using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Frontend;
using MHServerEmu.Games;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// An <see cref="IGameService"/> that manages connected players and routes messages to relevant <see cref="Game"/> instances.
    /// </summary>
    public class PlayerManagerService : IGameService, IFrontendService, IMessageBroadcaster
    {
        // TODO: Implement a way to request saves from the game without disconnecting.

        private const ushort MuxChannel = 1;   // All messages come to and from PlayerManager over mux channel 1
        private const int MaxAsyncRetryAttempts = 10;
        private const int GameFrameTimeMS = 50;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly SessionManager _sessionManager;
        private readonly GameManager _gameManager;
        private readonly Dictionary<ulong, FrontendClient> _playerDict = new();
        private readonly Dictionary<ulong, Task> _pendingSaveDict = new();

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
            _gameManager.ShutdownAllGames();

            // Wait for all data to be saved
            bool waitingForSave;
            lock (_pendingSaveDict) waitingForSave = _pendingSaveDict.Count > 0;

            while (waitingForSave)
            {
                Thread.Sleep(1);
                lock (_pendingSaveDict) waitingForSave = _pendingSaveDict.Count > 0;
            }
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

                    game.PostMessage(client, message);
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
            lock (_pendingSaveDict)
                return $"Sessions: {_sessionManager.SessionCount} | Games: {_gameManager.GameCount} | Pending Saves: {_pendingSaveDict.Count}";
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
            if (client.Session == null || client.Session.Account == null)
                return Logger.WarnReturn(false, "AddFrontendClient(): The client has no valid session assigned");

            ulong playerDbId = (ulong)client.Session.Account.Id;

            lock (_playerDict)
            {
                // Handle duplicate login by disconnecting the existing player
                if (_playerDict.TryGetValue(playerDbId, out FrontendClient existingClient))
                {
                    Logger.Info($"Duplicate login for {client}, terminating existing session 0x{existingClient.Session.Id:X}");
                    existingClient.Disconnect();
                }

                _playerDict.Add(playerDbId, client);
            }

            // Player is added to a game asynchronously as a task because their data may be pending a save after a previous session.
            Task.Run(async () => await AddPlayerToGameAsync(client));
            
            return true;
        }

        public bool RemoveFrontendClient(FrontendClient client)
        {
            if (client.Session == null || client.Session.Account == null)
                return Logger.WarnReturn(false, "RemoveFrontendClient(): The client has no valid session assigned");

            ulong playerDbId = (ulong)client.Session.Account.Id;

            lock (_playerDict)
            {
                if (_playerDict.ContainsKey(playerDbId) == false)
                    return Logger.WarnReturn(false, $"RemoveFrontendClient(): Player {client} not found");

                _playerDict.Remove(playerDbId);
            }

            _sessionManager.RemoveSession(client.Session.Id);
            GetGameByPlayer(client)?.RemoveClient(client);

            // Account data is saved asynchronously as a task because it takes some time for a player to leave a game
            lock (_pendingSaveDict)
                _pendingSaveDict.Add(playerDbId, Task.Run(async () => await SavePlayerDataAsync(client)));
            
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
            lock (_playerDict)
            {
                foreach (FrontendClient player in _playerDict.Values)
                    player.SendMessage(MuxChannel, message);
            }
        }

        /// <summary>
        /// Asynchronously waits for any pending account data saves and then adds the provided <see cref="FrontendClient"/> to an available game.
        /// </summary>
        private async Task AddPlayerToGameAsync(FrontendClient client)
        {
            ulong playerDbId = (ulong)client.Session.Account.Id;
            int numAttempts = 0;
            bool hasSavePending = false;
            bool refreshRequired = false;

            while (true)
            {
                numAttempts++;

                lock (_pendingSaveDict)
                    hasSavePending = _pendingSaveDict.ContainsKey(playerDbId);

                if (hasSavePending == false) break;

                refreshRequired = true;

                if (numAttempts >= MaxAsyncRetryAttempts)
                {
                    Logger.Warn($"AddPlayerToGameAsync(): Failed to add player to a game after {numAttempts} attempts, disconnecting");
                    client.Disconnect();
                    return;
                }

                await Task.Delay(GameFrameTimeMS);
            }

            // If we had to wait for a pending save, it means our account data is not up to date and needs to be refreshed.
            if (refreshRequired)
                ((ClientSession)client.Session).RefreshAccount();

            // Add the client to an available game once pending saves have been resolved
            _gameManager.GetAvailableGame().AddClient(client);
        }

        /// <summary>
        /// Asynchronously waits for the provided <see cref="FrontendClient"/> to leave the game and then saves their data.
        /// </summary>
        private async Task SavePlayerDataAsync(FrontendClient client)
        {
            ulong playerDbId = (ulong)client.Session.Account.Id;
            int numAttempts = 0;

            while (true)
            {
                numAttempts++;

                // Wait for the client to leave the game they are in
                if (client.IsInGame)
                {
                    // Players should generally leave games during the next game update.
                    // If it didn't happen, something must have gone terribly wrong.
                    if (numAttempts >= MaxAsyncRetryAttempts)
                    {
                        Logger.Warn($"SavePlayerDataAsync(): Failed to save player data after {numAttempts} attempts, bailing out");
                        lock (_pendingSaveDict) _pendingSaveDict.Remove(playerDbId);
                        return;
                    }

                    await Task.Delay(GameFrameTimeMS);
                    continue;
                }

                // Save data and remove pending save
                AccountManager.DBManager.UpdateAccountData(client.Session.Account);

                lock (_pendingSaveDict) _pendingSaveDict.Remove(playerDbId);
                Logger.Info($"Saved data for player {client}");
                return;
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
                // Avoid extra allocations and copying by using Unsafe.FromBytes() for session key and token
                authTicket = AuthTicket.CreateBuilder()
                    .SetSessionKey(ByteString.Unsafe.FromBytes(session.Key))
                    .SetSessionToken(ByteString.Unsafe.FromBytes(session.Token))
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
