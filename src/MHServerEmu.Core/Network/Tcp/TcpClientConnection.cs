using System.Net;
using System.Net.Sockets;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network.Tcp
{
    /// <summary>
    /// A wrapper around <see cref="System.Net.Sockets.Socket"/> that represents a TCP server's connection to a client.
    /// </summary>
    public class TcpClientConnection
    {
        // 640K ought to be enough for anybody
        public const int MaxPacketSize = 1024 * 640;

        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        private readonly TcpServer _server;
        private readonly byte[] _packetBuffer = new byte[MaxPacketSize];

        public byte[] ReceiveBuffer { get; } = new byte[1024 * 8];

        public Socket Socket { get; }
        public bool Connected { get => Socket.Connected; }
        public IPEndPoint RemoteEndPoint { get => (IPEndPoint)Socket.RemoteEndPoint; }

        public ITcpClient Client { get; set; }

        /// <summary>
        /// Constructs a new client connection instance.
        /// </summary>
        public TcpClientConnection(TcpServer server, Socket socket)
        {
            _server = server;
            Socket = socket;
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
            if (Connected) _server.DisconnectClient(this);
        }

        #region Send Methods

        /// <summary>
        /// Sends a <see cref="byte"/> buffer over this connection.
        /// </summary>
        public int Send(byte[] buffer, SocketFlags flags = SocketFlags.None)
        {
            return _server.Send(this, buffer, flags);
        }

        /// <summary>
        /// Sends an <see cref="IPacket"/> over this connection.
        /// </summary>
        public int Send(IPacket packet, SocketFlags flags = SocketFlags.None)
        {
            // Keep this buffer thread-safe
            lock (_packetBuffer)
            {
                int size = packet.Serialize(_packetBuffer);
                return _server.Send(this, _packetBuffer, size, flags);
            }
        }

        #endregion

        public override string ToString()
        {
            if (HideSensitiveInformation)
                return RemoteEndPoint?.ToStringMasked();

            return RemoteEndPoint?.ToString();
        }
    }
}
