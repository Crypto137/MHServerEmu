using Google.ProtocolBuffers;

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

        public GameMessage(IMessage message)
        {
            Id = ProtocolDispatchTable.GetMessageId(message);
            Content = message.ToByteArray();
        }
    }
}
