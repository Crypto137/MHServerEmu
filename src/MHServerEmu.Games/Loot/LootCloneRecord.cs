using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootCloneRecord : DropFilterArguments
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<AffixRecord> _affixRecordList = new();

        public IEnumerable<AffixRecord> AffixRecords { get => _affixRecordList != null ? _affixRecordList : Array.Empty<AffixRecord>(); }

        public LootCloneRecord() { }    // Use pooling instead of calling this directly

        public static void Initialize(LootCloneRecord args, LootContext lootContext, ItemSpec itemSpec, PrototypeId rollFor)
        {
            // NOTE: This is a replacement for a constructor to work with pooling

            Prototype proto = itemSpec.ItemProtoRef.As<Prototype>();

            DropFilterArguments.Initialize(args, proto, rollFor, itemSpec.ItemLevel, itemSpec.RarityProtoRef, 0, EquipmentInvUISlot.Invalid, lootContext);

            args._affixRecordList.Clear();
            IReadOnlyList<AffixSpec> affixSpecs = itemSpec.AffixSpecs;
            for (int i = 0; i < affixSpecs.Count; i++)
            {
                AffixSpec affixSpec = affixSpecs[i];
                args._affixRecordList.Add(new(affixSpec));
            }

            ItemPrototype itemProto = proto as ItemPrototype;
            if (itemProto == null) { Logger.Warn("Initialize(): itemProto == null"); return; }

            args.Rank = itemProto.GetRank(lootContext);

            if (args.RollFor == PrototypeId.Invalid)
            {
                // Determine RollFor dynamically if it's not provided
                if (itemProto is CostumePrototype costumeProto)
                {
                    args.RollFor = costumeProto.UsableBy;
                }
                else if (itemSpec.EquippableBy != PrototypeId.Invalid)
                {
                    args.RollFor = itemSpec.EquippableBy;
                }
                else
                {
                    // Fall back to the binding
                    itemSpec.GetBindingState(out PrototypeId agentProtoRef);
                    args.RollFor = agentProtoRef;
                }
            }

            args.Slot = itemProto.GetInventorySlotForAgent(args.RollFor.As<AgentPrototype>());
        }

        public static void Initialize(LootCloneRecord args, LootCloneRecord other)
        {
            // NOTE: This is a replacement for a constructor to work with pooling

            DropFilterArguments.Initialize(args, other);

            args._affixRecordList.Clear();
            if (other._affixRecordList.Count > 0)
                args._affixRecordList.AddRange(other._affixRecordList);
        }

        public override void ResetForPool()
        {
            base.ResetForPool();
            _affixRecordList.Clear();
        }

        public override void Dispose()
        {
            // Need to override Dispose so that loot clone records don't get pulled with regular drop filter args
            ObjectPoolManager.Instance.Return(this);
        }
    }
}
