using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// An interface for services that handle <see cref="MessagePackage"/> instances.
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
        /// Handles a <see cref="MessagePackage"/> instance from a specific <see cref="ITcpClient"/>.
        /// </summary>
        public void Handle(ITcpClient client, MessagePackage message);

        /// <summary>
        /// Handles an <see cref="IEnumerable{T}"/> of <see cref="MessagePackage"/> instances coming in from a specific <see cref="ITcpClient"/>.
        /// </summary>
        public void Handle(ITcpClient client, IEnumerable<MessagePackage> messages);

        /// <summary>
        /// Returns a <see cref="string"/> representing the status of this <see cref="IGameService"/>.
        /// </summary>
        public string GetStatus();
    }
}
