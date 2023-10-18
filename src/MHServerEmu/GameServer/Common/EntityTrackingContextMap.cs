using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Common
{
    public class EntityTrackingContextMap
    {
        public ulong Context { get; set; }
        public uint Flag { get; set; }

        public EntityTrackingContextMap(CodedInputStream stream)
        {
            Context = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            Flag = stream.ReadRawVarint32();
        }

        public EntityTrackingContextMap(ulong prototypeId, uint value)
        {
            Context = prototypeId;
            Flag = value;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WritePrototypeEnum(Context, PrototypeEnumType.All);
                cos.WriteRawVarint32(Flag);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Context: {GameDatabase.GetPrototypeName(Context)}");
            sb.AppendLine($"Flag: 0x{Flag:X}");
            return sb.ToString();
        }
    }
}
