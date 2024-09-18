using Google.ProtocolBuffers;

namespace MHServerEmu.Frontend
{
    /// <summary>
    /// An interface for game services that interact with the <see cref="FrontendServer"/> directly.
    /// </summary>
    public interface IFrontendService
    {
        /// <summary>
        /// Handles an <see cref="IMessage"/> from the <see cref="FrontendServer"/>.
        /// </summary>
        public void ReceiveFrontendMessage(FrontendClient client, IMessage message);

        /// <summary>
        /// Registers the provided <see cref="FrontendClient"/>.
        /// </summary>
        public bool AddFrontendClient(FrontendClient client);

        /// <summary>
        /// Unregisters the provided <see cref="FrontendClient"/>.
        /// </summary>
        public bool RemoveFrontendClient(FrontendClient client);
    }
}
