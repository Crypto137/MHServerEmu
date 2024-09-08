using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootCloneRecord : DropFilterArguments
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<AffixRecord> _affixRecordList;

        public IEnumerable<AffixRecord> AffixRecords { get => _affixRecordList != null ? _affixRecordList : Array.Empty<AffixRecord>(); }

        public LootCloneRecord(LootContext lootContext, ItemSpec itemSpec, PrototypeId rollFor)
            : base(itemSpec.ItemProtoRef.As<Prototype>(), rollFor, itemSpec.ItemLevel, itemSpec.RarityProtoRef, 0, EquipmentInvUISlot.Invalid, lootContext)
        {
            if (itemSpec.AffixSpecs.Any())
            {
                // Allocate affix record list on demand
                _affixRecordList = new();

                foreach (AffixSpec affixSpec in itemSpec.AffixSpecs)
                    _affixRecordList.Add(new(affixSpec));
            }

            ItemPrototype itemProto = itemSpec.ItemProtoRef.As<ItemPrototype>();
            if (itemProto == null) { Logger.Warn("LootCloneRecord(): itemProto == null"); return; }

            Rank = itemProto.GetRank(lootContext);

            if (RollFor == PrototypeId.Invalid)
            {
                // Determine RollFor dynamically if it's not provided
                if (itemProto is CostumePrototype costumeProto)
                {
                    RollFor = costumeProto.UsableBy;
                }
                else if (itemSpec.EquippableBy != PrototypeId.Invalid)
                {
                    RollFor = itemSpec.EquippableBy;
                }
                else
                {
                    // Fall back to the binding
                    itemSpec.GetBindingState(out PrototypeId agentProtoRef);
                    RollFor = agentProtoRef;
                }
            }

            Slot = itemProto.GetInventorySlotForAgent(RollFor.As<AgentPrototype>());
        }

        public LootCloneRecord(LootCloneRecord other) : base(other)
        {
            if (other._affixRecordList != null)
                _affixRecordList = new(_affixRecordList);
        }
    }
}
