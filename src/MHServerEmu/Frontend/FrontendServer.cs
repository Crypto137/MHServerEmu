using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Networking;
using MHServerEmu.Networking.Tcp;

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
                ServerManager.Instance.PlayerManagerService.RemovePlayer(client);
                ServerManager.Instance.GroupingManagerService.RemovePlayer(client);
                Logger.Info($"Client {client.Session.Account} disconnected");
            }
        }

        protected override void OnDataReceived(TcpClientConnection connection, byte[] data)
        {
            ((FrontendClient)connection.Client).Parse(data);
        }

        #endregion

        #region Message Self-Handling

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials:
                    if (message.TryDeserialize<ClientCredentials>(out var credentials))
                        ServerManager.Instance.PlayerManagerService.OnClientCredentials(client, credentials);
                    break;

                case FrontendProtocolMessage.InitialClientHandshake:
                    if (message.TryDeserialize<InitialClientHandshake>(out var handshake) == false) return;

                    Logger.Info($"Received InitialClientHandshake for {handshake.ServerType} on mux channel {muxId}");

                    if (handshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND && client.FinishedPlayerManagerHandshake == false)
                        ServerManager.Instance.PlayerManagerService.AcceptClientHandshake(client);
                    else if (handshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND && client.FinishedGroupingManagerHandshake == false)
                        ServerManager.Instance.GroupingManagerService.AcceptClientHandshake(client);

                    // Add the player to a game when both handshakes are finished
                    // Adding the player early can cause GroupingManager handshake to not finish properly, which leads to the chat not working
                    if (client.FinishedPlayerManagerHandshake && client.FinishedGroupingManagerHandshake)
                    {
                        // Disconnect the client if the account is already logged in
                        // TODO: disconnect the logged in player instead?
                        if (ServerManager.Instance.GroupingManagerService.AddPlayer(client) == false) client.Connection.Disconnect();
                        if (ServerManager.Instance.PlayerManagerService.AddPlayer(client) == false) client.Connection.Disconnect();
                    }

                    break;

                default:
                    Logger.Warn($"Received unhandled message {(FrontendProtocolMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages)
                Handle(client, muxId, message);
        }

        #endregion
    }
}
