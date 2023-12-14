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
        public PickMethod PickMethod { get; set; }
        public float NoDropPercent { get; set; }
        public LootNodePrototype[] Choices { get; set; }
        public ulong MissionLogRewardsText { get; set; }
        public bool LiveTuningDefaultEnabled { get; set; }
    }

    public class LootTableAssignmentPrototype : Prototype
    {
        public ulong Name { get; set; }
        public LootDropEventType Event { get; set; }
        public ulong Table { get; set; }
    }

    public class LootDropPrototype : LootNodePrototype
    {
        public short NumMin { get; set; }
        public short NumMax { get; set; }
    }

    public class LootNodePrototype : Prototype
    {
        public short Weight { get; set; }
        public LootRollModifierPrototype[] Modifiers { get; set; }
    }

    public class LootActionPrototype : LootNodePrototype
    {
        public LootNodePrototype Target { get; set; }
    }

    public class LootActionFirstTimePrototype : LootActionPrototype
    {
        public bool FirstTime { get; set; }
    }

    public class LootActionLoopOverAvatarsPrototype : LootActionPrototype
    {
    }

    public class LootCooldownPrototype : Prototype
    {
        public ulong Channel { get; set; }
    }

    public class LootCooldownEntityPrototype : LootCooldownPrototype
    {
        public ulong Entity { get; set; }
    }

    public class LootCooldownVendorTypePrototype : LootCooldownPrototype
    {
        public ulong VendorType { get; set; }
    }

    public class LootCooldownHierarchyPrototype : Prototype
    {
        public ulong Entity { get; set; }
        public ulong[] LocksOut { get; set; }
    }

    public class LootCooldownRolloverTimeEntryPrototype : Prototype
    {
        public float WallClockTime24Hr { get; set; }
        public Weekday WallClockTimeDay { get; set; }
    }

    public class LootCooldownChannelPrototype : Prototype
    {
    }

    public class LootCooldownChannelRollOverPrototype : LootCooldownChannelPrototype
    {
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; set; }
    }

    public class LootCooldownChannelTimePrototype : LootCooldownChannelPrototype
    {
        public float DurationMinutes { get; set; }
    }

    public class LootCooldownChannelCountPrototype : LootCooldownChannelPrototype
    {
        public int MaxDrops { get; set; }
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries { get; set; }
    }

    public class LootDropAgentPrototype : LootDropPrototype
    {
        public ulong Agent { get; set; }
    }

    public class LootDropCharacterTokenPrototype : LootNodePrototype
    {
        public CharacterTokenType AllowedTokenType { get; set; }
        public CharacterFilterType FilterType { get; set; }
        public LootNodePrototype OnTokenUnavailable { get; set; }
    }

    public class LootDropClonePrototype : LootNodePrototype
    {
        public LootMutationPrototype[] Mutations { get; set; }
        public short SourceIndex { get; set; }
    }

    public class LootDropCreditsPrototype : LootNodePrototype
    {
        public ulong Type { get; set; }
    }

    public class LootDropItemPrototype : LootDropPrototype
    {
        public ulong Item { get; set; }
        public LootMutationPrototype[] Mutations { get; set; }
    }

    public class LootDropItemFilterPrototype : LootDropPrototype
    {
        public short ItemRank { get; set; }
        public EquipmentInvUISlot UISlot { get; set; }
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
        public ulong XPCurve { get; set; }
    }

    public class LootDropRealMoneyPrototype : LootDropPrototype
    {
        public ulong CouponCode { get; set; }
        public ulong TransactionContext { get; set; }
    }

    public class LootDropBannerMessagePrototype : LootNodePrototype
    {
        public ulong BannerMessage { get; set; }
    }

    public class LootDropUsePowerPrototype : LootNodePrototype
    {
        public ulong Power { get; set; }
    }

    public class LootDropPlayVisualEffectPrototype : LootNodePrototype
    {
        public ulong RecipientVisualEffect { get; set; }
        public ulong DropperVisualEffect { get; set; }
    }

    public class LootDropChatMessagePrototype : LootNodePrototype
    {
        public ulong ChatMessage { get; set; }
        public PlayerScope MessageScope { get; set; }
    }

    public class LootDropVanityTitlePrototype : LootNodePrototype
    {
        public ulong VanityTitle { get; set; }
    }

    public class LootDropVendorXPPrototype : LootNodePrototype
    {
        public ulong Vendor { get; set; }
        public int XP { get; set; }
    }

    public class LootLocationModifierPrototype : Prototype
    {
    }

    public class LootSearchRadiusPrototype : LootLocationModifierPrototype
    {
        public float MinRadius { get; set; }
        public float MaxRadius { get; set; }
    }

    public class LootBoundsOverridePrototype : LootLocationModifierPrototype
    {
        public float Radius { get; set; }
    }

    public class LootLocationOffsetPrototype : LootLocationModifierPrototype
    {
        public float Offset { get; set; }
    }

    public class DropInPlacePrototype : LootLocationModifierPrototype
    {
        public bool Check { get; set; }
    }

    public class LootLocationNodePrototype : Prototype
    {
        public short Weight { get; set; }
        public LootLocationModifierPrototype[] Modifiers { get; set; }
    }

    public class LootLocationTablePrototype : LootLocationNodePrototype
    {
        public LootLocationNodePrototype[] Choices { get; set; }
    }

    #region LootRollModifier

    public class LootRollModifierPrototype : Prototype
    {
    }

    public class LootRollClampLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; set; }
        public int LevelMax { get; set; }
    }

    public class LootRollRequireLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin { get; set; }
        public int LevelMax { get; set; }
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
        public int LevelOffset { get; set; }
    }

    public class LootRollOnceDailyPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; set; }
    }

    public class LootRollCooldownOncePerRolloverPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; set; }
    }

    public class LootRollCooldownByChannelPrototype : LootRollModifierPrototype
    {
        public bool PerAccount { get; set; }
    }

    public class LootRollSetAvatarPrototype : LootRollModifierPrototype
    {
        public ulong Avatar { get; set; }
    }

    public class LootRollSetItemLevelPrototype : LootRollModifierPrototype
    {
        public int Level { get; set; }
    }

    public class LootRollModifyAffixLimitsPrototype : LootRollModifierPrototype
    {
        public AffixPosition Position { get; set; }
        public short ModifyMinBy { get; set; }
        public short ModifyMaxBy { get; set; }
        public ulong Category { get; set; }
    }

    public class LootRollSetRarityPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; set; }
    }

    public class LootRollSetUsablePrototype : LootRollModifierPrototype
    {
        public float Usable { get; set; }
    }

    public class LootRollUseLevelVerbatimPrototype : LootRollModifierPrototype
    {
        public bool UseLevelVerbatim { get; set; }
    }

    public class LootRollRequireDifficultyTierPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; set; }
    }

    public class LootRollModifyDropByDifficultyTierPrototype : LootRollModifierPrototype
    {
        public ulong ModifierCurve { get; set; }
    }

    public class LootRollRequireConditionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; set; }
    }

    public class LootRollForbidConditionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; set; }
    }

    public class LootRollRequireDropperKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; set; }
    }

    public class LootRollForbidDropperKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; set; }
    }

    public class LootRollRequireRegionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; set; }
    }

    public class LootRollForbidRegionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; set; }
    }

    public class LootRollRequireRegionScenarioRarityPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices { get; set; }
    }

    public class LootRollRequireKillCountPrototype : LootRollModifierPrototype
    {
        public int KillsRequired { get; set; }
    }

    public class LootRollRequireWeekdayPrototype : LootRollModifierPrototype
    {
        public Weekday[] Choices { get; set; }
    }

    public class LootRollIgnoreCooldownPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollIgnoreVendorXPCapPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollSetRegionAffixTablePrototype : LootRollModifierPrototype
    {
        public ulong RegionAffixTable { get; set; }
    }

    public class LootRollIncludeCurrencyBonusPrototype : LootRollModifierPrototype
    {
    }

    public class LootRollMissionStateRequiredPrototype : LootRollModifierPrototype
    {
        public ulong[] Missions { get; set; }
        public MissionState RequiredState { get; set; }
    }

    #endregion

    #region LootMutation

    public class LootMutationPrototype : Prototype
    {
    }

    public class LootAddAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; set; }
        public short Count { get; set; }
        public AffixPosition Position { get; set; }
        public AffixCategoryPrototype Categories { get; set; }
    }

    public class LootApplyNoVisualsOverridePrototype : LootMutationPrototype
    {
    }

    public class LootMutateBindingPrototype : LootMutationPrototype
    {
        public LootBindingType Binding { get; set; }
    }

    public class LootClampLevelPrototype : LootMutationPrototype
    {
        public int MaxLevel { get; set; }
        public int MinLevel { get; set; }
    }

    public class LootCloneAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; set; }
        public int SourceIndex { get; set; }
        public AffixPosition Position { get; set; }
        public bool EnforceAffixLimits { get; set; }
        public AffixCategoryPrototype Categories { get; set; }
    }

    public class LootCloneBuiltinAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; set; }
        public int SourceIndex { get; set; }
        public AffixPosition Position { get; set; }
        public bool EnforceAffixLimits { get; set; }
        public AffixCategoryPrototype Categories { get; set; }
    }

    public class LootCloneLevelPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; set; }
    }

    public class LootDropAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; set; }
        public AffixPosition Position { get; set; }
        public AffixCategoryPrototype Categories { get; set; }
    }

    public class LootMutateAffixesPrototype : LootMutationPrototype
    {
        public ulong[] NewItemKeywords { get; set; }
        public ulong[] OldItemKeywords { get; set; }
        public bool OnlyReplaceIfAllMatched { get; set; }
    }

    public class LootMutateAvatarPrototype : LootMutationPrototype
    {
    }

    public class LootMutateLevelPrototype : LootMutationPrototype
    {
    }

    public class OffsetLootLevelPrototype : LootMutationPrototype
    {
        public int LevelOffset { get; set; }
    }

    public class LootMutateRankPrototype : LootMutationPrototype
    {
        public int Rank { get; set; }
    }

    public class LootMutateRarityPrototype : LootMutationPrototype
    {
        public bool RerollAffixCount { get; set; }
    }

    public class LootMutateSlotPrototype : LootMutationPrototype
    {
        public EquipmentInvUISlot Slot { get; set; }
    }

    public class LootMutateBuiltinSeedPrototype : LootMutationPrototype
    {
    }

    public class LootMutateAffixSeedPrototype : LootMutationPrototype
    {
        public ulong[] Keywords { get; set; }
        public AffixPosition Position { get; set; }
        public AffixCategoryPrototype Categories { get; set; }
    }

    public class LootReplaceAffixesPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; set; }
        public ulong[] Keywords { get; set; }
        public AffixPosition Position { get; set; }
        public bool EnforceAffixLimits { get; set; }
        public AffixCategoryPrototype Categories { get; set; }
    }

    public class LootCloneSeedPrototype : LootMutationPrototype
    {
        public int SourceIndex { get; set; }
    }

    public class LootAddAffixPrototype : LootMutationPrototype
    {
        public ulong Affix { get; set; }
    }

    public class LootEvalPrototype : LootMutationPrototype
    {
        public EvalPrototype Eval { get; set; }
    }

    #endregion
}
