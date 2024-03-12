using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetMissionText : UISyncData
    {
        public LocaleStringId MissionName { get; set; }
        public LocaleStringId MissionObjectiveName { get; set; }

        public UIWidgetMissionText(PrototypeId widgetR, PrototypeId contextR, PrototypeId[] areas, LocaleStringId missionName, LocaleStringId missionObjectiveName) : base(widgetR, contextR, areas)
        {
            MissionName = missionName;
            MissionObjectiveName = missionObjectiveName;
        }

        public UIWidgetMissionText(PrototypeId widgetR, PrototypeId contextR, PrototypeId[] areas, CodedInputStream stream) : base(widgetR, contextR, areas)
        {
            MissionName = (LocaleStringId)stream.ReadRawVarint64();
            MissionObjectiveName = (LocaleStringId)stream.ReadRawVarint64();
        }

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawVarint64((ulong)MissionName);
            stream.WriteRawVarint64((ulong)MissionObjectiveName);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(MissionName)}: {MissionName}");
            sb.AppendLine($"{nameof(MissionObjectiveName)}: {MissionObjectiveName}");
        }
    }
}
