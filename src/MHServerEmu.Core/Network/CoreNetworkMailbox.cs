using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Deserializes <see cref="MessageBuffer"/> instances and stores them as <see cref="MailboxMessage"/> until retrieval.
    /// </summary>
    /// <remarks>
    /// This class does asynchronous message handling and should be thread-safe.
    /// </remarks>
    public class CoreNetworkMailbox<T> where T: Enum
    {
        // NOTE: This class combines the functionality of both the base IMessageSerializer and its derivative CoreNetworkMailbox class from the client.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly MessageList _messageList = new();

        /// <summary>
        /// Deserializes the provided <see cref="MessageBuffer"/> instance and adds its contents to this <see cref="CoreNetworkMailbox{TClient}"/> as a <see cref="MailboxMessage"/>.
        /// </summary>
        public bool Post(ITcpClient client, MessageBuffer messageBuffer)
        {
            uint messageId = messageBuffer.MessageId;

            // HACK: Timestamp client sync messages
            TimeSpan gameTimeReceived = default;
            TimeSpan dateTimeReceived = default;

            if (typeof(T) == typeof(ClientToGameServerMessage))
            {
                if (messageId == (uint)ClientToGameServerMessage.NetMessageSyncTimeRequest ||
                    messageId == (uint)ClientToGameServerMessage.NetMessagePing)
                {
                    gameTimeReceived = Clock.GameTime;
                    dateTimeReceived = Clock.UnixTime;
                }
            }

            // Deserialize
            IMessage message = messageBuffer.Deserialize<T>();
            if (message == null) return Logger.ErrorReturn(false, "Post(): Message deserialization failed");

            // CoreNetworkMailbox::OnDeserializeMessage()
            MailboxMessage mailboxMessage = new(messageId, message, gameTimeReceived, dateTimeReceived);

            lock (_messageList)
                _messageList.Enqueue(client, mailboxMessage);

            return true;
        }

        /// <summary>
        /// Transfers all <see cref="MailboxMessage"/> instances contained in this <see cref="CoreNetworkMailbox{TClient}"/> to the provided <see cref="MessageList{TClient}"/>.
        /// </summary>
        public void GetAllMessages(MessageList outputList)
        {
            lock (_messageList)
                outputList.TransferFrom(_messageList);
        }

        /// <summary>
        /// Clears all <see cref="MailboxMessage"/> instances from this <see cref="CoreNetworkMailbox{TClient}"/>.
        /// </summary>
        public void Clear()
        {
            lock (_messageList)
                _messageList.Clear();
        }
    }
}
