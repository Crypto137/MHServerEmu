using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
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

    public class DropRestrictionPrototype : Prototype
    {
    }

    public class ConditionalRestrictionPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Apply { get; set; }
        public LootContext[] ApplyFor { get; set; }
        public DropRestrictionPrototype[] Else { get; set; }
    }

    public class ContextRestrictionPrototype : DropRestrictionPrototype
    {
        public LootContext[] UsableFor { get; set; }
    }

    public class ItemTypeRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong AllowedTypes { get; set; }
    }

    public class ItemParentRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong AllowedParents { get; set; }
    }

    public class HasAffixInPositionRestrictionPrototype : DropRestrictionPrototype
    {
        public AffixPosition Position { get; set; }
    }

    public class HasVisualAffixRestrictionPrototype : DropRestrictionPrototype
    {
        public bool MustHaveNoVisualAffixes { get; set; }
        public bool MustHaveVisualAffix { get; set; }
    }

    public class LevelRestrictionPrototype : DropRestrictionPrototype
    {
        public int LevelMin { get; set; }
        public int LevelRange { get; set; }
    }

    public class OutputLevelPrototype : DropRestrictionPrototype
    {
        public int Value { get; set; }
        public bool UseAsFilter { get; set; }
    }

    public class OutputRankPrototype : DropRestrictionPrototype
    {
        public int Value { get; set; }
        public bool UseAsFilter { get; set; }
    }

    public class OutputRarityPrototype : DropRestrictionPrototype
    {
        public ulong Value { get; set; }
        public bool UseAsFilter { get; set; }
    }

    public class RarityRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong[] AllowedRarities { get; set; }
    }

    public class RankRestrictionPrototype : DropRestrictionPrototype
    {
        public int AllowedRanks { get; set; }
    }

    public class RestrictionListPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Children { get; set; }
    }

    public class SlotRestrictionPrototype : DropRestrictionPrototype
    {
        public EquipmentInvUISlot[] AllowedSlots { get; set; }
    }

    public class UsableByRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong Avatars { get; set; }
    }

    public class DistanceRestrictionPrototype : DropRestrictionPrototype
    {
    }
}
