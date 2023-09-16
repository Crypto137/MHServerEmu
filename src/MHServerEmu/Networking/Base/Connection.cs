using System.Net;
using System.Net.Sockets;

namespace MHServerEmu.Networking.Base
{
    // NOTE: This TCP server / connection implementation was ported from mooege

    public class Connection
    {
        public static readonly int BufferSize = 16 * 1024;      // 16 KB

        private readonly Server _server;                                // The server this connection is bound to
        private readonly byte[] _recvBuffer = new byte[BufferSize];     // Receive buffer     

        public Socket Socket { get; }           // Underlying socket
        public IClient Client { get; set; }     // Bound client

        public bool IsConnected { get => Socket.Connected; }
        public IPEndPoint RemoteEndPoint { get => Socket.RemoteEndPoint as IPEndPoint; }
        public IPEndPoint LocalEndPoint { get => Socket.LocalEndPoint as IPEndPoint; }
        public byte[] RecvBuffer { get => _recvBuffer; }

        public Connection(Server server, Socket socket)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        /// <summary>
        /// Begins receiving data async.
        /// </summary>
        /// <param name="callback">Callback function to call when recv() is complete.</param>
        /// <param name="state">State manager object.</param>
        /// <returns>Returns <see cref="IAsyncResult"/></returns>
        public IAsyncResult BeginReceive(AsyncCallback callback, object state)
        {
            return Socket.BeginReceive(_recvBuffer, 0, BufferSize, SocketFlags.None, callback, state);
        }

        public int EndReceive(IAsyncResult result)
        {
            return Socket.EndReceive(result);
        }

        #region Send Methods

        /// <summary>
        /// Sends a <see cref="PacketOut"/> to remote endpoint.
        /// </summary>
        /// <param name="packet"><see cref="PacketOut"/> to send.</param>
        /// <returns>Returns count of sent bytes.</returns>
        public int Send(PacketOut packet)
        {
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            return Send(packet.Data);
        }

        /// <summary>
        /// Sends a byte buffer to remote endpoint.
        /// </summary>
        /// <param name="buffer">Byte buffer to send.</param>
        /// <returns>Returns count of sent bytes.</returns>
        public int Send(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return Send(buffer, 0, buffer.Length, SocketFlags.None);
        }


        /// <summary>
        /// Sends a byte buffer to remote endpoint.
        /// </summary>
        /// <param name="buffer">Byte buffer to send.</param>
        /// <param name="flags">Sockets flags to use.</param>
        /// <returns>Returns count of sent bytes.</returns>
        public int Send(byte[] buffer, SocketFlags flags)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return Send(buffer, 0, buffer.Length, flags);
        }

        /// <summary>
        /// Sends a byte buffer to remote endpoint.
        /// </summary>
        /// <param name="buffer">Byte buffer to send.</param>
        /// <param name="start">Start index to read from buffer.</param>
        /// <param name="count">Count of bytes to send.</param>
        /// <returns>Returns count of sent bytes.</returns>
        public int Send(byte[] buffer, int start, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return Send(buffer, start, count, SocketFlags.None);
        }

        /// <summary>
        /// Sends a byte buffer to remote endpoint.
        /// </summary>
        /// <param name="buffer">Byte buffer to send.</param>
        /// <param name="start">Start index to read from buffer.</param>
        /// <param name="count">Count of bytes to send.</param>
        /// <param name="flags">Sockets flags to use.</param>
        /// <returns>Returns count of sent bytes.</returns>
        public int Send(byte[] buffer, int start, int count, SocketFlags flags)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            return _server.Send(this, buffer, start, count, flags);
        }

        /// <summary>
        /// Sends an enumerable byte buffer to remote endpoint.
        /// </summary>
        /// <param name="data">Enumerable byte buffer to send.</param>
        /// <returns>Returns count of sent bytes.</returns>
        public int Send(IEnumerable<byte> data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return Send(data, SocketFlags.None);
        }

        /// <summary>
        /// Sends an enumerable byte buffer to remote endpoint.
        /// </summary>
        /// <param name="data">Enumerable byte buffer to send.</param>
        /// <param name="flags">Sockets flags to use.</param>
        /// <returns>Returns count of sent bytes.</returns>
        public int Send(IEnumerable<byte> data, SocketFlags flags)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            return _server.Send(this, data, flags);
        }

        #endregion

        /// <summary>
        /// Kills the connection to remote endpoint.
        /// </summary>
        public void Disconnect()
        {
            if (IsConnected) _server.Disconnect(this);
        }

        public override string ToString()
        {
            return Socket.RemoteEndPoint != null ? Socket.RemoteEndPoint.ToString() : "Not Connected!";
        }
    }
}
