using Google.ProtocolBuffers;
using MHServerEmu.Common;

namespace MHServerEmu.Networking
{
    public enum MuxCommand
    {
        Connect = 0x01,
        Accept = 0x02,      // Expected response to Connect and Insert
        Disconnect = 0x03,
        Insert = 0x04,      // Purpose unclear, works similar to connect
        Message = 0x05      // Requires a body
    }

    public class ClientPacket
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ushort MuxId { get; }
        public MuxCommand Command { get; }
        public GameMessage[] Messages { get; }

        public ClientPacket(CodedInputStream stream)
        {
            // Read header (6 bytes)
            MuxId = BitConverter.ToUInt16(stream.ReadRawBytes(2));

            // Body length is stored as uint24
            byte[] lengthArray = stream.ReadRawBytes(3);
            byte[] bodyLengthArray = BitConverter.IsLittleEndian
                ? new byte[] { lengthArray[0], lengthArray[1], lengthArray[2], 0 }
                : new byte[] { 0, lengthArray[2], lengthArray[1], lengthArray[0] };
            int bodyLength = BitConverter.ToInt32(bodyLengthArray);

            Command = (MuxCommand)stream.ReadRawByte();

            // Read messages
            if (Command == MuxCommand.Message)
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
                    Logger.Warn($"Received empty message packet on {MuxId}");
                }
            }
            else
            {
                Messages = Array.Empty<GameMessage>();
            }
        }
    }
}
