using Gazillion;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum MusicStateEndBehavior
    {
        DoNothing = 0,
        PlayDefaultList = 1,
        StopMusic = 2,
    }

    [AssetEnum]
    public enum CoopOp
    {
        StartForSlot = 0,
        EndForSlot = 1,
    }

    [AssetEnum]
    public enum CoopOpResult
    {
        Success = 0,
        GenericError = 1,
        PartyFull = 2,
        RegionFull = 3,
        QueueGroupFull = 4,
        SpawnFailed = 5,
        SlotActive = 6,
        AvatarInUse = 7,
        AvatarLocked = 8,
    }

    [AssetEnum]
    public enum ObjectiveTrackerPageType
    {
        SharedQuests = 0,
        EventMissions = 1,
        LegendaryMissions = 2,
        MetaGameMissions = 3,
        OpenMissions = 4,
        StoryMissions = 5,
    }

    [AssetEnum]
    public enum MatchQueueStatus    // Regions/QueueStatus.type
    {
        Invalid = 1,
        SelectQueueMethod = 2,
        RemovedFromGroup = 3,
        RemovedGracePeriod = 4,
        RemovedGracePeriodExpired = 5,
        AddedToGroup = 8,
        GroupInviteAccepted = 9,
        GroupInviteDeclined = 10,
        GroupInviteExpired = 11,
        AddedToQueue = 12,
        AddedToWaitlist = 13,
        MatchLocked = 14,
        PendingMatch = 15,
        AddedToMatch = 16,
        MatchInviteExpired = 18,
        MatchInviteDeclined = 17,
        MovingToInstance = 19,
    }

    [AssetEnum]
    public enum GamepadInput
    {
        A = 0,
        B = 1,
        X = 2,
        Y = 3,
        LeftStick = 4,
        RightStick = 5,
        DPadLeft = 6,
        DPadRight = 7,
        DPadUp = 8,
        DPadDown = 9,
        LeftShoulder = 12,
        RightShoulder = 13,
        LeftTrigger = 10,
        RightTrigger = 11,
        None = 14,
    }

    [AssetEnum]
    public enum DeathReleaseBehavior    // Globals/AvatarDeathReleaseBehavior.type
    {
        ReturnToWaypoint = 0,
        ReturnToCheckpoint = 1,
    }

    [AssetEnum]
    public enum GlobalEventCriteriaLogic
    {
        And = 0,
        Or = 1,
    }

    #endregion

    public class GlobalsPrototype : Prototype
    {
        public ulong AdvancementGlobals { get; set; }
        public ulong AvatarSwapChannelPower { get; set; }
        public ulong ConnectionMarkerPrototype { get; set; }
        public ulong DebugGlobals { get; set; }
        public ulong UIGlobals { get; set; }
        public ulong DefaultPlayer { get; set; }
        public ulong DefaultStartTarget { get; set; }
        public ulong[] PVPAlliances { get; set; }
        public float HighFlyingHeight { get; set; }
        public float LowHealthTrigger { get; set; }
        public float MouseHitCollisionMultiplier { get; set; }
        public float MouseHitMovingTargetsIncrease { get; set; }
        public float MouseHitPowerTargetSearchDist { get; set; }
        public float MouseHitPreferredAddition { get; set; }
        public float MouseMovementNoPathRadius { get; set; }
        public ulong MissionGlobals { get; set; }
        public int TaggingResetDurationMS { get; set; }
        public int PlayerPartyMaxSize { get; set; }
        public float NaviBudgetBaseCellSizeWidth { get; set; }
        public float NaviBudgetBaseCellSizeLength { get; set; }
        public int NaviBudgetBaseCellMaxPoints { get; set; }
        public int NaviBudgetBaseCellMaxEdges { get; set; }
        public ulong[] UIConfigFiles { get; set; }
        public int InteractRange { get; set; }
        public ulong CreditsItemPrototype { get; set; }
        public ulong NegStatusEffectList { get; set; }
        public ulong PvPPrototype { get; set; }
        public ulong MissionPrototype { get; set; }
        public EvalPrototype ItemPriceMultiplierBuyFromVendor { get; set; }
        public EvalPrototype ItemPriceMultiplierSellToVendor { get; set; }
        public ModGlobalsPrototype ModGlobals { get; set; }
        public float MouseMoveDrivePathMaxLengthMult { get; set; }
        public ulong AudioGlobalEventsClass { get; set; }
        public ulong MetaGamePrototype { get; set; }
        public int MobLOSVisUpdatePeriodMS { get; set; }
        public int MobLOSVisStayVisibleDelayMS { get; set; }
        public bool MobLOSVisEnabled { get; set; }
        public ulong BeginPlayAssetTypes { get; set; }
        public ulong CachedAssetTypes { get; set; }
        public ulong FileVerificationAssetTypes { get; set; }
        public ulong LoadingMusic { get; set; }
        public ulong SystemLocalized { get; set; }
        public ulong PopulationGlobals { get; set; }
        public ulong PlayerAlliance { get; set; }
        public ulong ClusterConfigurationGlobals { get; set; }
        public ulong DownloadChunks { get; set; }
        public ulong UIItemInventory { get; set; }
        public ulong AIGlobals { get; set; }
        public ulong MusicAssetType { get; set; }
        public ulong ResurrectionDefaultInfo { get; set; }
        public ulong PartyJoinPortal { get; set; }
        public ulong MatchJoinPortal { get; set; }
        public ulong MovieAssetType { get; set; }
        public ulong WaypointGraph { get; set; }
        public ulong WaypointHotspot { get; set; }
        public float MouseHoldDeadZoneRadius { get; set; }
        public GlobalPropertiesPrototype Properties { get; set; }
        public int PlayerGracePeroidInSeconds { get; set; }
        public ulong CheckpointHotspot { get; set; }
        public ulong ReturnToHubPower { get; set; }
        public int DisableEndurRegenOnPowerEndMS { get; set; }
        public ulong PowerPrototype { get; set; }
        public ulong WorldEntityPrototype { get; set; }
        public ulong AreaPrototype { get; set; }
        public ulong PopulationObjectPrototype { get; set; }
        public ulong RegionPrototype { get; set; }
        public ulong AmbientSfxType { get; set; }
        public ulong CombatGlobals { get; set; }
        public float OrientForPowerMaxTimeSecs { get; set; }
        public ulong KismetSequenceEntityPrototype { get; set; }
        public ulong DynamicArea { get; set; }
        public ulong ReturnToFieldPower { get; set; }
        public float AssetCacheCellLoadOutRunSeconds { get; set; }
        public int AssetCacheMRUSize { get; set; }
        public int AssetCachePrefetchMRUSize { get; set; }
        public ulong AvatarSwapInPower { get; set; }
        public ulong PlayerStartingFaction { get; set; }
        public ulong VendorBuybackInventory { get; set; }
        public ulong AnyAlliancePrototype { get; set; }
        public ulong AnyFriendlyAlliancePrototype { get; set; }
        public ulong AnyHostileAlliancePrototype { get; set; }
        public ulong ExperienceBonusCurve { get; set; }
        public ulong TransitionGlobals { get; set; }
        public int PlayerGuildMaxSize { get; set; }
        public bool AutoPartyEnabledInitially { get; set; }
        public ulong ItemBindingAffix { get; set; }
        public int InteractFallbackRange { get; set; }
        public ulong ItemAcquiredThroughMTXStoreAffix { get; set; }
        public ulong TeleportToPartyMemberPower { get; set; }
        public ulong AvatarSwapOutPower { get; set; }
        public int KickIdlePlayerTimeSecs { get; set; }
        public ulong PlayerCameraSettings { get; set; }
        public ulong AvatarSynergyCondition { get; set; }
        public ulong MetaGameLocalized { get; set; }
        public ulong MetaGameTeamDefault { get; set; }
        public ulong ItemNoVisualsAffix { get; set; }
        public int AvatarSynergyConcurrentLimit { get; set; }
        public ulong LootGlobals { get; set; }
        public ulong MetaGameTeamBase { get; set; }
        public ulong AudioGlobals { get; set; }
        public int PlayerRaidMaxSize { get; set; }
        public int TimeZone { get; set; }
        public ulong TeamUpSummonPower { get; set; }
        public int AssistPvPDurationMS { get; set; }
        public ulong FulfillmentReceiptPrototype { get; set; }
        public ulong PetTechVacuumPower { get; set; }
        public ulong StolenPowerRestrictions { get; set; }
        public ulong PowerVisualsGlobals { get; set; }
        public ulong KeywordGlobals { get; set; }
        public ulong CurrencyGlobals { get; set; }
        public ulong PointerArrowTemplate { get; set; }
        public ulong ObjectiveMarkerTemplate { get; set; }
        public int VaporizedLootLifespanMS { get; set; }
        public ulong CookedIconAssetTypes { get; set; }
        public ulong LiveTuneAvatarXPDisplayCondition { get; set; }
        public ulong LiveTuneCreditsDisplayCondition { get; set; }
        public ulong LiveTuneRegionXPDisplayCondition { get; set; }
        public ulong LiveTuneRIFDisplayCondition { get; set; }
        public ulong LiveTuneSIFDisplayCondition { get; set; }
        public ulong LiveTuneXPDisplayCondition { get; set; }
        public ulong ItemLinkInventory { get; set; }
        public ulong LimitedEditionBlueprint { get; set; }
        public ulong MobileIconAssetTypes { get; set; }
        public ulong PetItemBlueprint { get; set; }
        public ulong AvatarPrototype { get; set; }
        public int ServerBonusUnlockLevel { get; set; }
        public ulong GamepadGlobals { get; set; }
        public ulong CraftingRecipeLibraryInventory { get; set; }
        public ulong ConditionPrototype { get; set; }
        public ulong[] LiveTuneServerConditions { get; set; }
        public ulong DefaultStartingAvatarPrototype { get; set; }
        public ulong DefaultStartTargetFallbackRegion { get; set; }
        public ulong DefaultStartTargetPrestigeRegion { get; set; }
        public ulong DefaultStartTargetStartingRegion { get; set; }
        public ulong DifficultyGlobals { get; set; }
        public ulong PublicEventPrototype { get; set; }
        public ulong AvatarCoopStartPower { get; set; }
        public ulong AvatarCoopEndPower { get; set; }
        public DifficultyTierPrototype DifficultyTiers { get; set; }
        public ulong DefaultLoadingLobbyRegion { get; set; }
        public ulong DifficultyTierDefault { get; set; }
        public ulong AvatarHealPower { get; set; }
        public ulong ConsoleGlobals { get; set; }
        public ulong TeamUpSynergyCondition { get; set; }
        public ulong MetricsFrequencyPrototype { get; set; }
        public ulong ConsumableItemBlueprint { get; set; }
        public int AvatarCoopInactiveTimeMS { get; set; }
        public int AvatarCoopInactiveOnDeadBufferMS { get; set; }
    }

    public class LoginRewardPrototype : Prototype
    {
        public int Day { get; set; }
        public ulong Item { get; set; }
        public ulong TooltipText { get; set; }
        public ulong LogoffPanelEntry { get; set; }
    }

    public class PrestigeLevelPrototype : Prototype
    {
        public ulong TextStyle { get; set; }
        public ulong Reward { get; set; }
    }

    public class PetTechAffixInfoPrototype : Prototype
    {
        public AffixPosition Position { get; set; }
        public ulong ItemRarityToConsume { get; set; }
        public int ItemsRequiredToUnlock { get; set; }
        public ulong LockedDescriptionText { get; set; }
    }

    public class AdvancementGlobalsPrototype : Prototype
    {
        public ulong LevelingCurve { get; set; }
        public ulong DeathPenaltyCost { get; set; }
        public ulong ItemEquipRequirementOffset { get; set; }
        public ulong VendorLevelingCurve { get; set; }
        public ulong StatsEval { get; set; }
        public ulong AvatarThrowabilityEval { get; set; }
        public EvalPrototype VendorLevelingEval { get; set; }
        public EvalPrototype VendorRollTableLevelEval { get; set; }
        public float RestedHealthPerMinMult { get; set; }
        public int PowerBoostMax { get; set; }
        public PrestigeLevelPrototype PrestigeLevels { get; set; }
        public ulong ItemAffixLevelingCurve { get; set; }
        public ulong ExperienceBonusAvatarSynergy { get; set; }
        public float ExperienceBonusAvatarSynergyMax { get; set; }
        public int OriginalMaxLevel { get; set; }
        public ulong ExperienceBonusLevel60Synergy { get; set; }
        public int TeamUpPowersPerTier { get; set; }
        public ulong TeamUpPowerTiersCurve { get; set; }
        public OmegaBonusSetPrototype OmegaBonusSets { get; set; }
        public int OmegaPointsCap { get; set; }
        public int OmegaSystemLevelUnlock { get; set; }
        public PetTechAffixInfoPrototype[] PetTechAffixInfo { get; set; }
        public ulong PetTechDonationItemPrototype { get; set; }
        public int AvatarPowerSpecsMax { get; set; }
        public ulong PctXPFromPrestigeLevelCurve { get; set; }
        public int StarterAvatarLevelCap { get; set; }
        public ulong TeamUpLevelingCurve { get; set; }
        public int TeamUpPowerSpecsMax { get; set; }
        public ulong PctXPFromLevelDeltaCurve { get; set; }
        public int InfinitySystemUnlockLevel { get; set; }
        public long InfinityPointsCapPerGem { get; set; }
        public InfinityGemSetPrototype InfinityGemSets { get; set; }
        public long InfinityXPCap { get; set; }
        public int TravelPowerUnlockLevel { get; set; }
        public float ExperienceBonusCoop { get; set; }
        public ulong CoopInactivityExperienceScalar { get; set; }
    }

    public class AIGlobalsPrototype : Prototype
    {
        public ulong LeashReturnHeal { get; set; }
        public ulong LeashReturnImmunity { get; set; }
        public ulong LeashingProceduralProfile { get; set; }
        public int RandomThinkVarianceMS { get; set; }
        public int ControlledAgentResurrectTimerMS { get; set; }
        public ulong ControlledAlliance { get; set; }
        public float OrbAggroRangeMax { get; set; }
        public ulong OrbAggroRangeBonusCurve { get; set; }
        public ulong DefaultSimpleNpcBrain { get; set; }
        public ulong CantBeControlledKeyword { get; set; }
        public int ControlledAgentSummonDurationMS { get; set; }
    }

    public class MusicStatePrototype : Prototype
    {
        public ulong StateGroupName { get; set; }
        public ulong StateName { get; set; }
        public MusicStateEndBehavior EndBehavior { get; set; }
    }

    public class AudioGlobalsPrototype : Prototype
    {
        public int DefaultMemPoolSizeMB { get; set; }
        public int LowerEngineMemPoolSizeMB { get; set; }
        public int StreamingMemPoolSizeMB { get; set; }
        public int MemoryBudgetMB { get; set; }
        public int MemoryBudgetDevMB { get; set; }
        public float MaxAnimNotifyRadius { get; set; }
        public float MaxMocoVolumeDB { get; set; }
        public float FootstepNotifyMinFpsThreshold { get; set; }
        public float BossCritBanterHealthPctThreshold { get; set; }
        public int LongDownTimeInDaysThreshold { get; set; }
        public float EncounterCheckRadius { get; set; }
    }

    public class DebugGlobalsPrototype : Prototype
    {
        public ulong CreateEntityShortcutEntity { get; set; }
        public ulong DynamicRegion { get; set; }
        public float HardModeMobDmgBuff { get; set; }
        public float HardModeMobHealthBuff { get; set; }
        public float HardModeMobMoveSpdBuff { get; set; }
        public float HardModePlayerEnduranceCostDebuff { get; set; }
        public ulong PowersArtModeEntity { get; set; }
        public int StartingLevelMobs { get; set; }
        public ulong TransitionRef { get; set; }
        public ulong CreateLootDummyEntity { get; set; }
        public ulong MapErrorMapInfo { get; set; }
        public bool IgnoreDeathPenalty { get; set; }
        public bool TrashedItemsDropInWorld { get; set; }
        public ulong PAMEnemyAlliance { get; set; }
        public EvalPrototype DebugEval { get; set; }
        public EvalPrototype DebugEvalUnitTest { get; set; }
        public BotSettingsPrototype BotSettings { get; set; }
        public ulong ReplacementTestingResultItem { get; set; }
        public ulong ReplacementTestingTriggerItem { get; set; }
        public ulong VendorEternitySplinterLoot { get; set; }
    }

    public class CharacterSheetDetailedStatPrototype : Prototype
    {
        public EvalPrototype Expression { get; set; }
        public ulong ExpressionExt { get; set; }
        public ulong Format { get; set; }
        public ulong Label { get; set; }
        public ulong Tooltip { get; set; }
        public ulong Icon { get; set; }
    }

    public class HelpGameTermPrototype : Prototype
    {
        public ulong Name { get; set; }
        public ulong Description { get; set; }
    }

    public class CoopOpUIDataEntryPrototype : Prototype
    {
        public CoopOp Op { get; set; }
        public CoopOpResult Result { get; set; }
        public ulong SystemMessage { get; set; }
        public ulong SystemMessageTemplate { get; set; }
        public ulong BannerMessage { get; set; }
    }

    public class HelpTextPrototype : Prototype
    {
        public ulong GeneralControls { get; set; }
        public HelpGameTermPrototype[] GameTerms { get; set; }
        public ulong Crafting { get; set; }
        public ulong EndgamePvE { get; set; }
        public ulong PvP { get; set; }
        public ulong Tutorial { get; set; }
    }

    public class AffixRollQualityPrototype : Prototype
    {
        public TextStylePrototype Style { get; set; }
        public float PercentThreshold { get; set; }
    }

    public class UIGlobalsPrototype : Prototype
    {
        public ulong MessageDefault { get; set; }
        public ulong MessageLevelUp { get; set; }
        public ulong MessageItemError { get; set; }
        public ulong MessageRegionChange { get; set; }
        public ulong MessageMissionAccepted { get; set; }
        public ulong MessageMissionCompleted { get; set; }
        public ulong MessageMissionFailed { get; set; }
        public int AvatarSwitchUIDeathDelayMS { get; set; }
        public ulong UINotificationGlobals { get; set; }
        public int RosterPageSize { get; set; }
        public ulong LocalizedInfoDirectory { get; set; }
        public int TooltipHideDelayMS { get; set; }
        public ulong MessagePowerError { get; set; }
        public ulong MessageWaypointError { get; set; }
        public ulong UIStringGlobals { get; set; }
        public ulong MessagePartyInvite { get; set; }
        public ulong MapInfoMissionGiver { get; set; }
        public ulong MapInfoMissionObjectiveTalk { get; set; }
        public int NumAvatarsToDisplayInItemUsableLists { get; set; }
        public ulong LoadingScreens { get; set; }
        public int ChatFadeInMS { get; set; }
        public int ChatBeginFadeOutMS { get; set; }
        public int ChatFadeOutMS { get; set; }
        public ulong MessageWaypointUnlocked { get; set; }
        public ulong MessagePowerUnlocked { get; set; }
        public ulong UIMapGlobals { get; set; }
        public ulong TextStyleCurrentlyEquipped { get; set; }
        public int ChatTextFadeOutMS { get; set; }
        public int ChatTextHistoryMax { get; set; }
        public ulong KeywordFemale { get; set; }
        public ulong KeywordMale { get; set; }
        public ulong TextStylePowerUpgradeImprovement { get; set; }
        public ulong TextStylePowerUpgradeNoImprovement { get; set; }
        public ulong LoadingScreenIntraRegion { get; set; }
        public ulong TextStyleVendorPriceCanBuy { get; set; }
        public ulong TextStyleVendorPriceCantBuy { get; set; }
        public ulong TextStyleItemRestrictionFailure { get; set; }
        public int CostumeClosetNumAvatarsVisible { get; set; }
        public int CostumeClosetNumCostumesVisible { get; set; }
        public ulong MessagePowerErrorDoNotQueue { get; set; }
        public ulong TextStylePvPShopPurchased { get; set; }
        public ulong TextStylePvPShopUnpurchased { get; set; }
        public ulong MessagePowerPointsAwarded { get; set; }
        public ulong MapInfoMissionObjectiveUse { get; set; }
        public ulong TextStyleMissionRewardFloaty { get; set; }
        public ulong PowerTooltipBodyCurRank0Unlkd { get; set; }
        public ulong PowerTooltipBodyCurRankLocked { get; set; }
        public ulong PowerTooltipBodyCurRank1AndUp { get; set; }
        public ulong PowerTooltipBodyNextRank1First { get; set; }
        public ulong PowerTooltipBodyNextRank2AndUp { get; set; }
        public ulong PowerTooltipHeader { get; set; }
        public ulong MapInfoFlavorNPC { get; set; }
        public int TooltipSpawnHideDelayMS { get; set; }
        public int KioskIdleResetTimeSec { get; set; }
        public ulong KioskSizzleMovie { get; set; }
        public int KioskSizzleMovieStartTimeSec { get; set; }
        public ulong MapInfoHealer { get; set; }
        public ulong TextStyleOpenMission { get; set; }
        public ulong MapInfoPartyMember { get; set; }
        public int LoadingScreenTipTimeIntervalMS { get; set; }
        public ulong TextStyleKillRewardFloaty { get; set; }
        public ulong TextStyleAvatarOverheadNormal { get; set; }
        public ulong TextStyleAvatarOverheadParty { get; set; }
        public CharacterSheetDetailedStatPrototype[] CharacterSheetDetailedStats { get; set; }
        public ulong PowerProgTableTabRefTab1 { get; set; }
        public ulong PowerProgTableTabRefTab2 { get; set; }
        public ulong PowerProgTableTabRefTab3 { get; set; }
        public float ScreenEdgeArrowRange { get; set; }
        public ulong HelpText { get; set; }
        public ulong MessagePvPFactionPortalFail { get; set; }
        public ulong PropertyTooltipTextOverride { get; set; }
        public ulong MessagePvPDisabledPortalFail { get; set; }
        public ulong MessageStatProgression { get; set; }
        public ulong MessagePvPPartyPortalFail { get; set; }
        public ulong TextStyleMissionHudOpenMission { get; set; }
        public ulong MapInfoAvatarDefeated { get; set; }
        public ulong MapInfoPartyMemberDefeated { get; set; }
        public ulong MessageGuildInvite { get; set; }
        public ulong MapInfoMissionObjectiveMob { get; set; }
        public ulong MapInfoMissionObjectivePortal { get; set; }
        public ulong CinematicsListLoginScreen { get; set; }
        public ulong TextStyleGuildLeader { get; set; }
        public ulong TextStyleGuildOfficer { get; set; }
        public ulong TextStyleGuildMember { get; set; }
        public AffixDisplaySlotPrototype[] CostumeAffixDisplaySlots { get; set; }
        public ulong MessagePartyError { get; set; }
        public ulong MessageRegionRestricted { get; set; }
        public ulong CreditsMovies { get; set; }
        public ulong MessageMetaGameDefault { get; set; }
        public ulong MessagePartyPvPPortalFail { get; set; }
        public int ChatNewMsgDarkenBgMS { get; set; }
        public ulong TextStyleKillZeroRewardFloaty { get; set; }
        public ulong MessageAvatarSwitchError { get; set; }
        public ulong TextStyleItemBlessed { get; set; }
        public ulong TextStyleItemAffixLocked { get; set; }
        public ulong MessageAlreadyInQueue { get; set; }
        public ulong MessageOnlyPartyLeaderCanQueue { get; set; }
        public ulong MessageTeleportTargetIsInMatch { get; set; }
        public ulong PowerGrantItemTutorialTip { get; set; }
        public ulong MessagePrivateDisallowedInRaid { get; set; }
        public ulong MessageQueueNotAvailableInRaid { get; set; }
        public ulong PowerTooltipBodyNextRank1Antireq { get; set; }
        public ulong CosmicEquippedTutorialTip { get; set; }
        public ulong MessageRegionDisabledPortalFail { get; set; }
        public CharacterSheetDetailedStatPrototype[] TeamUpDetailedStats { get; set; }
        public ulong MessageOmegaPointsAwarded { get; set; }
        public ulong MetaGameWidgetMissionName { get; set; }
        public UIConditionType[] BuffPageOrder { get; set; }
        public ObjectiveTrackerPageType[] ObjectiveTrackerPageOrder { get; set; }
        public ulong VanityTitleNoTitle { get; set; }
        public ulong MessageStealablePowerOccupied { get; set; }
        public ulong MessageStolenPowerDuplicate { get; set; }
        public ulong CurrencyDisplayList { get; set; }
        public ulong CinematicOpener { get; set; }
        public ulong MessageCantQueueInQueueRegion { get; set; }
        public int LogoffPanelStoryMissionLevelCap { get; set; }
        public StoreCategoryPrototype[] MTXStoreCategories { get; set; }
        public int GiftingAccessMinPlayerLevel { get; set; }
        public ulong AffixRollRangeTooltipText { get; set; }
        public ulong[] UISystemLockList { get; set; }
        public ulong MessageUISystemUnlocked { get; set; }
        public ulong TooltipInsigniaTeamAffiliations { get; set; }
        public ulong PowerTooltipBodySpecLocked { get; set; }
        public ulong PowerTooltipBodySpecUnlocked { get; set; }
        public ulong PropertyValuePercentFormat { get; set; }
        public ulong AffixStatDiffPositiveStyle { get; set; }
        public ulong AffixStatDiffNegativeStyle { get; set; }
        public ulong AffixStatDiffTooltipText { get; set; }
        public ulong AffixStatDiffNeutralStyle { get; set; }
        public ulong AffixStatFoundAffixStyle { get; set; }
        public ulong[] StashTabCustomIcons { get; set; }
        public ulong PropertyValueDefaultFormat { get; set; }
        public ulong[] ItemSortCategoryList { get; set; }
        public ulong[] ItemSortSubCategoryList { get; set; }
        public AffixRollQualityPrototype[] AffixRollRangeRollQuality { get; set; }
        public ulong RadialMenuEntriesList { get; set; }
        public ulong TextStylePowerChargesEmpty { get; set; }
        public ulong TextStylePowerChargesFull { get; set; }
        public ulong MessageLeaderboardRewarded { get; set; }
        public ulong GamepadIconDonateAction { get; set; }
        public ulong GamepadIconDropAction { get; set; }
        public ulong GamepadIconEquipAction { get; set; }
        public ulong GamepadIconMoveAction { get; set; }
        public ulong GamepadIconSelectAction { get; set; }
        public ulong GamepadIconSellAction { get; set; }
        public ulong ConsoleRadialMenuEntriesList { get; set; }
        public CoopOpUIDataEntryPrototype[] CoopOpUIDatas { get; set; }
        public ulong MessageOpenMissionEntered { get; set; }
        public ulong MessageInfinityPointsAwarded { get; set; }
        public ulong PowerTooltipBodyTalentLocked { get; set; }
        public ulong PowerTooltipBodyTalentUnlocked { get; set; }
        public ulong[] AffixTooltipOrder { get; set; }
        public ulong PowerTooltipBodyCurRank1Only { get; set; }
        public int InfinityMaxRanksHideThreshold { get; set; }
        public ulong MessagePlayingAtLevelCap { get; set; }
        public ulong GamepadIconRankDownAction { get; set; }
        public ulong GamepadIconRankUpAction { get; set; }
        public ulong MessageTeamUpDisabledCoop { get; set; }
        public ulong MessageStolenPowerAvailable { get; set; }
        public ulong BIFRewardMessage { get; set; }
        public ulong PowerTooltipBodyTeamUpLocked { get; set; }
        public ulong PowerTooltipBodyTeamUpUnlocked { get; set; }
        public int InfinityNotificationThreshold { get; set; }
        public ulong HelpTextConsole { get; set; }
        public ulong MessageRegionNotDownloaded { get; set; }
    }

    public class UINotificationGlobalsPrototype : Prototype
    {
        public ulong NotificationPartyInvite { get; set; }
        public ulong NotificationLevelUp { get; set; }
        public ulong NotificationServerMessage { get; set; }
        public ulong NotificationRemoteMission { get; set; }
        public ulong NotificationMissionUpdate { get; set; }
        public ulong NotificationMatchInvite { get; set; }
        public ulong NotificationMatchQueue { get; set; }
        public ulong NotificationMatchGroupInvite { get; set; }
        public ulong NotificationPvPShop { get; set; }
        public ulong NotificationPowerPointsAwarded { get; set; }
        public int NotificationPartyAIAggroRange { get; set; }
        public ulong NotificationOfferingUI { get; set; }
        public ulong NotificationGuildInvite { get; set; }
        public ulong NotificationMetaGameInfo { get; set; }
        public ulong NotificationLegendaryMission { get; set; }
        public ulong NotificationMatchPending { get; set; }
        public ulong NotificationMatchGroupPending { get; set; }
        public ulong NotificationMatchWaitlisted { get; set; }
        public ulong NotificationLegendaryQuestShare { get; set; }
        public ulong NotificationSynergyPoints { get; set; }
        public ulong NotificationPvPScoreboard { get; set; }
        public ulong NotificationOmegaPoints { get; set; }
        public ulong NotificationTradeInvite { get; set; }
        public ulong NotificationMatchLocked { get; set; }
        public ulong NotificationLoginReward { get; set; }
        public ulong NotificationMatchGracePeriod { get; set; }
        public ulong NotificationPartyKickGracePeriod { get; set; }
        public ulong NotificationGiftReceived { get; set; }
        public ulong NotificationLeaderboardRewarded { get; set; }
        public ulong NotificationCouponReceived { get; set; }
        public ulong NotificationPublicEvent { get; set; }
    }

    public class UIMapGlobalsPrototype : Prototype
    {
        public float DefaultRevealRadius { get; set; }
        public float DefaultZoom { get; set; }
        public float FullScreenMapAlphaMax { get; set; }
        public float FullScreenMapAlphaMin { get; set; }
        public int FullScreenMapResolutionHeight { get; set; }
        public int FullScreenMapResolutionWidth { get; set; }
        public float FullScreenMapScale { get; set; }
        public float LowResRevealMultiplier { get; set; }
        public ulong MapColorFiller { get; set; }
        public ulong MapColorWalkable { get; set; }
        public ulong MapColorWall { get; set; }
        public float MiniMapAlpha { get; set; }
        public int MiniMapResolution { get; set; }
        public float CameraAngleX { get; set; }
        public float CameraAngleY { get; set; }
        public float CameraAngleZ { get; set; }
        public float CameraFOV { get; set; }
        public float CameraNearPlane { get; set; }
        public float FullScreenMapPOISize { get; set; }
        public float POIScreenFacingRot { get; set; }
        public bool DrawPOIInCanvas { get; set; }
        public bool EnableMinimapProjection { get; set; }
        public float DefaultZoomMin { get; set; }
        public float DefaultZoomMax { get; set; }
        public float MiniMapPOISizeMin { get; set; }
        public float MiniMapPOISizeMax { get; set; }
        public ulong MapColorFillerConsole { get; set; }
        public ulong MapColorWalkableConsole { get; set; }
        public ulong MapColorWallConsole { get; set; }
        public float DefaultZoomConsole { get; set; }
        public float DefaultZoomMinConsole { get; set; }
        public float DefaultZoomMaxConsole { get; set; }
        public float MiniMapPOISizeMinConsole { get; set; }
        public float MiniMapPOISizeMaxConsole { get; set; }
        public float MiniMapAlphaConsole { get; set; }
    }

    public class MetricsFrequencyPrototype : Prototype
    {
        public float SampleRate { get; set; }
    }

    public class CameraSettingPrototype : Prototype
    {
        public float DirectionX { get; set; }
        public float DirectionY { get; set; }
        public float DirectionZ { get; set; }
        public float Distance { get; set; }
        public float FieldOfView { get; set; }
        public float ListenerDistance { get; set; }
        public int RotationPitch { get; set; }
        public int RotationRoll { get; set; }
        public int RotationYaw { get; set; }
        public float LookAtOffsetX { get; set; }
        public float LookAtOffsetY { get; set; }
        public float LookAtOffsetZ { get; set; }
        public bool AllowCharacterSpecificZOffset { get; set; }
        public bool OrbitalCam { get; set; }
        public float OrbitalFocusAngle { get; set; }
        public float OrbitalFocusPosX { get; set; }
        public float OrbitalFocusPosY { get; set; }
    }

    public class CameraSettingCollectionPrototype : Prototype
    {
        public CameraSettingPrototype[] CameraSettings { get; set; }
        public CameraSettingPrototype[] CameraSettingsFlying { get; set; }
        public int CameraStartingIndex { get; set; }
        public bool CameraAllowCustomMaxZoom { get; set; }
    }

    public class GlobalPropertiesPrototype : Prototype
    {
        public ulong Properties { get; set; }
    }

    public class PowerVisualsGlobalsPrototype : Prototype
    {
        public ulong DailyMissionCompleteClass { get; set; }
        public ulong UnlockPetTechR1CommonClass { get; set; }
        public ulong UnlockPetTechR2UncommonClass { get; set; }
        public ulong UnlockPetTechR3RareClass { get; set; }
        public ulong UnlockPetTechR4EpicClass { get; set; }
        public ulong UnlockPetTechR5CosmicClass { get; set; }
        public ulong LootVaporizedClass { get; set; }
        public ulong AchievementUnlockedClass { get; set; }
        public ulong OmegaPointGainedClass { get; set; }
        public ulong AvatarLeashTeleportClass { get; set; }
        public ulong InfinityTimePointEarnedClass { get; set; }
        public ulong InfinitySpacePointEarnedClass { get; set; }
        public ulong InfinitySoulPointEarnedClass { get; set; }
        public ulong InfinityMindPointEarnedClass { get; set; }
        public ulong InfinityRealityPointEarnedClass { get; set; }
        public ulong InfinityPowerPointEarnedClass { get; set; }
    }

    public class RankDefaultEntryPrototype : Prototype
    {
        public ulong Data { get; set; }
        public Rank Rank { get; set; }
    }

    public class PopulationGlobalsPrototype : Prototype
    {
        public ulong MessageEnemiesGrowStronger { get; set; }
        public ulong MessageEnemiesGrowWeaker { get; set; }
        public int SpawnMapPoolTickMS { get; set; }
        public int SpawnMapLevelTickMS { get; set; }
        public float CrowdSupressionRadius { get; set; }
        public bool SupressSpawnOnPlayer { get; set; }
        public int SpawnMapGimbalRadius { get; set; }
        public int SpawnMapHorizon { get; set; }
        public float SpawnMapMaxChance { get; set; }
        public ulong EmptyPopulation { get; set; }
        public ulong TwinEnemyBoost { get; set; }
        public int DestructiblesForceSpawnMS { get; set; }
        public ulong TwinEnemyCondition { get; set; }
        public int SpawnMapHeatPerSecondMax { get; set; }
        public int SpawnMapHeatPerSecondMin { get; set; }
        public int SpawnMapHeatPerSecondScalar { get; set; }
        public ulong TwinEnemyRank { get; set; }
        public RankDefaultEntryPrototype[] RankDefaults { get; set; }
    }

    public class ClusterConfigurationGlobalsPrototype : Prototype
    {
        public int MinutesToKeepOfflinePlayerGames { get; set; }
        public int MinutesToKeepUnusedRegions { get; set; }
        public bool HotspotCheckLOSInTown { get; set; }
        public int HotspotCheckTargetIntervalMS { get; set; }
        public int HotspotCheckTargetTownIntervalMS { get; set; }
        public int PartyKickGracePeriodMS { get; set; }
        public int QueueReservationGracePeriodMS { get; set; }
    }

    public class CombatGlobalsPrototype : Prototype
    {
        public float PowerDmgBonusHardcoreAttenuation { get; set; }
        public int MouseHoldStartMoveDelayMeleeMS { get; set; }
        public int MouseHoldStartMoveDelayRangedMS { get; set; }
        public float CriticalForceApplicationChance { get; set; }
        public float CriticalForceApplicationMag { get; set; }
        public float EnduranceCostChangePctMin { get; set; }
        public EvalPrototype EvalBlockChanceFormula { get; set; }
        public ulong EvalInterruptChanceFormula { get; set; }
        public ulong EvalNegStatusResistPctFormula { get; set; }
        public ulong ChannelInterruptCondition { get; set; }
        public EvalPrototype EvalDamageReduction { get; set; }
        public EvalPrototype EvalCritChanceFormula { get; set; }
        public EvalPrototype EvalSuperCritChanceFormula { get; set; }
        public EvalPrototype EvalDamageRatingFormula { get; set; }
        public EvalPrototype EvalCritDamageRatingFormula { get; set; }
        public EvalPrototype EvalDodgeChanceFormula { get; set; }
        public EvalPrototype EvalDamageReductionDefenseOnly { get; set; }
        public EvalPrototype EvalDamageReductionForDisplay { get; set; }
        public float TravelPowerMaxSpeed { get; set; }
        public ulong TUSynergyBonusPerLvl { get; set; }
        public ulong TUSynergyBonusPerMaxLvlTU { get; set; }
    }

    public class VendorXPCapInfoPrototype : Prototype
    {
        public ulong Vendor { get; set; }
        public int Cap { get; set; }
        public float WallClockTime24Hr { get; set; }
        public Weekday WallClockTimeDay { get; set; }
    }

    public class AffixCategoryTableEntryPrototype : Prototype
    {
        public ulong Category { get; set; }
        public ulong[] Affixes { get; set; }
    }

    public class LootGlobalsPrototype : Prototype
    {
        public ulong LootBonusRarityCurve { get; set; }
        public ulong LootBonusSpecialCurve { get; set; }
        public ulong LootContainerKeyword { get; set; }
        public float LootDropScalar { get; set; }
        public int LootInitializationLevelOffset { get; set; }
        public ulong LootLevelDistribution { get; set; }
        public float LootRarityScalar { get; set; }
        public float LootSpecialItemFindScalar { get; set; }
        public float LootUnrestedSpecialFindScalar { get; set; }
        public float LootUsableByRecipientPercent { get; set; }
        public ulong NoLootTable { get; set; }
        public ulong SpecialOnKilledLootTable { get; set; }
        public int SpecialOnKilledLootCooldownHours { get; set; }
        public ulong RarityCosmic { get; set; }
        public ulong LootBonusFlatCreditsCurve { get; set; }
        public ulong RarityUruForged { get; set; }
        public ulong LootTableBlueprint { get; set; }
        public ulong RarityUnique { get; set; }
        public int LootLevelMaxForDrops { get; set; }
        public ulong InsigniaBlueprint { get; set; }
        public ulong UniquesBoxCheatItem { get; set; }
        public ulong[] EmptySocketAffixes { get; set; }
        public ulong GemBlueprint { get; set; }
        public VendorXPCapInfoPrototype[] VendorXPCapInfo { get; set; }
        public float DropDistanceThreshold { get; set; }
        public AffixCategoryTableEntryPrototype[] AffixCategoryTable { get; set; }
        public ulong BonusItemFindCurve { get; set; }
        public int BonusItemFindNumPointsForBonus { get; set; }
        public ulong BonusItemFindLootTable { get; set; }
        public float LootCoopPlayerRewardPct { get; set; }
        public ulong RarityDefault { get; set; }
    }

    public class MatchQueueStringEntryPrototype : Prototype
    {
        public RegionRequestQueueUpdateVar StatusKey { get; set; }  // Regions/QueueStatus.type, also appears in protocol
        public ulong StringLog { get; set; }
        public ulong StringStatus { get; set; }
    }

    public class TransitionGlobalsPrototype : Prototype
    {
        public RegionPortalControlEntryPrototype[] ControlledRegions { get; set; }
        public ulong EnabledState { get; set; }
        public ulong DisabledState { get; set; }
        public MatchQueueStringEntryPrototype[] QueueStrings { get; set; }
        public ulong TransitionEmptyClass { get; set; }
    }

    public class KeywordGlobalsPrototype : Prototype
    {
        public ulong PowerKeywordPrototype { get; set; }
        public ulong DestructibleKeyword { get; set; }
        public ulong PetPowerKeyword { get; set; }
        public ulong VacuumableKeyword { get; set; }
        public ulong EntityKeywordPrototype { get; set; }
        public ulong BodysliderPowerKeyword { get; set; }
        public ulong OrbEntityKeyword { get; set; }
        public ulong UltimatePowerKeyword { get; set; }
        public ulong MeleePowerKeyword { get; set; }
        public ulong RangedPowerKeyword { get; set; }
        public ulong BasicPowerKeyword { get; set; }
        public ulong TeamUpSpecialPowerKeyword { get; set; }
        public ulong TeamUpDefaultPowerKeyword { get; set; }
        public ulong StealthPowerKeyword { get; set; }
        public ulong TeamUpAwayPowerKeyword { get; set; }
        public ulong VanityPetKeyword { get; set; }
        public ulong ControlPowerKeywordPrototype { get; set; }
        public ulong AreaPowerKeyword { get; set; }
        public ulong EnergyPowerKeyword { get; set; }
        public ulong MentalPowerKeyword { get; set; }
        public ulong PhysicalPowerKeyword { get; set; }
        public ulong MedKitKeyword { get; set; }
        public ulong OrbExperienceEntityKeyword { get; set; }
        public ulong TutorialRegionKeyword { get; set; }
        public ulong TeamUpKeyword { get; set; }
        public ulong MovementPowerKeyword { get; set; }
        public ulong ThrownPowerKeyword { get; set; }
        public ulong HoloSimKeyword { get; set; }
        public ulong ControlledSummonDurationKeyword { get; set; }
        public ulong TreasureRoomKeyword { get; set; }
        public ulong DangerRoomKeyword { get; set; }
        public ulong StealingPowerKeyword { get; set; }
        public ulong SummonPowerKeyword { get; set; }
    }

    public class CurrencyGlobalsPrototype : Prototype
    {
        public ulong CosmicWorldstones { get; set; }
        public ulong Credits { get; set; }
        public ulong CubeShards { get; set; }
        public ulong EternitySplinters { get; set; }
        public ulong EyesOfDemonfire { get; set; }
        public ulong HeartsOfDemonfire { get; set; }
        public ulong LegendaryMarks { get; set; }
        public ulong OmegaFiles { get; set; }
        public ulong PvPCrowns { get; set; }
        public ulong ResearchDrives { get; set; }
        public ulong GenoshaRaid { get; set; }
        public ulong DangerRoomMerits { get; set; }
        public ulong GazillioniteGs { get; set; }
    }

    public class GamepadInputAssetPrototype : Prototype
    {
        public GamepadInput Input { get; set; }
        public ulong DualShockPath { get; set; }
        public ulong XboxPath { get; set; }
    }

    public class GamepadSlotBindingPrototype : Prototype
    {
        public int OrbisSlotNumber { get; set; }
        public int PCSlotNumber { get; set; }
        public int SlotNumber { get; set; }
    }

    public class GamepadGlobalsPrototype : Prototype
    {
        public int GamepadDialogAcceptTimerMS { get; set; }
        public float GamepadMaxTargetingRange { get; set; }
        public float GamepadTargetingHalfAngle { get; set; }
        public float GamepadTargetingDeflectionCost { get; set; }
        public float GamepadTargetingPriorityCost { get; set; }
        public int UltimateActivationTimeoutMS { get; set; }
        public GamepadInputAssetPrototype[] InputAssets { get; set; }
        public float GamepadInteractionHalfAngle { get; set; }
        public float DisableInteractDangerRadius { get; set; }
        public int GamepadInteractRange { get; set; }
        public float GamepadInteractionOffset { get; set; }
        public float GamepadTargetLockAssistHalfAngle { get; set; }
        public float GamepadTargetLockAssistDflctCost { get; set; }
        public float GamepadInteractBoundsIncrease { get; set; }
        public float GamepadTargetLockDropRadius { get; set; }
        public int GamepadTargetLockDropTimeMS { get; set; }
        public GamepadSlotBindingPrototype[] GamepadSlotBindings { get; set; }
        public float GamepadMeleeMoveIntoRangeDist { get; set; }
        public float GamepadMeleeMoveIntoRangeSpeed { get; set; }
        public float GamepadAutoTargetLockRadius { get; set; }
        public float GamepadDestructTargetDeflctCost { get; set; }
        public float GamepadDestructTargetHalfAngle { get; set; }
        public float GamepadDestructTargetRange { get; set; }
    }

    public class ConsoleGlobalsPrototype : Prototype
    {
        public ulong OrbisDefaultSessionDescription { get; set; }
        public ulong OrbisDefaultSessionImage { get; set; }
        public int OrbisMaxSessionSize { get; set; }
        public int MaxSuggestedPlayers { get; set; }
        public ulong OrbisPlayerCameraSettings { get; set; }
        public ulong OrbisFriendsInvitationDialogDesc { get; set; }
        public int OrbisMaxFriendInvites { get; set; }
        public ulong OrbisFriendsSuggestionDialogDesc { get; set; }
        public int OrbisMaxFriendSuggestions { get; set; }
    }

    public class AvatarOnKilledInfoPrototype : Prototype
    {
        public DeathReleaseBehavior DeathReleaseBehavior { get; set; }
        public ulong DeathReleaseButton { get; set; }
        public ulong DeathReleaseDialogMessage { get; set; }
        public int DeathReleaseTimeoutMS { get; set; }
        public ulong ResurrectionDialogMessage { get; set; }
        public int ResurrectionTimeoutMS { get; set; }
        public int RespawnLockoutMS { get; set; }
    }

    public class GlobalEventPrototype : Prototype
    {
        public bool Active { get; set; }
        public ulong CriteriaList { get; set; }
        public GlobalEventCriteriaLogic CriteriaLogic { get; set; }
        public ulong DisplayName { get; set; }
        public int LeaderboardLength { get; set; }
    }

    public class GlobalEventCriteriaPrototype : Prototype
    {
        public ulong DisplayColor { get; set; }
        public ulong DisplayName { get; set; }
        public int Score { get; set; }
        public int ThresholdCount { get; set; }
        public ulong DisplayTooltip { get; set; }
    }

    public class GlobalEventCriteriaItemCollectPrototype : GlobalEventCriteriaPrototype
    {
        public EntityFilterPrototype ItemFilter { get; set; }
    }
}
