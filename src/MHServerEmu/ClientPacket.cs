using Gazillion;
using Google.ProtocolBuffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu
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

        public ushort MuxId { get;  }
        public byte BodyLength { get; }
        public byte Byte3 { get; }          // This might be a continuation of BodyLength if it's actually a ushort as well
        public byte Byte4 { get; }          // international byte of mystery
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
                        binaryWriter.Write(BodyLength);
                        binaryWriter.Write(Byte3);
                        binaryWriter.Write(Byte4);
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
            BodyLength = stream.ReadRawByte();
            Byte3 = stream.ReadRawByte();
            Byte4 = stream.ReadRawByte();
            Command = (MuxCommand)stream.ReadRawByte();

            // Read body
            Body = stream.ReadRawBytes(BodyLength);

            // Check bytes 3 and 4
            if (Byte3 != 0x00) Logger.Warn("Mux packet byte3 is NOT 0");
            if (Byte4 != 0x00) Logger.Warn("Mux packet byte4 is NOT 0");
        }

    }
}
