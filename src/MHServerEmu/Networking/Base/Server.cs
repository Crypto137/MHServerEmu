using System.Net;
using System.Net.Sockets;
using MHServerEmu.Common;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Networking.Base
{
    // NOTE: This TCP server / connection implementation was ported from mooege

    public class Server : IDisposable
    {
        protected static readonly Logger Logger = LogManager.CreateLogger();
        private bool _disposed;

        public bool IsListening { get; private set; }
        public int Port { get; private set; }

        protected Socket _listener;
        protected Dictionary<Socket, Connection> _connectionDict = new();
        protected object _connectionLock = new();

        public delegate void ConnectionEventHandler(object sender, ConnectionEventArgs e);
        public delegate void ConnectionDataEventHandler(object sender, ConnectionDataEventArgs e);

        public event ConnectionEventHandler OnConnect;
        public event ConnectionEventHandler OnDisconnect;
        public event ConnectionDataEventHandler DataReceived;
        public event ConnectionDataEventHandler DataSent;

        public virtual void Run() { }

        #region Listener

        public virtual bool Listen(string bindIP, int port)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name, "Server has been disposed.");
            if (IsListening) throw new InvalidOperationException("Server is already listening.");

            // Create a new TCP socket and set it up
            _listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);               // Disable packet coalescing to improve responsiveness
            _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);         // Don't keep disconnected sockets around

            // Bind the listener socket
            try
            {
                _listener.Bind(new IPEndPoint(IPAddress.Parse(bindIP), port));
                Port = port;
            }
            catch (SocketException)
            {
                Logger.Fatal($"{GetType().Name} cannot bind on {bindIP}, server shutting down...");
                Shutdown();
                return false;
            }

            // Start listening for incoming connections
            _listener.Listen(10);
            IsListening = true;

            // Begin accepting connections asynchronously
            _listener.BeginAccept(AcceptCallback, null);

            return true;
        }

        private void AcceptCallback(IAsyncResult result)
        {
            if (_listener == null) return;

            try
            {
                Socket socket = _listener.EndAccept(result);                    // Finish accepting the incoming connection.
                Connection connection = new(this, socket);                      // Create a new connection.

                lock (_connectionLock) _connectionDict.Add(socket, connection); // Add the new connection to the dictionary.

                OnClientConnection(new(connection));                            // Raise the OnConnect event.

                connection.BeginReceive(ReceiveCallback, connection);           // Begin receiving on the new connection.
                _listener.BeginAccept(AcceptCallback, null);                    // Resume accepting other incoming connections.
            }
            catch (NullReferenceException) { }  // We get this after issuing server shutdown, don't need to do anything about it
            catch (Exception e)
            {
                Logger.Debug($"AcceptCallback exception: {e}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            Connection connection = result.AsyncState as Connection;    // Get the connection passed to the callback.
            if (connection == null) return;

            try
            {
                int bytesRecv = connection.EndReceive(result);  // Finish receiving data from the socket.

                if (bytesRecv > 0)
                {
                    OnDataReceived(new(connection, connection.RecvBuffer.Enumerate(0, bytesRecv)));     // Raise the DataReceived event.

                    if (connection.IsConnected)
                        connection.BeginReceive(ReceiveCallback, connection);   // Begin receiving again on the socket if it is connected.
                    else
                        RemoveConnection(connection, true);                     // Otherwise remove it from the dictionary.
                }
                else
                {
                    RemoveConnection(connection, true);         // Connection was lost.
                }
            }
            catch (SocketException)
            {
                RemoveConnection(connection, true);             // An error occured while receiving, connection has disconnected.
            }
            catch (Exception e)
            {
                Logger.Debug($"ReceiveCallback exception: {e}");
            }
        }

        public virtual int Send(Connection connection, IEnumerable<byte> data, SocketFlags flags)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (data == null) throw new ArgumentNullException("data");

            byte[] buffer = data.ToArray();
            return Send(connection, buffer, 0, buffer.Length, SocketFlags.None);
        }

        public virtual int Send(Connection connection, byte[] buffer, int start, int count, SocketFlags flags)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (buffer == null) throw new ArgumentNullException("buffer");

            int totalBytesSent = 0;
            int bytesRemaining = buffer.Length;

            try
            {
                while (bytesRemaining > 0)  // Ensure we send every byte.
                {
                    int bytesSent = connection.Socket.Send(buffer, totalBytesSent, bytesRemaining, flags);  // Send the remaining data
                    if (bytesSent > 0)
                        OnDataSent(new(connection, buffer.Enumerate(totalBytesSent, bytesSent)));           // Raise the DataSent event.

                    // Decrement bytes remaining and increment bytes sent.
                    bytesRemaining -= bytesSent;
                    totalBytesSent += bytesSent;
                }
            }
            catch (SocketException)
            {
                RemoveConnection(connection, true);     // An error occured while sending, connection has disconnected.
            }
            catch (Exception e)
            {
                Logger.Debug($"Send exception: {e}");
            }

            return totalBytesSent;
        }

        #endregion

        #region Service Methods

        public IEnumerable<Connection> GetConnections()
        {
            lock (_connectionLock)
                foreach (Connection connection in _connectionDict.Values)
                    yield return connection;
        }

        #endregion

        #region Events

        protected virtual void OnClientConnection(ConnectionEventArgs e)
        {
            var handler = OnConnect;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnClientDisconnect(ConnectionEventArgs e)
        {
            var handler = OnDisconnect;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnDataReceived(ConnectionDataEventArgs e)
        {
            var handler = DataReceived;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnDataSent(ConnectionDataEventArgs e)
        {
            var handler = DataSent;
            if (handler != null) handler(this, e);
        }

        #endregion

        #region Disconnect and Shutdown Handlers

        public virtual void DisconnectAll()
        {
            lock (_connectionLock)
            {
                foreach (Connection connection in _connectionDict.Values.Cast<Connection>().Where(conn => conn.IsConnected))    // Check if the connection is connected.
                {
                    // Disconnect and raise the OnDisconnect event.
                    connection.Socket.Disconnect(false);
                    OnClientDisconnect(new(connection));
                }

                _connectionDict.Clear();
            }
        }

        public virtual void Disconnect(Connection connection)
        {
            if (connection == null) throw new ArgumentNullException("connection");
            if (connection.IsConnected == false) return;

            connection.Socket.Disconnect(false);
            RemoveConnection(connection, true);
        }

        private void RemoveConnection(Connection connection, bool raiseEvent)
        {
            // Remove the connection from the dictionary and raise the OnDisconnection event.
            lock (_connectionLock)
                if (_connectionDict.Remove(connection.Socket) && raiseEvent)
                    OnClientDisconnect(new(connection));
        }

        public virtual void Shutdown()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name, "Server has been disposed.");
            if (IsListening == false) return;

            // Close the listener socket.
            if (_listener != null) _listener.Close();

            // Disconnect the clients.
            foreach (var connectionKvp in _connectionDict.ToList())     // Use ToList() so we don't get collection modified exception here
            {
                connectionKvp.Value.Disconnect();
            }

            _listener = null;
            IsListening = false;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Shutdown();         // Close the listener socket.
                DisconnectAll();    // Disconnect all clients.
            }

            // Dispose of unmanaged resources here.

            _disposed = true;
        }

        #endregion
    }
}
