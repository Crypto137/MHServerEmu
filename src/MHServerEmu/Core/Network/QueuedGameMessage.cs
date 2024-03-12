using MHServerEmu.Frontend;

namespace MHServerEmu.Core.Network
{
    public class QueuedGameMessage
    {
        public FrontendClient Client { get; }
        public GameMessage Message { get; }

        public QueuedGameMessage(FrontendClient client, GameMessage message)
        {
            Client = client;
            Message = message;
        }
    }
}
