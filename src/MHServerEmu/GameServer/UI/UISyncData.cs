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

        public virtual byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                WriteParentFields(cos);
                cos.Flush();
                return ms.ToArray();
            }
        }

        public virtual void EncodeBools(BoolEncoder boolEncoder) { }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteParentString(sb);
            return sb.ToString();
        }

        protected void WriteParentFields(CodedOutputStream stream)
        {
            stream.WritePrototypeId(WidgetR, PrototypeEnumType.All);
            stream.WritePrototypeId(ContextR, PrototypeEnumType.All);

            stream.WriteRawInt32(Areas.Length);
            for (int i = 0; i < Areas.Length; i++)
                stream.WritePrototypeId(Areas[i], PrototypeEnumType.All);
        }

        protected void WriteParentString(StringBuilder sb)
        {
            sb.AppendLine($"WidgetR: {GameDatabase.GetPrototypeName(WidgetR)}");
            sb.AppendLine($"ContextR: {GameDatabase.GetPrototypeName(ContextR)}");
            for (int i = 0; i < Areas.Length; i++) sb.AppendLine($"Area{i}: {Areas[i]}");
        }
    }
}
