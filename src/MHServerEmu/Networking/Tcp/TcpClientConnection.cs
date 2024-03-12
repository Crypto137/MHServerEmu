using System.Net;
using System.Net.Sockets;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Networking.Tcp
{
    /// <summary>
    /// A wrapper around <see cref="System.Net.Sockets.Socket"/> that represents a TCP server's connection to a client.
    /// </summary>
    public class TcpClientConnection
    {
        private readonly TcpServer _server;

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
        /// Sends a data buffer over this connection.
        /// </summary>
        public int Send(byte[] buffer, SocketFlags flags = SocketFlags.None)
        {
            return _server.Send(this, buffer, flags);
        }

        /// <summary>
        /// Sends a packet over this connection.
        /// </summary>
        public int Send(IPacket packet, SocketFlags flags = SocketFlags.None)
        {
            return Send(packet.Data, flags);
        }

        #endregion

        public override string ToString()
        {
            if (ConfigManager.PlayerManager.HideSensitiveInformation)
                return RemoteEndPoint?.ToStringMasked();

            return RemoteEndPoint?.ToString();
        }
    }
}
