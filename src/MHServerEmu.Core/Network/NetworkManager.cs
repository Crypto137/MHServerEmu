using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Manages <see cref="NetClient"/> instances.
    /// </summary>
    public abstract class NetworkManager<TNetClient> where TNetClient: NetClient
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ITcpClient, TNetClient> _netClientDict = new();

        // Incoming messages are asynchronously posted to a mailbox where they are deserialized and stored for later retrieval.
        // When it's time to process messages, we copy all messages stored in our mailbox to a list.
        // Although we call it a "list" to match the client, it functions more like a queue (FIFO, pop/peeks).
        private readonly CoreNetworkMailbox _mailbox = new();
        private readonly MessageList _messagesToProcessList = new();

        // We swap queues with a lock when handling async client connect / disconnect events
        private Queue<ITcpClient> _asyncAddClientQueue = new();
        private Queue<ITcpClient> _asyncRemoveClientQueue = new();
        private Queue<ITcpClient> _addClientQueue = new();
        private Queue<ITcpClient> _removeClientQueue = new();

        public NetworkManager()
        {
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <summary>
        /// Returns the <see cref="NetClient"/> bound to the provided <see cref="ITcpClient"/>.
        /// </summary>
        public TNetClient GetNetClient(ITcpClient tcpClient)
        {
            if (_netClientDict.TryGetValue(tcpClient, out TNetClient netClient) == false)
                Logger.Warn($"GetNetClient(): ITcpClient {tcpClient} is not bound to a NetClient");

            return netClient;
        }

        public void Update()
        {
            // NOTE: It is important to remove disconnected client BEFORE registering new clients
            // to make sure we save data for cases such as duplicate logins.
            
            // markAsyncDisconnectedClients() -> we just do everything in RemoveDisconnectedClients()
            RemoveDisconnectedClients();

            ProcessAsyncAddedClients();
        }

        /// <summary>
        /// Enqueues registration of a new <see cref="NetClient"/> for the provided <see cref="ITcpClient"/> during the next update.
        /// </summary>
        public void AsyncAddClient(ITcpClient client)
        {
            lock (_asyncAddClientQueue)
                _asyncAddClientQueue.Enqueue(client);
        }

        /// <summary>
        /// Enqueues removal of the <see cref="NetClient"/> bound to the provided <see cref="ITcpClient"/> during the next update.
        /// </summary>
        public void AsyncRemoveClient(ITcpClient client)
        {
            lock (_asyncRemoveClientQueue)
                _asyncRemoveClientQueue.Enqueue(client);
        }

        /// <summary>
        /// Handles an incoming <see cref="MessagePackage"/> asynchronously.
        /// </summary>
        public void AsyncPostMessage(ITcpClient tcpClient, MessagePackage message)
        {
            // Gazillion's implementation does this in NetworkManager::ConnectionStatus()

            // If the message fails to deserialize it means either data got corrupted somehow or we have a hacker trying to mess things up.
            // In both cases it's better to bail out.
            if (_mailbox.Post(tcpClient, message) == false)
            {
                Logger.Error($"AsyncPostMessage(): Message deserialization error for data from client, disconnecting. Client: {tcpClient}");
                tcpClient.Disconnect();
            }
        }

        /// <summary>
        /// Processes all asynchronously posted messages.
        /// </summary>
        public void ReceiveAllPendingMessages()
        {
            // We reuse the same message list every time to avoid unnecessary allocations.
            _mailbox.GetAllMessages(_messagesToProcessList);

            while (_messagesToProcessList.HasMessages)
            {
                (ITcpClient tcpClient, MailboxMessage message) = _messagesToProcessList.PopNextMessage();
                TNetClient netClient = GetNetClient(tcpClient);

                if (netClient != null && netClient.CanSendOrReceiveMessages)
                    netClient.ReceiveMessage(message);

                // If the player connection was removed or it is currently unable to receive messages,
                // this message will be lost, like tears in rain...
            }
        }

        /// <summary>
        /// Flushes all active <see cref="PlayerConnection"/> instances.
        /// </summary>
        public void SendAllPendingMessages()
        {
            foreach (TNetClient netClient in this)
                netClient.FlushMessages();
        }

        protected bool RegisterNetClient(TNetClient netClient)
        {
            return _netClientDict.TryAdd(netClient.TcpClient, netClient);
        }

        protected abstract bool AcceptAndRegisterNewClient(ITcpClient tcpClient);

        protected abstract void OnNetClientDisconnected(TNetClient netClient);

        private void ProcessAsyncAddedClients()
        {
            // Swap queues so that we can continue queueing clients while we process
            lock (_asyncAddClientQueue)
                (_asyncAddClientQueue, _addClientQueue) = (_addClientQueue, _asyncAddClientQueue);

            while (_addClientQueue.Count > 0)
            {
                ITcpClient tcpClient = _addClientQueue.Dequeue();
                AcceptAndRegisterNewClient(tcpClient);
            }
        }

        private void RemoveDisconnectedClients()
        {
            // Swap queues so that we can continue queueing clients while we process
            lock (_asyncRemoveClientQueue)
                (_asyncRemoveClientQueue, _removeClientQueue) = (_removeClientQueue, _asyncRemoveClientQueue);

            while (_removeClientQueue.Count > 0)
            {
                ITcpClient tcpClient = _removeClientQueue.Dequeue();

                if (_netClientDict.Remove(tcpClient, out TNetClient netClient) == false)
                {
                    Logger.Warn($"RemoveDisconnectedClients(): ITcpClient {tcpClient} not found");
                    continue;
                }

                OnNetClientDisconnected(netClient);
                netClient.OnDisconnect();
            }
        }

        /// <summary>
        /// A simple wrapper around <see cref="Dictionary{TKey, TValue}.ValueCollection.Enumerator"/>
        /// to iterate <typeparamref name="TNetClient"/> instances managed by this <see cref="NetworkManager{TNetClient}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TNetClient>
        {
            private readonly NetworkManager<TNetClient> _networkManager;
            private Dictionary<ITcpClient, TNetClient>.ValueCollection.Enumerator _enumerator;

            public TNetClient Current { get => _enumerator.Current; }
            object IEnumerator.Current { get => Current; }

            public Enumerator(NetworkManager<TNetClient> networkManager)
            {
                _networkManager = networkManager;
                _enumerator = _networkManager._netClientDict.Values.GetEnumerator();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Dispose();
                _enumerator = _networkManager._netClientDict.Values.GetEnumerator();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}
