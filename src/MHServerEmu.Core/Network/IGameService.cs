using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// An interface for services that handle <see cref="MessagePackage"/> and <see cref="MailboxMessage"/> instances.
    /// </summary>
    public interface IGameService
    {
        /// <summary>
        /// Starts this <see cref="IGameService"/> instance.
        /// </summary>
        public void Run();

        /// <summary>
        /// Shuts down this <see cref="IGameService"/> instance.
        /// </summary>
        public void Shutdown();

        /// <summary>
        /// Handles the provided <see cref="MessagePackage"/> instance from the specified <see cref="ITcpClient"/>.
        /// </summary>
        public void Handle(ITcpClient client, MessagePackage message);

        /// <summary>
        /// Handles the provided <see cref="MessagePackage"/> instances from the specified <see cref="ITcpClient"/>.
        /// </summary>
        public void Handle(ITcpClient client, IReadOnlyList<MessagePackage> messages);

        /// <summary>
        /// Handles the provided <see cref="MailboxMessage"/> instance from the specified <see cref="ITcpClient"/>.
        /// </summary>
        public void Handle(ITcpClient client, MailboxMessage message);

        /// <summary>
        /// Returns a <see cref="string"/> representing the status of this <see cref="IGameService"/>.
        /// </summary>
        public string GetStatus();
    }
}
