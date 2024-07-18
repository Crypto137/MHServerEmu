using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootCloneRecord : DropFilterArguments
    {
        public LootCloneRecord(DropFilterArguments other) : base(other)
        {
        }

        public LootCloneRecord(Prototype itemProto, PrototypeId rollFor, int level, PrototypeId rarity,
            int rank, EquipmentInvUISlot slot, LootContext lootContext)
            : base(itemProto, rollFor, level, rarity, rank, slot, lootContext)
        {
        }
    }
}
