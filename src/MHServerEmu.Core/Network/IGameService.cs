namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// An interface for services that handle <see cref="IGameServiceMessage"/> instances.
    /// </summary>
    public interface IGameService
    {
        public GameServiceState State { get; }

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
        /// Adds the status of this <see cref="IGameService"/> to the provided dictionary.
        /// </summary>
        public void GetStatus(Dictionary<string, long> statusDict);
    }
}
