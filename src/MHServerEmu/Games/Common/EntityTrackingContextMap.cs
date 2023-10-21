using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Common
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

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeEnum(Context, PrototypeEnumType.All);
            stream.WriteRawVarint32(Flag);
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
