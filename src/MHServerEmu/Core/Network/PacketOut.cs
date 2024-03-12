using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Tcp;

namespace MHServerEmu.Core.Network
{
    public class PacketOut : IPacket
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly ushort _muxId;
        private readonly MuxCommand _muxCommand;
        private readonly List<GameMessage> _messageList = new();

        public byte[] Data
        {
            get
            {
                byte[] bodyBuffer = SerializeBody();

                using (MemoryStream stream = new())
                using (BinaryWriter writer = new(stream))
                {
                    writer.Write(_muxId);
                    writer.WriteUInt24(bodyBuffer.Length);
                    writer.Write((byte)_muxCommand);
                    writer.Write(bodyBuffer);
                    return stream.ToArray();
                }
            }
        }

        public PacketOut(ushort muxId, MuxCommand command)
        {
            _muxId = muxId;
            _muxCommand = command;
        }

        public void AddMessage(GameMessage message) => _messageList.Add(message);
        public void AddMessages(IEnumerable<GameMessage> messages) => _messageList.AddRange(messages);

        private byte[] SerializeBody()
        {
            if (_muxCommand != MuxCommand.Data)
                return Array.Empty<byte>();

            if (_messageList.Count == 0)
            {
                Logger.Warn("Data packet contains no messages!");
                return Array.Empty<byte>();
            }

            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                foreach (GameMessage message in _messageList) message.Encode(cos);
                cos.Flush();
                return ms.ToArray();
            }
        }
    }
}
