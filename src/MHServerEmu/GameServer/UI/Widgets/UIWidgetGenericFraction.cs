using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.UI.Widgets
{
    public class UIWidgetGenericFraction : UISyncData
    {
        public int CurrentCount { get; set; }
        public int TotalCount { get; set; }
        public ulong TimeStart { get; set; }
        public ulong TimeEnd { get; set; }
        public bool TimePaused { get; set; }

        public UIWidgetGenericFraction(ulong widgetR, ulong contextR, ulong[] areas, CodedInputStream stream, BoolDecoder boolDecoder) : base(widgetR, contextR, areas)
        {
            CurrentCount = stream.ReadRawInt32();
            TotalCount = stream.ReadRawInt32();
            TimeStart = stream.ReadRawVarint64();
            TimeEnd = stream.ReadRawVarint64();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            TimePaused = boolDecoder.ReadBool();
        }

        public override byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                WriteParentFields(cos);

                cos.WriteRawInt32(CurrentCount);
                cos.WriteRawInt32(TotalCount);
                cos.WriteRawVarint64(TimeStart);
                cos.WriteRawVarint64(TimeEnd);
                boolEncoder.WriteBuffer(cos);   // TimePaused

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override void EncodeBools(BoolEncoder boolEncoder)
        {
            boolEncoder.EncodeBool(TimePaused);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteParentString(sb);

            sb.AppendLine($"CurrentCount: {CurrentCount}");
            sb.AppendLine($"TotalCount: {TotalCount}");
            sb.AppendLine($"TimeStart: {TimeStart}");
            sb.AppendLine($"TimeEnd: {TimeEnd}");
            sb.AppendLine($"TimePaused: {TimePaused}");

            return sb.ToString();
        }
    }
}
