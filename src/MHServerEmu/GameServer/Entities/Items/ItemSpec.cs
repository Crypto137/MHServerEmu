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
            ItemProto = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            Rarity = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            ItemLevel = stream.ReadRawInt32();
            CreditsAmount = stream.ReadRawInt32();

            AffixSpec = new AffixSpec[stream.ReadRawVarint64()];
            for (int i = 0; i < AffixSpec.Length; i++)
                AffixSpec[i] = new(stream);

            Seed = stream.ReadRawInt32();
            EquippableBy = stream.ReadPrototypeEnum(PrototypeEnumType.All);
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

        public void Encode(CodedOutputStream stream)
        {
            stream.WritePrototypeEnum(ItemProto, PrototypeEnumType.All);
            stream.WritePrototypeEnum(Rarity, PrototypeEnumType.All);
            stream.WriteRawInt32(ItemLevel);
            stream.WriteRawInt32(CreditsAmount);

            foreach (AffixSpec affixSpec in AffixSpec) affixSpec.Encode(stream);

            stream.WritePrototypeEnum(EquippableBy, PrototypeEnumType.All);
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
