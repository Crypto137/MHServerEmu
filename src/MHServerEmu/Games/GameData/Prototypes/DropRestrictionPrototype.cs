using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class DropRestrictionPrototype : Prototype
    {
        public DropRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DropRestrictionPrototype), proto); }
    }

    public class ConditionalRestrictionPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Apply;
        public LootContext[] ApplyFor;
        public DropRestrictionPrototype[] Else;
        public ConditionalRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ConditionalRestrictionPrototype), proto); }
    }

    [Flags]
    public enum LootContext
    {
        None = 0,
        AchievementReward = 1,
        LeaderboardReward = 2,
        CashShop = 4,
        Crafting = 8,
        Drop = 16,
        Initialization = 32,
        Vendor = 64,
        MissionReward = 128,
        MysteryChest = 256,
    }
    public class ContextRestrictionPrototype : DropRestrictionPrototype
    {
        public LootContext[] UsableFor;
        public ContextRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ContextRestrictionPrototype), proto); }
    }

    public class ItemTypeRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong[] AllowedTypes;
        public ItemTypeRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemTypeRestrictionPrototype), proto); }
    }

    public class ItemParentRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong[] AllowedParents;
        public ItemParentRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemParentRestrictionPrototype), proto); }
    }

    public class HasAffixInPositionRestrictionPrototype : DropRestrictionPrototype
    {
        public AffixPosition Position;
        public HasAffixInPositionRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HasAffixInPositionRestrictionPrototype), proto); }
    }

    public class HasVisualAffixRestrictionPrototype : DropRestrictionPrototype
    {
        public bool MustHaveNoVisualAffixes;
        public bool MustHaveVisualAffix;
        public HasVisualAffixRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HasVisualAffixRestrictionPrototype), proto); }
    }

    public class LevelRestrictionPrototype : DropRestrictionPrototype
    {
        public int LevelMin;
        public int LevelRange;
        public LevelRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LevelRestrictionPrototype), proto); }
    }

    public class OutputLevelPrototype : DropRestrictionPrototype
    {
        public int Value;
        public bool UseAsFilter;
        public OutputLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OutputLevelPrototype), proto); }
    }

    public class OutputRankPrototype : DropRestrictionPrototype
    {
        public int Value;
        public bool UseAsFilter;
        public OutputRankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OutputRankPrototype), proto); }
    }

    public class OutputRarityPrototype : DropRestrictionPrototype
    {
        public ulong Value;
        public bool UseAsFilter;
        public OutputRarityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OutputRarityPrototype), proto); }
    }

    public class RarityRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong[] AllowedRarities;
        public RarityRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RarityRestrictionPrototype), proto); }
    }

    public class RankRestrictionPrototype : DropRestrictionPrototype
    {
        public int AllowedRanks;
        public RankRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RankRestrictionPrototype), proto); }
    }

    public class RestrictionListPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Children;
        public RestrictionListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RestrictionListPrototype), proto); }
    }

    public class SlotRestrictionPrototype : DropRestrictionPrototype
    {
        public EquipmentInvUISlot[] AllowedSlots;
        public SlotRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SlotRestrictionPrototype), proto); }
    }

    public class UsableByRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong[] Avatars;
        public UsableByRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UsableByRestrictionPrototype), proto); }
    }

    public class DistanceRestrictionPrototype : DropRestrictionPrototype
    {
        public DistanceRestrictionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DistanceRestrictionPrototype), proto); }
    }
}
