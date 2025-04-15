using MHServerEmu.Core.Network.Tcp;

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

        public readonly struct AddClient(ITcpClient client) : IGameServiceMessage
        {
            public readonly ITcpClient Client = client;
        }

        public readonly struct RemoveClient(ITcpClient client) : IGameServiceMessage
        {
            public readonly ITcpClient Client = client;
        }

        public readonly struct RouteMessages(ITcpClient client, IReadOnlyList<MessagePackage> messagePackages) : IGameServiceMessage
        {
            public readonly ITcpClient Client = client;
            public readonly IReadOnlyList<MessagePackage> Messages = messagePackages;
        }

        public readonly struct RouteMessagePackage(ITcpClient client, MessagePackage message) : IGameServiceMessage
        {
            public readonly ITcpClient Client = client;
            public readonly MessagePackage Message = message;
        }

        public readonly struct RouteMailboxMessage(ITcpClient client, MailboxMessage message) : IGameServiceMessage
        {
            public readonly ITcpClient Client = client;
            public readonly MailboxMessage Message = message;
        }
    }
}
