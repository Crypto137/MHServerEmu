using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Common
{
    public class EntityTrackingContextMap
    {
        public PrototypeId Context { get; set; }
        public uint Flag { get; set; }

        public EntityTrackingContextMap(CodedInputStream stream)
        {
            Context = stream.ReadPrototypeEnum<Prototype>();
            Flag = stream.ReadRawVarint32();
        }

        public EntityTrackingContextMap(PrototypeId context, uint value)
        {
            Context = context;
            Flag = value;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeEnum<Prototype>(Context);
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
