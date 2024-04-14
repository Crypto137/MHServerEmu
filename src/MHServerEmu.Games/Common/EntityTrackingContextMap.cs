using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.Dialog;
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
            Context = stream.ReadPrototypeRef<Prototype>();
            Flag = stream.ReadRawVarint32();
        }

        public EntityTrackingContextMap(PrototypeId context, uint value)
        {
            Context = context;
            Flag = value;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeRef<Prototype>(Context);
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

    // TODO Merge
    public class EntityTrackingContextMap2 : Dictionary<PrototypeId, EntityTrackingFlag>
    {
        public void Insert(PrototypeId contextRef, EntityTrackingFlag flag)
        {
            if (ContainsKey(contextRef))
                this[contextRef] |= flag;
            else
                Add(contextRef, flag);
        }
    }
}
