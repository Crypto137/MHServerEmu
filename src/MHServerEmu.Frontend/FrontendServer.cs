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

            // -1 indicates infinite duration for both Task.Delay() and Socket.SendTimeout
            _receiveTimeoutMS = config.ReceiveTimeoutMS > 0 ? config.ReceiveTimeoutMS : -1;
            _sendTimeoutMS = config.SendTimeoutMS > 0 ? config.SendTimeoutMS : -1;

            if (Start(config.BindIP, int.Parse(config.Port)) == false) 
                return;
            
            Logger.Info($"FrontendServer is listening on {config.BindIP}:{config.Port}...");
        }

        // Shutdown implemented by TcpServer

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
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

            client.OnDisconnected();
        }

        protected override void OnDataReceived(TcpClientConnection connection, byte[] buffer, int length)
        {
            ((FrontendClient)connection.Client).HandleIncomingData(buffer, length);
        }

        #endregion
    }
}
