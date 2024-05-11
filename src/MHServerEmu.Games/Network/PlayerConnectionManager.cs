﻿using System.Collections;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Frontend;

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

        // We swap queues with a lock when handling async client events
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
                playerConnection.BeginLoading();
            }
        }

        /// <summary>
        /// Requests a player to be reloaded.
        /// </summary>
        public void SetPlayerConnectionPending(PlayerConnection playerConnection)
        {
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
        /// Sends the provided <see cref="IMessage"/> instance over the specified <see cref="PlayerConnection"/>.
        /// </summary>
        public void SendMessage(PlayerConnection connection, IMessage message)
        {
            connection.PostMessage(message);
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

                // Save data and clean up
                playerConnection.OnDisconnect();

                Logger.Info($"Removed client {client} from {_game}");
            }
        }

        private void AcceptAndRegisterNewClient(FrontendClient client)
        {
            // NOTE: Potential duping exploits here if a player reconnects before their data is updated in the database.
            // We have some handling for this in the player manager, but better be safe than sorry.

            PlayerConnection connection = new(_game, client);
            client.GameId = _game.Id;

            if (_clientConnectionDict.TryAdd(client, connection) == false)
                Logger.Warn($"AcceptAndRegisterNewClient(): Failed to add client {client}");

            if (_dbIdConnectionDict.TryAdd(connection.PlayerDbId, connection) == false)
                Logger.Warn($"AcceptAndRegisterNewClient(): Failed to add player id 0x{connection.PlayerDbId}");

            SetPlayerConnectionPending(connection);

            Logger.Info($"Accepted and registered client {client} to {_game}");
        }
    }
}
