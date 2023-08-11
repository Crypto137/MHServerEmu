using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class ChatChannelOption
    {
        public ulong PrototypeEnum { get; set; }
        public bool Value { get; set; }

        public ChatChannelOption(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeEnum = stream.ReadRawVarint64();
            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            Value = boolDecoder.ReadBool();
        }

        public ChatChannelOption(ulong prototypeEnum, bool value)
        {
            PrototypeEnum = prototypeEnum;
            Value = value;
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(PrototypeEnum);

                byte bitBuffer = boolEncoder.GetBitBuffer();             //Value
                if (bitBuffer != 0) stream.WriteRawByte(bitBuffer);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"PrototypeEnum: 0x{PrototypeEnum.ToString("X")}");
                streamWriter.WriteLine($"Value: {Value}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
