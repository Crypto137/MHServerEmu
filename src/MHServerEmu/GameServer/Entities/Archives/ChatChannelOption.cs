using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class ChatChannelOption
    {
        public ulong PrototypeEnum { get; set; }
        public bool Value { get; set; }

        public ChatChannelOption(CodedInputStream stream, BoolBuffer boolBuffer)
        {
            PrototypeEnum = stream.ReadRawVarint64();
            if (boolBuffer.IsEmpty) boolBuffer.SetBits(stream.ReadRawByte());
            Value = boolBuffer.ReadBool();
        }

        public ChatChannelOption(ulong prototypeEnum, bool value)
        {
            PrototypeEnum = prototypeEnum;
            Value = value;
        }

        public byte[] Encode()
        {
            return Array.Empty<byte>();
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
