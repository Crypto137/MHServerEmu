namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// An interface for services that handle <see cref="IGameServiceMessage"/> instances.
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
        /// Receives an <see cref="IGameServiceMessage"/> from another <see cref="IGameService"/>.
        /// </summary>
        public void ReceiveServiceMessage<T>(in T message) where T: struct, IGameServiceMessage;

        /// <summary>
        /// Returns a <see cref="string"/> representing the status of this <see cref="IGameService"/>.
        /// </summary>
        public string GetStatus();
    }
}
