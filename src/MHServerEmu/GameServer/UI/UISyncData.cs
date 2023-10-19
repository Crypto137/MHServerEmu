using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.UI
{
    public class UISyncData
    {
        public ulong WidgetR { get; set; }
        public ulong ContextR { get; set; }
        public ulong[] Areas { get; set; }

        public UISyncData(ulong widgetR, ulong contextR, ulong[] areas)
        {
            WidgetR = widgetR;
            ContextR = contextR;
            Areas = areas;
        }

        public virtual void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WritePrototypeEnum(WidgetR, PrototypeEnumType.All);
            stream.WritePrototypeEnum(ContextR, PrototypeEnumType.All);

            stream.WriteRawInt32(Areas.Length);
            for (int i = 0; i < Areas.Length; i++)
                stream.WritePrototypeEnum(Areas[i], PrototypeEnumType.All);
        }

        public virtual void EncodeBools(BoolEncoder boolEncoder) { }

        public override string ToString()
        {
            StringBuilder sb = new();
            BuildString(sb);
            return sb.ToString();
        }

        protected virtual void BuildString(StringBuilder sb)
        {
            sb.AppendLine($"WidgetR: {GameDatabase.GetPrototypeName(WidgetR)}");
            sb.AppendLine($"ContextR: {GameDatabase.GetPrototypeName(ContextR)}");
            for (int i = 0; i < Areas.Length; i++) sb.AppendLine($"Area{i}: {Areas[i]}");
        }
    }
}
