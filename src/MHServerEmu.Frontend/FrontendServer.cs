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

        private readonly HashSet<FrontendClient> _clients = new();

        public GameServiceState State { get; private set; } = GameServiceState.Created;

        #region IGameService Implementation

        public override void Run()
        {
            var config = ConfigManager.Instance.GetConfig<FrontendConfig>();

            IFrontendClient.FrontendAddress = config.PublicAddress;
            IFrontendClient.FrontendPort = config.Port;

            // -1 indicates infinite duration for both Task.Delay() and Socket.SendTimeout
            _receiveTimeoutMS = config.ReceiveTimeoutMS > 0 ? config.ReceiveTimeoutMS : -1;
            _sendTimeoutMS = config.SendTimeoutMS > 0 ? config.SendTimeoutMS : -1;

            if (Start(config.BindIP, int.Parse(config.Port)) == false) 
                return;
            
            Logger.Info($"FrontendServer is listening on {config.BindIP}:{config.Port}...");
            State = GameServiceState.Running;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            State = GameServiceState.Shutdown;
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public void GetStatus(Dictionary<string, long> statusDict)
        {
            statusDict["FrontendConnections"] = ConnectionCount;
            statusDict["FrontendClients"] = _clients.Count;
        }

        #endregion

        #region TCP Server Event Handling

        protected override void OnClientConnected(TcpClientConnection connection)
        {
            Logger.Info($"Client connected from {connection}");

            _clients.Add(new FrontendClient(connection));
        }

        protected override void OnClientDisconnected(TcpClientConnection connection)
        {
            var client = (FrontendClient)connection.Client;
            Logger.Info($"Client [{client}] disconnected");

            client.OnDisconnected();

            _clients.Remove(client);
        }

        protected override void OnDataReceived(TcpClientConnection connection, byte[] buffer, int length)
        {
            ((FrontendClient)connection.Client).HandleIncomingData(buffer, length);
        }

        #endregion
    }
}
