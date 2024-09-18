using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Deserializes <see cref="MessagePackage"/> instances and stores them as <see cref="MailboxMessage"/> until retrieval.
    /// </summary>
    /// <remarks>
    /// This class does asynchronous message handling and should be thread-safe.
    /// </remarks>
    public class CoreNetworkMailbox<TClient> where TClient: ITcpClient
    {
        // NOTE: This class combines the functionality of both the base IMessageSerializer and its derivative CoreNetworkMailbox class from the client.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly object _lock = new();
        private readonly MessageList<TClient> _messageList = new();

        /// <summary>
        /// Deserializes the provided <see cref="MessagePackage"/> instance and adds its contents to this <see cref="CoreNetworkMailbox{TClient}"/> as a <see cref="MailboxMessage"/>.
        /// </summary>
        public bool Post(TClient client, MessagePackage messagePackage)
        {
            IMessage message = messagePackage.Deserialize();
            if (message == null) return Logger.ErrorReturn(false, "Post(): Message deserialization failed");

            // CoreNetworkMailbox::OnDeserializeMessage()
            MailboxMessage mailboxMessage = new(messagePackage.Id, message);

            lock (_lock)
            {
                _messageList.Enqueue(client, mailboxMessage);
            }

            return true;
        }

        /// <summary>
        /// Transfers all <see cref="MailboxMessage"/> instances contained in this <see cref="CoreNetworkMailbox{TClient}"/> to the provided <see cref="MessageList{TClient}"/>.
        /// </summary>
        public void GetAllMessages(MessageList<TClient> outputList)
        {
            lock (_lock)
            {
                outputList.TransferFrom(_messageList);
            }
        }

        /// <summary>
        /// Clears all <see cref="MailboxMessage"/> instances from this <see cref="CoreNetworkMailbox{TClient}"/>.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _messageList.Clear();
            }
        }
    }
}
