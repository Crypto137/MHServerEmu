using Gazillion;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.Grouping;
using MHServerEmu.Networking;
using MHServerEmu.Networking.Base;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Frontend
{
    public class FrontendServer : Server, IGameService
    {
        private new static readonly Logger Logger = LogManager.CreateLogger();  // Hide the Server.Logger so that this logger can show the actual server as log source.

        private readonly ServerManager _serverManager;

        public PlayerManagerService PlayerManagerService { get => _serverManager.PlayerManagerService; }
        public GroupingManagerService GroupingManagerService { get => _serverManager.GroupingManagerService; }

        public FrontendServer()
        {
            _serverManager = new(this);

            OnConnect += FrontendServer_OnConnect;
            OnDisconnect += FrontendServer_OnDisconnect;
            DataReceived += FrontendServer_DataReceived;
            DataSent += (sender, e) => { };
        }

        public override void Run()
        {
            if (Listen(ConfigManager.Frontend.BindIP, int.Parse(ConfigManager.Frontend.Port)) == false) return;
            Logger.Info($"FrontendServer is listening on {ConfigManager.Frontend.BindIP}:{ConfigManager.Frontend.Port}...");
        }

        #region Message Self-Handling

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((FrontendProtocolMessage)message.Id)
            {
                case FrontendProtocolMessage.ClientCredentials:
                    ClientCredentials credentials = ClientCredentials.ParseFrom(message.Payload);
                    Logger.Info($"Received ClientCredentials on mux channel {muxId}");
                    _serverManager.PlayerManagerService.HandleClientCredentials(client, credentials);
                    break;

                case FrontendProtocolMessage.InitialClientHandshake:
                    InitialClientHandshake handshake = InitialClientHandshake.ParseFrom(message.Payload);
                    Logger.Info($"Received InitialClientHandshake for {handshake.ServerType} on mux channel {muxId}");

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

        #endregion

        #region Event Handling

        private void FrontendServer_OnConnect(object sender, ConnectionEventArgs e)
        {
            Logger.Info($"Client connected from {e.Connection}");
            e.Connection.Client = new FrontendClient(e.Connection, _serverManager);
        }

        private void FrontendServer_OnDisconnect(object sender, ConnectionEventArgs e)
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

        private void FrontendServer_DataReceived(object sender, ConnectionDataEventArgs e)
        {
            ((FrontendClient)e.Connection.Client).Parse(e);
        }

        #endregion
    }
}
