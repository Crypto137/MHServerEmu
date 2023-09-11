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
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WritePrototypeId(PrototypeId, PrototypeEnumType.All);

                byte bitBuffer = boolEncoder.GetBitBuffer();             //Value
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            sb.AppendLine($"Value: {Value}");
            return sb.ToString();
        }
    }
}
