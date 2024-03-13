using System.Net;
using System.Net.Sockets;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network.Tcp
{
    /// <summary>
    /// An abstract TCP server implementation.
    /// </summary>
    public abstract class TcpServer : IDisposable
    {
        protected static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<Socket, TcpClientConnection> _connectionDict = new();
        private readonly object _connectionLock = new();

        private CancellationTokenSource _cts;

        private Socket _listener;
        private bool _isListening;
        private bool _isDisposed;

        /// <summary>
        /// Runs the server. This method should generally be executed by its own <see cref="Thread"/>.
        /// </summary>
        public abstract void Run();

        /// <summary>
        /// Creates a new socket and begins listening on the specified IP and port.
        /// </summary>
        public virtual bool Start(string bindIP, int port)
        {
            if (_isDisposed) throw new ObjectDisposedException(GetType().Name, "Server is disposed.");
            if (_isListening) throw new InvalidOperationException("Server is already listening.");

            // Reset CTS
            _cts?.Dispose();
            _cts = new();

            // Create a new listener socket
            _listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                LingerState = new(false, 0)
            };

            // Try to bind it
            try
            {
                _listener.Bind(new IPEndPoint(IPAddress.Parse(bindIP), port));
            }
            catch (SocketException)
            {
                Logger.Fatal($"{GetType().Name} cannot bind on {bindIP}, server shutting down...");
                Shutdown();
                return false;
            }

            // Start listening
            _listener.Listen();
            _isListening = true;

            // Start accepting connections
            Task.Run(async () => await AcceptConnections());

            return true;
        }

        /// <summary>
        /// Cancels async tasks, stops listening for connections, and disconnects all connected clients.
        /// </summary>
        public virtual void Shutdown()
        {
            if (_isDisposed) throw new ObjectDisposedException(GetType().Name, "Server is disposed.");
            if (_isListening == false) return;

            // Cancel async tasks
            _cts.Cancel();

            // Close the listener socket
            _listener?.Close();
            _listener = null;
            _isListening = false;

            // Disconnect all clients
            DisconnectAllClients();
        }

        /// <summary>
        /// Disconnects the specified client connection.
        /// </summary>
        public virtual void DisconnectClient(TcpClientConnection connection)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (connection.Connected == false) return;

            connection.Socket.Disconnect(false);
            RemoveClientConnection(connection);
        }

        /// <summary>
        /// Disconnects all connected clients.
        /// </summary>
        public virtual void DisconnectAllClients()
        {
            // Disconnect all clients within a single lock to prevent new clients from connecting while we do it
            lock (_connectionLock)
            {
                foreach (TcpClientConnection connection in _connectionDict.Values)
                {
                    if (connection.Connected == false) continue;
                    connection.Socket.Disconnect(false);
                    OnClientDisconnected(connection);
                }

                _connectionDict.Clear();
            }
        }

        /// <summary>
        /// Sends a data buffer over the provided client connection.
        /// </summary>
        public int Send(TcpClientConnection connection, byte[] buffer, SocketFlags flags)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            int bytesSentTotal = 0;
            int bytesRemaining = buffer.Length;

            try
            {
                while (bytesRemaining > 0)      // Send all bytes from our buffer
                {
                    int bytesSent = connection.Socket.Send(buffer, bytesSentTotal, bytesRemaining, flags);
                    bytesRemaining -= bytesSent;
                    bytesSentTotal += bytesSent;
                }
            }
            catch (SocketException) { RemoveClientConnection(connection); }
            catch (Exception e) { Logger.DebugException(e, nameof(Send)); }

            return bytesSentTotal;
        }

        #region Events

        /// <summary>
        /// Raised when a client connects.
        /// </summary>
        protected abstract void OnClientConnected(TcpClientConnection connection);

        /// <summary>
        /// Raised when a client disconnects.
        /// </summary>
        protected abstract void OnClientDisconnected(TcpClientConnection connection);

        /// <summary>
        /// Raised when the server receives data from a client connection.
        /// </summary>
        protected abstract void OnDataReceived(TcpClientConnection connection, byte[] data);

        #endregion

        /// <summary>
        /// Removes the specified client connection from the server's connection dictionary.
        /// </summary>
        private void RemoveClientConnection(TcpClientConnection connection)
        {
            lock (_connectionLock)
            {
                if (_connectionDict.Remove(connection.Socket))
                    OnClientDisconnected(connection);
            }
        }

        /// <summary>
        /// Accepts incoming client connections.
        /// </summary>
        private async Task AcceptConnections()
        {
            while (true)
            {
                try
                {
                    // Wait for a connection
                    Socket socket = await _listener.AcceptAsync().WaitAsync(_cts.Token);

                    // Establish a new client connection
                    TcpClientConnection connection = new(this, socket);
                    lock (_connectionLock) _connectionDict.Add(socket, connection);
                    OnClientConnected(connection);

                    // Begin receiving data from our new connection
                    _ = Task.Run(async () => await ReceiveData(connection));
                }
                catch (TaskCanceledException) { return; }
                catch (Exception e) { Logger.DebugException(e, nameof(AcceptConnections)); }
            }
        }

        /// <summary>
        /// Receives data from a client connection.
        /// </summary>
        private async Task ReceiveData(TcpClientConnection connection)
        {
            while (true)
            {
                try
                {
                    int bytesReceived = await connection.ReceiveAsync().WaitAsync(_cts.Token);

                    if (bytesReceived == 0)             // Connection lost
                    {
                        RemoveClientConnection(connection);
                        return;
                    }

                    // Copy the data we received from the buffer to a new array
                    byte[] data = new byte[bytesReceived];
                    Array.Copy(connection.ReceiveBuffer, data, bytesReceived);
                    OnDataReceived(connection, data);

                    if (connection.Connected == false)  // Stop receiving if no longer connected
                    {
                        RemoveClientConnection(connection);
                        return;
                    }
                }
                catch (SocketException)
                {
                    RemoveClientConnection(connection);
                    return;
                }
                catch (TaskCanceledException) { return; }
                catch (Exception e)
                {
                    Logger.DebugException(e, nameof(ReceiveData));
                    return;
                }
            }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            // Dispose of unmanaged resources here.
            if (disposing)
            {
                Shutdown();
                _cts.Dispose();
            }

            _isDisposed = true;
        }

        #endregion
    }
}
