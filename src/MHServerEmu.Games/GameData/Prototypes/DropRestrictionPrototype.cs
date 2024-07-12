using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class DropRestrictionPrototype : Prototype
    {
    }

    public class ConditionalRestrictionPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Apply { get; protected set; }
        public LootContext[] ApplyFor { get; protected set; }
        public DropRestrictionPrototype[] Else { get; protected set; }
    }

    public class ContextRestrictionPrototype : DropRestrictionPrototype
    {
        public LootContext[] UsableFor { get; protected set; }
    }

    public class ItemTypeRestrictionPrototype : DropRestrictionPrototype
    {
        public PrototypeId[] AllowedTypes { get; protected set; }
    }

    public class ItemParentRestrictionPrototype : DropRestrictionPrototype
    {
        public PrototypeId[] AllowedParents { get; protected set; }
    }

    public class HasAffixInPositionRestrictionPrototype : DropRestrictionPrototype
    {
        public AffixPosition Position { get; protected set; }
    }

    public class HasVisualAffixRestrictionPrototype : DropRestrictionPrototype
    {
        public bool MustHaveNoVisualAffixes { get; protected set; }
        public bool MustHaveVisualAffix { get; protected set; }
    }

    public class LevelRestrictionPrototype : DropRestrictionPrototype
    {
        public int LevelMin { get; protected set; }
        public int LevelRange { get; protected set; }
    }

    public class OutputLevelPrototype : DropRestrictionPrototype
    {
        public int Value { get; protected set; }
        public bool UseAsFilter { get; protected set; }
    }

    public class OutputRankPrototype : DropRestrictionPrototype
    {
        public int Value { get; protected set; }
        public bool UseAsFilter { get; protected set; }
    }

    public class OutputRarityPrototype : DropRestrictionPrototype
    {
        public PrototypeId Value { get; protected set; }
        public bool UseAsFilter { get; protected set; }
    }

    public class RarityRestrictionPrototype : DropRestrictionPrototype
    {
        public PrototypeId[] AllowedRarities { get; protected set; }
    }

    public class RankRestrictionPrototype : DropRestrictionPrototype
    {
        public int AllowedRanks { get; protected set; }
    }

    public class RestrictionListPrototype : DropRestrictionPrototype
    {
        public DropRestrictionPrototype[] Children { get; protected set; }
    }

    public class SlotRestrictionPrototype : DropRestrictionPrototype
    {
        public EquipmentInvUISlot[] AllowedSlots { get; protected set; }
    }

    public class UsableByRestrictionPrototype : DropRestrictionPrototype
    {
        public PrototypeId[] Avatars { get; protected set; }
    }

    public class DistanceRestrictionPrototype : DropRestrictionPrototype
    {
    }
}
