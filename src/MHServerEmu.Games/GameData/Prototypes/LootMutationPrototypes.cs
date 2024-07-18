using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class LootMutationPrototype : Prototype
    {
    }

    public class LootAddAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public short Count { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }       // VectorPrototypeRefPtr AffixCategoryPrototype 
    }

    public class LootApplyNoVisualsOverridePrototype : LootMutationPrototype
    {
    }

    public class LootMutateBindingPrototype : LootMutationPrototype
    {
        public LootBindingType Binding { get; protected set; }
    }

    public class LootClampLevelPrototype : LootMutationPrototype
    {
        public int MaxLevel { get; protected set; }
        public int MinLevel { get; protected set; }
    }

    public class LootCloneAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public int SourceIndex { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public bool EnforceAffixLimits { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 
    }

    public class LootCloneBuiltinAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public int SourceIndex { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public bool EnforceAffixLimits { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 
    }

    public class LootCloneLevelPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; protected set; }
    }

    public class LootDropAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 
    }

    public class LootMutateAffixesPrototype : LootMutationPrototype
    {
        public AssetId[] NewItemKeywords { get; protected set; }
        public AssetId[] OldItemKeywords { get; protected set; }
        public bool OnlyReplaceIfAllMatched { get; protected set; }
    }

    public class LootMutateAvatarPrototype : LootMutationPrototype
    {
    }

    public class LootMutateLevelPrototype : LootMutationPrototype
    {
    }

    public class OffsetLootLevelPrototype : LootMutationPrototype
    {
        public int LevelOffset { get; protected set; }
    }

    public class LootMutateRankPrototype : LootMutationPrototype
    {
        public int Rank { get; protected set; }
    }

    public class LootMutateRarityPrototype : LootMutationPrototype
    {
        public bool RerollAffixCount { get; protected set; }
    }

    public class LootMutateSlotPrototype : LootMutationPrototype
    {
        public EquipmentInvUISlot Slot { get; protected set; }
    }

    public class LootMutateBuiltinSeedPrototype : LootMutationPrototype
    {
    }

    public class LootMutateAffixSeedPrototype : LootMutationPrototype
    {
        public AssetId[] Keywords { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 
    }

    public class LootReplaceAffixesPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; protected set; }
        public AssetId[] Keywords { get; protected set; }
        public AffixPosition Position { get; protected set; }
        public bool EnforceAffixLimits { get; protected set; }
        public PrototypeId[] Categories { get; protected set; }    // VectorPrototypeRefPtr AffixCategoryPrototype 
    }

    public class LootCloneSeedPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; protected set; }
    }

    public class LootAddAffixPrototype : LootMutationPrototype
    {
        public PrototypeId Affix { get; protected set; }
    }

    public class LootEvalPrototype : LootMutationPrototype
    {
        public EvalPrototype Eval { get; protected set; }
    }
}
