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

        public IReadOnlyList<AffixRecord> AffixRecords { get => _affixRecordList != null ? _affixRecordList : Array.Empty<AffixRecord>(); }

        public int Seed { get; set; }
        public int StackCount { get; set; } = 1;
        public RestrictionTestFlags RestrictionFlags { get; set; } = RestrictionTestFlags.All;
        public PrototypeId EquippableBy { get; set; }

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

            args.Seed = itemSpec.Seed;
            args.StackCount = itemSpec.StackCount;
            args.RestrictionFlags = RestrictionTestFlags.All;
            args.EquippableBy = itemSpec.EquippableBy;

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

            args.Seed = other.Seed;
            args.StackCount = other.StackCount;
            args.RestrictionFlags = other.RestrictionFlags;
            args.EquippableBy = other.EquippableBy;
        }

        public override void ResetForPool()
        {
            base.ResetForPool();

            _affixRecordList.Clear();

            Seed = 0;
            StackCount = 1;
            RestrictionFlags = RestrictionTestFlags.All;
            EquippableBy = PrototypeId.Invalid;
        }

        public override void Dispose()
        {
            // Need to override Dispose so that loot clone records don't get pulled with regular drop filter args
            ObjectPoolManager.Instance.Return(this);
        }

        public ItemSpec ToItemSpec()
        {
            List<AffixSpec> affixSpecs = ListPool<AffixSpec>.Instance.Get();
            foreach (AffixRecord affixRecord in _affixRecordList)
            {
                AffixSpec affixSpec = new(affixRecord.AffixProtoRef.As<AffixPrototype>(), affixRecord.ScopeProtoRef, affixRecord.Seed);
                affixSpecs.Add(affixSpec);
            }

            ItemSpec itemSpec = new ItemSpec(ItemProto.DataRef, Rarity, Level, 0, affixSpecs, Seed, EquippableBy);
            itemSpec.StackCount = StackCount;

            // The constructor for ItemSpec takes only AffixSpec references from the provided list, so we can return the list itself to the pool now
            ListPool<AffixSpec>.Instance.Return(affixSpecs);

            return itemSpec;
        }
    }
}
