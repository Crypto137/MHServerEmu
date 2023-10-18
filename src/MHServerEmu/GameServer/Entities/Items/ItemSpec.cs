using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Entities.Items
{
    public class ItemSpec
    {
        public ulong ItemProto { get; set; }
        public ulong Rarity { get; set; }
        public int ItemLevel { get; set; }
        public int CreditsAmount { get; set; }
        public AffixSpec[] AffixSpec { get; set; }
        public int Seed { get; set; }
        public ulong EquippableBy { get; set; }

        public ItemSpec(CodedInputStream stream)
        {            
            ItemProto = stream.ReadPrototypeId(PrototypeEnumType.All);
            Rarity = stream.ReadPrototypeId(PrototypeEnumType.All);
            ItemLevel = stream.ReadRawInt32();
            CreditsAmount = stream.ReadRawInt32();

            AffixSpec = new AffixSpec[stream.ReadRawVarint64()];
            for (int i = 0; i < AffixSpec.Length; i++)
                AffixSpec[i] = new(stream);

            Seed = stream.ReadRawInt32();
            EquippableBy = stream.ReadPrototypeId(PrototypeEnumType.All);
        }

        public ItemSpec(ulong itemProto, ulong rarity, int itemLevel, int creditsAmount, AffixSpec[] affixSpec, int seed, ulong equippableBy)
        {
            ItemProto = itemProto;
            Rarity = rarity;
            ItemLevel = itemLevel;
            CreditsAmount = creditsAmount;
            AffixSpec = affixSpec;
            Seed = seed;
            EquippableBy = equippableBy;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);                

                cos.WritePrototypeId(ItemProto, PrototypeEnumType.All);
                cos.WritePrototypeId(Rarity, PrototypeEnumType.All);
                cos.WriteRawInt32(ItemLevel);
                cos.WriteRawInt32(CreditsAmount);

                foreach (AffixSpec affixSpec in AffixSpec) cos.WriteRawBytes(affixSpec.Encode());

                cos.WritePrototypeId(EquippableBy, PrototypeEnumType.All);

                cos.Flush();
                return ms.ToArray();
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ItemProto: {GameDatabase.GetPrototypeName(ItemProto)}");
            sb.AppendLine($"Rarity: {GameDatabase.GetPrototypeName(Rarity)}");
            sb.AppendLine($"ItemLevel: {ItemLevel}");
            sb.AppendLine($"CreditsAmount: {CreditsAmount}");
            for (int i = 0; i < AffixSpec.Length; i++) sb.AppendLine($"AffixSpec{i}: {AffixSpec[i]}");
            sb.AppendLine($"Seed: {Seed}");
            sb.AppendLine($"EquippableBy: {GameDatabase.GetPrototypeName(EquippableBy)}");

            return sb.ToString();
        }
    }
}
