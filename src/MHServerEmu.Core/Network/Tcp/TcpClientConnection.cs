using System.Buffers;
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
        public const int ReceiveBufferSize = 1024 * 8;

        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        private readonly TcpServer _server;

        public byte[] ReceiveBuffer { get; } = new byte[ReceiveBufferSize];

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
            if (Connected)
                _server.DisconnectClient(this);
        }

        #region Send Methods

        /// <summary>
        /// Sends a <see cref="byte"/> buffer over this connection.
        /// </summary>
        public int Send(byte[] buffer, int size, SocketFlags flags = SocketFlags.None)
        {
            // Send one message at a time for each client
            lock (_server)
                return _server.Send(this, buffer, size, flags);
        }

        private static readonly Logger Logger = LogManager.CreateLogger();

        /// <summary>
        /// Sends an <see cref="IPacket"/> over this connection.
        /// </summary>
        public int Send(IPacket packet, SocketFlags flags = SocketFlags.None)
        {
            int size = packet.SerializedSize;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            
            packet.Serialize(buffer);
            int sent = Send(buffer, size, flags);
            
            ArrayPool<byte>.Shared.Return(buffer);
            return sent;
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
