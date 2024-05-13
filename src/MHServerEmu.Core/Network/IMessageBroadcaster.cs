using Google.ProtocolBuffers;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Exposes <see cref="IMessage"/> broadcasting functionality.
    /// </summary>
    public interface IMessageBroadcaster
    {
        /// <summary>
        /// Sends the provided <see cref="IMessage"/> instance to all connected clients.
        /// </summary>
        public void BroadcastMessage(IMessage message);
    }
}
