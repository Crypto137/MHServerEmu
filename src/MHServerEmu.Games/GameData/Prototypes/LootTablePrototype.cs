using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Missions;
using System.Xml.Linq;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum LootEventType   // Loot/LootDropEventType.type
    {
        None = 0,
        OnInteractedWith = 3,
        OnHealthBelowPct = 2,
        OnHealthBelowPctHit = 1,
        OnKilled = 4,
        OnKilledChampion = 5,
        OnKilledElite = 6,
        OnKilledMiniBoss = 7,
        OnHit = 8,
        OnDamagedForPctHealth = 9,
    }

    [AssetEnum((int)None)]
    public enum LootActionType
    {
        None = 0,
        Spawn = 1,
        Give = 2
    }

    [AssetEnum]
    public enum CharacterFilterType
    {
        None = 0,
        DropCurrentAvatarOnly = 1,
        DropUnownedAvatarOnly = 2,
    }

    [AssetEnum((int)CurrentRecipientOnly)]
    public enum PlayerScope
    {
        CurrentRecipientOnly = 0,
        Party = 1,
        Nearby = 2,
        Friends = 3,
        Guild = 4,
    }

    [AssetEnum((int)None)]
    public enum AffixPosition
    {
        None = 0,
        Prefix = 1,
        Suffix = 2,
        Visual = 3,
        Cosmic = 5,
        Unique = 6,
        Ultimate = 4,
        Blessing = 7,
        Runeword = 8,
        TeamUp = 9,
        Metadata = 10,
        PetTech1 = 11,
        PetTech2 = 12,
        PetTech3 = 13,
        PetTech4 = 14,
        PetTech5 = 15,
        RegionAffix = 16,
        Socket1 = 17,
        Socket2 = 18,
        Socket3 = 19,
    }

    [AssetEnum((int)All)]
    public enum Weekday
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6,
        All = 7,
    }

    [AssetEnum]
    public enum LootBindingType
    {
        None = 0,
        TradeRestricted = 1,
        TradeRestrictedRemoveBinding = 2,
        Avatar = 3,
    }

    #endregion

    public class LootTablePrototype : LootDropPrototype
    {
        public PickMethod PickMethod { get; protected set; }
        public float NoDropPercent { get; protected set; }
        public LootNodePrototype[] Choices { get; protected set; }
        public LocaleStringId MissionLogRewardsText { get; protected set; }
        public bool LiveTuningDefaultEnabled { get; protected set; }

        [DoNotCopy]
        public int LootTablePrototypeEnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            NoDropPercent = Math.Clamp(NoDropPercent, 0f, 1f);

            LootTablePrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetLootTableBlueprintDataRef());
        }
    }

    public class LootTableAssignmentPrototype : Prototype
    {
        public AssetId Name { get; protected set; }
        public LootDropEventType Event { get; protected set; }
        public PrototypeId Table { get; protected set; }
    }

    public class LootDropPrototype : LootNodePrototype
    {
        public short NumMin { get; protected set; }
        public short NumMax { get; protected set; }
    }

    public class LootNodePrototype : Prototype
    {
        public short Weight { get; protected set; }
        public LootRollModifierPrototype[] Modifiers { get; protected set; }
    }

    public class LootActionPrototype : LootNodePrototype
    {
        public LootNodePrototype Target { get; protected set; }
    }

    public class LootActionFirstTimePrototype : LootActionPrototype
    {
        public bool FirstTime { get; protected set; }
    }

    public class LootActionLoopOverAvatarsPrototype : LootActionPrototype
    {
    }

    public class LootCooldownPrototype : Prototype
    {
        public PrototypeId Channel { get; protected set; }

        [DoNotCopy]
        public virtual PrototypeId CooldownRef { get => PrototypeId.Invalid; }
    }

    public class LootCooldownEntityPrototype : LootCooldownPrototype
    {
        public PrototypeId Entity { get; protected set; }

        [DoNotCopy]
        public override PrototypeId CooldownRef { get => Entity; }
    }

    public class LootCooldownVendorTypePrototype : LootCooldownPrototype
    {
        public PrototypeId VendorType { get; protected set; }

        [DoNotCopy]
        public override PrototypeId CooldownRef { get => VendorType; }
    }

    public class LootCooldownHierarchyPrototype : Prototype
    {
        public PrototypeId Entity { get; protected set; }
        public PrototypeId[] LocksOut { get; protected set; }
    }

    public class LootCooldownRolloverTimeEntryPrototype : Prototype
    {
        public float WallClockTime24Hr { get; protected set; }
        public Weekday WallClockTimeDay { get; protected set; }
    }

    public class LootCooldownChannelPrototype : Prototype
    {
    }

    public class LootCooldownChannelRollOverPrototype : LootCooldownChannelPrototype
    {
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; protected set; }
    }

    public class LootCooldownChannelTimePrototype : LootCooldownChannelPrototype
    {
        public float DurationMinutes { get; protected set; }
    }

    public class LootCooldownChannelCountPrototype : LootCooldownChannelPrototype
    {
        public int MaxDrops { get; protected set; }
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; protected set; }
    }

    public class LootDropAgentPrototype : LootDropPrototype
    {
        public PrototypeId Agent { get; protected set; }
    }

    public class LootDropCharacterTokenPrototype : LootNodePrototype
    {
        public CharacterTokenType AllowedTokenType { get; protected set; }
        public CharacterFilterType FilterType { get; protected set; }
        public LootNodePrototype OnTokenUnavailable { get; protected set; }
    }

    public class LootDropClonePrototype : LootNodePrototype
    {
        public LootMutationPrototype[] Mutations { get; protected set; }
        public short SourceIndex { get; protected set; }
    }

    public class LootDropCreditsPrototype : LootNodePrototype
    {
        public CurveId Type { get; protected set; }
    }

    public class LootDropItemPrototype : LootDropPrototype
    {
        public PrototypeId Item { get; protected set; }
        public LootMutationPrototype[] Mutations { get; protected set; }
    }

    public class LootDropItemFilterPrototype : LootDropPrototype
    {
        public short ItemRank { get; protected set; }
        public EquipmentInvUISlot UISlot { get; protected set; }
    }

    public class LootDropPowerPointsPrototype : LootDropPrototype
    {
    }

    public class LootDropHealthBonusPrototype : LootDropPrototype
    {
    }

    public class LootDropEnduranceBonusPrototype : LootDropPrototype
    {
    }

    public class LootDropXPPrototype : LootNodePrototype
    {
        public CurveId XPCurve { get; protected set; }
    }

    public class LootDropRealMoneyPrototype : LootDropPrototype
    {
        public LocaleStringId CouponCode { get; protected set; }
        public PrototypeId TransactionContext { get; protected set; }
    }

    public class LootDropBannerMessagePrototype : LootNodePrototype
    {
        public PrototypeId BannerMessage { get; protected set; }
    }

    public class LootDropUsePowerPrototype : LootNodePrototype
    {
        public PrototypeId Power { get; protected set; }
    }

    public class LootDropPlayVisualEffectPrototype : LootNodePrototype
    {
        public AssetId RecipientVisualEffect { get; protected set; }
        public AssetId DropperVisualEffect { get; protected set; }
    }

    public class LootDropChatMessagePrototype : LootNodePrototype
    {
        public LocaleStringId ChatMessage { get; protected set; }
        public PlayerScope MessageScope { get; protected set; }
    }

    public class LootDropVanityTitlePrototype : LootNodePrototype
    {
        public PrototypeId VanityTitle { get; protected set; }
    }

    public class LootDropVendorXPPrototype : LootNodePrototype
    {
        public PrototypeId Vendor { get; protected set; }
        public int XP { get; protected set; }
    }

    public class LootLocationModifierPrototype : Prototype
    {
    }

    public class LootSearchRadiusPrototype : LootLocationModifierPrototype
    {
        public float MinRadius { get; protected set; }
        public float MaxRadius { get; protected set; }
    }

    public class LootBoundsOverridePrototype : LootLocationModifierPrototype
    {
        public float Radius { get; protected set; }
    }

    public class LootLocationOffsetPrototype : LootLocationModifierPrototype
    {
        public float Offset { get; protected set; }
    }

    public class DropInPlacePrototype : LootLocationModifierPrototype
    {
        public bool Check { get; protected set; }
    }

    public class LootLocationNodePrototype : Prototype
    {
        public short Weight { get; protected set; }
        public LootLocationModifierPrototype[] Modifiers { get; protected set; }
    }

    public class LootLocationTablePrototype : LootLocationNodePrototype
    {
        public LootLocationNodePrototype[] Choices { get; protected set; }
    }

    #region LootRollModifier

    public class LootRollModifierPrototype : Prototype
    {
    }

    public class LootRollClampLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; protected set; }
        public int LevelMax { get; protected set; }
    }

    public class LootRollRequireLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; protected set; }
        public int LevelMax { get; protected set; }
    }

    public class LootRollMarkSpecialPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollUnmarkSpecialPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollMarkRarePrototype : LootRollModifierPrototype
    {
    }

    public class LootRollUnmarkRarePrototype : LootRollModifierPrototype
    {
    }

    public class LootRollOffsetLevelPrototype : LootRollModifierPrototype
    {
        public int LevelOffset { get; protected set; }
    }

    public class LootRollOnceDailyPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; protected set; }
    }

    public class LootRollCooldownOncePerRolloverPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; protected set; }
    }

    public class LootRollCooldownByChannelPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; protected set; }
    }

    public class LootRollSetAvatarPrototype : LootRollModifierPrototype
    {
        public PrototypeId Avatar { get; protected set; }
    }

    public class LootRollSetItemLevelPrototype : LootRollModifierPrototype
    {
        public int Level { get; protected set; }
    }

    public class LootRollModifyAffixLimitsPrototype : LootRollModifierPrototype
    {
        public AffixPosition Position { get; protected set; }
        public short ModifyMinBy { get; protected set; }
        public short ModifyMaxBy { get; protected set; }
        public PrototypeId Category { get; protected set; }
    }

    public class LootRollSetRarityPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollSetUsablePrototype : LootRollModifierPrototype
    {
        public float Usable { get; protected set; }
    }

    public class LootRollUseLevelVerbatimPrototype : LootRollModifierPrototype
    {
        public bool UseLevelVerbatim { get; protected set; }
    }

    public class LootRollRequireDifficultyTierPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollModifyDropByDifficultyTierPrototype : LootRollModifierPrototype
    {
        public CurveId ModifierCurve { get; protected set; }
    }

    public class LootRollRequireConditionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollForbidConditionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollRequireDropperKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollForbidDropperKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollRequireRegionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollForbidRegionKeywordPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollRequireRegionScenarioRarityPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Choices { get; protected set; }
    }

    public class LootRollRequireKillCountPrototype : LootRollModifierPrototype
    {
        public int KillsRequired { get; protected set; }
    }

    public class LootRollRequireWeekdayPrototype : LootRollModifierPrototype
    {
        public Weekday[] Choices { get; protected set; }
    }

    public class LootRollIgnoreCooldownPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollIgnoreVendorXPCapPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollSetRegionAffixTablePrototype : LootRollModifierPrototype
    {
        public PrototypeId RegionAffixTable { get; protected set; }
    }

    public class LootRollIncludeCurrencyBonusPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollMissionStateRequiredPrototype : LootRollModifierPrototype
    {
        public PrototypeId[] Missions { get; protected set; }
        public MissionState RequiredState { get; protected set; }
    }

    #endregion

    #region LootMutation

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

    #endregion
}
