using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// A wrapper around <see cref="Queue{T}"/> that imitates the functionality of Gazillion's CoreNetworkMailbox::MessageList.
    /// </summary>
    /// <remarks>
    /// This class is NOT thread-safe. Asynchronous thread-safe message handling should be done through <see cref="CoreNetworkMailbox{TClient}"/>.
    /// </remarks>
    public class MessageList<TClient> where TClient : ITcpClient
    {
        // NOTE: In the client this class is based on a "FastList" data structure, which appears to be a variation of a linked list.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Queue<(TClient, MailboxMessage)> _messageQueue = new();

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="MessageList{TClient}"/> instance has any queued messages.
        /// </summary>
        public bool HasMessages { get => _messageQueue.Count > 0; }

        // NOTE: Rather than exposing the underlying data structure like the client, we encapsulate it with Enqueue() / TransferFrom() / Clear() methods.

        /// <summary>
        /// Enqueues the provided <see cref="MailboxMessage"/> from a <typeparamref name="TClient"/>.
        /// </summary>
        public void Enqueue(TClient client, MailboxMessage message)
        {
            // NOTE: In the client this is done by calling FastList::InsertTailList()
            _messageQueue.Enqueue((client, message));
        }

        /// <summary>
        /// Transfers all <see cref="MailboxMessage"/> instances from another <see cref="MessageList{TClient}"/>.
        /// </summary>
        public void TransferFrom(MessageList<TClient> other)
        {
            // NOTE: In the client this is done by calling FastList::Concat().
            // There is probably a more elegant / faster way of doing this.
            while (other._messageQueue.Count > 0)
                _messageQueue.Enqueue(other._messageQueue.Dequeue());
        }

        /// <summary>
        /// Clears all enqueued <see cref="MailboxMessage"/> instances.
        /// </summary>
        public void Clear()
        {
            _messageQueue.Clear();
        }

        /// <summary>
        /// Retrieves the next queued <see cref="MailboxMessage"/> instance without removing it from the queue.
        /// </summary>
        public (TClient, MailboxMessage) PeekNextMessage()
        {
            // Do we even need peeking considering we have the HasMessages properties?
            if (_messageQueue.TryPeek(out var result) == false)
                return Logger.WarnReturn<(TClient, MailboxMessage)>(default, $"PeekNextMessage(): No messages to peek");

            return result;
        }

        /// <summary>
        /// Retrieves the next queued <see cref="MailboxMessage"/> instance.
        /// </summary>
        public (TClient, MailboxMessage) PopNextMessage()
        {
            if (_messageQueue.TryDequeue(out var result) == false)
                return Logger.WarnReturn<(TClient, MailboxMessage)>(default, $"PopNextMessage(): No messages to pop");

            return result;
        }
    }
}
