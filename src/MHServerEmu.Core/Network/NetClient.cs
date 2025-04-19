using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// A high-level representation of a client connection.
    /// </summary>
    public abstract class NetClient
    {
        private readonly ushort _muxChannel;
        private readonly List<IMessage> _pendingMessageList = new();

        public IFrontendClient FrontendClient { get; }
        public virtual bool CanSendOrReceiveMessages { get => true; }

        /// <summary>
        /// Constructs a new <see cref="NetClient"/> bound to the provided <see cref="IFrontendClient"/>.
        /// </summary>
        public NetClient(ushort muxChannel, IFrontendClient frontendClient)
        {
            ArgumentNullException.ThrowIfNull(frontendClient);

            _muxChannel = muxChannel;
            FrontendClient = frontendClient;
        }

        public void Disconnect()
        {
            FrontendClient.Disconnect();
        }

        /// <summary>
        /// Queues a new <see cref="IMessage"/> to be sent when messages are flushed.
        /// </summary>
        public void PostMessage(IMessage message)
        {
            _pendingMessageList.Add(message);
        }

        /// <summary>
        /// Sends all pending <see cref="IMessage"/> instances queued via <see cref="PostMessage(IMessage)"/>.
        /// </summary>
        public void FlushMessages()
        {
            if (_pendingMessageList.Count == 0)
                return;

            FrontendClient.SendMessageList(_muxChannel, _pendingMessageList);
            _pendingMessageList.Clear();
        }

        /// <summary>
        /// Handles a <see cref="MailboxMessage"/>.
        /// </summary>
        public abstract void ReceiveMessage(in MailboxMessage message);

        /// <summary>
        /// Does implementation-specific handling when this <see cref="NetClient"/> disconnects.
        /// </summary>
        public abstract void OnDisconnect();
    }
}
