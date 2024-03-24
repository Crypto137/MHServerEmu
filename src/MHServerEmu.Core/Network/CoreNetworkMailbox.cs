using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Deserializes <see cref="MessagePackage"/> instances and stores them as <see cref="MailboxMessage"/> until retrieval.
    /// </summary>
    public class CoreNetworkMailbox<TClient> where TClient: ITcpClient
    {
        // TODO: Optimize this for cases when there is not enough time to deserialize messages between game updates
        // TODO: Implement mailbox serialization to offload this from game update

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Queue<(TClient, MailboxMessage)> _messageQueue = new();

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="CoreNetworkMailbox{TClient}"/> instance has any pending messages.
        /// </summary>
        public bool HasMessages { get => _messageQueue.Any(); }

        /// <summary>
        /// Adds a new <see cref="MessagePackage"/> to this <see cref="CoreNetworkMailbox{TClient}"/> instance.
        /// </summary>
        public void Post(TClient client, MessagePackage message)
        {
            _messageQueue.Enqueue((client, new(message)));
        }

        /// <summary>
        /// Retrieves the next queued <see cref="MessagePackage"/> instance.
        /// </summary>
        public (TClient, MailboxMessage) PopNextMessage()
        {
            if (_messageQueue.TryDequeue(out var result) == false)
                return Logger.WarnReturn<(TClient, MailboxMessage)>(default, $"PopNextMessage(): Failed to dequeue");

            return result;
        }
    }
}
