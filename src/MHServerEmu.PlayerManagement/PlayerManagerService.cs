using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
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

        // Async retry consts for saving and adding players
        private const int AsyncRetryAttemptIntervalMS = 10 * 1000;  // Retry window every 10 sec
        private const int AsyncRetryTicksPerAttempt = 10;           // Do 10 ticks per attempt window
        private const int AsyncRetryTickIntervalMS = 50;            // Wait at least target game frame time between each tick

        private const int AsyncRetryNumAttemptsSavePlayer = 3;
        private const int AsyncRetryNumAttemptsAddPlayer = AsyncRetryNumAttemptsSavePlayer + 1;   // Do an extra attempt when adding players

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
                return $"Games: {_gameManager.GameCount} | Sessions: {_sessionManager.ActiveSessionCount} [{_sessionManager.PendingSessionCount}] | Pending Saves: {_pendingSaveDict.Count}";
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
                return Logger.WarnReturn(false, $"AddFrontendClient(): Client [{client}] has no valid session assigned");

            ulong playerDbId = (ulong)client.Session.Account.Id;

            lock (_playerDict)
            {
                // Handle duplicate login by disconnecting the existing player
                if (_playerDict.TryGetValue(playerDbId, out FrontendClient existingClient))
                {
                    Logger.Info($"Duplicate login for client [{client}], terminating existing session 0x{existingClient.Session.Id:X}");
                    existingClient.Disconnect();
                }

                _playerDict.Add(playerDbId, client);
            }

            Logger.Info($"Added client [{client}]");

            // Player is added to a game asynchronously as a task because their data may be pending a save after a previous session.
            Task.Run(async () => await AddPlayerToGameAsync(client));
            
            return true;
        }

        public bool RemoveFrontendClient(FrontendClient client)
        {
            if (client.Session == null || client.Session.Account == null)
                return Logger.WarnReturn(false, $"RemoveFrontendClient(): Client [{client}] has no valid session assigned");

            ulong playerDbId = (ulong)client.Session.Account.Id;

            lock (_playerDict)
            {
                if (_playerDict.Remove(playerDbId) == false)
                    return Logger.WarnReturn(false, $"RemoveFrontendClient(): Client [{client}] not found");
            }

            _sessionManager.RemoveActiveSession(client.Session.Id);
            GetGameByPlayer(client)?.RemoveClient(client);

            // Account data is saved asynchronously as a task because it takes some time for a player to leave a game
            lock (_pendingSaveDict)
            {
                if (_pendingSaveDict.ContainsKey(playerDbId))
                {
                    Logger.Warn($"RemoveFrontendClient(): Client [{client}] already has a pending save task");
                }
                else if (client.IsInGame == false)
                {
                    // We skip saving here to avoid overwriting player data with empty data from an account that hasn't been fully loaded yet.
                    Logger.Warn($"RemoveFrontendClient(): Client [{client}] is not in a game, skipping saving");
                }
                else
                {
                    _pendingSaveDict.Add(playerDbId, Task.Run(async () => await SavePlayerDataAsync(client)));
                }
            }

            TimeSpan sessionLength = client.Session != null ? ((ClientSession)client.Session).SessionLength : TimeSpan.Zero;
            Logger.Info($"Removed client [{client}] (SessionLength={sessionLength:hh\\:mm\\:ss})");
            return true;
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Retrieves the <see cref="ClientSession"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetSession(ulong sessionId, out ClientSession session) => _sessionManager.TryGetActiveSession(sessionId, out session);

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

            bool hasSavePending = false;

            // Wait for the player to finish saving while checking in short bursts
            // [check x10] - [wait 10 sec] - [check x10] - [wait 10 sec], and so on
            // Time out after a few long pauses.

            int numAttempts = 0;

            while (numAttempts < AsyncRetryNumAttemptsAddPlayer)
            {
                numAttempts++;
                Logger.Info($"Adding client [{client}] to a game ({numAttempts}/{AsyncRetryNumAttemptsAddPlayer})...");

                int numTicks = 0;

                while (numTicks < AsyncRetryTicksPerAttempt)
                {
                    numTicks++;

                    lock (_pendingSaveDict)
                        hasSavePending = _pendingSaveDict.ContainsKey(playerDbId);

                    // Wait a little if we have a pending save
                    if (hasSavePending)
                    {
                        await Task.Delay(AsyncRetryTickIntervalMS);
                        continue;
                    }

                    // Make sure the client is still connected after waiting
                    if (client.IsConnected == false)
                    {
                        Logger.Warn($"AddPlayerToGameAsync(): Client [{client}] disconnected while waiting for a pending save");
                        return;
                    }

                    // Load player data associated with this account now that any pending saves are resolved
                    AccountManager.LoadPlayerDataForAccount(client.Session.Account);

                    // Add to an available game
                    Game game = _gameManager.GetAvailableGame();
                    game.AddClient(client);
                    Logger.Info($"Queued client [{client}] to be added to game [{game}]");
                    return;
                }

                // Do a longer wait between attempts
                await Task.Delay(AsyncRetryAttemptIntervalMS);
            }

            Logger.Warn($"AddPlayerToGameAsync(): Timed out trying to add client [{client}] to a game after {numAttempts} attempts, disconnecting");
            client.Disconnect();
        }

        /// <summary>
        /// Asynchronously waits for the provided <see cref="FrontendClient"/> to leave the game and then saves their data.
        /// </summary>
        private async Task SavePlayerDataAsync(FrontendClient client)
        {
            // Wait for the player to leave the game while checking in short bursts.
            // [check x10] - [wait 10 sec] - [check x10] - [wait 10 sec], and so on.

            int numAttempts = 0;

            while (numAttempts < AsyncRetryNumAttemptsSavePlayer)
            {
                numAttempts++;
                Logger.Info($"Waiting for client [{client}] to leave game ({numAttempts}/{AsyncRetryNumAttemptsSavePlayer})...");

                int numTicks = 0;

                while (numTicks < AsyncRetryTicksPerAttempt)
                {
                    numTicks++;

                    if (client.IsInGame)
                    {
                        // Do a short wait between ticks equal to target game framerate
                        await Task.Delay(AsyncRetryTickIntervalMS);
                        continue;
                    }

                    // The player was removed from its game, save the latest data
                    DoSavePlayerData(client);
                    return;
                }

                // Do a longer wait between attempts
                await Task.Delay(AsyncRetryAttemptIntervalMS);
            }

            // Timeout, just save whatever data was there and move on.
            Logger.Warn($"SavePlayerDataAsync(): Timed out waiting for client [{client}] to leave game 0x{client.GameId:X} after {numAttempts} attempts");
            DoSavePlayerData(client);
        }

        private void DoSavePlayerData(FrontendClient client)
        {
            // Save data and remove pending save
            Logger.Info($"Saving player data for client [{client}]...");
            DBAccount account = client.Session.Account;

            // NOTE: We are locking on the account instance to prevent account data from being modified while
            // it is being written to the database. This could potentially cause deadlocks if not used correctly.
            lock (account)
            {
                if (AccountManager.DBManager.SavePlayerData(account))
                    Logger.Info($"Saved player data for client [{client}]");
                else
                    Logger.Error($"SavePlayerDataAsync(): Failed to save player data for client [{client}]");
            }

            lock (_pendingSaveDict) _pendingSaveDict.Remove((ulong)account.Id);
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
            if (Config.SimulateQueue)
            {
                Logger.Debug("Responding with LoginQueueStatus message");
                client.SendMessage(MuxChannel, LoginQueueStatus.CreateBuilder()
                    .SetPlaceInLine(Config.QueuePlaceInLine)
                    .SetNumberOfPlayersInLine(Config.QueueNumberOfPlayersInLine)
                    .Build());

                return;
            }

            if (_sessionManager.VerifyClientCredentials(client, credentials) == false)
            {
                Logger.Warn($"OnClientCredentials(): Failed to verify client credentials, disconnecting client on {client.Connection}");
                client.Disconnect();
                return;
            }

            // Success!
            Logger.Info($"Successful auth for client [{client}]");
            client.SendMessage(MuxChannel, SessionEncryptionChanged.CreateBuilder()
                .SetRandomNumberIndex(0)
                .SetEncryptedRandomNumber(ByteString.Empty)
                .Build());
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

            Logger.Info($"Received NetMessageReadyForGameJoin from client [{client}], logging in");
            //Logger.Trace(readyForGameJoin.ToString());

            // Log the player in
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
