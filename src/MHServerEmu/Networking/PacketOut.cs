using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Networking
{
    public class PacketOut
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly ushort _muxId;
        private readonly MuxCommand _muxCommand;
        private readonly List<GameMessage> _messageList = new();

        public byte[] Data
        {
            get
            {
                byte[] bodyBuffer = Array.Empty<byte>();
                if (_muxCommand == MuxCommand.Data)
                {
                    if (_messageList.Count > 0)
                    {
                        using (MemoryStream ms = new())
                        {
                            CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                            foreach (GameMessage message in _messageList)
                            {
                                cos.WriteRawVarint32(message.Id);
                                cos.WriteRawVarint32((uint)message.Payload.Length);
                                cos.WriteRawBytes(message.Payload);
                            }

                            cos.Flush();
                            bodyBuffer = ms.ToArray();
                        }
                    }
                    else
                    {
                        Logger.Warn("Data packet contains no messages!");
                    }
                }

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
    }
}
