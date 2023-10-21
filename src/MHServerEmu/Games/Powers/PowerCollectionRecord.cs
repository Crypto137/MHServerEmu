using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Powers
{
    public class PowerCollectionRecord
    {
        private const int FlagCount = 9;

        public ulong PowerPrototypeId { get; set; }
        public bool[] Flags { get; set; }
        public PowerIndexProperties IndexProps { get; set; }
        public uint PowerRefCount { get; set; }

        public PowerCollectionRecord(CodedInputStream stream, PowerCollectionRecord previousRecord)
        {
            PowerPrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.Power);
            Flags = stream.ReadRawVarint32().ToBoolArray(FlagCount);
            IndexProps = new();
            IndexProps.PowerRank = Flags[1] ? 0 : stream.ReadRawVarint32();

            // CharacterLevel
            if (Flags[2])
                IndexProps.CharacterLevel = 1;
            else if (Flags[3])
                IndexProps.CharacterLevel = previousRecord.IndexProps.CharacterLevel;
            else
                IndexProps.CharacterLevel = stream.ReadRawVarint32();

            // CombatLevel
            if (Flags[4])
                IndexProps.CombatLevel = 1;
            else if (Flags[5])
                IndexProps.CombatLevel = previousRecord.IndexProps.CombatLevel;
            else if (Flags[6])
                IndexProps.CombatLevel = IndexProps.CharacterLevel;
            else
                IndexProps.CombatLevel = stream.ReadRawVarint32();

            IndexProps.ItemLevel = Flags[7] ? 1 : stream.ReadRawVarint32();
            IndexProps.ItemVariation = Flags[8] ? 1.0f : stream.ReadRawFloat();
            PowerRefCount = Flags[0] ? 1 : stream.ReadRawVarint32();
        }

        public PowerCollectionRecord() { IndexProps = new(); }

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeEnum(PowerPrototypeId, PrototypeEnumType.Power);
            stream.WriteRawVarint32(Flags.ToUInt32());
            if (Flags[1] == false) stream.WriteRawVarint32(IndexProps.PowerRank);
            if (Flags[2] == false && Flags[3] == false) stream.WriteRawVarint32(IndexProps.CharacterLevel);
            if (Flags[4] == false && Flags[5] == false && Flags[6] == false) stream.WriteRawVarint32(IndexProps.CombatLevel);
            if (Flags[7] == false) stream.WriteRawVarint32(IndexProps.ItemLevel);
            if (Flags[8] == false) stream.WriteRawFloat(IndexProps.ItemVariation);
            if (Flags[0] == false) stream.WriteRawVarint32(PowerRefCount);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PowerPrototypeId: {GameDatabase.GetPrototypeName(PowerPrototypeId)}");

            sb.Append("Flags: ");
            for (int i = 0; i < Flags.Length; i++) if (Flags[i]) sb.Append($"{i} ");
            sb.AppendLine();

            sb.AppendLine($"IndexProps: {IndexProps}");
            sb.AppendLine($"PowerRefCount: {PowerRefCount}");

            return sb.ToString();
        }
    }

    public class PowerIndexProperties
    {
        public uint PowerRank { get; set; }
        public uint CharacterLevel { get; set; }
        public uint CombatLevel { get; set; }
        public uint ItemLevel { get; set; }
        public float ItemVariation { get; set; }

        public PowerIndexProperties()
        {
            PowerRank = 0;
            CharacterLevel = 1;
            CombatLevel = 1;
            ItemLevel = 1;
            ItemVariation = 1.0f;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PowerRank: {PowerRank}");
            sb.AppendLine($"CharacterLevel: {CharacterLevel}");
            sb.AppendLine($"CombatLevel: {CombatLevel}");
            sb.AppendLine($"ItemLevel: {ItemLevel}");
            sb.AppendLine($"ItemVariation: {ItemVariation}");
            return sb.ToString();
        }
    }
}
