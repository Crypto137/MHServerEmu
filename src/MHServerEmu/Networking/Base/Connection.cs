using System.Net;
using System.Net.Sockets;

namespace MHServerEmu.Networking.Base
{
    // NOTE: This TCP server / connection implementation was ported from mooege

    public class Connection
    {
        public static readonly int BufferSize = 16 * 1024;      // 16 KB

        private readonly Server _server;
        private readonly byte[] _recvBuffer = new byte[BufferSize];

        public Socket Socket { get; }
        public IClient Client { get; set; }

        public bool IsConnected { get => Socket.Connected; }
        public IPEndPoint RemoteEndPoint { get => Socket.RemoteEndPoint as IPEndPoint; }
        public IPEndPoint LocalEndPoint { get => Socket.LocalEndPoint as IPEndPoint; }
        public byte[] RecvBuffer { get => _recvBuffer; }

        public Connection(Server server, Socket socket)
        {
            if (server == null) throw new ArgumentNullException("server");
            if (socket == null) throw new ArgumentNullException("socket");

            _server = server;
            Socket = socket;
        }

        public IAsyncResult BeginReceive(AsyncCallback callback, object state)
        {
            return Socket.BeginReceive(_recvBuffer, 0, BufferSize, SocketFlags.None, callback, state);
        }

        public int EndReceive(IAsyncResult result)
        {
            return Socket.EndReceive(result);
        }

        public int Send(PacketOut packet)
        {
            if (packet == null) throw new ArgumentNullException("packet");
            return Send(packet.Data);
        }

        public int Send(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            return Send(buffer, 0, buffer.Length, SocketFlags.None);
        }

        public int Send(byte[] buffer, SocketFlags flags)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            return Send(buffer, 0, buffer.Length, flags);
        }

        public int Send(byte[] buffer, int start, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            return Send(buffer, start, count, SocketFlags.None);
        }

        public int Send(byte[] buffer, int start, int count, SocketFlags flags)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            return _server.Send(this, buffer, start, count, flags);
        }

        public int Send(IEnumerable<byte> data)
        {
            if (data == null) throw new ArgumentNullException("data");
            return Send(data, SocketFlags.None);
        }

        public int Send(IEnumerable<byte> data, SocketFlags flags)
        {
            if (data == null) throw new ArgumentNullException("data");
            return _server.Send(this, data, flags);
        }

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
