using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.UI
{
    public class UISyncData
    {
        public PrototypeId WidgetR { get; set; }
        public PrototypeId ContextR { get; set; }
        public PrototypeId[] Areas { get; set; }

        public UISyncData(PrototypeId widgetR, PrototypeId contextR, PrototypeId[] areas)
        {
            WidgetR = widgetR;
            ContextR = contextR;
            Areas = areas;
        }

        public virtual void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WritePrototypeEnum<Prototype>(WidgetR);
            stream.WritePrototypeEnum<Prototype>(ContextR);

            stream.WriteRawInt32(Areas.Length);
            for (int i = 0; i < Areas.Length; i++)
                stream.WritePrototypeEnum<Prototype>(Areas[i]);
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
