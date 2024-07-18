using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class DropFilterArguments
    {
        public Prototype ItemProto { get; set; }
        public PrototypeId RollFor { get; set; }
        public int Level { get; set; }
        public PrototypeId Rarity { get; set; }
        public int Rank { get; set; }
        public EquipmentInvUISlot Slot { get; set; }
        public LootContext LootContext { get; }

        public float DropDistanceSq { get; set; } = 0f;

        public DropFilterArguments() { }

        public DropFilterArguments(Prototype itemProto, PrototypeId rollFor, int level, PrototypeId rarityProtoRef,
            int rank, EquipmentInvUISlot slot, LootContext lootContext)
        {
            ItemProto = itemProto;
            RollFor = rollFor;
            Level = level;
            Rarity = rarityProtoRef;
            Rank = rank;
            Slot = slot;
            LootContext = lootContext;
        }

        public DropFilterArguments(DropFilterArguments other)
        {
            ItemProto = other.ItemProto;
            RollFor = other.RollFor;
            Level = other.Level;
            Rarity = other.Rarity;
            Rank = other.Rank;
            Slot = other.Slot;
            LootContext = other.LootContext;

            DropDistanceSq = other.DropDistanceSq;
        }

        public override string ToString()
        {
            return string.Format("Item: {0} Context: {1} Level: {2} Rarity: {3} Rank: {4} Slot: {5} RollFor: {6}",
                ItemProto,
                LootContext,
                Level,
                GameDatabase.GetFormattedPrototypeName(Rarity),
                Rank,
                Slot,
                RollFor.GetName());
        }
    }
}
