using System.Diagnostics;
using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games;
using MHServerEmu.PlayerManagement.Games;
using MHServerEmu.PlayerManagement.Network;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Regions;
using MHServerEmu.PlayerManagement.Social;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// An <see cref="IGameService"/> that manages connected players and routes messages to relevant <see cref="Game"/> instances.
    /// </summary>
    public class PlayerManagerService : IGameService
    {
        public const int TargetTickTimeMS = 150;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly PlayerManagerServiceMailbox _serviceMailbox;

        internal static PlayerManagerService Instance { get; private set; }     // Naughty singleton-like access without being an actual singleton

        internal SessionManager SessionManager { get; }
        internal LoginQueueManager LoginQueueManager { get; }
        internal GameHandleManager GameHandleManager { get; }
        internal WorldManager WorldManager { get; }
        internal ClientManager ClientManager { get; }
        internal CommunityRegistry CommunityRegistry { get; }
        internal MasterPartyManager PartyManager { get; }

        public PlayerManagerConfig Config { get; }

        public GameServiceState State { get; private set; } = GameServiceState.Created;

        /// <summary>
        /// Constructs a new <see cref="PlayerManagerService"/> instance.
        /// </summary>
        public PlayerManagerService()
        {
            _serviceMailbox = new(this);

            SessionManager = new(this);
            LoginQueueManager = new(this);
            GameHandleManager = new(this);
            WorldManager = new(this);
            ClientManager = new(this);
            CommunityRegistry = new(this);
            PartyManager = new(this);

            Config = ConfigManager.Instance.GetConfig<PlayerManagerConfig>();
        }

        #region IGameService Implementation

        public void Run()
        {
            Instance = this;

            State = GameServiceState.Running;
            while (State == GameServiceState.Running)
            {
                TimeSpan referenceTime = _stopwatch.Elapsed;

                _serviceMailbox.ProcessMessages();

                SessionManager.Update();
                LoginQueueManager.Update();
                ClientManager.Update();
                CommunityRegistry.Update();

                double tickTimeMS = (_stopwatch.Elapsed - referenceTime).TotalMilliseconds;
                int sleepTimeMS = (int)Math.Max(TargetTickTimeMS - tickTimeMS, 0);

                Thread.Sleep(sleepTimeMS);
            }

            // Shutdown

            // Shutting down the frontend will disconnect all clients, here we just wait for everything to be cleaned up and saved
            ClientManager.AllowNewClients = false;
            while (ClientManager.PlayerCount > 0)
            {
                _serviceMailbox.ProcessMessages();
                ClientManager.Update();
                Thread.Sleep(1);
            }

            GameHandleManager.Shutdown();
            while (GameHandleManager.GameCount > 0)
            {
                _serviceMailbox.ProcessMessages();
                Thread.Sleep(1);
            }

            State = GameServiceState.Shutdown;
        }

        public void Shutdown()
        {
            State = GameServiceState.ShuttingDown;
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                // Message buffers are routed asynchronously rather than in ticks to have the lowest latency possible.
                case ServiceMessage.RouteMessageBuffer routeMessagePackage:
                    OnRouteMessageBuffer(routeMessagePackage);
                    break;

                case ServiceMessage.RouteMessage routeMessage:
                    OnRouteMessage(routeMessage);
                    break;

                // Service messages are handled in ticks
                case ServiceMessage.AddClient:
                case ServiceMessage.RemoveClient:
                case ServiceMessage.GameInstanceOp:
                case ServiceMessage.GameInstanceClientOp:
                case ServiceMessage.CreateRegionResult:
                case ServiceMessage.RequestRegionShutdown:
                case ServiceMessage.ChangeRegionRequest:
                case ServiceMessage.RegionTransferFinished:
                case ServiceMessage.ClearPrivateStoryRegions:
                case ServiceMessage.SetDifficultyTierPreference:
                case ServiceMessage.PlayerLookupByNameRequest:
                case ServiceMessage.PlayerNameChanged:
                case ServiceMessage.CommunityStatusUpdate:
                case ServiceMessage.CommunityStatusRequest:
                case ServiceMessage.PartyOperationRequest:
                case ServiceMessage.PartyBoostUpdate:
                    _serviceMailbox.PostMessage(message);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"Games: {GameHandleManager.GameCount} | Players: {ClientManager.PlayerCount} | Sessions: {SessionManager.ActiveSessionCount} [{SessionManager.PendingSessionCount}]";
        }

        private void OnRouteMessageBuffer(in ServiceMessage.RouteMessageBuffer routeMessageBuffer)
        {
            IFrontendClient client = routeMessageBuffer.Client;
            MessageBuffer messageBuffer = routeMessageBuffer.MessageBuffer;

            // Self-handle or route messages
            switch ((ClientToGameServerMessage)messageBuffer.MessageId)
            {
                case ClientToGameServerMessage.NetMessageReadyForGameJoin:  OnReadyForGameJoin(client, messageBuffer); break;

                default:
                    // Route the rest of messages to the GIS
                    ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, routeMessageBuffer);
                    break;
            }
        }

        private void OnRouteMessage(in ServiceMessage.RouteMessage routeMessage)
        {
            IFrontendClient client = routeMessage.Client;
            MailboxMessage message = routeMessage.Message;

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials: OnClientCredentials(client, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
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

        #endregion

        #region Metrics

        public void GetRegionReportData(RegionReport report)
        {
            WorldManager.GetRegionReportData(report);
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

            if (SessionManager.VerifyClientCredentials(client, clientCredentials) == false)
            {
                Logger.Warn($"OnClientCredentials(): Failed to verify client credentials, disconnecting client [{client}]");
                client.Disconnect();
                return false;
            }

            // Success!
            Logger.Info($"Successful auth for client [{client}]");
            LoginQueueManager.EnqueueNewClient(client);

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

            // ReadyForGameJoin is sent right after InitialClientHandshake, and we currently don't use any data from it.
            // TODO: PlayerManager shouldn't try to put clients into games until it receives this message.
            Logger.Trace($"Received NetMessageReadyForGameJoin from client [{client}]");

            return true;
        }

        #endregion
    }
}
