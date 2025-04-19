namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Marker interface for <see cref="IGameService"/> messages.
    /// </summary>
    public interface IGameServiceMessage
    {
    }

    public static class GameServiceProtocol
    {
        // NOTE: Although we are currently using readonly structs here, unfortunately it seems
        // using pattern matching to switch on the message type causes boxing. Need to figure
        // out a more performant way to send messages without overcomplicating everything
        // (e.g. using the visitor pattern here would probably work, but it may be too cumbersome).

        public readonly struct AddClient(IFrontendClient client) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
        }

        public readonly struct RemoveClient(IFrontendClient client) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
        }

        public readonly struct RouteMessageBuffer(IFrontendClient client, MessageBuffer messageBuffer) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
            public readonly MessageBuffer MessageBuffer = messageBuffer;
        }

        public readonly struct RouteMessage(IFrontendClient client, Type protocol, MailboxMessage message) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
            public readonly Type Protocol = protocol;
            public readonly MailboxMessage Message = message;
        }
    }
}
