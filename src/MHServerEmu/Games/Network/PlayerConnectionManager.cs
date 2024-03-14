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
    public class PlayerConnectionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly object _connectionLock = new();
        private readonly Dictionary<FrontendClient, PlayerConnection> _connectionDict = new();
        private readonly Game _game;

        /// <summary>
        /// Constructs a new <see cref="PlayerConnectionManager"/> instance for the provided <see cref="Game"/>.
        /// </summary>
        public PlayerConnectionManager(Game game)
        {
            _game = game;
        }

        /// <summary>
        /// Creates and returns a new <see cref="PlayerConnection"/> for the provided <see cref="FrontendClient"/>.
        /// </summary>
        public PlayerConnection AddPlayer(FrontendClient frontendClient)
        {
            lock (_connectionLock)
            {
                PlayerConnection connection = new(_game, frontendClient);
                if (_connectionDict.TryAdd(frontendClient, connection) == false)
                    return Logger.WarnReturn<PlayerConnection>(null, "AddPlayer(): Already added");

                return connection;
            }
        }

        /// <summary>
        /// Removes the <see cref="PlayerConnection"/> bound to the provided <see cref="FrontendClient"/>.
        /// </summary>
        public bool RemovePlayer(FrontendClient frontendClient)
        {
            lock (_connectionLock)
            {
                if (_connectionDict.TryGetValue(frontendClient, out PlayerConnection playerConnection) == false)
                    Logger.WarnReturn(false, $"RemovePlayer(): Not found");

                playerConnection.UpdateDBAccount();
                return true;
            }
        }

        /// <summary>
        /// Returns the <see cref="PlayerConnection"/> bound to the provided <see cref="FrontendClient"/>.
        /// </summary>
        public PlayerConnection GetPlayerConnection(FrontendClient frontendClient)
        {
            lock (_connectionLock)
            {
                if (_connectionDict.TryGetValue(frontendClient, out PlayerConnection connection) == false)
                    Logger.Warn($"GetPlayer(): Not found");

                return connection;
            }
        }

        /// <summary>
        /// Sends the provided <see cref="IMessage"/> instance over the specified <see cref="PlayerConnection"/>.
        /// </summary>
        public void SendMessage(PlayerConnection connection, IMessage message)
        {
            lock (_connectionLock)
            {
                connection.PostMessage(message);
            }
        }

        /// <summary>
        /// Broadcasts an <see cref="IMessage"/> instance to all active <see cref="PlayerConnection"/> instances.
        /// </summary>
        public void BroadcastMessage(IMessage message)
        {
            lock (_connectionLock)
            {
                foreach (PlayerConnection connection in _connectionDict.Values)
                    connection.PostMessage(message);
            }
        }

        /// <summary>
        /// Posts the provided <see cref="IMessage"/> to the specified <see cref="PlayerConnection"/> and immediately flushes it.
        /// </summary>
        public void SendMessageImmediate(PlayerConnection connection, IMessage message)
        {
            lock (_connectionLock)
            {
                connection.PostMessage(message);
                connection.FlushMessages();
            }
        }

        /// <summary>
        /// Flushes all active <see cref="PlayerConnection"/> instances.
        /// </summary>
        public void SendAllPendingMessages()
        {
            lock (_connectionLock)
            {
                foreach (PlayerConnection connection in _connectionDict.Values)
                    connection.FlushMessages();
            }
        }
    }
}
