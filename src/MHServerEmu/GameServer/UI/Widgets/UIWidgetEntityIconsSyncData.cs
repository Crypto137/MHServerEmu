using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.UI.Widgets
{
    public class UIWidgetEntityIconsSyncData : UISyncData
    {
        public FilterEntry[] FilterEntries { get; set; }

        public UIWidgetEntityIconsSyncData(ulong widgetR, ulong contextR, ulong[] areas, CodedInputStream stream, BoolDecoder boolDecoder) : base(widgetR, contextR, areas)
        {
            FilterEntries = new FilterEntry[stream.ReadRawVarint64()];
            for (int i = 0; i < FilterEntries.Length; i++)
                FilterEntries[i] = new(stream, boolDecoder);
        }

        public override byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                WriteParentFields(cos);

                cos.WriteRawVarint64((ulong)FilterEntries.Length);
                for (int i = 0; i < FilterEntries.Length; i++)
                    cos.WriteRawBytes(FilterEntries[i].Encode(boolEncoder));

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override void WriteBools(BoolEncoder boolEncoder)
        {
            foreach (FilterEntry entry in FilterEntries)
                entry.WriteBools(boolEncoder);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteParentString(sb);
            for (int i = 0; i < FilterEntries.Length; i++) sb.AppendLine($"FilterEntry{i}: {FilterEntries[i]}");
            return sb.ToString();
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

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(Index);

                cos.WriteRawVarint64((ulong)KnownEntityEntries.Length);
                for (int i = 0; i < KnownEntityEntries.Length; i++)
                    cos.WriteRawBytes(KnownEntityEntries[i].Encode(boolEncoder));

                cos.Flush();
                return ms.ToArray();
            }
        }

        public void WriteBools(BoolEncoder boolEncoder)
        {
            foreach (KnownEntityEntry entry in KnownEntityEntries)
                entry.WriteBools(boolEncoder);
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

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            ForceRefreshEntityHealthPercent = boolDecoder.ReadBool();

            EnrageStartTime = stream.ReadRawVarint64();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            HasPropertyEntryEval = boolDecoder.ReadBool();

            PropertyEntryIndex = stream.ReadRawInt32();
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(EntryId);
                cos.WriteRawInt32(State);
                cos.WriteRawInt32(HealthPercent);
                cos.WriteRawInt32(IconIndexForHealthPercentEval);

                byte bitBuffer = boolEncoder.GetBitBuffer();    // ForceRefreshEntityHealthPercent
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                cos.WriteRawVarint64(EnrageStartTime);

                bitBuffer = boolEncoder.GetBitBuffer();         // HasPropertyEntryEval
                if (bitBuffer != 0) cos.WriteRawByte(bitBuffer);

                cos.WriteRawInt32(PropertyEntryIndex);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public void WriteBools(BoolEncoder boolEncoder)
        {
            boolEncoder.WriteBool(ForceRefreshEntityHealthPercent);
            boolEncoder.WriteBool(HasPropertyEntryEval);
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
