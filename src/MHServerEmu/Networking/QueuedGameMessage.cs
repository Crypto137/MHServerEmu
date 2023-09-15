namespace MHServerEmu.Networking
{
    public class QueuedGameMessage
    {
        public FrontendClient Client { get; }
        public ushort MuxId { get; }
        public GameMessage Message { get; }

        public QueuedGameMessage (FrontendClient client, ushort muxId, GameMessage message)
        {
            Client = client;
            MuxId = muxId;
            Message = message;
        }
    }
}
