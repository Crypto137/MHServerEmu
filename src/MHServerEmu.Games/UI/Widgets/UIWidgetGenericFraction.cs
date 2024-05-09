using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetGenericFraction : UISyncData
    {
        private int _currentCount;
        private int _totalCount;

        public UIWidgetGenericFraction(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef) : base(uiDataProvider, widgetRef, contextRef) { }

        public override bool Serialize(Archive archive)
        {
            bool success = base.Serialize(archive);

            success &= Serializer.Transfer(archive, ref _currentCount);
            success &= Serializer.Transfer(archive, ref _totalCount);
            success &= Serializer.Transfer(archive, ref _timeStart);
            success &= Serializer.Transfer(archive, ref _timeEnd);
            success &= Serializer.Transfer(archive, ref _timePaused);

            return success;
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"Count: {_currentCount} / {_totalCount}");
        }

        public void SetCount(int current, int total)
        {
            _currentCount = current;
            _totalCount = total;
            UpdateUI();
        }
    }
}
