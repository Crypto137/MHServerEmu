using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetGenericFraction : UISyncData
    {
        public int CurrentCount { get; set; }
        public int TotalCount { get; set; }
        public long TimeStart { get; set; }
        public long TimeEnd { get; set; }
        public bool TimePaused { get; set; }

        public UIWidgetGenericFraction(PrototypeId widgetR, PrototypeId contextR, PrototypeId[] areas,
            int currentCount, int totalCount, long timeStart, long timeEnd, bool timePaused) : base(null, widgetR, contextR)
        {
            _areas = areas;

            CurrentCount = currentCount;
            TotalCount = totalCount;
            TimeStart = timeStart;
            TimeEnd = timeEnd;
            TimePaused = timePaused;
        }

        public UIWidgetGenericFraction(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef) : base(uiDataProvider, widgetRef, contextRef) { }

        public override void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            base.Decode(stream, boolDecoder);

            CurrentCount = stream.ReadRawInt32();
            TotalCount = stream.ReadRawInt32();
            TimeStart = stream.ReadRawInt64();
            TimeEnd = stream.ReadRawInt64();
            TimePaused = boolDecoder.ReadBool(stream);
        }

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawInt32(CurrentCount);
            stream.WriteRawInt32(TotalCount);
            stream.WriteRawInt64(TimeStart);
            stream.WriteRawInt64(TimeEnd);
            boolEncoder.WriteBuffer(stream);   // TimePaused
        }

        public override void EncodeBools(BoolEncoder boolEncoder)
        {
            boolEncoder.EncodeBool(TimePaused);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            sb.AppendLine($"CurrentCount: {CurrentCount}");
            sb.AppendLine($"TotalCount: {TotalCount}");
            sb.AppendLine($"TimeStart: {TimeStart}");
            sb.AppendLine($"TimeEnd: {TimeEnd}");
            sb.AppendLine($"TimePaused: {TimePaused}");
        }
    }
}
