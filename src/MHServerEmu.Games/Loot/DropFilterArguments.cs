using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public struct DropFilterArguments
    {
        public Prototype ItemProto { get; set; }
        public PrototypeId RollFor { get; set; }
        public int Level { get; }
        public PrototypeId Rarity { get; set; }
        public int Rank { get; }
        public EquipmentInvUISlot Slot { get; }
        public LootContext LootContext { get; }

        public float DropDistanceThresholdSq { get; set; } = 0f;

        public DropFilterArguments(Prototype itemProto, PrototypeId rollFor, int level, PrototypeId rarity,
            int rank, EquipmentInvUISlot slot, LootContext lootContext)
        {
            ItemProto = itemProto;
            RollFor = rollFor;
            Level = level;
            Rarity = rarity;
            Rank = rank;
            Slot = slot;
            LootContext = lootContext;
        }
    }
}
