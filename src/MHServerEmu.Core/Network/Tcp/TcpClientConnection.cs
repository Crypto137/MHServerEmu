using System.Net;
using System.Net.Sockets;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network.Tcp
{
    /// <summary>
    /// A wrapper around <see cref="System.Net.Sockets.Socket"/> that represents a TCP server's connection to a client.
    /// </summary>
    public class TcpClientConnection
    {
        public const int ReceiveBufferSize = 1024 * 8;

        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        private readonly TcpServer _server;

        public byte[] ReceiveBuffer { get; } = new byte[ReceiveBufferSize];

        public Socket Socket { get; }
        public bool Connected { get => Socket.Connected; }
        public IPEndPoint RemoteEndPoint { get => (IPEndPoint)Socket.RemoteEndPoint; }

        public TcpClient Client { get; internal set; }
        public bool IsReceiveTimeoutSuspended { get; set; }

        /// <summary>
        /// Constructs a new client connection instance.
        /// </summary>
        public TcpClientConnection(TcpServer server, Socket socket)
        {
            _server = server;
            Socket = socket;
        }

        public override string ToString()
        {
            if (RemoteEndPoint == null)
                return "NULL";

            if (HideSensitiveInformation)
                return $"0x{HashHelper.Djb2(RemoteEndPoint.Address.ToString()):X4}";

            return RemoteEndPoint.ToString();
        }

        /// <summary>
        /// Receives data from a connection asynchronously.
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReceiveAsync()
        {
            return await Socket.ReceiveAsync(ReceiveBuffer, SocketFlags.None);
        }

        /// <summary>
        /// Disconnects this client connection.
        /// </summary>
        public void Disconnect()
        {
            if (Connected)
                _server.DisconnectClient(this);
        }

        /// <summary>
        /// Sends a <see cref="byte"/> buffer over this connection.
        /// </summary>
        public void Send(byte[] buffer, int size, SocketFlags flags = SocketFlags.None)
        {
            _server.Send(this, buffer, size, flags);
        }

        /// <summary>
        /// Sends an <see cref="IPacket"/> over this connection.
        /// </summary>
        public void Send<T>(T packet, SocketFlags flags = SocketFlags.None) where T: IPacket
        {
            _server.Send(this, packet, flags);
        }
    }
}
