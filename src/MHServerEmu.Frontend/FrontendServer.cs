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

        #region IGameService Implementation

        public override void Run()
        {
            var config = ConfigManager.Instance.GetConfig<FrontendConfig>();

            if (Start(config.BindIP, int.Parse(config.Port)) == false) return;
            Logger.Info($"FrontendServer is listening on {config.BindIP}:{config.Port}...");
        }

        // Shutdown implemented by TcpServer

        public void Handle(ITcpClient tcpClient, MessagePackage message)
        {
            var client = (FrontendClient)tcpClient;

            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials:
                    if (message.TryDeserialize<ClientCredentials>(out var credentials))
                        OnClientCredentials(client, credentials);
                    break;

                case FrontendProtocolMessage.InitialClientHandshake:
                    if (message.TryDeserialize<InitialClientHandshake>(out var handshake))
                        OnInitialClientHandshake(client, handshake);
                    break;

                default:
                    Logger.Warn($"Handle(): Unhandled message [{message.Id}] {(FrontendProtocolMessage)message.Id}");
                    break;
            }
        }

        public void Handle(ITcpClient client, IEnumerable<MessagePackage> messages)
        {
            foreach (MessagePackage message in messages)
                Handle(client, message);
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

            if (client.Session == null)
            {
                Logger.Info("Client disconnected");
            }
            else
            {
                var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as IFrontendService;
                playerManager?.RemoveFrontendClient(client);

                var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as IFrontendService;
                groupingManager?.RemoveFrontendClient(client);

                Logger.Info($"Client {client.Session.Account} disconnected");
            }
        }

        protected override void OnDataReceived(TcpClientConnection connection, byte[] data)
        {
            ((FrontendClient)connection.Client).Parse(data);
        }

        #endregion

        #region Message Self-Handling

        /// <summary>
        /// Handles <see cref="ClientCredentials"/>.
        /// </summary>
        private void OnClientCredentials(FrontendClient client, ClientCredentials credentials)
        {
            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as IFrontendService;
            if (playerManager == null)
            {
                Logger.Error($"OnClientCredentials(): Failed to connect to the player manager");
                return;
            }

            playerManager.ReceiveFrontendMessage(client, credentials);
        }

        /// <summary>
        /// Handles <see cref="InitialClientHandshake"/>.
        /// </summary>
        private void OnInitialClientHandshake(FrontendClient client, InitialClientHandshake handshake)
        {
            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as IFrontendService;
            if (playerManager == null)
            {
                Logger.Error($"OnClientCredentials(): Failed to connect to the player manager");
                return;
            }

            var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as IFrontendService;
            if (groupingManager == null)
            {
                Logger.Error($"OnClientCredentials(): Failed to connect to the grouping manager");
                return;
            }

            Logger.Info($"Received InitialClientHandshake for {handshake.ServerType}");

            if (handshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND && client.FinishedPlayerManagerHandshake == false)
                playerManager.ReceiveFrontendMessage(client, handshake);
            else if (handshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND && client.FinishedGroupingManagerHandshake == false)
                groupingManager.ReceiveFrontendMessage(client, handshake);

            // Add the player to a game when both handshakes are finished
            // Adding the player early can cause GroupingManager handshake to not finish properly, which leads to the chat not working
            if (client.FinishedPlayerManagerHandshake && client.FinishedGroupingManagerHandshake)
            {
                // Add to the player manager first to handle duplicate login if there is one
                playerManager.AddFrontendClient(client);
                groupingManager.AddFrontendClient(client);
            }
        }

        #endregion
    }
}
