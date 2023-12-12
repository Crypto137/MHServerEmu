using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.GameData.Prototypes
{

    public class LootTablePrototype : LootDropPrototype
    {
        public PickMethod PickMethod;
        public float NoDropPercent;
        public LootNodePrototype[] Choices;
        public ulong MissionLogRewardsText;
        public bool LiveTuningDefaultEnabled;
        public LootTablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootTablePrototype), proto); }
    }

    public class LootTableAssignmentPrototype : Prototype
    {
        public ulong Name;
        public LootEventType Event;
        public ulong Table;

        public LootTableAssignmentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootTableAssignmentPrototype), proto); }
    }

    public enum LootEventType
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

    public enum PickMethod
    {
        PickWeight,
        PickWeightTryAll,
        PickAll,
    }

    public class LootDropPrototype : LootNodePrototype
    {
        public short NumMin;
        public short NumMax;
        public LootDropPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropPrototype), proto); }

    }

    public class LootNodePrototype : Prototype
    {
        public LootRollModifierPrototype[] Modifiers;
        public short Weight;
        public LootNodePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootNodePrototype), proto); }

    }


    public class LootActionPrototype : LootNodePrototype
    {
        public LootNodePrototype Target;
        public LootActionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootActionPrototype), proto); }
    }


    public class LootActionFirstTimePrototype : LootActionPrototype
    {
        public bool FirstTime;
        public LootActionFirstTimePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootActionFirstTimePrototype), proto); }
    }

    public class LootActionLoopOverAvatarsPrototype : LootActionPrototype
    {
        public LootActionLoopOverAvatarsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootActionLoopOverAvatarsPrototype), proto); }
    }

    public class LootCooldownPrototype : Prototype
    {
        public ulong Channel;
        public LootCooldownPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCooldownPrototype), proto); }
    }

    public class LootCooldownEntityPrototype : LootCooldownPrototype
    {
        public ulong Entity;
        public LootCooldownEntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCooldownEntityPrototype), proto); }
    }

    public class LootCooldownVendorTypePrototype : LootCooldownPrototype
    {
        public ulong VendorType;
        public LootCooldownVendorTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCooldownVendorTypePrototype), proto); }
    }

    public class LootCooldownHierarchyPrototype : Prototype
    {
        public ulong Entity;
        public ulong[] LocksOut;
        public LootCooldownHierarchyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCooldownHierarchyPrototype), proto); }
    }

    public class LootCooldownRolloverTimeEntryPrototype : Prototype
    {
        public float WallClockTime24Hr;
        public WeekdayEnum WallClockTimeDay;
        public LootCooldownRolloverTimeEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCooldownRolloverTimeEntryPrototype), proto); }
    }

    public class LootCooldownChannelPrototype : Prototype
    {
        public LootCooldownChannelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCooldownChannelPrototype), proto); }
    }

    public class LootCooldownChannelRollOverPrototype : LootCooldownChannelPrototype
    {
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries;
        public LootCooldownChannelRollOverPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCooldownChannelRollOverPrototype), proto); }
    }

    public class LootCooldownChannelTimePrototype : LootCooldownChannelPrototype
    {
        public float DurationMinutes;
        public LootCooldownChannelTimePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCooldownChannelTimePrototype), proto); }
    }

    public class LootCooldownChannelCountPrototype : LootCooldownChannelPrototype
    {
        public int MaxDrops;
        public LootCooldownRolloverTimeEntryPrototype[] RolloverTimeEntries;
        public LootCooldownChannelCountPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCooldownChannelCountPrototype), proto); }
    }

    public class LootDropAgentPrototype : LootDropPrototype
    {
        public ulong Agent;
        public LootDropAgentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropAgentPrototype), proto); }
    }

    public class LootDropCharacterTokenPrototype : LootNodePrototype
    {
        public CharacterTokenType AllowedTokenType;
        public CharacterFilterType FilterType;
        public LootNodePrototype OnTokenUnavailable;
        public LootDropCharacterTokenPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropCharacterTokenPrototype), proto); }
    }
    public enum CharacterFilterType
    {
        None = 0,
        DropCurrentAvatarOnly = 1,
        DropUnownedAvatarOnly = 2,
    }

    public class LootDropClonePrototype : LootNodePrototype
    {
        public LootMutationPrototype[] Mutations;
        public short SourceIndex;
        public LootDropClonePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropClonePrototype), proto); }
    }

    public class LootDropCreditsPrototype : LootNodePrototype
    {
        public ulong Type;
        public LootDropCreditsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropCreditsPrototype), proto); }
    }

    public class LootDropItemPrototype : LootDropPrototype
    {
        public ulong Item;
        public LootMutationPrototype[] Mutations;
        public LootDropItemPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropItemPrototype), proto); }
    }

    public class LootDropItemFilterPrototype : LootDropPrototype
    {
        public short ItemRank;
        public EquipmentInvUISlot UISlot;
        public LootDropItemFilterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropItemFilterPrototype), proto); }
    }

    public class LootDropPowerPointsPrototype : LootDropPrototype
    {
        public LootDropPowerPointsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropPowerPointsPrototype), proto); }
    }

    public class LootDropHealthBonusPrototype : LootDropPrototype
    {
        public LootDropHealthBonusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropHealthBonusPrototype), proto); }
    }

    public class LootDropEnduranceBonusPrototype : LootDropPrototype
    {
        public LootDropEnduranceBonusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropEnduranceBonusPrototype), proto); }
    }

    public class LootDropXPPrototype : LootNodePrototype
    {
        public ulong XPCurve;
        public LootDropXPPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropXPPrototype), proto); }
    }

    public class LootDropRealMoneyPrototype : LootDropPrototype
    {
        public ulong CouponCode;
        public ulong TransactionContext;
        public LootDropRealMoneyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropRealMoneyPrototype), proto); }
    }

    public class LootDropBannerMessagePrototype : LootNodePrototype
    {
        public ulong BannerMessage;
        public LootDropBannerMessagePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropBannerMessagePrototype), proto); }
    }

    public class LootDropUsePowerPrototype : LootNodePrototype
    {
        public ulong Power;
        public LootDropUsePowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropUsePowerPrototype), proto); }
    }

    public class LootDropPlayVisualEffectPrototype : LootNodePrototype
    {
        public ulong RecipientVisualEffect;
        public ulong DropperVisualEffect;
        public LootDropPlayVisualEffectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropPlayVisualEffectPrototype), proto); }
    }

    public class LootDropChatMessagePrototype : LootNodePrototype
    {
        public ulong ChatMessage;
        public PlayerScope MessageScope;
        public LootDropChatMessagePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropChatMessagePrototype), proto); }
    }

    public enum PlayerScope
    {
        CurrentRecipientOnly = 0,
        Party = 1,
        Nearby = 2,
        Friends = 3,
        Guild = 4,
    }

    public class LootDropVanityTitlePrototype : LootNodePrototype
    {
        public ulong VanityTitle;
        public LootDropVanityTitlePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropVanityTitlePrototype), proto); }
    }

    public class LootDropVendorXPPrototype : LootNodePrototype
    {
        public ulong Vendor;
        public int XP;
        public LootDropVendorXPPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropVendorXPPrototype), proto); }
    }

    public class LootLocationModifierPrototype : Prototype
    {
        public LootLocationModifierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootLocationModifierPrototype), proto); }
    }

    public class LootSearchRadiusPrototype : LootLocationModifierPrototype
    {
        public float MinRadius;
        public float MaxRadius;
        public LootSearchRadiusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootSearchRadiusPrototype), proto); }
    }

    public class LootBoundsOverridePrototype : LootLocationModifierPrototype
    {
        public float Radius;
        public LootBoundsOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootBoundsOverridePrototype), proto); }
    }

    public class LootLocationOffsetPrototype : LootLocationModifierPrototype
    {
        public float Offset;
        public LootLocationOffsetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootLocationOffsetPrototype), proto); }
    }

    public class DropInPlacePrototype : LootLocationModifierPrototype
    {
        public bool Check;
        public DropInPlacePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DropInPlacePrototype), proto); }
    }

    public class LootLocationNodePrototype : Prototype
    {
        public short Weight;
        public LootLocationModifierPrototype[] Modifiers;
        public LootLocationNodePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootLocationNodePrototype), proto); }
    }

    public class LootLocationTablePrototype : LootLocationNodePrototype
    {
        public LootLocationNodePrototype[] Choices;
        public LootLocationTablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootLocationTablePrototype), proto); }
    }

    #region LootRollModifier

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

    public enum WeekdayEnum
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

    public class LootRollModifierPrototype : Prototype
    {
        public LootRollModifierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollModifierPrototype), proto); }
    }

    public class LootRollClampLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin;
        public int LevelMax;
        public LootRollClampLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollClampLevelPrototype), proto); }
    }

    public class LootRollRequireLevelPrototype : LootRollModifierPrototype
    {
        public int LevelMin;
        public int LevelMax;
        public LootRollRequireLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireLevelPrototype), proto); }
    }

    public class LootRollMarkSpecialPrototype : LootRollModifierPrototype
    {
        public LootRollMarkSpecialPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollMarkSpecialPrototype), proto); }
    }

    public class LootRollUnmarkSpecialPrototype : LootRollModifierPrototype
    {
        public LootRollUnmarkSpecialPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollUnmarkSpecialPrototype), proto); }
    }

    public class LootRollMarkRarePrototype : LootRollModifierPrototype
    {
        public LootRollMarkRarePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollMarkRarePrototype), proto); }
    }

    public class LootRollUnmarkRarePrototype : LootRollModifierPrototype
    {
        public LootRollUnmarkRarePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollUnmarkRarePrototype), proto); }
    }

    public class LootRollOffsetLevelPrototype : LootRollModifierPrototype
    {
        public int LevelOffset;
        public LootRollOffsetLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollOffsetLevelPrototype), proto); }
    }

    public class LootRollOnceDailyPrototype : LootRollModifierPrototype
    {
        public bool PerAccount;
        public LootRollOnceDailyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollOnceDailyPrototype), proto); }
    }

    public class LootRollCooldownOncePerRolloverPrototype : LootRollModifierPrototype
    {
        public bool PerAccount;
        public LootRollCooldownOncePerRolloverPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollCooldownOncePerRolloverPrototype), proto); }
    }

    public class LootRollCooldownByChannelPrototype : LootRollModifierPrototype
    {
        public bool PerAccount;
        public LootRollCooldownByChannelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollCooldownByChannelPrototype), proto); }
    }

    public class LootRollSetAvatarPrototype : LootRollModifierPrototype
    {
        public ulong Avatar;
        public LootRollSetAvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetAvatarPrototype), proto); }
    }

    public class LootRollSetItemLevelPrototype : LootRollModifierPrototype
    {
        public int Level;
        public LootRollSetItemLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetItemLevelPrototype), proto); }
    }

    public class LootRollModifyAffixLimitsPrototype : LootRollModifierPrototype
    {
        public AffixPosition Position;
        public short ModifyMinBy;
        public short ModifyMaxBy;
        public ulong Category;
        public LootRollModifyAffixLimitsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollModifyAffixLimitsPrototype), proto); }
    }

    public class LootRollSetRarityPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices;
        public LootRollSetRarityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetRarityPrototype), proto); }
    }

    public class LootRollSetUsablePrototype : LootRollModifierPrototype
    {
        public float Usable;
        public LootRollSetUsablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetUsablePrototype), proto); }
    }

    public class LootRollUseLevelVerbatimPrototype : LootRollModifierPrototype
    {
        public bool UseLevelVerbatim;
        public LootRollUseLevelVerbatimPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollUseLevelVerbatimPrototype), proto); }
    }

    public class LootRollRequireDifficultyTierPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices;
        public LootRollRequireDifficultyTierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireDifficultyTierPrototype), proto); }
    }

    public class LootRollModifyDropByDifficultyTierPrototype : LootRollModifierPrototype
    {
        public ulong ModifierCurve;
        public LootRollModifyDropByDifficultyTierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollModifyDropByDifficultyTierPrototype), proto); }
    }

    public class LootRollRequireConditionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices;
        public LootRollRequireConditionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireConditionKeywordPrototype), proto); }
    }

    public class LootRollForbidConditionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices;
        public LootRollForbidConditionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollForbidConditionKeywordPrototype), proto); }
    }

    public class LootRollRequireDropperKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices;
        public LootRollRequireDropperKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireDropperKeywordPrototype), proto); }
    }

    public class LootRollForbidDropperKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices;
        public LootRollForbidDropperKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollForbidDropperKeywordPrototype), proto); }
    }

    public class LootRollRequireRegionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices;
        public LootRollRequireRegionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireRegionKeywordPrototype), proto); }
    }

    public class LootRollForbidRegionKeywordPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices;
        public LootRollForbidRegionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollForbidRegionKeywordPrototype), proto); }
    }

    public class LootRollRequireRegionScenarioRarityPrototype : LootRollModifierPrototype
    {
        public ulong[] Choices;
        public LootRollRequireRegionScenarioRarityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireRegionScenarioRarityPrototype), proto); }
    }

    public class LootRollRequireKillCountPrototype : LootRollModifierPrototype
    {
        public int KillsRequired;
        public LootRollRequireKillCountPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireKillCountPrototype), proto); }
    }

    public class LootRollRequireWeekdayPrototype : LootRollModifierPrototype
    {
        public WeekdayEnum[] Choices;
        public LootRollRequireWeekdayPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollRequireWeekdayPrototype), proto); }
    }

    public class LootRollIgnoreCooldownPrototype : LootRollModifierPrototype
    {
        public LootRollIgnoreCooldownPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollIgnoreCooldownPrototype), proto); }
    }

    public class LootRollIgnoreVendorXPCapPrototype : LootRollModifierPrototype
    {
        public LootRollIgnoreVendorXPCapPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollIgnoreVendorXPCapPrototype), proto); }
    }

    public class LootRollSetRegionAffixTablePrototype : LootRollModifierPrototype
    {
        public ulong RegionAffixTable;
        public LootRollSetRegionAffixTablePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollSetRegionAffixTablePrototype), proto); }
    }

    public class LootRollIncludeCurrencyBonusPrototype : LootRollModifierPrototype
    {
        public LootRollIncludeCurrencyBonusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollIncludeCurrencyBonusPrototype), proto); }
    }

    public class LootRollMissionStateRequiredPrototype : LootRollModifierPrototype
    {
        public ulong[] Missions;
        public MissionState RequiredState;
        public LootRollMissionStateRequiredPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootRollMissionStateRequiredPrototype), proto); }
    }

    #endregion

    #region LootMutation

    public class LootMutationPrototype : Prototype
    {
        public LootMutationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutationPrototype), proto); }
    }

    public class LootAddAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords;
        public short Count;
        public AffixPosition Position;
        public AffixCategoryPrototype Categories;
        public LootAddAffixesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootAddAffixesPrototype), proto); }
    }

    public class LootApplyNoVisualsOverridePrototype : LootMutationPrototype
    {
        public LootApplyNoVisualsOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootApplyNoVisualsOverridePrototype), proto); }
    }

    public class LootMutateBindingPrototype : LootMutationPrototype
    {
        public Binding Binding;
        public LootMutateBindingPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutateBindingPrototype), proto); }
    }

    public enum Binding
    {
        None = 0,
        TradeRestricted = 1,
        TradeRestrictedRemoveBinding = 2,
        Avatar = 3,
    }

    public class LootClampLevelPrototype : LootMutationPrototype
    {
        public int MaxLevel;
        public int MinLevel;
        public LootClampLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootClampLevelPrototype), proto); }
    }

    public class LootCloneAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords;
        public int SourceIndex;
        public AffixPosition Position;
        public bool EnforceAffixLimits;
        public AffixCategoryPrototype Categories;
        public LootCloneAffixesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCloneAffixesPrototype), proto); }
    }

    public class LootCloneBuiltinAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords;
        public int SourceIndex;
        public AffixPosition Position;
        public bool EnforceAffixLimits;
        public AffixCategoryPrototype Categories;
        public LootCloneBuiltinAffixesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCloneBuiltinAffixesPrototype), proto); }
    }

    public class LootCloneLevelPrototype : LootMutationPrototype
    {
        public int SourceIndex;
        public LootCloneLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCloneLevelPrototype), proto); }
    }

    public class LootDropAffixesPrototype : LootMutationPrototype
    {
        public ulong[] Keywords;
        public AffixPosition Position;
        public AffixCategoryPrototype Categories;
        public LootDropAffixesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootDropAffixesPrototype), proto); }
    }

    public class LootMutateAffixesPrototype : LootMutationPrototype
    {
        public ulong[] NewItemKeywords;
        public ulong[] OldItemKeywords;
        public bool OnlyReplaceIfAllMatched;
        public LootMutateAffixesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutateAffixesPrototype), proto); }
    }

    public class LootMutateAvatarPrototype : LootMutationPrototype
    {
        public LootMutateAvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutateAvatarPrototype), proto); }
    }

    public class LootMutateLevelPrototype : LootMutationPrototype
    {
        public LootMutateLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutateLevelPrototype), proto); }
    }

    public class OffsetLootLevelPrototype : LootMutationPrototype
    {
        public int LevelOffset;
        public OffsetLootLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OffsetLootLevelPrototype), proto); }
    }

    public class LootMutateRankPrototype : LootMutationPrototype
    {
        public int Rank;
        public LootMutateRankPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutateRankPrototype), proto); }
    }

    public class LootMutateRarityPrototype : LootMutationPrototype
    {
        public bool RerollAffixCount;
        public LootMutateRarityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutateRarityPrototype), proto); }
    }

    public class LootMutateSlotPrototype : LootMutationPrototype
    {
        public EquipmentInvUISlot Slot;
        public LootMutateSlotPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutateSlotPrototype), proto); }
    }

    public class LootMutateBuiltinSeedPrototype : LootMutationPrototype
    {
        public LootMutateBuiltinSeedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutateBuiltinSeedPrototype), proto); }
    }

    public class LootMutateAffixSeedPrototype : LootMutationPrototype
    {
        public ulong[] Keywords;
        public AffixPosition Position;
        public AffixCategoryPrototype Categories;
        public LootMutateAffixSeedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootMutateAffixSeedPrototype), proto); }
    }

    public class LootReplaceAffixesPrototype : LootMutationPrototype
    {
        public int SourceIndex;
        public ulong[] Keywords;
        public AffixPosition Position;
        public bool EnforceAffixLimits;
        public AffixCategoryPrototype Categories;
        public LootReplaceAffixesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootReplaceAffixesPrototype), proto); }
    }

    public class LootCloneSeedPrototype : LootMutationPrototype
    {
        public int SourceIndex;
        public LootCloneSeedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootCloneSeedPrototype), proto); }
    }

    public class LootAddAffixPrototype : LootMutationPrototype
    {
        public ulong Affix;
        public LootAddAffixPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootAddAffixPrototype), proto); }
    }

    public class LootEvalPrototype : LootMutationPrototype
    {
        public EvalPrototype Eval;
        public LootEvalPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootEvalPrototype), proto); }
    }


    #endregion
}
