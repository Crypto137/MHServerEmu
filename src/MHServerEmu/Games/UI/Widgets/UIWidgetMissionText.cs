using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;

namespace MHServerEmu.Games.UI.Widgets
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

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawVarint64(MissionName);
            stream.WriteRawVarint64(MissionObjectiveName);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"MissionName: {MissionName}");
            sb.AppendLine($"MissionObjectiveName: {MissionObjectiveName}");
        }
    }
}
