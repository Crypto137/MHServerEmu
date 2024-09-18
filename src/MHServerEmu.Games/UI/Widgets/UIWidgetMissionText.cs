using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetMissionText : UISyncData
    {
        private LocaleStringId _missionName;
        private LocaleStringId _missionObjectiveName;

        public UIWidgetMissionText(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef) : base(uiDataProvider, widgetRef, contextRef) { }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            success &= Serializer.Transfer(archive, ref _missionName);
            success &= Serializer.Transfer(archive, ref _missionObjectiveName);

            return success;
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"{nameof(_missionName)}: {_missionName}");
            sb.AppendLine($"{nameof(_missionObjectiveName)}: {_missionObjectiveName}");
        }

        public void SetText(LocaleStringId missionName, LocaleStringId missionObjectiveName)
        {
            _missionName = missionName;
            _missionObjectiveName = missionObjectiveName;
            UpdateUI();
        }
    }
}
