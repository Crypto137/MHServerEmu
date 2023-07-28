using System;

namespace MHServerEmu.Networking
{
    public class GameMessage
    {
        public byte Id { get; }
        public byte[] Content { get; }

        public GameMessage(byte id, byte[] content)
        {
            Id = id;
            Content = content;
        }

        public GameMessage(FrontendProtocolMessage id, byte[] content)
        {
            Id = (byte)id;
            Content = content;
        }

        public GameMessage(GameServerToClientMessage id, byte[] content)
        {
            Id = (byte)id;
            Content = content;
        }

        public GameMessage(GroupingManagerMessage id, byte[] content)
        {
            Id = (byte)id;
            Content = content;
        }
    }
}
