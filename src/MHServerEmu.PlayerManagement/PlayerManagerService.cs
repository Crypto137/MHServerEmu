using System.Diagnostics;
using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games;
using MHServerEmu.PlayerManagement.Auth;
using MHServerEmu.PlayerManagement.Games;
using MHServerEmu.PlayerManagement.Matchmaking;
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
        internal RegionRequestQueueManager RegionRequestQueueManager { get; }

        internal PlayerManagerEventScheduler EventScheduler { get; }

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
            RegionRequestQueueManager = new(this);

            EventScheduler = new();

            Config = ConfigManager.Instance.GetConfig<PlayerManagerConfig>();
        }

        #region IGameService Implementation

        public void Run()
        {
            Instance = this;
            State = GameServiceState.Starting;

            RegionRequestQueueManager.Initialize();

            State = GameServiceState.Running;
            while (State == GameServiceState.Running)
            {
                TimeSpan referenceTime = _stopwatch.Elapsed;

                _serviceMailbox.ProcessMessages();

                SessionManager.Update();
                LoginQueueManager.Update();
                ClientManager.Update();
                CommunityRegistry.Update();

                EventScheduler.TriggerEvents();

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
                // Message buffers are routed right away to have the lowest latency possible.
                case ServiceMessage.RouteMessageBuffer routeMessagePackage:
                    OnRouteMessageBuffer(routeMessagePackage);
                    break;

                // Regular service messages are handled by the service thread when the next tick comes.
                default:
                    _serviceMailbox.PostMessage(message);
                    break;
            }
        }

        public void GetStatus(Dictionary<string, long> statusDict)
        {
            statusDict["PlayerManagerGames"] = GameHandleManager.GameCount;
            statusDict["PlayerManagerPlayers"] = ClientManager.PlayerCount;
            statusDict["PlayerManagerActiveSessions"] = SessionManager.ActiveSessionCount;
            statusDict["PlayerManagerPendingSessions"] = SessionManager.PendingSessionCount;
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
