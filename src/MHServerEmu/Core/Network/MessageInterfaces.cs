using MHServerEmu.Frontend;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// An interface for classes that handle raw GameMessages coming in from the frontend.
    /// </summary>
    public interface IGameService
    {
        public void Handle(FrontendClient client, ushort muxId, GameMessage message);
        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages);
    }

    /// <summary>
    /// An interface for classes that handle routed GameMessages coming in from GameServices.
    /// </summary>
    public interface IMessageHandler
    {
        public void Handle(FrontendClient client, GameMessage message);
        public void Handle(FrontendClient client, IEnumerable<GameMessage> messages);
    }
}
