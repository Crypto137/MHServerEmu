using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Powers
{
    public class PowerCollectionRecord
    {
        private const int FlagCount = 9;

        public ulong PowerPrototypeId { get; set; }
        public bool[] Flags { get; set; }
        public uint PowerRank { get; set; }
        public uint CharacterLevel { get; set; }
        public uint CombatLevel { get; set; }
        public uint ItemLevel { get; set; }
        public float ItemVariation { get; set; }
        public uint Field7 { get; set; }

        public PowerCollectionRecord(CodedInputStream stream, PowerCollectionRecord previousRecord)
        {
            PowerPrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Power);
            Flags = stream.ReadRawVarint32().ToBoolArray(FlagCount);
            PowerRank = Flags[1] ? 0 : stream.ReadRawVarint32();

            // CharacterLevel
            if (Flags[2])
                CharacterLevel = 1;
            else if (Flags[3])
                CharacterLevel = previousRecord.CharacterLevel;
            else
                CharacterLevel = stream.ReadRawVarint32();

            // CombatLevel
            if (Flags[4])
                CombatLevel = 1;
            else if (Flags[5])
                CombatLevel = previousRecord.CombatLevel;
            else if (Flags[6])
                CombatLevel = CharacterLevel;
            else
                CombatLevel = stream.ReadRawVarint32();

            ItemLevel = Flags[7] ? 1 : stream.ReadRawVarint32();
            ItemVariation = Flags[8] ? 1.0f : BitConverter.ToSingle(BitConverter.GetBytes(stream.ReadRawVarint32()));
            Field7 = Flags[0] ? 1 : stream.ReadRawVarint32();
        }

        public PowerCollectionRecord() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WritePrototypeId(PowerPrototypeId, PrototypeEnumType.Power);
                cos.WriteRawVarint32(Flags.ToUInt32());
                if (Flags[1] == false) cos.WriteRawVarint32(PowerRank);
                if (Flags[2] == false && Flags[3] == false) cos.WriteRawVarint32(CharacterLevel);
                if (Flags[4] == false && Flags[5] == false && Flags[6] == false) cos.WriteRawVarint32(CombatLevel);
                if (Flags[7] == false) cos.WriteRawVarint32(ItemLevel);
                if (Flags[8] == false) cos.WriteRawVarint32(BitConverter.ToUInt32(BitConverter.GetBytes(ItemVariation)));
                if (Flags[0] == false) cos.WriteRawVarint32(Field7);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PowerPrototypeId: {GameDatabase.GetPrototypePath(PowerPrototypeId)}");

            sb.Append("Flags: ");
            for (int i = 0; i < Flags.Length; i++) if (Flags[i]) sb.Append($"{i} ");
            sb.AppendLine();

            sb.AppendLine($"PowerRank: {PowerRank}");
            sb.AppendLine($"CharacterLevel: {CharacterLevel}");
            sb.AppendLine($"CombatLevel: {CombatLevel}");
            sb.AppendLine($"ItemLevel: {ItemLevel}");
            sb.AppendLine($"ItemVariation: {ItemVariation}");
            sb.AppendLine($"Field7: {Field7}");

            return sb.ToString();
        }
    }
}
