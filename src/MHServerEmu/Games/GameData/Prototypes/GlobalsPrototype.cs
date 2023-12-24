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
        public ulong AdvancementGlobals { get; private set; }
        public ulong AvatarSwapChannelPower { get; private set; }
        public ulong ConnectionMarkerPrototype { get; private set; }
        public ulong DebugGlobals { get; private set; }
        public ulong UIGlobals { get; private set; }
        public ulong DefaultPlayer { get; private set; }
        public ulong DefaultStartTarget { get; private set; }
        public ulong[] PVPAlliances { get; private set; }
        public float HighFlyingHeight { get; private set; }
        public float LowHealthTrigger { get; private set; }
        public float MouseHitCollisionMultiplier { get; private set; }
        public float MouseHitMovingTargetsIncrease { get; private set; }
        public float MouseHitPowerTargetSearchDist { get; private set; }
        public float MouseHitPreferredAddition { get; private set; }
        public float MouseMovementNoPathRadius { get; private set; }
        public ulong MissionGlobals { get; private set; }
        public int TaggingResetDurationMS { get; private set; }
        public int PlayerPartyMaxSize { get; private set; }
        public float NaviBudgetBaseCellSizeWidth { get; private set; }
        public float NaviBudgetBaseCellSizeLength { get; private set; }
        public int NaviBudgetBaseCellMaxPoints { get; private set; }
        public int NaviBudgetBaseCellMaxEdges { get; private set; }
        public ulong[] UIConfigFiles { get; private set; }
        public int InteractRange { get; private set; }
        public ulong CreditsItemPrototype { get; private set; }
        public ulong NegStatusEffectList { get; private set; }
        public ulong PvPPrototype { get; private set; }
        public ulong MissionPrototype { get; private set; }
        public EvalPrototype ItemPriceMultiplierBuyFromVendor { get; private set; }
        public EvalPrototype ItemPriceMultiplierSellToVendor { get; private set; }
        public ModGlobalsPrototype ModGlobals { get; private set; }
        public float MouseMoveDrivePathMaxLengthMult { get; private set; }
        public ulong AudioGlobalEventsClass { get; private set; }
        public ulong MetaGamePrototype { get; private set; }
        public int MobLOSVisUpdatePeriodMS { get; private set; }
        public int MobLOSVisStayVisibleDelayMS { get; private set; }
        public bool MobLOSVisEnabled { get; private set; }
        public ulong BeginPlayAssetTypes { get; private set; }
        public ulong CachedAssetTypes { get; private set; }
        public ulong FileVerificationAssetTypes { get; private set; }
        public ulong LoadingMusic { get; private set; }
        public ulong SystemLocalized { get; private set; }
        public ulong PopulationGlobals { get; private set; }
        public ulong PlayerAlliance { get; private set; }
        public ulong ClusterConfigurationGlobals { get; private set; }
        public ulong DownloadChunks { get; private set; }
        public ulong UIItemInventory { get; private set; }
        public ulong AIGlobals { get; private set; }
        public ulong MusicAssetType { get; private set; }
        public ulong ResurrectionDefaultInfo { get; private set; }
        public ulong PartyJoinPortal { get; private set; }
        public ulong MatchJoinPortal { get; private set; }
        public ulong MovieAssetType { get; private set; }
        public ulong WaypointGraph { get; private set; }
        public ulong WaypointHotspot { get; private set; }
        public float MouseHoldDeadZoneRadius { get; private set; }
        public GlobalPropertiesPrototype Properties { get; private set; }
        public int PlayerGracePeroidInSeconds { get; private set; }
        public ulong CheckpointHotspot { get; private set; }
        public ulong ReturnToHubPower { get; private set; }
        public int DisableEndurRegenOnPowerEndMS { get; private set; }
        public ulong PowerPrototype { get; private set; }
        public ulong WorldEntityPrototype { get; private set; }
        public ulong AreaPrototype { get; private set; }
        public ulong PopulationObjectPrototype { get; private set; }
        public ulong RegionPrototype { get; private set; }
        public ulong AmbientSfxType { get; private set; }
        public ulong CombatGlobals { get; private set; }
        public float OrientForPowerMaxTimeSecs { get; private set; }
        public ulong KismetSequenceEntityPrototype { get; private set; }
        public ulong DynamicArea { get; private set; }
        public ulong ReturnToFieldPower { get; private set; }
        public float AssetCacheCellLoadOutRunSeconds { get; private set; }
        public int AssetCacheMRUSize { get; private set; }
        public int AssetCachePrefetchMRUSize { get; private set; }
        public ulong AvatarSwapInPower { get; private set; }
        public ulong PlayerStartingFaction { get; private set; }
        public ulong VendorBuybackInventory { get; private set; }
        public ulong AnyAlliancePrototype { get; private set; }
        public ulong AnyFriendlyAlliancePrototype { get; private set; }
        public ulong AnyHostileAlliancePrototype { get; private set; }
        public ulong ExperienceBonusCurve { get; private set; }
        public ulong TransitionGlobals { get; private set; }
        public int PlayerGuildMaxSize { get; private set; }
        public bool AutoPartyEnabledInitially { get; private set; }
        public ulong ItemBindingAffix { get; private set; }
        public int InteractFallbackRange { get; private set; }
        public ulong ItemAcquiredThroughMTXStoreAffix { get; private set; }
        public ulong TeleportToPartyMemberPower { get; private set; }
        public ulong AvatarSwapOutPower { get; private set; }
        public int KickIdlePlayerTimeSecs { get; private set; }
        public ulong PlayerCameraSettings { get; private set; }
        public ulong AvatarSynergyCondition { get; private set; }
        public ulong MetaGameLocalized { get; private set; }
        public ulong MetaGameTeamDefault { get; private set; }
        public ulong ItemNoVisualsAffix { get; private set; }
        public int AvatarSynergyConcurrentLimit { get; private set; }
        public ulong LootGlobals { get; private set; }
        public ulong MetaGameTeamBase { get; private set; }
        public ulong AudioGlobals { get; private set; }
        public int PlayerRaidMaxSize { get; private set; }
        public int TimeZone { get; private set; }
        public ulong TeamUpSummonPower { get; private set; }
        public int AssistPvPDurationMS { get; private set; }
        public ulong FulfillmentReceiptPrototype { get; private set; }
        public ulong PetTechVacuumPower { get; private set; }
        public ulong StolenPowerRestrictions { get; private set; }
        public ulong PowerVisualsGlobals { get; private set; }
        public ulong KeywordGlobals { get; private set; }
        public ulong CurrencyGlobals { get; private set; }
        public ulong PointerArrowTemplate { get; private set; }
        public ulong ObjectiveMarkerTemplate { get; private set; }
        public int VaporizedLootLifespanMS { get; private set; }
        public ulong CookedIconAssetTypes { get; private set; }
        public ulong LiveTuneAvatarXPDisplayCondition { get; private set; }
        public ulong LiveTuneCreditsDisplayCondition { get; private set; }
        public ulong LiveTuneRegionXPDisplayCondition { get; private set; }
        public ulong LiveTuneRIFDisplayCondition { get; private set; }
        public ulong LiveTuneSIFDisplayCondition { get; private set; }
        public ulong LiveTuneXPDisplayCondition { get; private set; }
        public ulong ItemLinkInventory { get; private set; }
        public ulong LimitedEditionBlueprint { get; private set; }
        public ulong MobileIconAssetTypes { get; private set; }
        public ulong PetItemBlueprint { get; private set; }
        public ulong AvatarPrototype { get; private set; }
        public int ServerBonusUnlockLevel { get; private set; }
        public ulong GamepadGlobals { get; private set; }
        public ulong CraftingRecipeLibraryInventory { get; private set; }
        public ulong ConditionPrototype { get; private set; }
        public ulong[] LiveTuneServerConditions { get; private set; }
        public ulong DefaultStartingAvatarPrototype { get; private set; }
        public ulong DefaultStartTargetFallbackRegion { get; private set; }
        public ulong DefaultStartTargetPrestigeRegion { get; private set; }
        public ulong DefaultStartTargetStartingRegion { get; private set; }
        public ulong DifficultyGlobals { get; private set; }
        public ulong PublicEventPrototype { get; private set; }
        public ulong AvatarCoopStartPower { get; private set; }
        public ulong AvatarCoopEndPower { get; private set; }
        public DifficultyTierPrototype DifficultyTiers { get; private set; }
        public ulong DefaultLoadingLobbyRegion { get; private set; }
        public ulong DifficultyTierDefault { get; private set; }
        public ulong AvatarHealPower { get; private set; }
        public ulong ConsoleGlobals { get; private set; }
        public ulong TeamUpSynergyCondition { get; private set; }
        public ulong MetricsFrequencyPrototype { get; private set; }
        public ulong ConsumableItemBlueprint { get; private set; }
        public int AvatarCoopInactiveTimeMS { get; private set; }
        public int AvatarCoopInactiveOnDeadBufferMS { get; private set; }
    }

    public class LoginRewardPrototype : Prototype
    {
        public int Day { get; private set; }
        public ulong Item { get; private set; }
        public ulong TooltipText { get; private set; }
        public ulong LogoffPanelEntry { get; private set; }
    }

    public class PrestigeLevelPrototype : Prototype
    {
        public ulong TextStyle { get; private set; }
        public ulong Reward { get; private set; }
    }

    public class PetTechAffixInfoPrototype : Prototype
    {
        public AffixPosition Position { get; private set; }
        public ulong ItemRarityToConsume { get; private set; }
        public int ItemsRequiredToUnlock { get; private set; }
        public ulong LockedDescriptionText { get; private set; }
    }

    public class AdvancementGlobalsPrototype : Prototype
    {
        public ulong LevelingCurve { get; private set; }
        public ulong DeathPenaltyCost { get; private set; }
        public ulong ItemEquipRequirementOffset { get; private set; }
        public ulong VendorLevelingCurve { get; private set; }
        public ulong StatsEval { get; private set; }
        public ulong AvatarThrowabilityEval { get; private set; }
        public EvalPrototype VendorLevelingEval { get; private set; }
        public EvalPrototype VendorRollTableLevelEval { get; private set; }
        public float RestedHealthPerMinMult { get; private set; }
        public int PowerBoostMax { get; private set; }
        public PrestigeLevelPrototype PrestigeLevels { get; private set; }
        public ulong ItemAffixLevelingCurve { get; private set; }
        public ulong ExperienceBonusAvatarSynergy { get; private set; }
        public float ExperienceBonusAvatarSynergyMax { get; private set; }
        public int OriginalMaxLevel { get; private set; }
        public ulong ExperienceBonusLevel60Synergy { get; private set; }
        public int TeamUpPowersPerTier { get; private set; }
        public ulong TeamUpPowerTiersCurve { get; private set; }
        public OmegaBonusSetPrototype OmegaBonusSets { get; private set; }
        public int OmegaPointsCap { get; private set; }
        public int OmegaSystemLevelUnlock { get; private set; }
        public PetTechAffixInfoPrototype[] PetTechAffixInfo { get; private set; }
        public ulong PetTechDonationItemPrototype { get; private set; }
        public int AvatarPowerSpecsMax { get; private set; }
        public ulong PctXPFromPrestigeLevelCurve { get; private set; }
        public int StarterAvatarLevelCap { get; private set; }
        public ulong TeamUpLevelingCurve { get; private set; }
        public int TeamUpPowerSpecsMax { get; private set; }
        public ulong PctXPFromLevelDeltaCurve { get; private set; }
        public int InfinitySystemUnlockLevel { get; private set; }
        public long InfinityPointsCapPerGem { get; private set; }
        public InfinityGemSetPrototype InfinityGemSets { get; private set; }
        public long InfinityXPCap { get; private set; }
        public int TravelPowerUnlockLevel { get; private set; }
        public float ExperienceBonusCoop { get; private set; }
        public ulong CoopInactivityExperienceScalar { get; private set; }
    }

    public class AIGlobalsPrototype : Prototype
    {
        public ulong LeashReturnHeal { get; private set; }
        public ulong LeashReturnImmunity { get; private set; }
        public ulong LeashingProceduralProfile { get; private set; }
        public int RandomThinkVarianceMS { get; private set; }
        public int ControlledAgentResurrectTimerMS { get; private set; }
        public ulong ControlledAlliance { get; private set; }
        public float OrbAggroRangeMax { get; private set; }
        public ulong OrbAggroRangeBonusCurve { get; private set; }
        public ulong DefaultSimpleNpcBrain { get; private set; }
        public ulong CantBeControlledKeyword { get; private set; }
        public int ControlledAgentSummonDurationMS { get; private set; }
    }

    public class MusicStatePrototype : Prototype
    {
        public ulong StateGroupName { get; private set; }
        public ulong StateName { get; private set; }
        public MusicStateEndBehavior EndBehavior { get; private set; }
    }

    public class AudioGlobalsPrototype : Prototype
    {
        public int DefaultMemPoolSizeMB { get; private set; }
        public int LowerEngineMemPoolSizeMB { get; private set; }
        public int StreamingMemPoolSizeMB { get; private set; }
        public int MemoryBudgetMB { get; private set; }
        public int MemoryBudgetDevMB { get; private set; }
        public float MaxAnimNotifyRadius { get; private set; }
        public float MaxMocoVolumeDB { get; private set; }
        public float FootstepNotifyMinFpsThreshold { get; private set; }
        public float BossCritBanterHealthPctThreshold { get; private set; }
        public int LongDownTimeInDaysThreshold { get; private set; }
        public float EncounterCheckRadius { get; private set; }
    }

    public class DebugGlobalsPrototype : Prototype
    {
        public ulong CreateEntityShortcutEntity { get; private set; }
        public ulong DynamicRegion { get; private set; }
        public float HardModeMobDmgBuff { get; private set; }
        public float HardModeMobHealthBuff { get; private set; }
        public float HardModeMobMoveSpdBuff { get; private set; }
        public float HardModePlayerEnduranceCostDebuff { get; private set; }
        public ulong PowersArtModeEntity { get; private set; }
        public int StartingLevelMobs { get; private set; }
        public ulong TransitionRef { get; private set; }
        public ulong CreateLootDummyEntity { get; private set; }
        public ulong MapErrorMapInfo { get; private set; }
        public bool IgnoreDeathPenalty { get; private set; }
        public bool TrashedItemsDropInWorld { get; private set; }
        public ulong PAMEnemyAlliance { get; private set; }
        public EvalPrototype DebugEval { get; private set; }
        public EvalPrototype DebugEvalUnitTest { get; private set; }
        public BotSettingsPrototype BotSettings { get; private set; }
        public ulong ReplacementTestingResultItem { get; private set; }
        public ulong ReplacementTestingTriggerItem { get; private set; }
        public ulong VendorEternitySplinterLoot { get; private set; }
    }

    public class CharacterSheetDetailedStatPrototype : Prototype
    {
        public EvalPrototype Expression { get; private set; }
        public ulong ExpressionExt { get; private set; }
        public ulong Format { get; private set; }
        public ulong Label { get; private set; }
        public ulong Tooltip { get; private set; }
        public ulong Icon { get; private set; }
    }

    public class HelpGameTermPrototype : Prototype
    {
        public ulong Name { get; private set; }
        public ulong Description { get; private set; }
    }

    public class CoopOpUIDataEntryPrototype : Prototype
    {
        public CoopOp Op { get; private set; }
        public CoopOpResult Result { get; private set; }
        public ulong SystemMessage { get; private set; }
        public ulong SystemMessageTemplate { get; private set; }
        public ulong BannerMessage { get; private set; }
    }

    public class HelpTextPrototype : Prototype
    {
        public ulong GeneralControls { get; private set; }
        public HelpGameTermPrototype[] GameTerms { get; private set; }
        public ulong Crafting { get; private set; }
        public ulong EndgamePvE { get; private set; }
        public ulong PvP { get; private set; }
        public ulong Tutorial { get; private set; }
    }

    public class AffixRollQualityPrototype : Prototype
    {
        public TextStylePrototype Style { get; private set; }
        public float PercentThreshold { get; private set; }
    }

    public class UIGlobalsPrototype : Prototype
    {
        public ulong MessageDefault { get; private set; }
        public ulong MessageLevelUp { get; private set; }
        public ulong MessageItemError { get; private set; }
        public ulong MessageRegionChange { get; private set; }
        public ulong MessageMissionAccepted { get; private set; }
        public ulong MessageMissionCompleted { get; private set; }
        public ulong MessageMissionFailed { get; private set; }
        public int AvatarSwitchUIDeathDelayMS { get; private set; }
        public ulong UINotificationGlobals { get; private set; }
        public int RosterPageSize { get; private set; }
        public ulong LocalizedInfoDirectory { get; private set; }
        public int TooltipHideDelayMS { get; private set; }
        public ulong MessagePowerError { get; private set; }
        public ulong MessageWaypointError { get; private set; }
        public ulong UIStringGlobals { get; private set; }
        public ulong MessagePartyInvite { get; private set; }
        public ulong MapInfoMissionGiver { get; private set; }
        public ulong MapInfoMissionObjectiveTalk { get; private set; }
        public int NumAvatarsToDisplayInItemUsableLists { get; private set; }
        public ulong LoadingScreens { get; private set; }
        public int ChatFadeInMS { get; private set; }
        public int ChatBeginFadeOutMS { get; private set; }
        public int ChatFadeOutMS { get; private set; }
        public ulong MessageWaypointUnlocked { get; private set; }
        public ulong MessagePowerUnlocked { get; private set; }
        public ulong UIMapGlobals { get; private set; }
        public ulong TextStyleCurrentlyEquipped { get; private set; }
        public int ChatTextFadeOutMS { get; private set; }
        public int ChatTextHistoryMax { get; private set; }
        public ulong KeywordFemale { get; private set; }
        public ulong KeywordMale { get; private set; }
        public ulong TextStylePowerUpgradeImprovement { get; private set; }
        public ulong TextStylePowerUpgradeNoImprovement { get; private set; }
        public ulong LoadingScreenIntraRegion { get; private set; }
        public ulong TextStyleVendorPriceCanBuy { get; private set; }
        public ulong TextStyleVendorPriceCantBuy { get; private set; }
        public ulong TextStyleItemRestrictionFailure { get; private set; }
        public int CostumeClosetNumAvatarsVisible { get; private set; }
        public int CostumeClosetNumCostumesVisible { get; private set; }
        public ulong MessagePowerErrorDoNotQueue { get; private set; }
        public ulong TextStylePvPShopPurchased { get; private set; }
        public ulong TextStylePvPShopUnpurchased { get; private set; }
        public ulong MessagePowerPointsAwarded { get; private set; }
        public ulong MapInfoMissionObjectiveUse { get; private set; }
        public ulong TextStyleMissionRewardFloaty { get; private set; }
        public ulong PowerTooltipBodyCurRank0Unlkd { get; private set; }
        public ulong PowerTooltipBodyCurRankLocked { get; private set; }
        public ulong PowerTooltipBodyCurRank1AndUp { get; private set; }
        public ulong PowerTooltipBodyNextRank1First { get; private set; }
        public ulong PowerTooltipBodyNextRank2AndUp { get; private set; }
        public ulong PowerTooltipHeader { get; private set; }
        public ulong MapInfoFlavorNPC { get; private set; }
        public int TooltipSpawnHideDelayMS { get; private set; }
        public int KioskIdleResetTimeSec { get; private set; }
        public ulong KioskSizzleMovie { get; private set; }
        public int KioskSizzleMovieStartTimeSec { get; private set; }
        public ulong MapInfoHealer { get; private set; }
        public ulong TextStyleOpenMission { get; private set; }
        public ulong MapInfoPartyMember { get; private set; }
        public int LoadingScreenTipTimeIntervalMS { get; private set; }
        public ulong TextStyleKillRewardFloaty { get; private set; }
        public ulong TextStyleAvatarOverheadNormal { get; private set; }
        public ulong TextStyleAvatarOverheadParty { get; private set; }
        public CharacterSheetDetailedStatPrototype[] CharacterSheetDetailedStats { get; private set; }
        public ulong PowerProgTableTabRefTab1 { get; private set; }
        public ulong PowerProgTableTabRefTab2 { get; private set; }
        public ulong PowerProgTableTabRefTab3 { get; private set; }
        public float ScreenEdgeArrowRange { get; private set; }
        public ulong HelpText { get; private set; }
        public ulong MessagePvPFactionPortalFail { get; private set; }
        public ulong PropertyTooltipTextOverride { get; private set; }
        public ulong MessagePvPDisabledPortalFail { get; private set; }
        public ulong MessageStatProgression { get; private set; }
        public ulong MessagePvPPartyPortalFail { get; private set; }
        public ulong TextStyleMissionHudOpenMission { get; private set; }
        public ulong MapInfoAvatarDefeated { get; private set; }
        public ulong MapInfoPartyMemberDefeated { get; private set; }
        public ulong MessageGuildInvite { get; private set; }
        public ulong MapInfoMissionObjectiveMob { get; private set; }
        public ulong MapInfoMissionObjectivePortal { get; private set; }
        public ulong CinematicsListLoginScreen { get; private set; }
        public ulong TextStyleGuildLeader { get; private set; }
        public ulong TextStyleGuildOfficer { get; private set; }
        public ulong TextStyleGuildMember { get; private set; }
        public AffixDisplaySlotPrototype[] CostumeAffixDisplaySlots { get; private set; }
        public ulong MessagePartyError { get; private set; }
        public ulong MessageRegionRestricted { get; private set; }
        public ulong CreditsMovies { get; private set; }
        public ulong MessageMetaGameDefault { get; private set; }
        public ulong MessagePartyPvPPortalFail { get; private set; }
        public int ChatNewMsgDarkenBgMS { get; private set; }
        public ulong TextStyleKillZeroRewardFloaty { get; private set; }
        public ulong MessageAvatarSwitchError { get; private set; }
        public ulong TextStyleItemBlessed { get; private set; }
        public ulong TextStyleItemAffixLocked { get; private set; }
        public ulong MessageAlreadyInQueue { get; private set; }
        public ulong MessageOnlyPartyLeaderCanQueue { get; private set; }
        public ulong MessageTeleportTargetIsInMatch { get; private set; }
        public ulong PowerGrantItemTutorialTip { get; private set; }
        public ulong MessagePrivateDisallowedInRaid { get; private set; }
        public ulong MessageQueueNotAvailableInRaid { get; private set; }
        public ulong PowerTooltipBodyNextRank1Antireq { get; private set; }
        public ulong CosmicEquippedTutorialTip { get; private set; }
        public ulong MessageRegionDisabledPortalFail { get; private set; }
        public CharacterSheetDetailedStatPrototype[] TeamUpDetailedStats { get; private set; }
        public ulong MessageOmegaPointsAwarded { get; private set; }
        public ulong MetaGameWidgetMissionName { get; private set; }
        public UIConditionType[] BuffPageOrder { get; private set; }
        public ObjectiveTrackerPageType[] ObjectiveTrackerPageOrder { get; private set; }
        public ulong VanityTitleNoTitle { get; private set; }
        public ulong MessageStealablePowerOccupied { get; private set; }
        public ulong MessageStolenPowerDuplicate { get; private set; }
        public ulong CurrencyDisplayList { get; private set; }
        public ulong CinematicOpener { get; private set; }
        public ulong MessageCantQueueInQueueRegion { get; private set; }
        public int LogoffPanelStoryMissionLevelCap { get; private set; }
        public StoreCategoryPrototype[] MTXStoreCategories { get; private set; }
        public int GiftingAccessMinPlayerLevel { get; private set; }
        public ulong AffixRollRangeTooltipText { get; private set; }
        public ulong[] UISystemLockList { get; private set; }
        public ulong MessageUISystemUnlocked { get; private set; }
        public ulong TooltipInsigniaTeamAffiliations { get; private set; }
        public ulong PowerTooltipBodySpecLocked { get; private set; }
        public ulong PowerTooltipBodySpecUnlocked { get; private set; }
        public ulong PropertyValuePercentFormat { get; private set; }
        public ulong AffixStatDiffPositiveStyle { get; private set; }
        public ulong AffixStatDiffNegativeStyle { get; private set; }
        public ulong AffixStatDiffTooltipText { get; private set; }
        public ulong AffixStatDiffNeutralStyle { get; private set; }
        public ulong AffixStatFoundAffixStyle { get; private set; }
        public ulong[] StashTabCustomIcons { get; private set; }
        public ulong PropertyValueDefaultFormat { get; private set; }
        public ulong[] ItemSortCategoryList { get; private set; }
        public ulong[] ItemSortSubCategoryList { get; private set; }
        public AffixRollQualityPrototype[] AffixRollRangeRollQuality { get; private set; }
        public ulong RadialMenuEntriesList { get; private set; }
        public ulong TextStylePowerChargesEmpty { get; private set; }
        public ulong TextStylePowerChargesFull { get; private set; }
        public ulong MessageLeaderboardRewarded { get; private set; }
        public ulong GamepadIconDonateAction { get; private set; }
        public ulong GamepadIconDropAction { get; private set; }
        public ulong GamepadIconEquipAction { get; private set; }
        public ulong GamepadIconMoveAction { get; private set; }
        public ulong GamepadIconSelectAction { get; private set; }
        public ulong GamepadIconSellAction { get; private set; }
        public ulong ConsoleRadialMenuEntriesList { get; private set; }
        public CoopOpUIDataEntryPrototype[] CoopOpUIDatas { get; private set; }
        public ulong MessageOpenMissionEntered { get; private set; }
        public ulong MessageInfinityPointsAwarded { get; private set; }
        public ulong PowerTooltipBodyTalentLocked { get; private set; }
        public ulong PowerTooltipBodyTalentUnlocked { get; private set; }
        public ulong[] AffixTooltipOrder { get; private set; }
        public ulong PowerTooltipBodyCurRank1Only { get; private set; }
        public int InfinityMaxRanksHideThreshold { get; private set; }
        public ulong MessagePlayingAtLevelCap { get; private set; }
        public ulong GamepadIconRankDownAction { get; private set; }
        public ulong GamepadIconRankUpAction { get; private set; }
        public ulong MessageTeamUpDisabledCoop { get; private set; }
        public ulong MessageStolenPowerAvailable { get; private set; }
        public ulong BIFRewardMessage { get; private set; }
        public ulong PowerTooltipBodyTeamUpLocked { get; private set; }
        public ulong PowerTooltipBodyTeamUpUnlocked { get; private set; }
        public int InfinityNotificationThreshold { get; private set; }
        public ulong HelpTextConsole { get; private set; }
        public ulong MessageRegionNotDownloaded { get; private set; }
    }

    public class UINotificationGlobalsPrototype : Prototype
    {
        public ulong NotificationPartyInvite { get; private set; }
        public ulong NotificationLevelUp { get; private set; }
        public ulong NotificationServerMessage { get; private set; }
        public ulong NotificationRemoteMission { get; private set; }
        public ulong NotificationMissionUpdate { get; private set; }
        public ulong NotificationMatchInvite { get; private set; }
        public ulong NotificationMatchQueue { get; private set; }
        public ulong NotificationMatchGroupInvite { get; private set; }
        public ulong NotificationPvPShop { get; private set; }
        public ulong NotificationPowerPointsAwarded { get; private set; }
        public int NotificationPartyAIAggroRange { get; private set; }
        public ulong NotificationOfferingUI { get; private set; }
        public ulong NotificationGuildInvite { get; private set; }
        public ulong NotificationMetaGameInfo { get; private set; }
        public ulong NotificationLegendaryMission { get; private set; }
        public ulong NotificationMatchPending { get; private set; }
        public ulong NotificationMatchGroupPending { get; private set; }
        public ulong NotificationMatchWaitlisted { get; private set; }
        public ulong NotificationLegendaryQuestShare { get; private set; }
        public ulong NotificationSynergyPoints { get; private set; }
        public ulong NotificationPvPScoreboard { get; private set; }
        public ulong NotificationOmegaPoints { get; private set; }
        public ulong NotificationTradeInvite { get; private set; }
        public ulong NotificationMatchLocked { get; private set; }
        public ulong NotificationLoginReward { get; private set; }
        public ulong NotificationMatchGracePeriod { get; private set; }
        public ulong NotificationPartyKickGracePeriod { get; private set; }
        public ulong NotificationGiftReceived { get; private set; }
        public ulong NotificationLeaderboardRewarded { get; private set; }
        public ulong NotificationCouponReceived { get; private set; }
        public ulong NotificationPublicEvent { get; private set; }
    }

    public class UIMapGlobalsPrototype : Prototype
    {
        public float DefaultRevealRadius { get; private set; }
        public float DefaultZoom { get; private set; }
        public float FullScreenMapAlphaMax { get; private set; }
        public float FullScreenMapAlphaMin { get; private set; }
        public int FullScreenMapResolutionHeight { get; private set; }
        public int FullScreenMapResolutionWidth { get; private set; }
        public float FullScreenMapScale { get; private set; }
        public float LowResRevealMultiplier { get; private set; }
        public ulong MapColorFiller { get; private set; }
        public ulong MapColorWalkable { get; private set; }
        public ulong MapColorWall { get; private set; }
        public float MiniMapAlpha { get; private set; }
        public int MiniMapResolution { get; private set; }
        public float CameraAngleX { get; private set; }
        public float CameraAngleY { get; private set; }
        public float CameraAngleZ { get; private set; }
        public float CameraFOV { get; private set; }
        public float CameraNearPlane { get; private set; }
        public float FullScreenMapPOISize { get; private set; }
        public float POIScreenFacingRot { get; private set; }
        public bool DrawPOIInCanvas { get; private set; }
        public bool EnableMinimapProjection { get; private set; }
        public float DefaultZoomMin { get; private set; }
        public float DefaultZoomMax { get; private set; }
        public float MiniMapPOISizeMin { get; private set; }
        public float MiniMapPOISizeMax { get; private set; }
        public ulong MapColorFillerConsole { get; private set; }
        public ulong MapColorWalkableConsole { get; private set; }
        public ulong MapColorWallConsole { get; private set; }
        public float DefaultZoomConsole { get; private set; }
        public float DefaultZoomMinConsole { get; private set; }
        public float DefaultZoomMaxConsole { get; private set; }
        public float MiniMapPOISizeMinConsole { get; private set; }
        public float MiniMapPOISizeMaxConsole { get; private set; }
        public float MiniMapAlphaConsole { get; private set; }
    }

    public class MetricsFrequencyPrototype : Prototype
    {
        public float SampleRate { get; private set; }
    }

    public class CameraSettingPrototype : Prototype
    {
        public float DirectionX { get; private set; }
        public float DirectionY { get; private set; }
        public float DirectionZ { get; private set; }
        public float Distance { get; private set; }
        public float FieldOfView { get; private set; }
        public float ListenerDistance { get; private set; }
        public int RotationPitch { get; private set; }
        public int RotationRoll { get; private set; }
        public int RotationYaw { get; private set; }
        public float LookAtOffsetX { get; private set; }
        public float LookAtOffsetY { get; private set; }
        public float LookAtOffsetZ { get; private set; }
        public bool AllowCharacterSpecificZOffset { get; private set; }
        public bool OrbitalCam { get; private set; }
        public float OrbitalFocusAngle { get; private set; }
        public float OrbitalFocusPosX { get; private set; }
        public float OrbitalFocusPosY { get; private set; }
    }

    public class CameraSettingCollectionPrototype : Prototype
    {
        public CameraSettingPrototype[] CameraSettings { get; private set; }
        public CameraSettingPrototype[] CameraSettingsFlying { get; private set; }
        public int CameraStartingIndex { get; private set; }
        public bool CameraAllowCustomMaxZoom { get; private set; }
    }

    public class GlobalPropertiesPrototype : Prototype
    {
        public ulong Properties { get; private set; }
    }

    public class PowerVisualsGlobalsPrototype : Prototype
    {
        public ulong DailyMissionCompleteClass { get; private set; }
        public ulong UnlockPetTechR1CommonClass { get; private set; }
        public ulong UnlockPetTechR2UncommonClass { get; private set; }
        public ulong UnlockPetTechR3RareClass { get; private set; }
        public ulong UnlockPetTechR4EpicClass { get; private set; }
        public ulong UnlockPetTechR5CosmicClass { get; private set; }
        public ulong LootVaporizedClass { get; private set; }
        public ulong AchievementUnlockedClass { get; private set; }
        public ulong OmegaPointGainedClass { get; private set; }
        public ulong AvatarLeashTeleportClass { get; private set; }
        public ulong InfinityTimePointEarnedClass { get; private set; }
        public ulong InfinitySpacePointEarnedClass { get; private set; }
        public ulong InfinitySoulPointEarnedClass { get; private set; }
        public ulong InfinityMindPointEarnedClass { get; private set; }
        public ulong InfinityRealityPointEarnedClass { get; private set; }
        public ulong InfinityPowerPointEarnedClass { get; private set; }
    }

    public class RankDefaultEntryPrototype : Prototype
    {
        public ulong Data { get; private set; }
        public Rank Rank { get; private set; }
    }

    public class PopulationGlobalsPrototype : Prototype
    {
        public ulong MessageEnemiesGrowStronger { get; private set; }
        public ulong MessageEnemiesGrowWeaker { get; private set; }
        public int SpawnMapPoolTickMS { get; private set; }
        public int SpawnMapLevelTickMS { get; private set; }
        public float CrowdSupressionRadius { get; private set; }
        public bool SupressSpawnOnPlayer { get; private set; }
        public int SpawnMapGimbalRadius { get; private set; }
        public int SpawnMapHorizon { get; private set; }
        public float SpawnMapMaxChance { get; private set; }
        public ulong EmptyPopulation { get; private set; }
        public ulong TwinEnemyBoost { get; private set; }
        public int DestructiblesForceSpawnMS { get; private set; }
        public ulong TwinEnemyCondition { get; private set; }
        public int SpawnMapHeatPerSecondMax { get; private set; }
        public int SpawnMapHeatPerSecondMin { get; private set; }
        public int SpawnMapHeatPerSecondScalar { get; private set; }
        public ulong TwinEnemyRank { get; private set; }
        public RankDefaultEntryPrototype[] RankDefaults { get; private set; }
    }

    public class ClusterConfigurationGlobalsPrototype : Prototype
    {
        public int MinutesToKeepOfflinePlayerGames { get; private set; }
        public int MinutesToKeepUnusedRegions { get; private set; }
        public bool HotspotCheckLOSInTown { get; private set; }
        public int HotspotCheckTargetIntervalMS { get; private set; }
        public int HotspotCheckTargetTownIntervalMS { get; private set; }
        public int PartyKickGracePeriodMS { get; private set; }
        public int QueueReservationGracePeriodMS { get; private set; }
    }

    public class CombatGlobalsPrototype : Prototype
    {
        public float PowerDmgBonusHardcoreAttenuation { get; private set; }
        public int MouseHoldStartMoveDelayMeleeMS { get; private set; }
        public int MouseHoldStartMoveDelayRangedMS { get; private set; }
        public float CriticalForceApplicationChance { get; private set; }
        public float CriticalForceApplicationMag { get; private set; }
        public float EnduranceCostChangePctMin { get; private set; }
        public EvalPrototype EvalBlockChanceFormula { get; private set; }
        public ulong EvalInterruptChanceFormula { get; private set; }
        public ulong EvalNegStatusResistPctFormula { get; private set; }
        public ulong ChannelInterruptCondition { get; private set; }
        public EvalPrototype EvalDamageReduction { get; private set; }
        public EvalPrototype EvalCritChanceFormula { get; private set; }
        public EvalPrototype EvalSuperCritChanceFormula { get; private set; }
        public EvalPrototype EvalDamageRatingFormula { get; private set; }
        public EvalPrototype EvalCritDamageRatingFormula { get; private set; }
        public EvalPrototype EvalDodgeChanceFormula { get; private set; }
        public EvalPrototype EvalDamageReductionDefenseOnly { get; private set; }
        public EvalPrototype EvalDamageReductionForDisplay { get; private set; }
        public float TravelPowerMaxSpeed { get; private set; }
        public ulong TUSynergyBonusPerLvl { get; private set; }
        public ulong TUSynergyBonusPerMaxLvlTU { get; private set; }
    }

    public class VendorXPCapInfoPrototype : Prototype
    {
        public ulong Vendor { get; private set; }
        public int Cap { get; private set; }
        public float WallClockTime24Hr { get; private set; }
        public Weekday WallClockTimeDay { get; private set; }
    }

    public class AffixCategoryTableEntryPrototype : Prototype
    {
        public ulong Category { get; private set; }
        public ulong[] Affixes { get; private set; }
    }

    public class LootGlobalsPrototype : Prototype
    {
        public ulong LootBonusRarityCurve { get; private set; }
        public ulong LootBonusSpecialCurve { get; private set; }
        public ulong LootContainerKeyword { get; private set; }
        public float LootDropScalar { get; private set; }
        public int LootInitializationLevelOffset { get; private set; }
        public ulong LootLevelDistribution { get; private set; }
        public float LootRarityScalar { get; private set; }
        public float LootSpecialItemFindScalar { get; private set; }
        public float LootUnrestedSpecialFindScalar { get; private set; }
        public float LootUsableByRecipientPercent { get; private set; }
        public ulong NoLootTable { get; private set; }
        public ulong SpecialOnKilledLootTable { get; private set; }
        public int SpecialOnKilledLootCooldownHours { get; private set; }
        public ulong RarityCosmic { get; private set; }
        public ulong LootBonusFlatCreditsCurve { get; private set; }
        public ulong RarityUruForged { get; private set; }
        public ulong LootTableBlueprint { get; private set; }
        public ulong RarityUnique { get; private set; }
        public int LootLevelMaxForDrops { get; private set; }
        public ulong InsigniaBlueprint { get; private set; }
        public ulong UniquesBoxCheatItem { get; private set; }
        public ulong[] EmptySocketAffixes { get; private set; }
        public ulong GemBlueprint { get; private set; }
        public VendorXPCapInfoPrototype[] VendorXPCapInfo { get; private set; }
        public float DropDistanceThreshold { get; private set; }
        public AffixCategoryTableEntryPrototype[] AffixCategoryTable { get; private set; }
        public ulong BonusItemFindCurve { get; private set; }
        public int BonusItemFindNumPointsForBonus { get; private set; }
        public ulong BonusItemFindLootTable { get; private set; }
        public float LootCoopPlayerRewardPct { get; private set; }
        public ulong RarityDefault { get; private set; }
    }

    public class MatchQueueStringEntryPrototype : Prototype
    {
        public RegionRequestQueueUpdateVar StatusKey { get; private set; }  // Regions/QueueStatus.type, also appears in protocol
        public ulong StringLog { get; private set; }
        public ulong StringStatus { get; private set; }
    }

    public class TransitionGlobalsPrototype : Prototype
    {
        public RegionPortalControlEntryPrototype[] ControlledRegions { get; private set; }
        public ulong EnabledState { get; private set; }
        public ulong DisabledState { get; private set; }
        public MatchQueueStringEntryPrototype[] QueueStrings { get; private set; }
        public ulong TransitionEmptyClass { get; private set; }
    }

    public class KeywordGlobalsPrototype : Prototype
    {
        public ulong PowerKeywordPrototype { get; private set; }
        public ulong DestructibleKeyword { get; private set; }
        public ulong PetPowerKeyword { get; private set; }
        public ulong VacuumableKeyword { get; private set; }
        public ulong EntityKeywordPrototype { get; private set; }
        public ulong BodysliderPowerKeyword { get; private set; }
        public ulong OrbEntityKeyword { get; private set; }
        public ulong UltimatePowerKeyword { get; private set; }
        public ulong MeleePowerKeyword { get; private set; }
        public ulong RangedPowerKeyword { get; private set; }
        public ulong BasicPowerKeyword { get; private set; }
        public ulong TeamUpSpecialPowerKeyword { get; private set; }
        public ulong TeamUpDefaultPowerKeyword { get; private set; }
        public ulong StealthPowerKeyword { get; private set; }
        public ulong TeamUpAwayPowerKeyword { get; private set; }
        public ulong VanityPetKeyword { get; private set; }
        public ulong ControlPowerKeywordPrototype { get; private set; }
        public ulong AreaPowerKeyword { get; private set; }
        public ulong EnergyPowerKeyword { get; private set; }
        public ulong MentalPowerKeyword { get; private set; }
        public ulong PhysicalPowerKeyword { get; private set; }
        public ulong MedKitKeyword { get; private set; }
        public ulong OrbExperienceEntityKeyword { get; private set; }
        public ulong TutorialRegionKeyword { get; private set; }
        public ulong TeamUpKeyword { get; private set; }
        public ulong MovementPowerKeyword { get; private set; }
        public ulong ThrownPowerKeyword { get; private set; }
        public ulong HoloSimKeyword { get; private set; }
        public ulong ControlledSummonDurationKeyword { get; private set; }
        public ulong TreasureRoomKeyword { get; private set; }
        public ulong DangerRoomKeyword { get; private set; }
        public ulong StealingPowerKeyword { get; private set; }
        public ulong SummonPowerKeyword { get; private set; }
    }

    public class CurrencyGlobalsPrototype : Prototype
    {
        public ulong CosmicWorldstones { get; private set; }
        public ulong Credits { get; private set; }
        public ulong CubeShards { get; private set; }
        public ulong EternitySplinters { get; private set; }
        public ulong EyesOfDemonfire { get; private set; }
        public ulong HeartsOfDemonfire { get; private set; }
        public ulong LegendaryMarks { get; private set; }
        public ulong OmegaFiles { get; private set; }
        public ulong PvPCrowns { get; private set; }
        public ulong ResearchDrives { get; private set; }
        public ulong GenoshaRaid { get; private set; }
        public ulong DangerRoomMerits { get; private set; }
        public ulong GazillioniteGs { get; private set; }
    }

    public class GamepadInputAssetPrototype : Prototype
    {
        public GamepadInput Input { get; private set; }
        public ulong DualShockPath { get; private set; }
        public ulong XboxPath { get; private set; }
    }

    public class GamepadSlotBindingPrototype : Prototype
    {
        public int OrbisSlotNumber { get; private set; }
        public int PCSlotNumber { get; private set; }
        public int SlotNumber { get; private set; }
    }

    public class GamepadGlobalsPrototype : Prototype
    {
        public int GamepadDialogAcceptTimerMS { get; private set; }
        public float GamepadMaxTargetingRange { get; private set; }
        public float GamepadTargetingHalfAngle { get; private set; }
        public float GamepadTargetingDeflectionCost { get; private set; }
        public float GamepadTargetingPriorityCost { get; private set; }
        public int UltimateActivationTimeoutMS { get; private set; }
        public GamepadInputAssetPrototype[] InputAssets { get; private set; }
        public float GamepadInteractionHalfAngle { get; private set; }
        public float DisableInteractDangerRadius { get; private set; }
        public int GamepadInteractRange { get; private set; }
        public float GamepadInteractionOffset { get; private set; }
        public float GamepadTargetLockAssistHalfAngle { get; private set; }
        public float GamepadTargetLockAssistDflctCost { get; private set; }
        public float GamepadInteractBoundsIncrease { get; private set; }
        public float GamepadTargetLockDropRadius { get; private set; }
        public int GamepadTargetLockDropTimeMS { get; private set; }
        public GamepadSlotBindingPrototype[] GamepadSlotBindings { get; private set; }
        public float GamepadMeleeMoveIntoRangeDist { get; private set; }
        public float GamepadMeleeMoveIntoRangeSpeed { get; private set; }
        public float GamepadAutoTargetLockRadius { get; private set; }
        public float GamepadDestructTargetDeflctCost { get; private set; }
        public float GamepadDestructTargetHalfAngle { get; private set; }
        public float GamepadDestructTargetRange { get; private set; }
    }

    public class ConsoleGlobalsPrototype : Prototype
    {
        public ulong OrbisDefaultSessionDescription { get; private set; }
        public ulong OrbisDefaultSessionImage { get; private set; }
        public int OrbisMaxSessionSize { get; private set; }
        public int MaxSuggestedPlayers { get; private set; }
        public ulong OrbisPlayerCameraSettings { get; private set; }
        public ulong OrbisFriendsInvitationDialogDesc { get; private set; }
        public int OrbisMaxFriendInvites { get; private set; }
        public ulong OrbisFriendsSuggestionDialogDesc { get; private set; }
        public int OrbisMaxFriendSuggestions { get; private set; }
    }

    public class AvatarOnKilledInfoPrototype : Prototype
    {
        public DeathReleaseBehavior DeathReleaseBehavior { get; private set; }
        public ulong DeathReleaseButton { get; private set; }
        public ulong DeathReleaseDialogMessage { get; private set; }
        public int DeathReleaseTimeoutMS { get; private set; }
        public ulong ResurrectionDialogMessage { get; private set; }
        public int ResurrectionTimeoutMS { get; private set; }
        public int RespawnLockoutMS { get; private set; }
    }

    public class GlobalEventPrototype : Prototype
    {
        public bool Active { get; private set; }
        public ulong CriteriaList { get; private set; }
        public GlobalEventCriteriaLogic CriteriaLogic { get; private set; }
        public ulong DisplayName { get; private set; }
        public int LeaderboardLength { get; private set; }
    }

    public class GlobalEventCriteriaPrototype : Prototype
    {
        public ulong DisplayColor { get; private set; }
        public ulong DisplayName { get; private set; }
        public int Score { get; private set; }
        public int ThresholdCount { get; private set; }
        public ulong DisplayTooltip { get; private set; }
    }

    public class GlobalEventCriteriaItemCollectPrototype : GlobalEventCriteriaPrototype
    {
        public EntityFilterPrototype ItemFilter { get; private set; }
    }
}
