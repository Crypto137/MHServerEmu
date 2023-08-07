using System;

namespace MHServerEmu.Networking
{
    public interface IGameMessageHandler
    {
        public void Handle(FrontendClient client, ushort muxId, GameMessage message);
        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages);
    }
}
