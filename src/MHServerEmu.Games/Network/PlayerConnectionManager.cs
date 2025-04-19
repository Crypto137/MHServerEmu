using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Network
{
    // This is the equivalent of the client-side ClientServiceConnectionManager and GameConnectionManager implementations of the NetworkManager abstract class.

    /// <summary>
    /// Manages <see cref="PlayerConnection"/> instances.
    /// </summary>
    public class PlayerConnectionManager : NetworkManager<PlayerConnection, ClientToGameServerMessage>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Game _game;

        // HashSet for preventing the same account from logging in multiple times
        private readonly HashSet<ulong> _playerDbIds = new();

        // Queue for pending player connections (i.e. players currently loading)
        private readonly Queue<PlayerConnection> _pendingPlayerConnectionQueue = new();

        /// <summary>
        /// Constructs a new <see cref="PlayerConnectionManager"/> instance for the provided <see cref="Game"/>.
        /// </summary>
        public PlayerConnectionManager(Game game)
        {
            _game = game;
        }

        #region Player Connection Getters

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

        #endregion

        #region Pending Processing

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

        #endregion

        #region Message Sending

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
            foreach (PlayerConnection playerConnection in this)
                playerConnection.PostMessage(message);
        }

        /// <summary>
        /// Posts the provided <see cref="IMessage"/> to the specified <see cref="PlayerConnection"/> and immediately flushes it.
        /// </summary>
        public void SendMessageImmediate(PlayerConnection playerConnection, IMessage message)
        {
            playerConnection.PostMessage(message);
            playerConnection.FlushMessages();
        }

        #endregion

        protected override bool AcceptAndRegisterNewClient(IFrontendClient frontendClient)
        {
            // Make sure this client is still connected (it may not be if we are lagging hard)
            if (frontendClient.IsConnected == false)
                return Logger.WarnReturn(false, $"AcceptAndRegisterNewClient(): Client [{frontendClient}] is no longer connected");

            // Construct a new PlayerConnection bound to this IFrontendClient
            PlayerConnection playerConnection = new(_game, frontendClient);

            // Make sure this client's account is not being used by another client pending disconnection.
            // We do this after constructing the connection to keep the IFrontendClient -> PlayerDbId retrieval in one place.
            ulong dbId = playerConnection.PlayerDbId;

            if (_playerDbIds.Add(dbId) == false)
            {
                Logger.Warn($"AcceptAndRegisterNewClient(): Attempting to add client [{frontendClient}] to game [{_game}], but its account dbId 0x{dbId:X} is already in use");
                frontendClient.Disconnect();
                return false;
            }

            // Register the client to allow it to receive messages
            if (RegisterNetClient(playerConnection) == false)
                Logger.Error($"AcceptAndRegisterNewClient(): Failed to add client [{frontendClient}]");

            // TODO: Replace this with a message to PlayerManager
            frontendClient.GameId = _game.Id;

            // Send time sync straight away for the client to be able to initialize its EventScheduler (needed for loading screens).
            // This will also make the client start sending pings, so it needs to be done after we assign game id.
            SendMessageImmediate(playerConnection, NetMessageInitialTimeSync.CreateBuilder()
                .SetGameTimeServerSent(Clock.GameTime.Ticks / 10)
                .SetDateTimeServerSent(Clock.UnixTime.Ticks / 10)
                .Build());

            // Initializing a player connection sends the achievement database dump and a region availability query
            if (playerConnection.Initialize() == false)
            {
                playerConnection.Disconnect();
                return false;
            }

            // This connection will be set as pending when we receive region availability query response

            Logger.Info($"Accepted and registered client [{frontendClient}] to game [{_game}]");
            return true;
        }

        protected override void OnNetClientDisconnected(PlayerConnection playerConnection)
        {
            ulong dbId = playerConnection.PlayerDbId;

            if (_playerDbIds.Remove(dbId) == false)
                Logger.Warn($"OnNetClientDisconnected(): Account id 0x{dbId:X} not found");
        }
    }
}
