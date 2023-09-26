using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;

namespace MHServerEmu.GameServer.UI.Widgets
{
    public class UIWidgetMissionText : UISyncData
    {
        public ulong MissionName { get; set; }
        public ulong MissionObjectiveName { get; set; }

        public UIWidgetMissionText(ulong widgetR, ulong contextR, ulong[] areas, CodedInputStream stream) : base(widgetR, contextR, areas)
        {
            MissionName = stream.ReadRawVarint64();
            MissionObjectiveName = stream.ReadRawVarint64();
        }

        public override byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                WriteParentFields(cos);

                cos.WriteRawVarint64(MissionName);
                cos.WriteRawVarint64(MissionObjectiveName);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteParentString(sb);

            sb.AppendLine($"MissionName: {MissionName}");
            sb.AppendLine($"MissionObjectiveName: {MissionObjectiveName}");

            return sb.ToString();
        }
    }
}
