using System.Collections;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Manages <see cref="NetClient"/> instances.
    /// </summary>
    public abstract class NetworkManager<TNetClient, TProtocol>
        where TNetClient: NetClient
        where TProtocol: Enum
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<IFrontendClient, TNetClient> _netClientDict = new();

        // Incoming messages are asynchronously posted to a mailbox where they are deserialized and stored for later retrieval.
        // When it's time to process messages, we copy all messages stored in our mailbox to a list.
        // Although we call it a "list" to match the client, it functions more like a queue (FIFO, pop/peeks).
        private readonly CoreNetworkMailbox<TProtocol> _mailbox = new();
        private readonly MessageList _messagesToProcessList = new();

        private readonly DoubleBufferQueue<IFrontendClient> _addClientQueue = new();
        private readonly DoubleBufferQueue<IFrontendClient> _removeClientQueue = new();

        public int Count { get => _netClientDict.Count; }

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
        public TNetClient GetNetClient(IFrontendClient frontendClient)
        {
            if (_netClientDict.TryGetValue(frontendClient, out TNetClient netClient) == false)
                return null;    // This is valid when transferring between games, so don't log this.

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
        /// Enqueues registration of a new <see cref="NetClient"/> for the provided <see cref="IFrontendClient"/> during the next update.
        /// </summary>
        public void AsyncAddClient(IFrontendClient client)
        {
            _addClientQueue.Enqueue(client);
        }

        /// <summary>
        /// Enqueues removal of the <see cref="NetClient"/> bound to the provided <see cref="IFrontendClient"/> during the next update.
        /// </summary>
        public void AsyncRemoveClient(IFrontendClient client)
        {
            _removeClientQueue.Enqueue(client);
        }

        /// <summary>
        /// Handles an incoming <see cref="MessageBuffer"/> asynchronously.
        /// </summary>
        public void AsyncReceiveMessageBuffer(IFrontendClient frontendClient, in MessageBuffer messageBuffer)
        {
            // Gazillion's implementation does this in NetworkManager::ConnectionStatus()

            // If the message fails to deserialize it means either data got corrupted somehow or we have a hacker trying to mess things up.
            // In both cases it's better to bail out.
            if (_mailbox.Post(frontendClient, messageBuffer) == false)
            {
                Logger.Error($"AsyncPostMessage(): Message deserialization error for data from client, disconnecting. Client: {frontendClient}");
                frontendClient.Disconnect();
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
                (IFrontendClient frontendClient, MailboxMessage message) = _messagesToProcessList.PopNextMessage();
                TNetClient netClient = GetNetClient(frontendClient);

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
            return _netClientDict.TryAdd(netClient.FrontendClient, netClient);
        }

        protected abstract bool AcceptAndRegisterNewClient(IFrontendClient tcpClient);

        protected abstract void OnNetClientDisconnected(TNetClient netClient);

        private void ProcessAsyncAddedClients()
        {
            _addClientQueue.Swap();       

            while (_addClientQueue.CurrentCount > 0)
            {
                IFrontendClient frontendClient = _addClientQueue.Dequeue();
                AcceptAndRegisterNewClient(frontendClient);
            }
        }

        private void RemoveDisconnectedClients()
        {
            _removeClientQueue.Swap();

            while (_removeClientQueue.CurrentCount > 0)
            {
                IFrontendClient frontendClient = _removeClientQueue.Dequeue();

                if (_netClientDict.Remove(frontendClient, out TNetClient netClient) == false)
                {
                    Logger.Warn($"RemoveDisconnectedClients(): IFrontendClient {frontendClient} not found");
                    continue;
                }

                OnNetClientDisconnected(netClient);
                netClient.OnDisconnect();
                netClient.FlushMessages();
            }
        }

        /// <summary>
        /// A simple wrapper around <see cref="Dictionary{TKey, TValue}.ValueCollection.Enumerator"/>
        /// to iterate <typeparamref name="TNetClient"/> instances managed by this <see cref="NetworkManager{TNetClient, TProtocol}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<TNetClient>
        {
            private readonly NetworkManager<TNetClient, TProtocol> _networkManager;
            private Dictionary<IFrontendClient, TNetClient>.ValueCollection.Enumerator _enumerator;

            public TNetClient Current { get => _enumerator.Current; }
            object IEnumerator.Current { get => Current; }

            public Enumerator(NetworkManager<TNetClient, TProtocol> networkManager)
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
