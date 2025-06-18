using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootCloneRecord : DropFilterArguments
    {
        // LootCloneRecord is effectively a fully mutable version of ItemSpec used for cloning and mutating

        private static readonly Logger Logger = LogManager.CreateLogger();

        // Because LootCloneRecord is intended to be mutable, we expose the full List instead of just IReadOnlyList
        public List<AffixRecord> AffixRecords { get; } = new();

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

            args.AffixRecords.Clear();
            IReadOnlyList<AffixSpec> affixSpecs = itemSpec.AffixSpecs;
            for (int i = 0; i < affixSpecs.Count; i++)
            {
                AffixSpec affixSpec = affixSpecs[i];
                args.AffixRecords.Add(new(affixSpec));
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

            args.AffixRecords.Clear();
            args.AffixRecords.AddRange(other.AffixRecords);

            args.Seed = other.Seed;
            args.StackCount = other.StackCount;
            args.RestrictionFlags = other.RestrictionFlags;
            args.EquippableBy = other.EquippableBy;
        }

        public override void ResetForPool()
        {
            base.ResetForPool();

            AffixRecords.Clear();

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

        public void SetAffixes(IReadOnlyList<AffixSpec> affixSpecs)
        {
            AffixRecords.Clear();
            for (int i = 0; i < affixSpecs.Count; i++)
            {
                AffixSpec affixSpec = affixSpecs[i];
                AffixRecords.Add(new(affixSpec));
            }
        }

        public ItemSpec ToItemSpec()
        {
            return new(this);
        }
    }
}
