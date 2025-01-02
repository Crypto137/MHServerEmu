using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Frontend;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Network
{
    // This is the equivalent of the client-side ClientServiceConnectionManager and GameConnectionManager implementations of the NetworkManager abstract class.
    // We flatten everything into a single class since we don't have to worry about client-side.

    /// <summary>
    /// Manages <see cref="PlayerConnection"/> instances.
    /// </summary>
    public class PlayerConnectionManager : IEnumerable<PlayerConnection>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<FrontendClient, PlayerConnection> _clientConnectionDict = new();
        private readonly Dictionary<ulong, PlayerConnection> _dbIdConnectionDict = new();
        private readonly Game _game;

        // Incoming messages are asynchronously posted to a mailbox where they are deserialized and stored for later retrieval.
        // When it's time to process messages, we copy all messages stored in our mailbox to a list.
        // Although we call it a "list" to match the client, it functions more like a queue (FIFO, pop/peeks).
        private readonly CoreNetworkMailbox<FrontendClient> _mailbox = new();
        private readonly MessageList<FrontendClient> _messagesToProcessList = new();

        // We swap queues with a lock when handling async client connect / disconnect events
        private Queue<FrontendClient> _asyncAddClientQueue = new();
        private Queue<FrontendClient> _asyncRemoveClientQueue = new();
        private Queue<FrontendClient> _addClientQueue = new();
        private Queue<FrontendClient> _removeClientQueue = new();

        // Queue for pending player connections (i.e. players currently loading)
        private Queue<PlayerConnection> _pendingPlayerConnectionQueue = new();

        /// <summary>
        /// Constructs a new <see cref="PlayerConnectionManager"/> instance for the provided <see cref="Game"/>.
        /// </summary>
        public PlayerConnectionManager(Game game)
        {
            _game = game;
        }

        /// <summary>
        /// Returns the <see cref="PlayerConnection"/> bound to the provided <see cref="FrontendClient"/>.
        /// </summary>
        public PlayerConnection GetPlayerConnection(FrontendClient frontendClient)
        {
            if (_clientConnectionDict.TryGetValue(frontendClient, out PlayerConnection connection) == false)
                Logger.Warn($"GetPlayerConnection(): Client {frontendClient.Session.Account} is not bound to a player connection");

            return connection;
        }

        /// <summary>
        /// Returns the <see cref="PlayerConnection"/> bound to the provided account dbId.
        /// </summary>
        public PlayerConnection GetPlayerConnection(ulong playerDbId)
        {
            if (_dbIdConnectionDict.TryGetValue(playerDbId, out PlayerConnection connection) == false)
                Logger.Warn($"GetPlayerConnection(): DbId 0x{playerDbId:X} is not bound to a player connection");

            return connection;
        }

        /// <summary>
        /// Adds all <see cref="Player"/> instances that are interested in the provided <see cref="Entity"/> to the provided <see cref="List{T}"/>.
        /// Returns <see langword="true"/> if the number of interested players is > 0.
        /// </summary>
        public bool GetInterestedPlayers(List<Player> interestedPlayerList, Entity entity,
            AOINetworkPolicyValues interestFilter = AOINetworkPolicyValues.AllChannels, bool skipOwner = false)
        {
            // Early out if we already know that none of the players match the interest channel filter
            if ((entity.InterestedPoliciesUnion & interestFilter) == 0)
                return false;

            // Use InterestReferences to skip players that we know for sure are not interested in this entity
            EntityManager entityManager = _game.EntityManager;
            foreach (ulong playerId in entity.InterestReferences)
            {
                Player player = entityManager.GetEntity<Player>(playerId);
                if (player == null)
                {
                    Logger.Warn("GetInterestedPlayers(): player == null");
                    continue;
                }

                if (player.PlayerConnection == null)
                    continue;  // This can happen during packet parsing

                // Check ownership
                if (skipOwner && entity.IsOwnedBy(playerId))
                    continue;

                // Check channel filter
                if (player.AOI.InterestedInEntity(entity.Id, interestFilter) == false)
                    continue;

                interestedPlayerList.Add(player);
            }

            return interestedPlayerList.Count > 0;
        }

        /// <summary>
        /// Adds all <see cref="Player"/> instances that are interested in the provided <see cref="Region"/> to the provided <see cref="List{T}"/>.
        /// Returns <see langword="true"/> if the number of interested players is > 0.
        /// </summary>
        public bool GetInterestedPlayers(List<Player> interestedPlayerList, Region region)
        {
            foreach (Player player in new PlayerIterator(region))
            {
                if (player.AOI.Region == region)
                    interestedPlayerList.Add(player);
            }

            return interestedPlayerList.Count > 0;
        }

        /// <summary>
        /// Adds all <see cref="PlayerConnection"/> instances that are interested in the provided <see cref="Entity"/> to the provided <see cref="List{T}"/>.
        /// Returns <see langword="true"/> if the number of interested player connections is > 0.
        /// </summary>
        public bool GetInterestedClients(List<PlayerConnection> interestedClientList, Entity entity,
            AOINetworkPolicyValues interestFilter = AOINetworkPolicyValues.AllChannels, bool skipOwner = false)
        {
            List<Player> interestedPlayerList = ListPool<Player>.Instance.Get();
            GetInterestedPlayers(interestedPlayerList, entity, interestFilter, skipOwner);

            foreach (Player player in interestedPlayerList)
                interestedClientList.Add(player.PlayerConnection);

            ListPool<Player>.Instance.Return(interestedPlayerList);
            return interestedClientList.Count > 0;
        }

        /// <summary>
        /// Returns <see cref="PlayerConnection"/> instances that are bound to players that are interested in the provided <see cref="Region"/>.
        /// </summary>
        /// <summary>
        /// Adds all <see cref="PlayerConnection"/> instances that are interested in the provided <see cref="Region"/> to the provided <see cref="List{T}"/>.
        /// Returns <see langword="true"/> if the number of interested clients is > 0.
        /// </summary>
        public bool GetInterestedClients(List<PlayerConnection> interestedClientList, Region region)
        {
            List<Player> interestedPlayerList = ListPool<Player>.Instance.Get();
            GetInterestedPlayers(interestedPlayerList, region);

            foreach (Player player in interestedPlayerList)
                interestedClientList.Add(player.PlayerConnection);

            ListPool<Player>.Instance.Return(interestedPlayerList);
            return interestedClientList.Count > 0;
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
        /// Loads pending players.
        /// </summary>
        public void ProcessPendingPlayerConnections()
        {
            while (_pendingPlayerConnectionQueue.Count > 0)
            {
                PlayerConnection playerConnection = _pendingPlayerConnectionQueue.Dequeue();
                playerConnection.EnterGame();
            }
        }

        /// <summary>
        /// Requests a player to be loaded.
        /// </summary>
        public void SetPlayerConnectionPending(PlayerConnection playerConnection)
        {
            // NOTE: We flush messages when we set the connection as pending so that
            // we can deliver the loading screen message to the client ASAP.
            playerConnection.FlushMessages();
            _pendingPlayerConnectionQueue.Enqueue(playerConnection);
        }

        /// <summary>
        /// Enqueues registration of a new <see cref="PlayerConnection"/> for the provided <see cref="FrontendClient"/> during the next update.
        /// </summary>
        public void AsyncAddClient(FrontendClient client)
        {
            lock (_asyncAddClientQueue)
                _asyncAddClientQueue.Enqueue(client);
        }

        /// <summary>
        /// Enqueues removal of the <see cref="PlayerConnection"/> for the provided <see cref="FrontendClient"/> during the next update.
        /// </summary>
        public void AsyncRemoveClient(FrontendClient client)
        {
            lock (_asyncRemoveClientQueue)
                _asyncRemoveClientQueue.Enqueue(client);
        }

        /// <summary>
        /// Handles an incoming <see cref="MessagePackage"/> asynchronously.
        /// </summary>
        public void AsyncPostMessage(FrontendClient client, MessagePackage message)
        {
            // If the message fails to deserialize it means either data got corrupted somehow or we have a hacker trying to mess things up.
            // In both cases it's better to bail out.
            if (_mailbox.Post(client, message) == false)
            {
                Logger.Error($"AsyncPostMessage(): Deserialization failed, disconnecting client {client} from {_game}");
                client.Disconnect();
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
                (FrontendClient client, MailboxMessage message) = _messagesToProcessList.PopNextMessage();
                PlayerConnection playerConnection = GetPlayerConnection(client);

                if (playerConnection != null && playerConnection.CanSendOrReceiveMessages())
                    playerConnection.ReceiveMessage(message);

                // If the player connection was removed or it is currently unable to receive messages,
                // this message will be lost, like tears in rain...
            }
        }

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> instance over the specified <see cref="PlayerConnection"/>.
        /// </summary>
        public void SendMessage(PlayerConnection connection, IMessage message)
        {
            connection.PostMessage(message);
        }

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> over all <see cref="PlayerConnection"/> instaces in the provided <see cref="List{T}"/>.
        /// </summary>
        public void SendMessageToMultiple(List<PlayerConnection> clientList, IMessage message)
        {
            foreach (PlayerConnection playerConnection in clientList)
                playerConnection.SendMessage(message);
        }

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> to all <see cref="PlayerConnection"/> instances that are interested in the provided <see cref="Region"/>.
        /// </summary>
        public void SendMessageToInterested(IMessage message, Region region)
        {
            List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
            GetInterestedClients(interestedClientList, region);

            foreach (PlayerConnection playerConnection in interestedClientList)
                playerConnection.SendMessage(message);

            ListPool<PlayerConnection>.Instance.Return(interestedClientList);
        }

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> to all <see cref="PlayerConnection"/> instances that are interested in the provided <see cref="Entity"/>.
        /// </summary>
        public void SendMessageToInterested(IMessage message, Entity entity, AOINetworkPolicyValues interestFilter = AOINetworkPolicyValues.AllChannels, bool skipOwner = false)
        {
            List<PlayerConnection> interestedClientList = ListPool<PlayerConnection>.Instance.Get();
            GetInterestedClients(interestedClientList, entity, interestFilter, skipOwner);

            foreach (PlayerConnection playerConnection in interestedClientList)
                playerConnection.SendMessage(message);

            ListPool<PlayerConnection>.Instance.Return(interestedClientList);
        }

        /// <summary>
        /// Broadcasts an <see cref="IMessage"/> instance to all active <see cref="PlayerConnection"/> instances.
        /// </summary>
        public void BroadcastMessage(IMessage message)
        {
            foreach (PlayerConnection connection in _clientConnectionDict.Values)
                connection.PostMessage(message);
        }

        /// <summary>
        /// Posts the provided <see cref="IMessage"/> to the specified <see cref="PlayerConnection"/> and immediately flushes it.
        /// </summary>
        public void SendMessageImmediate(PlayerConnection connection, IMessage message)
        {
            connection.PostMessage(message);
            connection.FlushMessages();
        }

        /// <summary>
        /// Flushes all active <see cref="PlayerConnection"/> instances.
        /// </summary>
        public void SendAllPendingMessages()
        {
            foreach (PlayerConnection connection in this)
                connection.FlushMessages();
        }

        #region IEnumerable Implementation

        public IEnumerator<PlayerConnection> GetEnumerator() => _clientConnectionDict.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        private void ProcessAsyncAddedClients()
        {
            // Swap queues so that we can continue queueing clients while we process
            lock (_asyncAddClientQueue)
                (_asyncAddClientQueue, _addClientQueue) = (_addClientQueue, _asyncAddClientQueue);

            while (_addClientQueue.Count > 0)
            {
                FrontendClient client = _addClientQueue.Dequeue();
                AcceptAndRegisterNewClient(client);
            }
        }

        private void RemoveDisconnectedClients()
        {
            // Swap queues so that we can continue queueing clients while we process
            lock (_asyncRemoveClientQueue)
                (_asyncRemoveClientQueue, _removeClientQueue) = (_removeClientQueue, _asyncRemoveClientQueue);

            while (_removeClientQueue.Count > 0)
            {
                FrontendClient client = _removeClientQueue.Dequeue();

                if (_clientConnectionDict.Remove(client, out PlayerConnection playerConnection) == false)
                {
                    Logger.Warn($"RemoveDisconnectedClients(): Client {client} not found");
                    continue;
                }

                ulong dbId = playerConnection.PlayerDbId;

                if (_dbIdConnectionDict.Remove(dbId) == false)
                    Logger.Warn($"RemoveDisconnectedClients(): Account id  0x{dbId:X} not found");

                // Update db models and clean up
                playerConnection.OnDisconnect();

                // Remove game id to let the player manager know that it is now safe to write to the database.
                client.GameId = 0;

                Logger.Info($"Removed client [{client}] from game [{_game}]");
            }
        }

        private bool AcceptAndRegisterNewClient(FrontendClient client)
        {
            // Make sure this client is still connected (it may not be if we are lagging hard)
            if (client.IsConnected == false)
                return Logger.WarnReturn(false, $"AcceptAndRegisterNewClient(): Client [{client}] is no longer connected");

            // Make sure this client's account is not being used by another client pending disconnection
            if (_dbIdConnectionDict.ContainsKey((ulong)client.Session.Account.Id))
            {
                Logger.Warn($"AcceptAndRegisterNewClient(): Attempting to add client [{client}] to game [{_game}], but its account is already in use by another client");
                client.Disconnect();
                return false;
            }

            // Creating a player sends the achievement database dump and a region availability query
            PlayerConnection connection = new(_game, client);
            client.GameId = _game.Id;

            // Any of these two checks failing is bad time
            if (_clientConnectionDict.TryAdd(client, connection) == false)
                Logger.Error($"AcceptAndRegisterNewClient(): Failed to add client [{client}]");

            if (_dbIdConnectionDict.TryAdd(connection.PlayerDbId, connection) == false)
                Logger.Error($"AcceptAndRegisterNewClient(): Failed to add player id 0x{connection.PlayerDbId}");

            if (connection.Initialize() == false)
            {
                connection.Disconnect();
                return false;
            }

            //SetPlayerConnectionPending(connection);   // This will be set when we receive region availability query response

            Logger.Info($"Accepted and registered client [{client}] to game [{_game}]");
            return true;
        }
    }
}
