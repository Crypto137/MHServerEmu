using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Container for incoming <see cref="GameMessage"/> instances.
    /// </summary>
    public class CoreNetworkMailbox<TClient> where TClient: ITcpClient
    {
        // NOTE: The current implementation is just a wrapper around Queue.
        // We should do deserialization when we receive a message rather than during game update.
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Queue<(TClient, GameMessage)> _messageQueue = new();

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="CoreNetworkMailbox{TClient}"/> instance has any pending messages.
        /// </summary>
        public bool HasMessages { get => _messageQueue.Any(); }

        /// <summary>
        /// Adds a new <see cref="GameMessage"/> to this <see cref="CoreNetworkMailbox{TClient}"/> instance.
        /// </summary>
        public void Post(TClient client, GameMessage message)
        {
            _messageQueue.Enqueue((client, message));
        }

        /// <summary>
        /// Retrieves the next queued <see cref="GameMessage"/> instance.
        /// </summary>
        public (TClient, GameMessage) PopNextMessage()
        {
            if (_messageQueue.TryDequeue(out var result) == false)
                return Logger.WarnReturn<(TClient, GameMessage)>(default, $"PopNextMessage(): Failed to dequeue");

            return result;
        }
    }
}
