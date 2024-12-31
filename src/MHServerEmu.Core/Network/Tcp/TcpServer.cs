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

        protected bool _isRunning;

        public int ConnectionCount { get => _connectionDict.Count; }

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
            Task.Run(async () => await AcceptConnectionsAsync());

            _isRunning = true;

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

            _isRunning = false;
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
        /// Sends data over the provided <see cref="TcpClientConnection">.
        /// </summary>
        public int Send(TcpClientConnection connection, byte[] buffer, SocketFlags flags)
        {
            return Send(connection, buffer, buffer.Length, flags);
        }

        /// <summary>
        /// Sends data over the provided <see cref="TcpClientConnection">.
        /// </summary>
        public int Send(TcpClientConnection connection, byte[] buffer, int size, SocketFlags flags)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));

            int bytesSentTotal = 0;
            int bytesRemaining = size;

            try
            {
                while (bytesRemaining > 0)      // Send all bytes from our buffer
                {
                    int bytesSent = connection.Socket.Send(buffer, bytesSentTotal, bytesRemaining, flags);
                    bytesRemaining -= bytesSent;
                    bytesSentTotal += bytesSent;
                }
            }
            catch (SocketException)
            {
                RemoveClientConnection(connection);
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, nameof(Send));
            }

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
        protected abstract void OnDataReceived(TcpClientConnection connection, byte[] buffer, int length);

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
        /// Accepts incoming client connections asynchronously.
        /// </summary>
        private async Task AcceptConnectionsAsync()
        {
            const int SendTimeout = 6000;           // Exile-like
            const int SendBufferSize = 1024 * 512;  // 512 KB

            const int MaxErrorCount = 100;
            int errorCount = 0;

            while (true)
            {
                try
                {
                    // Wait for a connection
                    Socket socket = await _listener.AcceptAsync().WaitAsync(_cts.Token);
                    socket.SendTimeout = SendTimeout;
                    socket.SendBufferSize = SendBufferSize;

                    // Establish a new client connection
                    TcpClientConnection connection = new(this, socket);
                    lock (_connectionLock) _connectionDict.Add(socket, connection);
                    OnClientConnected(connection);

                    // Begin receiving data from our new connection
                    _ = Task.Run(async () => await ReceiveDataAsync(connection));

                    // Reset the error counter if everything is fine
                    errorCount = 0;
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Logger.ErrorException(e, nameof(AcceptConnectionsAsync));

                    // Limit the number of errors in a row to prevent the server from infinitely writing error messages when it's stuck in an error loop.
                    // We have only a single report of this happening so far, which was on Linux, but better safe than sorry.
                    if (errorCount >= MaxErrorCount)
                        throw new($"AcceptConnectionsAsync: Maximum error count ({MaxErrorCount}) reached.");
                }
            }
        }

        /// <summary>
        /// Receives data from a <see cref="TcpClientConnection"/> asynchronously.
        /// </summary>
        private async Task ReceiveDataAsync(TcpClientConnection connection)
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

                    // Parse received data straight from the connection's buffer.
                    // NOTE: We do it in a somewhat awkward way because we used to copy
                    // data to a new array and pass it around, maybe we should refactor this more.
                    OnDataReceived(connection, connection.ReceiveBuffer, bytesReceived);

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
                catch (TaskCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Logger.ErrorException(e, nameof(ReceiveDataAsync));
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
