using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetGenericFraction : UISyncData
    {
        private int _currentCount;
        private int _totalCount;

        public UIWidgetGenericFraction(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef) : base(uiDataProvider, widgetRef, contextRef) { }

        public override void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            base.Decode(stream, boolDecoder);

            _currentCount = stream.ReadRawInt32();
            _totalCount = stream.ReadRawInt32();

            _timeStart = stream.ReadRawInt64();
            _timeEnd = stream.ReadRawInt64();
            _timePaused = boolDecoder.ReadBool(stream);
        }

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawInt32(_currentCount);
            stream.WriteRawInt32(_totalCount);

            stream.WriteRawInt64(_timeStart);
            stream.WriteRawInt64(_timeEnd);
            boolEncoder.WriteBuffer(stream);   // TimePaused
        }

        public override void EncodeBools(BoolEncoder boolEncoder)
        {
            boolEncoder.EncodeBool(_timePaused);
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
