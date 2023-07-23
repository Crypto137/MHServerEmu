using Google.ProtocolBuffers;
using MHServerEmu.Common;

namespace MHServerEmu.Networking
{
    public class PacketOut
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private ushort _muxId;
        private MuxCommand _muxCommand;
        private List<GameMessage> _messageList = new();

        public byte[] Data
        {
            get
            {
                byte[] bodyBuffer = Array.Empty<byte>();
                if (_muxCommand == MuxCommand.Message)
                {
                    if (_messageList.Count > 0)
                    {
                        using (MemoryStream memoryStream = new())
                        {
                            CodedOutputStream outputStream = CodedOutputStream.CreateInstance(memoryStream);

                            foreach (GameMessage message in _messageList)
                            {
                                outputStream.WriteRawVarint64(message.Id);
                                outputStream.WriteRawVarint64((ulong)message.Content.Length);
                                outputStream.WriteRawBytes(message.Content);
                            }

                            outputStream.Flush();
                            bodyBuffer = memoryStream.ToArray();
                        }
                    }
                    else
                    {
                        Logger.Warn("Message packet contains no messages!");
                    }
                }

                using (MemoryStream memoryStream = new())
                {
                    using (BinaryWriter binaryWriter = new(memoryStream))
                    {
                        binaryWriter.Write(_muxId);
                        binaryWriter.Write(bodyBuffer.Length.ToUInt24ByteArray());
                        binaryWriter.Write((byte)_muxCommand);
                        binaryWriter.Write(bodyBuffer);
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        public PacketOut(ushort muxId, MuxCommand command)
        {
            _muxId = muxId;
            _muxCommand = command;
        }

        public void AddMessage(GameMessage message)
        {
            _messageList.Add(message);
        }
    }
}
