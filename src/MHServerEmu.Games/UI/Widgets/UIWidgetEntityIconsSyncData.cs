using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.UI.Widgets
{
    public class UIWidgetEntityIconsSyncData : UISyncData
    {
        public FilterEntry[] FilterEntries { get; set; }

        public UIWidgetEntityIconsSyncData(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef) : base(uiDataProvider, widgetRef, contextRef) { }

        public override void Decode(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            base.Decode(stream, boolDecoder);

            FilterEntries = new FilterEntry[stream.ReadRawVarint64()];
            for (int i = 0; i < FilterEntries.Length; i++)
                FilterEntries[i] = new(stream, boolDecoder);
        }

        public override void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            base.Encode(stream, boolEncoder);

            stream.WriteRawVarint64((ulong)FilterEntries.Length);
            for (int i = 0; i < FilterEntries.Length; i++)
                FilterEntries[i].Encode(stream, boolEncoder);
        }

        public override void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (FilterEntry entry in FilterEntries)
                entry.EncodeBools(boolEncoder);
        }

        protected override void BuildString(StringBuilder sb)
        {
            base.BuildString(sb);

            for (int i = 0; i < FilterEntries.Length; i++)
                sb.AppendLine($"FilterEntry{i}: {FilterEntries[i]}");
        }
    }

    public class FilterEntry
    {
        public ulong Index { get; set; }
        public KnownEntityEntry[] KnownEntityEntries { get; set; }

        public FilterEntry(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            Index = stream.ReadRawVarint64();

            KnownEntityEntries = new KnownEntityEntry[stream.ReadRawVarint64()];
            for (int i = 0; i < KnownEntityEntries.Length; i++)
                KnownEntityEntries[i] = new(stream, boolDecoder);
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WriteRawVarint64(Index);

            stream.WriteRawVarint64((ulong)KnownEntityEntries.Length);
            for (int i = 0; i < KnownEntityEntries.Length; i++)
                KnownEntityEntries[i].Encode(stream, boolEncoder);
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (KnownEntityEntry entry in KnownEntityEntries)
                entry.EncodeBools(boolEncoder);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Index: {Index}");
            for (int i = 0; i < KnownEntityEntries.Length; i++) sb.AppendLine($"KnownEntityEntry{i}: {KnownEntityEntries[i]}");
            return sb.ToString();
        }
    }

    public class KnownEntityEntry
    {
        public ulong EntryId { get; set; }
        public int State { get; set; }
        public int HealthPercent { get; set; }
        public int IconIndexForHealthPercentEval { get; set; }
        public bool ForceRefreshEntityHealthPercent { get; set; }
        public ulong EnrageStartTime { get; set; }
        public bool HasPropertyEntryEval { get; set; }
        public int PropertyEntryIndex { get; set; }

        public KnownEntityEntry(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            EntryId = stream.ReadRawVarint64();
            State = stream.ReadRawInt32();
            HealthPercent = stream.ReadRawInt32();
            IconIndexForHealthPercentEval = stream.ReadRawInt32();
            ForceRefreshEntityHealthPercent = boolDecoder.ReadBool(stream);
            EnrageStartTime = stream.ReadRawVarint64();
            HasPropertyEntryEval = boolDecoder.ReadBool(stream);
            PropertyEntryIndex = stream.ReadRawInt32();
        }

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WriteRawVarint64(EntryId);
            stream.WriteRawInt32(State);
            stream.WriteRawInt32(HealthPercent);
            stream.WriteRawInt32(IconIndexForHealthPercentEval);
            boolEncoder.WriteBuffer(stream);   // ForceRefreshEntityHealthPercent
            stream.WriteRawVarint64(EnrageStartTime);
            boolEncoder.WriteBuffer(stream);   // HasPropertyEntryEval
            stream.WriteRawInt32(PropertyEntryIndex);
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            boolEncoder.EncodeBool(ForceRefreshEntityHealthPercent);
            boolEncoder.EncodeBool(HasPropertyEntryEval);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"EntryId: {EntryId}");
            sb.AppendLine($"State: {State}");
            sb.AppendLine($"HealthPercent: {HealthPercent}");
            sb.AppendLine($"IconIndexForHealthPercentEval: {IconIndexForHealthPercentEval}");
            sb.AppendLine($"ForceRefreshEntityHealthPercent: {ForceRefreshEntityHealthPercent}");
            sb.AppendLine($"EnrageStartTime: {EnrageStartTime}");
            sb.AppendLine($"HasPropertyEntryEval: {HasPropertyEntryEval}");
            sb.AppendLine($"PropertyEntryIndex: {PropertyEntryIndex}");
            return sb.ToString();
        }
    }
}
