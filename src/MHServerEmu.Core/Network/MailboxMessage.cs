using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Contains a deserialized <see cref="IMessage"/>.
    /// </summary>
    public readonly struct MailboxMessage
    {
        private readonly IMessage _message;

        public uint Id { get; }

        /// <summary>
        /// Constructs a new <see cref="MailboxMessage{TClient}"/>.
        /// </summary>
        public MailboxMessage(uint id, IMessage message)
        {
            Id = id;
            _message = message;
        }

        /// <summary>
        /// Returns the contents of this <see cref="MailboxMessage"/> as <typeparamref name="T"/>.
        /// </summary>
        public T As<T>() where T: class, IMessage
        {
            return _message as T;
        }
    }
}
