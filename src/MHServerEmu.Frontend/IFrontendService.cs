using Google.ProtocolBuffers;

namespace MHServerEmu.Frontend
{
    /// <summary>
    /// Interface for game services that interact with the <see cref="FrontendServer"/> directly.
    /// </summary>
    public interface IFrontendService
    {
        public void ReceiveFrontendMessage(FrontendClient client, IMessage message);
        public bool AddFrontendClient(FrontendClient client);
        public bool RemoveFrontendClient(FrontendClient client);
    }
}
