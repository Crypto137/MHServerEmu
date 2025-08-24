using MHServerEmu.Core.Collections;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Base class for <see cref="IGameServiceMessage"/> handlers.
    /// </summary>
    public abstract class ServiceMailbox
    {
        // IGameServiceMessage are boxed anyway when doing pattern matching, so it should probably be fine.
        // If we encounter performance issues here, replace this with a specialized data structure.
        private readonly DoubleBufferQueue<IGameServiceMessage> _messageQueue = new();

        /// <summary>
        /// Called from other threads to post an <see cref="IGameServiceMessage"/>
        /// </summary>
        public void PostMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            _messageQueue.Enqueue(message);
        }

        /// <summary>
        /// Processes enqueued <see cref="IGameServiceMessage"/> instances.
        /// </summary>
        public void ProcessMessages()
        {
            _messageQueue.Swap();

            while (_messageQueue.CurrentCount > 0)
            {
                IGameServiceMessage serviceMessage = _messageQueue.Dequeue();
                HandleServiceMessage(serviceMessage);
            }
        }

        /// <summary>
        /// Handles the provided <see cref="IGameServiceMessage"/> instance.
        /// </summary>
        protected abstract void HandleServiceMessage(IGameServiceMessage message);
    }
}
