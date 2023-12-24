using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
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

    #endregion

    public class DropRestrictionPrototype : Prototype
    {
    }

    public class ConditionalRestrictionPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Apply { get; private set; }
        public LootContext[] ApplyFor { get; private set; }
        public DropRestrictionPrototype[] Else { get; private set; }
    }

    public class ContextRestrictionPrototype : DropRestrictionPrototype
    {
        public LootContext[] UsableFor { get; private set; }
    }

    public class ItemTypeRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong AllowedTypes { get; private set; }
    }

    public class ItemParentRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong AllowedParents { get; private set; }
    }

    public class HasAffixInPositionRestrictionPrototype : DropRestrictionPrototype
    {
        public AffixPosition Position { get; private set; }
    }

    public class HasVisualAffixRestrictionPrototype : DropRestrictionPrototype
    {
        public bool MustHaveNoVisualAffixes { get; private set; }
        public bool MustHaveVisualAffix { get; private set; }
    }

    public class LevelRestrictionPrototype : DropRestrictionPrototype
    {
        public int LevelMin { get; private set; }
        public int LevelRange { get; private set; }
    }

    public class OutputLevelPrototype : DropRestrictionPrototype
    {
        public int Value { get; private set; }
        public bool UseAsFilter { get; private set; }
    }

    public class OutputRankPrototype : DropRestrictionPrototype
    {
        public int Value { get; private set; }
        public bool UseAsFilter { get; private set; }
    }

    public class OutputRarityPrototype : DropRestrictionPrototype
    {
        public ulong Value { get; private set; }
        public bool UseAsFilter { get; private set; }
    }

    public class RarityRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong[] AllowedRarities { get; private set; }
    }

    public class RankRestrictionPrototype : DropRestrictionPrototype
    {
        public int AllowedRanks { get; private set; }
    }

    public class RestrictionListPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Children { get; private set; }
    }

    public class SlotRestrictionPrototype : DropRestrictionPrototype
    {
        public EquipmentInvUISlot[] AllowedSlots { get; private set; }
    }

    public class UsableByRestrictionPrototype : DropRestrictionPrototype
    {
        public ulong Avatars { get; private set; }
    }

    public class DistanceRestrictionPrototype : DropRestrictionPrototype
    {
    }
}
