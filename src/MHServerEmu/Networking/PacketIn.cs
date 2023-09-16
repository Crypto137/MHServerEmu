using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;

namespace MHServerEmu.Networking
{
    public enum MuxCommand
    {
        Connect = 0x01,
        ConnectAck = 0x02,
        Disconnect = 0x03,
        ConnectWithData = 0x04,
        Data = 0x05
    }

    public class PacketIn
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ushort MuxId { get; }
        public MuxCommand Command { get; }
        public GameMessage[] Messages { get; } = Array.Empty<GameMessage>();

        public PacketIn(CodedInputStream stream)
        {
            // Read header (6 bytes)
            MuxId = stream.ReadRawUInt16();
            int bodyLength = stream.ReadRawUInt24();
            Command = (MuxCommand)stream.ReadRawByte();

            // Read messages
            if (Command == MuxCommand.Data)
            {
                if (bodyLength > 0)
                {
                    List<GameMessage> messageList = new();

                    CodedInputStream messageInputStream = CodedInputStream.CreateInstance(stream.ReadRawBytes(bodyLength));

                    while (!messageInputStream.IsAtEnd)
                    {
                        byte messageId = (byte)messageInputStream.ReadRawVarint64();
                        int messageSize = (int)messageInputStream.ReadRawVarint64();
                        byte[] messageContent = messageInputStream.ReadRawBytes(messageSize);
                        messageList.Add(new(messageId, messageContent));
                    }

                    Messages = messageList.ToArray();
                }
                else
                {
                    Logger.Warn($"Received empty data packet on {MuxId}");
                }
            }
        }

        public PacketOut ToPacketOut()
        {
            PacketOut packetOut = new(MuxId, Command);
            packetOut.AddMessages(Messages);
            return packetOut;
        }
    }
}
