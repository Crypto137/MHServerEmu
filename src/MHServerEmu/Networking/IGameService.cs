namespace MHServerEmu.Networking
{
    /// <summary>
    /// An interface for classes that handle raw GameMessages coming in from the frontend.
    /// </summary>
    public interface IGameService
    {
        public void Handle(FrontendClient client, ushort muxId, GameMessage message);
        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages);
    }
}
