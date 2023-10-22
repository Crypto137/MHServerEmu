using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Networking;
using MHServerEmu.Networking.Base;

namespace MHServerEmu.Frontend
{
    public class FrontendService : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly ServerManager _serverManager;

        public FrontendService(ServerManager serverManager)
        {
            _serverManager = serverManager;
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials:
                    ClientCredentials credentials = ClientCredentials.ParseFrom(message.Payload);
                    Logger.Info($"Received ClientCredentials on muxId {muxId}");
                    _serverManager.PlayerManagerService.HandleClientCredentials(client, credentials);
                    break;

                case FrontendProtocolMessage.InitialClientHandshake:
                    InitialClientHandshake handshake = InitialClientHandshake.ParseFrom(message.Payload);
                    Logger.Info($"Received InitialClientHandshake for {handshake.ServerType} on muxId {muxId}");
                    HandleInitialClientHandshake(client, handshake);
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(FrontendProtocolMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }

        public void OnClientDisconnect(object sender, ConnectionEventArgs e)
        {
            FrontendClient client = e.Connection.Client as FrontendClient;

            if (client.Session == null)
            {
                Logger.Info("Client disconnected");
            }
            else
            {
                _serverManager.PlayerManagerService.RemovePlayer(client);
                _serverManager.GroupingManagerService.RemovePlayer(client);
                Logger.Info($"Client {client.Session.Account} disconnected");
            }
        }

        private void HandleInitialClientHandshake(FrontendClient client, InitialClientHandshake handshake)
        {
            if (handshake.ServerType == PubSubServerTypes.PLAYERMGR_SERVER_FRONTEND && client.FinishedPlayerManagerHandshake == false)
                _serverManager.PlayerManagerService.AcceptClientHandshake(client);
            else if (handshake.ServerType == PubSubServerTypes.GROUPING_MANAGER_FRONTEND && client.FinishedGroupingManagerHandshake == false)
                _serverManager.GroupingManagerService.AcceptClientHandshake(client);

            // Add the player to a game when both handshakes are finished
            // Adding the player early can cause GroupingManager handshake to not finish properly, which leads to the chat not working
            if (client.FinishedPlayerManagerHandshake && client.FinishedGroupingManagerHandshake)
            {
                _serverManager.GroupingManagerService.AddPlayer(client);
                _serverManager.PlayerManagerService.AddPlayer(client);
            }
        }
    }
}
