using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Grouping;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Frontend
{
    public class FrontendServer : TcpServer, IGameService
    {
        private new static readonly Logger Logger = LogManager.CreateLogger();  // Hide the Server.Logger so that this logger can show the actual server as log source.

        public override void Run()
        {
            if (Start(ConfigManager.Frontend.BindIP, int.Parse(ConfigManager.Frontend.Port)) == false) return;
            Logger.Info($"FrontendServer is listening on {ConfigManager.Frontend.BindIP}:{ConfigManager.Frontend.Port}...");
        }

        // Shutdown implemented by TcpServer

        public void Handle(ITcpClient tcpClient, GameMessage message)
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
                    Logger.Warn($"Received unhandled message {(FrontendProtocolMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(ITcpClient client, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages)
                Handle(client, message);
        }

        public string GetStatus()
        {
            return "Running";
        }


        #region Event Handling

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
                var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
                playerManager?.RemovePlayer(client);

                var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as GroupingManagerService;
                groupingManager?.RemovePlayer(client);

                Logger.Info($"Client {client.Session.Account} disconnected");
            }
        }

        protected override void OnDataReceived(TcpClientConnection connection, byte[] data)
        {
            ((FrontendClient)connection.Client).Parse(data);
        }

        #endregion

        #region Message Self-Handling

        private void OnClientCredentials(FrontendClient client, ClientCredentials credentials)
        {
            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null)
            {
                Logger.Error($"OnClientCredentials(): Failed to connect to the player manager");
                return;
            }

            playerManager.OnClientCredentials(client, credentials);
        }

        private void OnInitialClientHandshake(FrontendClient client, InitialClientHandshake handshake)
        {
            var playerManager = ServerManager.Instance.GetGameService(ServerType.PlayerManager) as PlayerManagerService;
            if (playerManager == null)
            {
                Logger.Error($"OnClientCredentials(): Failed to connect to the player manager");
                return;
            }

            var groupingManager = ServerManager.Instance.GetGameService(ServerType.GroupingManager) as GroupingManagerService;
            if (groupingManager == null)
            {
                Logger.Error($"OnClientCredentials(): Failed to connect to the grouping manager");
                return;
            }

            Logger.Info($"Received InitialClientHandshake for {handshake.ServerType}");

            if (handshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND && client.FinishedPlayerManagerHandshake == false)
                playerManager.AcceptClientHandshake(client);
            else if (handshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND && client.FinishedGroupingManagerHandshake == false)
                groupingManager.AcceptClientHandshake(client);

            // Add the player to a game when both handshakes are finished
            // Adding the player early can cause GroupingManager handshake to not finish properly, which leads to the chat not working
            if (client.FinishedPlayerManagerHandshake && client.FinishedGroupingManagerHandshake)
            {
                // Disconnect the client if the account is already logged in
                // TODO: disconnect the logged in player instead?
                if (groupingManager.AddPlayer(client) == false) client.Connection.Disconnect();
                if (playerManager.AddPlayer(client) == false) client.Connection.Disconnect();
            }
        }

        #endregion
    }
}
