using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Loot;

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

    [AssetEnum]
    public enum LootActionType
    {
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

    [AssetEnum]
    public enum PlayerScope
    {
        CurrentRecipientOnly = 0,
        Party = 1,
        Nearby = 2,
        Friends = 3,
        Guild = 4,
    }

    [AssetEnum]
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

    [AssetEnum]
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
        public PickMethod PickMethod { get; private set; }
        public float NoDropPercent { get; private set; }
        public LootNodePrototype[] Choices { get; private set; }
        public ulong MissionLogRewardsText { get; private set; }
        public bool LiveTuningDefaultEnabled { get; private set; }
    }

    public class LootTableAssignmentPrototype : Prototype
    {
        public ulong Name { get; private set; }
        public LootDropEventType Event { get; private set; }
        public ulong Table { get; private set; }
    }

    public class LootDropPrototype : LootNodePrototype
    {
        public short NumMin { get; private set; }
        public short NumMax { get; private set; }
    }

    public class LootNodePrototype : Prototype
    {
        public short Weight { get; private set; }
        public LootRollModifierPrototype[] Modifiers { get; private set; }
    }

    public class LootActionPrototype : LootNodePrototype
    {
        public LootNodePrototype Target { get; private set; }
    }

    public class LootActionFirstTimePrototype : LootActionPrototype
    {
        public bool FirstTime { get; private set; }
    }

    public class LootActionLoopOverAvatarsPrototype : LootActionPrototype
    {
    }

    public class LootCooldownPrototype : Prototype
    {
        public ulong Channel { get; private set; }
    }

    public class LootCooldownEntityPrototype : LootCooldownPrototype
    {
        public ulong Entity { get; private set; }
    }

    public class LootCooldownVendorTypePrototype : LootCooldownPrototype
    {
        public ulong VendorType { get; private set; }
    }

    public class LootCooldownHierarchyPrototype : Prototype
    {
        public ulong Entity { get; private set; }
        public ulong[] LocksOut { get; private set; }
    }

    public class LootCooldownRolloverTimeEntryPrototype : Prototype
    {
        public float WallClockTime24Hr { get; private set; }
        public Weekday WallClockTimeDay { get; private set; }
    }

    public class LootCooldownChannelPrototype : Prototype
    {
    }

    public class LootCooldownChannelRollOverPrototype : LootCooldownChannelPrototype
    {
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; private set; }
    }

    public class LootCooldownChannelTimePrototype : LootCooldownChannelPrototype
    {
        public float DurationMinutes { get; private set; }
    }

    public class LootCooldownChannelCountPrototype : LootCooldownChannelPrototype
    {
        public int MaxDrops { get; private set; }
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; private set; }
    }

    public class LootDropAgentPrototype : LootDropPrototype
    {
        public ulong Agent { get; private set; }
    }

    public class LootDropCharacterTokenPrototype : LootNodePrototype
    {
        public CharacterTokenType AllowedTokenType { get; private set; }
        public CharacterFilterType FilterType { get; private set; }
        public LootNodePrototype OnTokenUnavailable { get; private set; }
    }

    public class LootDropClonePrototype : LootNodePrototype
    {
        public LootMutationPrototype[] Mutations { get; private set; }
        public short SourceIndex { get; private set; }
    }

    public class LootDropCreditsPrototype : LootNodePrototype
    {
        public ulong Type { get; private set; }
    }

    public class LootDropItemPrototype : LootDropPrototype
    {
        public ulong Item { get; private set; }
        public LootMutationPrototype[] Mutations { get; private set; }
    }

    public class LootDropItemFilterPrototype : LootDropPrototype
    {
        public short ItemRank { get; private set; }
        public EquipmentInvUISlot UISlot { get; private set; }
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
        public ulong XPCurve { get; private set; }
    }

    public class LootDropRealMoneyPrototype : LootDropPrototype
    {
        public ulong CouponCode { get; private set; }
        public ulong TransactionContext { get; private set; }
    }

    public class LootDropBannerMessagePrototype : LootNodePrototype
    {
        public ulong BannerMessage { get; private set; }
    }

    public class LootDropUsePowerPrototype : LootNodePrototype
    {
        public ulong Power { get; private set; }
    }

    public class LootDropPlayVisualEffectPrototype : LootNodePrototype
    {
        public ulong RecipientVisualEffect { get; private set; }
        public ulong DropperVisualEffect { get; private set; }
    }

    public class LootDropChatMessagePrototype : LootNodePrototype
    {
        public ulong ChatMessage { get; private set; }
        public PlayerScope MessageScope { get; private set; }
    }

    public class LootDropVanityTitlePrototype : LootNodePrototype
    {
        public ulong VanityTitle { get; private set; }
    }

    public class LootDropVendorXPPrototype : LootNodePrototype
    {
        public ulong Vendor { get; private set; }
        public int XP { get; private set; }
    }

    public class LootLocationModifierPrototype : Prototype
    {
    }

    public class LootSearchRadiusPrototype : LootLocationModifierPrototype
    {
        public float MinRadius { get; private set; }
        public float MaxRadius { get; private set; }
    }

    public class LootBoundsOverridePrototype : LootLocationModifierPrototype
    {
        public float Radius { get; private set; }
    }

    public class LootLocationOffsetPrototype : LootLocationModifierPrototype
    {
        public float Offset { get; private set; }
    }

    public class DropInPlacePrototype : LootLocationModifierPrototype
    {
        public bool Check { get; private set; }
    }

    public class LootLocationNodePrototype : Prototype
    {
        public short Weight { get; private set; }
        public LootLocationModifierPrototype[] Modifiers { get; private set; }
    }

    public class LootLocationTablePrototype : LootLocationNodePrototype
    {
        public LootLocationNodePrototype[] Choices { get; private set; }
    }

    #region LootRollModifier

    public class LootRollModifierPrototype : Prototype
    {
    }

    public class LootRollClampLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; private set; }
        public int LevelMax { get; private set; }
    }

    public class LootRollRequireLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; private set; }
        public int LevelMax { get; private set; }
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
        public int LevelOffset { get; private set; }
    }

    public class LootRollOnceDailyPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; private set; }
    }

    public class LootRollCooldownOncePerRolloverPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; private set; }
    }

    public class LootRollCooldownByChannelPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; private set; }
    }

    public class LootRollSetAvatarPrototype : LootRollModifierPrototype
    {
        public ulong Avatar { get; private set; }
    }

    public class LootRollSetItemLevelPrototype : LootRollModifierPrototype
    {
        public int Level { get; private set; }
    }

    public class LootRollModifyAffixLimitsPrototype : LootRollModifierPrototype
    {
        public AffixPosition Position { get; private set; }
        public short ModifyMinBy { get; private set; }
        public short ModifyMaxBy { get; private set; }
        public ulong Category { get; private set; }
    }

    public class LootRollSetRarityPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; private set; }
    }

    public class LootRollSetUsablePrototype : LootRollModifierPrototype
    {
        public float Usable { get; private set; }
    }

    public class LootRollUseLevelVerbatimPrototype : LootRollModifierPrototype
    {
        public bool UseLevelVerbatim { get; private set; }
    }

    public class LootRollRequireDifficultyTierPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; private set; }
    }

    public class LootRollModifyDropByDifficultyTierPrototype : LootRollModifierPrototype
    {
        public ulong ModifierCurve { get; private set; }
    }

    public class LootRollRequireConditionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; private set; }
    }

    public class LootRollForbidConditionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; private set; }
    }

    public class LootRollRequireDropperKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; private set; }
    }

    public class LootRollForbidDropperKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; private set; }
    }

    public class LootRollRequireRegionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; private set; }
    }

    public class LootRollForbidRegionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; private set; }
    }

    public class LootRollRequireRegionScenarioRarityPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; private set; }
    }

    public class LootRollRequireKillCountPrototype : LootRollModifierPrototype
    {
        public int KillsRequired { get; private set; }
    }

    public class LootRollRequireWeekdayPrototype : LootRollModifierPrototype
    {
        public Weekday[] Choices { get; private set; }
    }

    public class LootRollIgnoreCooldownPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollIgnoreVendorXPCapPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollSetRegionAffixTablePrototype : LootRollModifierPrototype
    {
        public ulong RegionAffixTable { get; private set; }
    }

    public class LootRollIncludeCurrencyBonusPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollMissionStateRequiredPrototype : LootRollModifierPrototype
    {
        public ulong[] Missions { get; private set; }
        public MissionState RequiredState { get; private set; }
    }

    #endregion

    #region LootMutation

    public class LootMutationPrototype : Prototype
    {
    }

    public class LootAddAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; private set; }
        public short Count { get; private set; }
        public AffixPosition Position { get; private set; }
        public AffixCategoryPrototype Categories { get; private set; }
    }

    public class LootApplyNoVisualsOverridePrototype : LootMutationPrototype
    {
    }

    public class LootMutateBindingPrototype : LootMutationPrototype
    {
        public LootBindingType Binding { get; private set; }
    }

    public class LootClampLevelPrototype : LootMutationPrototype
    {
        public int MaxLevel { get; private set; }
        public int MinLevel { get; private set; }
    }

    public class LootCloneAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; private set; }
        public int SourceIndex { get; private set; }
        public AffixPosition Position { get; private set; }
        public bool EnforceAffixLimits { get; private set; }
        public AffixCategoryPrototype Categories { get; private set; }
    }

    public class LootCloneBuiltinAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; private set; }
        public int SourceIndex { get; private set; }
        public AffixPosition Position { get; private set; }
        public bool EnforceAffixLimits { get; private set; }
        public AffixCategoryPrototype Categories { get; private set; }
    }

    public class LootCloneLevelPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; private set; }
    }

    public class LootDropAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; private set; }
        public AffixPosition Position { get; private set; }
        public AffixCategoryPrototype Categories { get; private set; }
    }

    public class LootMutateAffixesPrototype : LootMutationPrototype
    {
        public ulong[] NewItemKeywords { get; private set; }
        public ulong[] OldItemKeywords { get; private set; }
        public bool OnlyReplaceIfAllMatched { get; private set; }
    }

    public class LootMutateAvatarPrototype : LootMutationPrototype
    {
    }

    public class LootMutateLevelPrototype : LootMutationPrototype
    {
    }

    public class OffsetLootLevelPrototype : LootMutationPrototype
    {
        public int LevelOffset { get; private set; }
    }

    public class LootMutateRankPrototype : LootMutationPrototype
    {
        public int Rank { get; private set; }
    }

    public class LootMutateRarityPrototype : LootMutationPrototype
    {
        public bool RerollAffixCount { get; private set; }
    }

    public class LootMutateSlotPrototype : LootMutationPrototype
    {
        public EquipmentInvUISlot Slot { get; private set; }
    }

    public class LootMutateBuiltinSeedPrototype : LootMutationPrototype
    {
    }

    public class LootMutateAffixSeedPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; private set; }
        public AffixPosition Position { get; private set; }
        public AffixCategoryPrototype Categories { get; private set; }
    }

    public class LootReplaceAffixesPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; private set; }
        public ulong[] Keywords { get; private set; }
        public AffixPosition Position { get; private set; }
        public bool EnforceAffixLimits { get; private set; }
        public AffixCategoryPrototype Categories { get; private set; }
    }

    public class LootCloneSeedPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; private set; }
    }

    public class LootAddAffixPrototype : LootMutationPrototype
    {
        public ulong Affix { get; private set; }
    }

    public class LootEvalPrototype : LootMutationPrototype
    {
        public EvalPrototype Eval { get; private set; }
    }

    #endregion
}
