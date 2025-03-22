using System.Collections.Concurrent;
using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Frontend
{
    /// <summary>
    /// A <see cref="TcpServer"/> that clients connect to.
    /// </summary>
    public class FrontendServer : TcpServer, IGameService
    {
        private new static readonly Logger Logger = LogManager.CreateLogger();  // Hide the Server.Logger so that this logger can show the actual server as log source.

        private readonly ConcurrentQueue<(FrontendClient, MessagePackage)> _pendingMessageQueue = new();

        #region IGameService Implementation

        public override void Run()
        {
            var config = ConfigManager.Instance.GetConfig<FrontendConfig>();

            // -1 indicates infinite duration for both Task.Delay() and Socket.SendTimeout
            _receiveTimeoutMS = config.ReceiveTimeoutMS > 0 ? config.ReceiveTimeoutMS : -1;
            _sendTimeoutMS = config.SendTimeoutMS > 0 ? config.SendTimeoutMS : -1;

            if (Start(config.BindIP, int.Parse(config.Port)) == false) return;
            Logger.Info($"FrontendServer is listening on {config.BindIP}:{config.Port}...");

            while (_isRunning)
            {
                if (_pendingMessageQueue.IsEmpty == false)
                {
                    while (_pendingMessageQueue.TryDequeue(out var pendingMessage))
                    {
                        HandlePendingMessage(pendingMessage.Item1, pendingMessage.Item2);
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        // Shutdown implemented by TcpServer

        public void Handle(ITcpClient tcpClient, MessagePackage message)
        {
            _pendingMessageQueue.Enqueue(((FrontendClient)tcpClient, message));
        }

        public void Handle(ITcpClient client, IReadOnlyList<MessagePackage> messages)
        {
            for (int i = 0; i < messages.Count; i++)
                Handle(client, messages[i]);
        }

        public void Handle(ITcpClient client, MailboxMessage message)
        {
            Logger.Warn($"Handle(): Unhandled MailboxMessage");
        }

        public string GetStatus()
        {
            return $"Connections: {ConnectionCount}";
        }

        #endregion

        #region TCP Server Event Handling

        protected override void OnClientConnected(TcpClientConnection connection)
        {
            Logger.Info($"Client connected from {connection}");
            connection.Client = new FrontendClient(connection);
        }

        protected override void OnClientDisconnected(TcpClientConnection connection)
        {
            var client = (FrontendClient)connection.Client;
            Logger.Info($"Client [{client}] disconnected");

            if (client.Session != null)
            {
                var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as IFrontendService;
                playerManager?.RemoveFrontendClient(client);

                var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as IFrontendService;
                groupingManager?.RemoveFrontendClient(client);
            }
        }

        protected override void OnDataReceived(TcpClientConnection connection, byte[] buffer, int length)
        {
            ((FrontendClient)connection.Client).Parse(buffer, length);
        }

        #endregion

        #region Message Handling

        private bool HandlePendingMessage(FrontendClient client, MessagePackage message)
        {
            // Skip messages from clients that have already disconnected
            if (client.Connection.Connected == false)
                return Logger.WarnReturn(false, $"HandlePendingMessage(): Client [{client}] has already disconnected");

            // Route to the destination service if initial frontend business has already been done
            if (message.MuxId == 1 && client.FinishedPlayerManagerHandshake)
            {
                ServerManager.Instance.RouteMessage(client, message, ServerType.PlayerManager);
                return true;
            }
            else if (message.MuxId == 2 && client.FinishedGroupingManagerHandshake)
            {
                ServerManager.Instance.RouteMessage(client, message, ServerType.GroupingManager);
                return true;
            }

            // Self-handling for initial connection
            message.Protocol = typeof(FrontendProtocolMessage);

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials:         OnClientCredentials(client, message); break;
                case FrontendProtocolMessage.InitialClientHandshake:    OnInitialClientHandshake(client, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(FrontendProtocolMessage)message.Id} [{message.Id}]"); break;
            }

            return true;
        }

        /// <summary>
        /// Handles <see cref="ClientCredentials"/>.
        /// </summary>
        private bool OnClientCredentials(FrontendClient client, MessagePackage message)
        {
            var clientCredentials = message.Deserialize() as ClientCredentials;
            if (clientCredentials == null) return Logger.WarnReturn(false, $"OnClientCredentials(): Failed to retrieve message");

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as IFrontendService;
            if (playerManager == null) Logger.ErrorReturn(false, $"OnClientCredentials(): Failed to connect to the player manager");

            playerManager.ReceiveFrontendMessage(client, clientCredentials);
            return true;
        }

        /// <summary>
        /// Handles <see cref="InitialClientHandshake"/>.
        /// </summary>
        private bool OnInitialClientHandshake(FrontendClient client, MessagePackage message)
        {
            var initialClientHandshake = message.Deserialize() as InitialClientHandshake;
            if (initialClientHandshake == null) return Logger.WarnReturn(false, $"OnInitialClientHandshake(): Failed to retrieve message");

            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as IFrontendService;
            if (playerManager == null) return Logger.ErrorReturn(false, $"OnClientCredentials(): Failed to connect to the player manager");

            var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as IFrontendService;
            if (groupingManager == null) return Logger.ErrorReturn(false, $"OnClientCredentials(): Failed to connect to the grouping manager");

            Logger.Trace($"Received InitialClientHandshake for {initialClientHandshake.ServerType}");

            if (initialClientHandshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND && client.FinishedPlayerManagerHandshake == false)
                playerManager.ReceiveFrontendMessage(client, initialClientHandshake);
            else if (initialClientHandshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND && client.FinishedGroupingManagerHandshake == false)
                groupingManager.ReceiveFrontendMessage(client, initialClientHandshake);

            // Add the player to a game when both handshakes are finished
            // Adding the player early can cause GroupingManager handshake to not finish properly, which leads to the chat not working
            if (client.FinishedPlayerManagerHandshake && client.FinishedGroupingManagerHandshake)
            {
                // Add to the player manager first to handle duplicate login if there is one
                playerManager.AddFrontendClient(client);
                groupingManager.AddFrontendClient(client);
            }

            return true;
        }

        #endregion
    }
}
