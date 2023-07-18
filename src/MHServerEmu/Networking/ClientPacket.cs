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

        private readonly CodedInputStream _stream;

        public ushort MuxId { get; }
        public int BodyLength { get; }
        public MuxCommand Command { get; }

        public byte[] Body { get; }

        public byte[] RawData
        {
            get
            {
                using (MemoryStream memoryStream = new())
                {
                    using (BinaryWriter binaryWriter = new(memoryStream))
                    {
                        binaryWriter.Write(MuxId);
                        binaryWriter.Write(BodyLength.ToUInt24ByteArray());
                        binaryWriter.Write((byte)Command);
                        binaryWriter.Write(Body);
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        public ClientPacket(CodedInputStream stream)
        {
            _stream = stream;

            // Read header (6 bytes)
            MuxId = BitConverter.ToUInt16(stream.ReadRawBytes(2));

            // Body length is stored as uint24
            byte[] lengthArray = stream.ReadRawBytes(3);
            byte[] bodyLengthArray = BitConverter.IsLittleEndian
                ? new byte[] { lengthArray[0], lengthArray[1], lengthArray[2], 0 }
                : new byte[] { 0, lengthArray[2], lengthArray[1], lengthArray[0] };

            BodyLength = BitConverter.ToInt32(bodyLengthArray);
            Command = (MuxCommand)stream.ReadRawByte();

            // Read body
            Body = stream.ReadRawBytes(BodyLength);
        }
    }
}
