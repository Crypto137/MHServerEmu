using MHServerEmu.Core.Logging;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// A wrapper around <see cref="Queue{T}"/> that imitates the functionality of Gazillion's CoreNetworkMailbox::MessageList.
    /// </summary>
    /// <remarks>
    /// This class is NOT thread-safe. Asynchronous thread-safe message handling should be done through <see cref="CoreNetworkMailbox{T}"/>.
    /// </remarks>
    public class MessageList
    {
        // NOTE: In the client this class is based on a "FastList" data structure, which appears to be a variation of an intrusive linked list.
        // Using a linked list in our case would cause node allocations, so our implemenetation is Queue<T> based instead.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private Queue<(IFrontendClient, MailboxMessage)> _messageQueue = new();

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="MessageList{TClient}"/> instance has any queued messages.
        /// </summary>
        public bool HasMessages { get => _messageQueue.Count > 0; }

        // NOTE: Rather than exposing the underlying data structure like the client, we encapsulate it with Enqueue() / TransferFrom() / Clear() methods.

        /// <summary>
        /// Enqueues the provided <see cref="MailboxMessage"/> from an <see cref="IFrontendClient"/>.
        /// </summary>
        public void Enqueue(IFrontendClient client, MailboxMessage message)
        {
            // NOTE: In the client this is done by calling FastList::InsertTailList()
            _messageQueue.Enqueue((client, message));
        }

        /// <summary>
        /// Transfers all <see cref="MailboxMessage"/> instances from another <see cref="MessageList"/>.
        /// </summary>
        /// <remarks>
        /// This method works faster when this <see cref="MessageList"/> is empty (which is the intended use case).
        /// </remarks>
        public void TransferFrom(MessageList other)
        {
            // NOTE: In the client this is done by calling FastList::Concat().
            if (_messageQueue.Count == 0)
            {
                // When this list is empty, we can swap the underlying queues instead of transferring messages one by one
                (_messageQueue, other._messageQueue) = (other._messageQueue, _messageQueue);
            }
            else
            {
                // Fall back to the slow one by one transfer (shouldn't be happening)
                Logger.Warn("TransferFrom(): This MessageList is not empty");
                while (other._messageQueue.Count > 0)
                    _messageQueue.Enqueue(other._messageQueue.Dequeue());
            }
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
        public (IFrontendClient, MailboxMessage) PeekNextMessage()
        {
            // Do we even need peeking considering we have the HasMessages properties?
            if (_messageQueue.TryPeek(out var result) == false)
                return Logger.WarnReturn<(IFrontendClient, MailboxMessage)>(default, $"PeekNextMessage(): No messages to peek");

            return result;
        }

        /// <summary>
        /// Retrieves the next queued <see cref="MailboxMessage"/> instance.
        /// </summary>
        public (IFrontendClient, MailboxMessage) PopNextMessage()
        {
            if (_messageQueue.TryDequeue(out var result) == false)
                return Logger.WarnReturn<(IFrontendClient, MailboxMessage)>(default, $"PopNextMessage(): No messages to pop");

            return result;
        }
    }
}
