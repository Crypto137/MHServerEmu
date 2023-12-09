using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Items
{
    public class ItemSpec
    {
        public PrototypeId ItemProto { get; set; }
        public PrototypeId Rarity { get; set; }
        public int ItemLevel { get; set; }
        public int CreditsAmount { get; set; }
        public AffixSpec[] AffixSpec { get; set; }
        public int Seed { get; set; }
        public PrototypeId EquippableBy { get; set; }

        public ItemSpec(CodedInputStream stream)
        {            
            ItemProto = stream.ReadPrototypeEnum<Prototype>();
            Rarity = stream.ReadPrototypeEnum<Prototype>();
            ItemLevel = stream.ReadRawInt32();
            CreditsAmount = stream.ReadRawInt32();

            AffixSpec = new AffixSpec[stream.ReadRawVarint64()];
            for (int i = 0; i < AffixSpec.Length; i++)
                AffixSpec[i] = new(stream);

            Seed = stream.ReadRawInt32();
            EquippableBy = stream.ReadPrototypeEnum<Prototype>();
        }

        public ItemSpec(PrototypeId itemProto, PrototypeId rarity, int itemLevel, int creditsAmount, AffixSpec[] affixSpec, int seed, PrototypeId equippableBy)
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
            stream.WritePrototypeEnum<Prototype>(ItemProto);
            stream.WritePrototypeEnum<Prototype>(Rarity);
            stream.WriteRawInt32(ItemLevel);
            stream.WriteRawInt32(CreditsAmount);
            stream.WriteRawVarint64((ulong)AffixSpec.Length);
            foreach (AffixSpec affixSpec in AffixSpec) affixSpec.Encode(stream);
            stream.WriteRawInt32(Seed);
            stream.WritePrototypeEnum<Prototype>(EquippableBy);
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
