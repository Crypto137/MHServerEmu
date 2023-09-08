using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Social
{
    public class ChatChannelOption
    {
        public ulong PrototypeId { get; set; }
        public bool Value { get; set; }

        public ChatChannelOption(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);
            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            Value = boolDecoder.ReadBool();
        }

        public ChatChannelOption(ulong prototypeId, bool value)
        {
            PrototypeId = prototypeId;
            Value = value;
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WritePrototypeId(PrototypeId, PrototypeEnumType.All);

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
                streamWriter.WriteLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
                streamWriter.WriteLine($"Value: {Value}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
