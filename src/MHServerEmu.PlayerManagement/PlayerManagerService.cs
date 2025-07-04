using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// An <see cref="IGameService"/> that manages connected players and routes messages to relevant <see cref="Game"/> instances.
    /// </summary>
    public class PlayerManagerService : IGameService, IMessageBroadcaster
    {
        private const ushort MuxChannel = 1;   // All messages come to and from PlayerManager over mux channel 1

        private static readonly Logger Logger = LogManager.CreateLogger();

        private bool _isRunning = true;

        internal SessionManager SessionManager { get; }
        internal GameHandleManager GameHandleManager { get; }
        internal ClientManager ClientManager { get; }

        public PlayerManagerConfig Config { get; }

        /// <summary>
        /// Constructs a new <see cref="PlayerManagerService"/> instance.
        /// </summary>
        public PlayerManagerService()
        {
            SessionManager = new(this);
            GameHandleManager = new();
            ClientManager = new(this);

            Config = ConfigManager.Instance.GetConfig<PlayerManagerConfig>();
        }

        #region IGameService Implementation

        public void Run()
        {
            GameHandleManager.InitializeGames(Config.GameInstanceCount, Config.PlayerCountDivisor);

            while (_isRunning)
            {
                ClientManager.Update();  // Add / remove clients

                Thread.Sleep(1);
            }
        }

        public void Shutdown()
        {
            _isRunning = false;
            //_gameManager.ShutdownAllGames();
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                case GameServiceProtocol.RouteMessageBuffer routeMessagePackage:
                    // Message buffers are routed asynchronously rather than in ticks to have the lowest latency possible.
                    OnRouteMessageBuffer(routeMessagePackage);
                    break;

                case GameServiceProtocol.RouteMessage routeMessage:
                    OnRouteMessage(routeMessage);
                    break;

                case GameServiceProtocol.GameInstanceOp gameInstanceOp:
                    OnGameInstanceOp(gameInstanceOp);
                    break;
                
                // Client messages are handled in ticks by the ClientManager
                case GameServiceProtocol.AddClient:
                case GameServiceProtocol.RemoveClient:
                case GameServiceProtocol.GameInstanceClientOp:
                    ClientManager.EnqueueMessage(message);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"Games: {GameHandleManager.GameCount} | Sessions: {SessionManager.ActiveSessionCount} [{SessionManager.PendingSessionCount}]";
        }

        private void OnRouteMessageBuffer(in GameServiceProtocol.RouteMessageBuffer routeMessageBuffer)
        {
            IFrontendClient client = routeMessageBuffer.Client;
            MessageBuffer messageBuffer = routeMessageBuffer.MessageBuffer;

            // Self-handle or route messages
            switch ((ClientToGameServerMessage)messageBuffer.MessageId)
            {
                case ClientToGameServerMessage.NetMessageReadyForGameJoin:  OnReadyForGameJoin(client, messageBuffer); break;

                default:
                    // Route the rest of messages to the GIS
                    ServerManager.Instance.SendMessageToService(ServerType.GameInstanceServer, routeMessageBuffer);
                    break;
            }
        }

        private void OnRouteMessage(in GameServiceProtocol.RouteMessage routeMessage)
        {
            IFrontendClient client = routeMessage.Client;
            MailboxMessage message = routeMessage.Message;

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials: OnClientCredentials(client, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private bool OnGameInstanceOp(in GameServiceProtocol.GameInstanceOp gameInstanceOp)
        {
            ulong gameId = gameInstanceOp.GameId;

            if (GameHandleManager.TryGetGameById(gameId, out GameHandle game) == false)
                return Logger.WarnReturn(false, $"OnGameInstanceOp(): No handle found for gameId 0x{gameId:X}");

            switch (gameInstanceOp.Type)
            {
                case GameServiceProtocol.GameInstanceOp.OpType.CreateAck:
                    game.OnInstanceCreationAck();
                    break;

                case GameServiceProtocol.GameInstanceOp.OpType.ShutdownAck:
                    game.OnInstanceShutdownAck();
                    break;

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceOp(): Unhandled operation type {gameInstanceOp.Type}");
            }

            return true;
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Retrieves the <see cref="ClientSession"/> for the specified session id. Returns <see langword="true"/> if successful.
        /// </summary>
        public bool TryGetSession(ulong sessionId, out ClientSession session)
        {
            return SessionManager.TryGetActiveSession(sessionId, out session);
        }

        /// <summary>
        /// Sends an <see cref="IMessage"/> to all connected <see cref="FrontendClient"/> instances.
        /// </summary>
        public void BroadcastMessage(IMessage message)
        {
            throw new NotImplementedException();    // TODO
        }

        #endregion

        #region Message Handling

        /// <summary>
        /// Handles <see cref="LoginDataPB"/>.
        /// </summary>
        public AuthStatusCode OnLoginDataPB(LoginDataPB loginDataPB, out AuthTicket authTicket)
        {
            authTicket = AuthTicket.DefaultInstance;

            var statusCode = SessionManager.TryCreateSessionFromLoginDataPB(loginDataPB, out ClientSession session);

            if (statusCode == AuthStatusCode.Success)
            {
                // Avoid extra allocations and copying by using Unsafe.FromBytes() for session key and token
                authTicket = AuthTicket.CreateBuilder()
                    .SetSessionKey(ByteString.Unsafe.FromBytes(session.Key))
                    .SetSessionToken(ByteString.Unsafe.FromBytes(session.Token))
                    .SetSessionId(session.Id)
                    .SetFrontendServer(IFrontendClient.FrontendAddress)
                    .SetFrontendPort(IFrontendClient.FrontendPort)
                    .SetPlatformTicket("")
                    .SetHasnews(Config.ShowNewsOnLogin)
                    .SetNewsurl(Config.NewsUrl)
                    .SetSuccess(true)
                    .Build();
            }

            return statusCode;
        }

        /// <summary>
        /// Handles <see cref="ClientCredentials"/>.
        /// </summary>
        private bool OnClientCredentials(IFrontendClient client, MailboxMessage message)
        {
            var clientCredentials = message.As<ClientCredentials>();
            if (clientCredentials == null) return Logger.WarnReturn(false, "OnClientCredentials(): clientCredentials == null");

            if (Config.SimulateQueue)
            {
                Logger.Debug("Responding with LoginQueueStatus message");
                client.SendMessage(MuxChannel, LoginQueueStatus.CreateBuilder()
                    .SetPlaceInLine(Config.QueuePlaceInLine)
                    .SetNumberOfPlayersInLine(Config.QueueNumberOfPlayersInLine)
                    .Build());

                return false;
            }

            if (SessionManager.VerifyClientCredentials(client, clientCredentials) == false)
            {
                Logger.Warn($"OnClientCredentials(): Failed to verify client credentials, disconnecting client [{client}]");
                client.Disconnect();
                return false;
            }

            // Success!
            Logger.Info($"Successful auth for client [{client}]");
            client.SendMessage(MuxChannel, SessionEncryptionChanged.CreateBuilder()
                .SetRandomNumberIndex(0)
                .SetEncryptedRandomNumber(ByteString.Empty)
                .Build());

            return true;
        }

        /// <summary>
        /// Handles <see cref="NetMessageReadyForGameJoin"/>.
        /// </summary>
        private bool OnReadyForGameJoin(IFrontendClient client, MessageBuffer messageBuffer)
        {
            // There is a client-side bug with NetMessageReadyForGameJoin that requires special handling, see DeserializeReadyForGameJoin() for more info.
            var readyForGameJoin = messageBuffer.DeserializeReadyForGameJoin();
            if (readyForGameJoin == null) return Logger.WarnReturn(false, "OnReadyForGameJoin(): readyForGameJoin == null");

            Logger.Info($"Received NetMessageReadyForGameJoin from client [{client}], logging in");
            //Logger.Trace(readyForGameJoin.ToString());

            // Log the player in
            client.SendMessage(MuxChannel, NetMessageReadyAndLoggedIn.DefaultInstance); // add report defect (bug) config here

            return true;
        }

        #endregion
    }
}
