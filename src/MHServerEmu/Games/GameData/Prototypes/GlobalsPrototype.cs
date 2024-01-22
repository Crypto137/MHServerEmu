using Gazillion;
using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)DoNothing)]
    public enum MusicStateEndBehavior
    {
        DoNothing = 0,
        PlayDefaultList = 1,
        StopMusic = 2,
    }

    [AssetEnum((int)StartForSlot)]
    public enum CoopOp
    {
        StartForSlot = 0,
        EndForSlot = 1,
    }

    [AssetEnum((int)GenericError)]
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

    [AssetEnum((int)Invalid)]
    public enum MatchQueueStatus    // Regions/QueueStatus.type, equivalent to Gazillion::RegionRequestQueueUpdateVar from the protocol
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

    [AssetEnum((int)None)]
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

    [AssetEnum((int)ReturnToWaypoint)]
    public enum DeathReleaseBehavior    // Globals/AvatarDeathReleaseBehavior.type
    {
        ReturnToWaypoint = 0,
        ReturnToCheckpoint = 1,
    }

    [AssetEnum((int)Invalid)]
    public enum GlobalEventCriteriaLogic
    {
        Invalid = -1,
        And = 0,
        Or = 1,
    }

    #endregion

    public class GlobalsPrototype : Prototype
    {
        public ulong AdvancementGlobals { get; protected set; }
        public ulong AvatarSwapChannelPower { get; protected set; }
        public ulong ConnectionMarkerPrototype { get; protected set; }
        public ulong DebugGlobals { get; protected set; }
        public ulong UIGlobals { get; protected set; }
        public ulong DefaultPlayer { get; protected set; }
        public ulong DefaultStartTarget { get; protected set; }
        public ulong[] PVPAlliances { get; protected set; }
        public float HighFlyingHeight { get; protected set; }
        public float LowHealthTrigger { get; protected set; }
        public float MouseHitCollisionMultiplier { get; protected set; }
        public float MouseHitMovingTargetsIncrease { get; protected set; }
        public float MouseHitPowerTargetSearchDist { get; protected set; }
        public float MouseHitPreferredAddition { get; protected set; }
        public float MouseMovementNoPathRadius { get; protected set; }
        public ulong MissionGlobals { get; protected set; }
        public int TaggingResetDurationMS { get; protected set; }
        public int PlayerPartyMaxSize { get; protected set; }
        public float NaviBudgetBaseCellSizeWidth { get; protected set; }
        public float NaviBudgetBaseCellSizeLength { get; protected set; }
        public int NaviBudgetBaseCellMaxPoints { get; protected set; }
        public int NaviBudgetBaseCellMaxEdges { get; protected set; }
        public ulong[] UIConfigFiles { get; protected set; }
        public int InteractRange { get; protected set; }
        public ulong CreditsItemPrototype { get; protected set; }
        public ulong[] NegStatusEffectList { get; protected set; }
        public ulong PvPPrototype { get; protected set; }
        public ulong MissionPrototype { get; protected set; }
        public EvalPrototype ItemPriceMultiplierBuyFromVendor { get; protected set; }
        public EvalPrototype ItemPriceMultiplierSellToVendor { get; protected set; }
        public ModGlobalsPrototype ModGlobals { get; protected set; }
        public float MouseMoveDrivePathMaxLengthMult { get; protected set; }
        public ulong AudioGlobalEventsClass { get; protected set; }
        public ulong MetaGamePrototype { get; protected set; }
        public int MobLOSVisUpdatePeriodMS { get; protected set; }
        public int MobLOSVisStayVisibleDelayMS { get; protected set; }
        public bool MobLOSVisEnabled { get; protected set; }
        public ulong[] BeginPlayAssetTypes { get; protected set; }
        public ulong[] CachedAssetTypes { get; protected set; }
        public ulong[] FileVerificationAssetTypes { get; protected set; }
        public ulong LoadingMusic { get; protected set; }
        public ulong SystemLocalized { get; protected set; }
        public ulong PopulationGlobals { get; protected set; }
        public ulong PlayerAlliance { get; protected set; }
        public ulong ClusterConfigurationGlobals { get; protected set; }
        public ulong DownloadChunks { get; protected set; }
        public ulong UIItemInventory { get; protected set; }
        public ulong AIGlobals { get; protected set; }
        public ulong MusicAssetType { get; protected set; }
        public ulong ResurrectionDefaultInfo { get; protected set; }
        public ulong PartyJoinPortal { get; protected set; }
        public ulong MatchJoinPortal { get; protected set; }
        public ulong MovieAssetType { get; protected set; }
        public ulong WaypointGraph { get; protected set; }
        public ulong WaypointHotspot { get; protected set; }
        public float MouseHoldDeadZoneRadius { get; protected set; }
        public GlobalPropertiesPrototype Properties { get; protected set; }
        public int PlayerGracePeroidInSeconds { get; protected set; }
        public ulong CheckpointHotspot { get; protected set; }
        public ulong ReturnToHubPower { get; protected set; }
        public int DisableEndurRegenOnPowerEndMS { get; protected set; }
        public ulong PowerPrototype { get; protected set; }
        public ulong WorldEntityPrototype { get; protected set; }
        public ulong AreaPrototype { get; protected set; }
        public ulong PopulationObjectPrototype { get; protected set; }
        public ulong RegionPrototype { get; protected set; }
        public ulong AmbientSfxType { get; protected set; }
        public ulong CombatGlobals { get; protected set; }
        public float OrientForPowerMaxTimeSecs { get; protected set; }
        public ulong KismetSequenceEntityPrototype { get; protected set; }
        public ulong DynamicArea { get; protected set; }
        public ulong ReturnToFieldPower { get; protected set; }
        public float AssetCacheCellLoadOutRunSeconds { get; protected set; }
        public int AssetCacheMRUSize { get; protected set; }
        public int AssetCachePrefetchMRUSize { get; protected set; }
        public ulong AvatarSwapInPower { get; protected set; }
        public ulong PlayerStartingFaction { get; protected set; }
        public ulong VendorBuybackInventory { get; protected set; }
        public ulong AnyAlliancePrototype { get; protected set; }
        public ulong AnyFriendlyAlliancePrototype { get; protected set; }
        public ulong AnyHostileAlliancePrototype { get; protected set; }
        public ulong ExperienceBonusCurve { get; protected set; }
        public ulong TransitionGlobals { get; protected set; }
        public int PlayerGuildMaxSize { get; protected set; }
        public bool AutoPartyEnabledInitially { get; protected set; }
        public ulong ItemBindingAffix { get; protected set; }
        public int InteractFallbackRange { get; protected set; }
        public ulong ItemAcquiredThroughMTXStoreAffix { get; protected set; }
        public ulong TeleportToPartyMemberPower { get; protected set; }
        public ulong AvatarSwapOutPower { get; protected set; }
        public int KickIdlePlayerTimeSecs { get; protected set; }
        public ulong PlayerCameraSettings { get; protected set; }
        public ulong AvatarSynergyCondition { get; protected set; }
        public ulong MetaGameLocalized { get; protected set; }
        public ulong MetaGameTeamDefault { get; protected set; }
        public ulong ItemNoVisualsAffix { get; protected set; }
        public int AvatarSynergyConcurrentLimit { get; protected set; }
        public ulong LootGlobals { get; protected set; }
        public ulong MetaGameTeamBase { get; protected set; }
        public ulong AudioGlobals { get; protected set; }
        public int PlayerRaidMaxSize { get; protected set; }
        public int TimeZone { get; protected set; }
        public ulong TeamUpSummonPower { get; protected set; }
        public int AssistPvPDurationMS { get; protected set; }
        public ulong FulfillmentReceiptPrototype { get; protected set; }
        public ulong PetTechVacuumPower { get; protected set; }
        public ulong[] StolenPowerRestrictions { get; protected set; }
        public ulong PowerVisualsGlobals { get; protected set; }
        public ulong KeywordGlobals { get; protected set; }
        public ulong CurrencyGlobals { get; protected set; }
        public ulong PointerArrowTemplate { get; protected set; }
        public ulong ObjectiveMarkerTemplate { get; protected set; }
        public int VaporizedLootLifespanMS { get; protected set; }
        public ulong[] CookedIconAssetTypes { get; protected set; }
        public ulong LiveTuneAvatarXPDisplayCondition { get; protected set; }
        public ulong LiveTuneCreditsDisplayCondition { get; protected set; }
        public ulong LiveTuneRegionXPDisplayCondition { get; protected set; }
        public ulong LiveTuneRIFDisplayCondition { get; protected set; }
        public ulong LiveTuneSIFDisplayCondition { get; protected set; }
        public ulong LiveTuneXPDisplayCondition { get; protected set; }
        public ulong ItemLinkInventory { get; protected set; }
        public ulong LimitedEditionBlueprint { get; protected set; }
        public ulong[] MobileIconAssetTypes { get; protected set; }
        public ulong PetItemBlueprint { get; protected set; }
        public ulong AvatarPrototype { get; protected set; }
        public int ServerBonusUnlockLevel { get; protected set; }
        public ulong GamepadGlobals { get; protected set; }
        public ulong CraftingRecipeLibraryInventory { get; protected set; }
        public ulong ConditionPrototype { get; protected set; }
        public ulong[] LiveTuneServerConditions { get; protected set; }
        public ulong DefaultStartingAvatarPrototype { get; protected set; }
        public ulong DefaultStartTargetFallbackRegion { get; protected set; }
        public ulong DefaultStartTargetPrestigeRegion { get; protected set; }
        public ulong DefaultStartTargetStartingRegion { get; protected set; }
        public ulong DifficultyGlobals { get; protected set; }
        public ulong PublicEventPrototype { get; protected set; }
        public ulong AvatarCoopStartPower { get; protected set; }
        public ulong AvatarCoopEndPower { get; protected set; }
        public ulong[] DifficultyTiers { get; protected set; }      // VectorPrototypeRefPtr DifficultyTierPrototype
        public ulong DefaultLoadingLobbyRegion { get; protected set; }
        public ulong DifficultyTierDefault { get; protected set; }
        public ulong AvatarHealPower { get; protected set; }
        public ulong ConsoleGlobals { get; protected set; }
        public ulong TeamUpSynergyCondition { get; protected set; }
        public ulong MetricsFrequencyPrototype { get; protected set; }
        public ulong ConsumableItemBlueprint { get; protected set; }
        public int AvatarCoopInactiveTimeMS { get; protected set; }
        public int AvatarCoopInactiveOnDeadBufferMS { get; protected set; }
    }

    public class LoginRewardPrototype : Prototype
    {
        public int Day { get; protected set; }
        public ulong Item { get; protected set; }
        public ulong TooltipText { get; protected set; }
        public ulong LogoffPanelEntry { get; protected set; }
    }

    public class PrestigeLevelPrototype : Prototype
    {
        public ulong TextStyle { get; protected set; }
        public ulong Reward { get; protected set; }
    }

    public class PetTechAffixInfoPrototype : Prototype
    {
        public AffixPosition Position { get; protected set; }
        public ulong ItemRarityToConsume { get; protected set; }
        public int ItemsRequiredToUnlock { get; protected set; }
        public ulong LockedDescriptionText { get; protected set; }
    }

    public class AdvancementGlobalsPrototype : Prototype
    {
        public ulong LevelingCurve { get; protected set; }
        public ulong DeathPenaltyCost { get; protected set; }
        public ulong ItemEquipRequirementOffset { get; protected set; }
        public ulong VendorLevelingCurve { get; protected set; }
        public ulong StatsEval { get; protected set; }
        public ulong AvatarThrowabilityEval { get; protected set; }
        public EvalPrototype VendorLevelingEval { get; protected set; }
        public EvalPrototype VendorRollTableLevelEval { get; protected set; }
        public float RestedHealthPerMinMult { get; protected set; }
        public int PowerBoostMax { get; protected set; }
        public ulong[] PrestigeLevels { get; protected set; }   // VectorPrototypeRefPtr PrestigeLevelPrototype
        public ulong ItemAffixLevelingCurve { get; protected set; }
        public ulong ExperienceBonusAvatarSynergy { get; protected set; }
        public float ExperienceBonusAvatarSynergyMax { get; protected set; }
        public int OriginalMaxLevel { get; protected set; }
        public ulong ExperienceBonusLevel60Synergy { get; protected set; }
        public int TeamUpPowersPerTier { get; protected set; }
        public ulong TeamUpPowerTiersCurve { get; protected set; }
        public ulong[] OmegaBonusSets { get; protected set; }   // VectorPrototypeRefPtr OmegaBonusSetPrototype
        public int OmegaPointsCap { get; protected set; }
        public int OmegaSystemLevelUnlock { get; protected set; }
        public PetTechAffixInfoPrototype[] PetTechAffixInfo { get; protected set; }
        public ulong PetTechDonationItemPrototype { get; protected set; }
        public int AvatarPowerSpecsMax { get; protected set; }
        public ulong PctXPFromPrestigeLevelCurve { get; protected set; }
        public int StarterAvatarLevelCap { get; protected set; }
        public ulong TeamUpLevelingCurve { get; protected set; }
        public int TeamUpPowerSpecsMax { get; protected set; }
        public ulong PctXPFromLevelDeltaCurve { get; protected set; }
        public int InfinitySystemUnlockLevel { get; protected set; }
        public long InfinityPointsCapPerGem { get; protected set; }
        public ulong[] InfinityGemSets { get; protected set; }  // VectorPrototypeRefPtr InfinityGemSetPrototype
        public long InfinityXPCap { get; protected set; }
        public int TravelPowerUnlockLevel { get; protected set; }
        public float ExperienceBonusCoop { get; protected set; }
        public ulong CoopInactivityExperienceScalar { get; protected set; }
    }

    public class AIGlobalsPrototype : Prototype
    {
        public ulong LeashReturnHeal { get; protected set; }
        public ulong LeashReturnImmunity { get; protected set; }
        public ulong LeashingProceduralProfile { get; protected set; }
        public int RandomThinkVarianceMS { get; protected set; }
        public int ControlledAgentResurrectTimerMS { get; protected set; }
        public ulong ControlledAlliance { get; protected set; }
        public float OrbAggroRangeMax { get; protected set; }
        public ulong OrbAggroRangeBonusCurve { get; protected set; }
        public ulong DefaultSimpleNpcBrain { get; protected set; }
        public ulong CantBeControlledKeyword { get; protected set; }
        public int ControlledAgentSummonDurationMS { get; protected set; }
    }

    public class MusicStatePrototype : Prototype
    {
        public ulong StateGroupName { get; protected set; }
        public ulong StateName { get; protected set; }
        public MusicStateEndBehavior EndBehavior { get; protected set; }
    }

    public class AudioGlobalsPrototype : Prototype
    {
        public int DefaultMemPoolSizeMB { get; protected set; }
        public int LowerEngineMemPoolSizeMB { get; protected set; }
        public int StreamingMemPoolSizeMB { get; protected set; }
        public int MemoryBudgetMB { get; protected set; }
        public int MemoryBudgetDevMB { get; protected set; }
        public float MaxAnimNotifyRadius { get; protected set; }
        public float MaxMocoVolumeDB { get; protected set; }
        public float FootstepNotifyMinFpsThreshold { get; protected set; }
        public float BossCritBanterHealthPctThreshold { get; protected set; }
        public int LongDownTimeInDaysThreshold { get; protected set; }
        public float EncounterCheckRadius { get; protected set; }
    }

    public class DebugGlobalsPrototype : Prototype
    {
        public ulong CreateEntityShortcutEntity { get; protected set; }
        public ulong DynamicRegion { get; protected set; }
        public float HardModeMobDmgBuff { get; protected set; }
        public float HardModeMobHealthBuff { get; protected set; }
        public float HardModeMobMoveSpdBuff { get; protected set; }
        public float HardModePlayerEnduranceCostDebuff { get; protected set; }
        public ulong PowersArtModeEntity { get; protected set; }
        public int StartingLevelMobs { get; protected set; }
        public ulong TransitionRef { get; protected set; }
        public ulong CreateLootDummyEntity { get; protected set; }
        public ulong MapErrorMapInfo { get; protected set; }
        public bool IgnoreDeathPenalty { get; protected set; }
        public bool TrashedItemsDropInWorld { get; protected set; }
        public ulong PAMEnemyAlliance { get; protected set; }
        public EvalPrototype DebugEval { get; protected set; }
        public EvalPrototype DebugEvalUnitTest { get; protected set; }
        public BotSettingsPrototype BotSettings { get; protected set; }
        public ulong ReplacementTestingResultItem { get; protected set; }
        public ulong ReplacementTestingTriggerItem { get; protected set; }
        public ulong VendorEternitySplinterLoot { get; protected set; }
    }

    public class CharacterSheetDetailedStatPrototype : Prototype
    {
        public EvalPrototype Expression { get; protected set; }
        public ulong ExpressionExt { get; protected set; }
        public ulong Format { get; protected set; }
        public ulong Label { get; protected set; }
        public ulong Tooltip { get; protected set; }
        public ulong Icon { get; protected set; }
    }

    public class HelpGameTermPrototype : Prototype
    {
        public ulong Name { get; protected set; }
        public ulong Description { get; protected set; }
    }

    public class CoopOpUIDataEntryPrototype : Prototype
    {
        public CoopOp Op { get; protected set; }
        public CoopOpResult Result { get; protected set; }
        public ulong SystemMessage { get; protected set; }
        public ulong SystemMessageTemplate { get; protected set; }
        public ulong BannerMessage { get; protected set; }
    }

    public class HelpTextPrototype : Prototype
    {
        public ulong GeneralControls { get; protected set; }
        public HelpGameTermPrototype[] GameTerms { get; protected set; }
        public ulong Crafting { get; protected set; }
        public ulong EndgamePvE { get; protected set; }
        public ulong PvP { get; protected set; }
        public ulong Tutorial { get; protected set; }
    }

    public class AffixRollQualityPrototype : Prototype
    {
        public TextStylePrototype Style { get; protected set; }
        public float PercentThreshold { get; protected set; }
    }

    public class UIGlobalsPrototype : Prototype
    {
        public ulong MessageDefault { get; protected set; }
        public ulong MessageLevelUp { get; protected set; }
        public ulong MessageItemError { get; protected set; }
        public ulong MessageRegionChange { get; protected set; }
        public ulong MessageMissionAccepted { get; protected set; }
        public ulong MessageMissionCompleted { get; protected set; }
        public ulong MessageMissionFailed { get; protected set; }
        public int AvatarSwitchUIDeathDelayMS { get; protected set; }
        public ulong UINotificationGlobals { get; protected set; }
        public int RosterPageSize { get; protected set; }
        public ulong LocalizedInfoDirectory { get; protected set; }
        public int TooltipHideDelayMS { get; protected set; }
        public ulong MessagePowerError { get; protected set; }
        public ulong MessageWaypointError { get; protected set; }
        public ulong UIStringGlobals { get; protected set; }
        public ulong MessagePartyInvite { get; protected set; }
        public ulong MapInfoMissionGiver { get; protected set; }
        public ulong MapInfoMissionObjectiveTalk { get; protected set; }
        public int NumAvatarsToDisplayInItemUsableLists { get; protected set; }
        public ulong[] LoadingScreens { get; protected set; }
        public int ChatFadeInMS { get; protected set; }
        public int ChatBeginFadeOutMS { get; protected set; }
        public int ChatFadeOutMS { get; protected set; }
        public ulong MessageWaypointUnlocked { get; protected set; }
        public ulong MessagePowerUnlocked { get; protected set; }
        public ulong UIMapGlobals { get; protected set; }
        public ulong TextStyleCurrentlyEquipped { get; protected set; }
        public int ChatTextFadeOutMS { get; protected set; }
        public int ChatTextHistoryMax { get; protected set; }
        public ulong KeywordFemale { get; protected set; }
        public ulong KeywordMale { get; protected set; }
        public ulong TextStylePowerUpgradeImprovement { get; protected set; }
        public ulong TextStylePowerUpgradeNoImprovement { get; protected set; }
        public ulong LoadingScreenIntraRegion { get; protected set; }
        public ulong TextStyleVendorPriceCanBuy { get; protected set; }
        public ulong TextStyleVendorPriceCantBuy { get; protected set; }
        public ulong TextStyleItemRestrictionFailure { get; protected set; }
        public int CostumeClosetNumAvatarsVisible { get; protected set; }
        public int CostumeClosetNumCostumesVisible { get; protected set; }
        public ulong MessagePowerErrorDoNotQueue { get; protected set; }
        public ulong TextStylePvPShopPurchased { get; protected set; }
        public ulong TextStylePvPShopUnpurchased { get; protected set; }
        public ulong MessagePowerPointsAwarded { get; protected set; }
        public ulong MapInfoMissionObjectiveUse { get; protected set; }
        public ulong TextStyleMissionRewardFloaty { get; protected set; }
        public ulong PowerTooltipBodyCurRank0Unlkd { get; protected set; }
        public ulong PowerTooltipBodyCurRankLocked { get; protected set; }
        public ulong PowerTooltipBodyCurRank1AndUp { get; protected set; }
        public ulong PowerTooltipBodyNextRank1First { get; protected set; }
        public ulong PowerTooltipBodyNextRank2AndUp { get; protected set; }
        public ulong PowerTooltipHeader { get; protected set; }
        public ulong MapInfoFlavorNPC { get; protected set; }
        public int TooltipSpawnHideDelayMS { get; protected set; }
        public int KioskIdleResetTimeSec { get; protected set; }
        public ulong KioskSizzleMovie { get; protected set; }
        public int KioskSizzleMovieStartTimeSec { get; protected set; }
        public ulong MapInfoHealer { get; protected set; }
        public ulong TextStyleOpenMission { get; protected set; }
        public ulong MapInfoPartyMember { get; protected set; }
        public int LoadingScreenTipTimeIntervalMS { get; protected set; }
        public ulong TextStyleKillRewardFloaty { get; protected set; }
        public ulong TextStyleAvatarOverheadNormal { get; protected set; }
        public ulong TextStyleAvatarOverheadParty { get; protected set; }
        public CharacterSheetDetailedStatPrototype[] CharacterSheetDetailedStats { get; protected set; }
        public ulong PowerProgTableTabRefTab1 { get; protected set; }
        public ulong PowerProgTableTabRefTab2 { get; protected set; }
        public ulong PowerProgTableTabRefTab3 { get; protected set; }
        public float ScreenEdgeArrowRange { get; protected set; }
        public ulong HelpText { get; protected set; }
        public ulong MessagePvPFactionPortalFail { get; protected set; }
        public ulong PropertyTooltipTextOverride { get; protected set; }
        public ulong MessagePvPDisabledPortalFail { get; protected set; }
        public ulong MessageStatProgression { get; protected set; }
        public ulong MessagePvPPartyPortalFail { get; protected set; }
        public ulong TextStyleMissionHudOpenMission { get; protected set; }
        public ulong MapInfoAvatarDefeated { get; protected set; }
        public ulong MapInfoPartyMemberDefeated { get; protected set; }
        public ulong MessageGuildInvite { get; protected set; }
        public ulong MapInfoMissionObjectiveMob { get; protected set; }
        public ulong MapInfoMissionObjectivePortal { get; protected set; }
        public ulong CinematicsListLoginScreen { get; protected set; }
        public ulong TextStyleGuildLeader { get; protected set; }
        public ulong TextStyleGuildOfficer { get; protected set; }
        public ulong TextStyleGuildMember { get; protected set; }
        public AffixDisplaySlotPrototype[] CostumeAffixDisplaySlots { get; protected set; }
        public ulong MessagePartyError { get; protected set; }
        public ulong MessageRegionRestricted { get; protected set; }
        public ulong[] CreditsMovies { get; protected set; }
        public ulong MessageMetaGameDefault { get; protected set; }
        public ulong MessagePartyPvPPortalFail { get; protected set; }
        public int ChatNewMsgDarkenBgMS { get; protected set; }
        public ulong TextStyleKillZeroRewardFloaty { get; protected set; }
        public ulong MessageAvatarSwitchError { get; protected set; }
        public ulong TextStyleItemBlessed { get; protected set; }
        public ulong TextStyleItemAffixLocked { get; protected set; }
        public ulong MessageAlreadyInQueue { get; protected set; }
        public ulong MessageOnlyPartyLeaderCanQueue { get; protected set; }
        public ulong MessageTeleportTargetIsInMatch { get; protected set; }
        public ulong PowerGrantItemTutorialTip { get; protected set; }
        public ulong MessagePrivateDisallowedInRaid { get; protected set; }
        public ulong MessageQueueNotAvailableInRaid { get; protected set; }
        public ulong PowerTooltipBodyNextRank1Antireq { get; protected set; }
        public ulong CosmicEquippedTutorialTip { get; protected set; }
        public ulong MessageRegionDisabledPortalFail { get; protected set; }
        public CharacterSheetDetailedStatPrototype[] TeamUpDetailedStats { get; protected set; }
        public ulong MessageOmegaPointsAwarded { get; protected set; }
        public ulong MetaGameWidgetMissionName { get; protected set; }
        public UIConditionType[] BuffPageOrder { get; protected set; }
        public ObjectiveTrackerPageType[] ObjectiveTrackerPageOrder { get; protected set; }
        public ulong VanityTitleNoTitle { get; protected set; }
        public ulong MessageStealablePowerOccupied { get; protected set; }
        public ulong MessageStolenPowerDuplicate { get; protected set; }
        public ulong[] CurrencyDisplayList { get; protected set; }
        public ulong CinematicOpener { get; protected set; }
        public ulong MessageCantQueueInQueueRegion { get; protected set; }
        public int LogoffPanelStoryMissionLevelCap { get; protected set; }
        public StoreCategoryPrototype[] MTXStoreCategories { get; protected set; }
        public int GiftingAccessMinPlayerLevel { get; protected set; }
        public ulong AffixRollRangeTooltipText { get; protected set; }
        public ulong[] UISystemLockList { get; protected set; }
        public ulong MessageUISystemUnlocked { get; protected set; }
        public ulong TooltipInsigniaTeamAffiliations { get; protected set; }
        public ulong PowerTooltipBodySpecLocked { get; protected set; }
        public ulong PowerTooltipBodySpecUnlocked { get; protected set; }
        public ulong PropertyValuePercentFormat { get; protected set; }
        public ulong AffixStatDiffPositiveStyle { get; protected set; }
        public ulong AffixStatDiffNegativeStyle { get; protected set; }
        public ulong AffixStatDiffTooltipText { get; protected set; }
        public ulong AffixStatDiffNeutralStyle { get; protected set; }
        public ulong AffixStatFoundAffixStyle { get; protected set; }
        public ulong[] StashTabCustomIcons { get; protected set; }
        public ulong PropertyValueDefaultFormat { get; protected set; }
        public ulong[] ItemSortCategoryList { get; protected set; }
        public ulong[] ItemSortSubCategoryList { get; protected set; }
        public AffixRollQualityPrototype[] AffixRollRangeRollQuality { get; protected set; }
        public ulong[] RadialMenuEntriesList { get; protected set; }
        public ulong TextStylePowerChargesEmpty { get; protected set; }
        public ulong TextStylePowerChargesFull { get; protected set; }
        public ulong MessageLeaderboardRewarded { get; protected set; }
        public ulong GamepadIconDonateAction { get; protected set; }
        public ulong GamepadIconDropAction { get; protected set; }
        public ulong GamepadIconEquipAction { get; protected set; }
        public ulong GamepadIconMoveAction { get; protected set; }
        public ulong GamepadIconSelectAction { get; protected set; }
        public ulong GamepadIconSellAction { get; protected set; }
        public ulong[] ConsoleRadialMenuEntriesList { get; protected set; }
        public CoopOpUIDataEntryPrototype[] CoopOpUIDatas { get; protected set; }
        public ulong MessageOpenMissionEntered { get; protected set; }
        public ulong MessageInfinityPointsAwarded { get; protected set; }
        public ulong PowerTooltipBodyTalentLocked { get; protected set; }
        public ulong PowerTooltipBodyTalentUnlocked { get; protected set; }
        public ulong[] AffixTooltipOrder { get; protected set; }
        public ulong PowerTooltipBodyCurRank1Only { get; protected set; }
        public int InfinityMaxRanksHideThreshold { get; protected set; }
        public ulong MessagePlayingAtLevelCap { get; protected set; }
        public ulong GamepadIconRankDownAction { get; protected set; }
        public ulong GamepadIconRankUpAction { get; protected set; }
        public ulong MessageTeamUpDisabledCoop { get; protected set; }
        public ulong MessageStolenPowerAvailable { get; protected set; }
        public ulong BIFRewardMessage { get; protected set; }
        public ulong PowerTooltipBodyTeamUpLocked { get; protected set; }
        public ulong PowerTooltipBodyTeamUpUnlocked { get; protected set; }
        public int InfinityNotificationThreshold { get; protected set; }
        public ulong HelpTextConsole { get; protected set; }
        public ulong MessageRegionNotDownloaded { get; protected set; }
    }

    public class UINotificationGlobalsPrototype : Prototype
    {
        public ulong NotificationPartyInvite { get; protected set; }
        public ulong NotificationLevelUp { get; protected set; }
        public ulong NotificationServerMessage { get; protected set; }
        public ulong NotificationRemoteMission { get; protected set; }
        public ulong NotificationMissionUpdate { get; protected set; }
        public ulong NotificationMatchInvite { get; protected set; }
        public ulong NotificationMatchQueue { get; protected set; }
        public ulong NotificationMatchGroupInvite { get; protected set; }
        public ulong NotificationPvPShop { get; protected set; }
        public ulong NotificationPowerPointsAwarded { get; protected set; }
        public int NotificationPartyAIAggroRange { get; protected set; }
        public ulong NotificationOfferingUI { get; protected set; }
        public ulong NotificationGuildInvite { get; protected set; }
        public ulong NotificationMetaGameInfo { get; protected set; }
        public ulong NotificationLegendaryMission { get; protected set; }
        public ulong NotificationMatchPending { get; protected set; }
        public ulong NotificationMatchGroupPending { get; protected set; }
        public ulong NotificationMatchWaitlisted { get; protected set; }
        public ulong NotificationLegendaryQuestShare { get; protected set; }
        public ulong NotificationSynergyPoints { get; protected set; }
        public ulong NotificationPvPScoreboard { get; protected set; }
        public ulong NotificationOmegaPoints { get; protected set; }
        public ulong NotificationTradeInvite { get; protected set; }
        public ulong NotificationMatchLocked { get; protected set; }
        public ulong NotificationLoginReward { get; protected set; }
        public ulong NotificationMatchGracePeriod { get; protected set; }
        public ulong NotificationPartyKickGracePeriod { get; protected set; }
        public ulong NotificationGiftReceived { get; protected set; }
        public ulong NotificationLeaderboardRewarded { get; protected set; }
        public ulong NotificationCouponReceived { get; protected set; }
        public ulong NotificationPublicEvent { get; protected set; }
    }

    public class UIMapGlobalsPrototype : Prototype
    {
        public float DefaultRevealRadius { get; protected set; }
        public float DefaultZoom { get; protected set; }
        public float FullScreenMapAlphaMax { get; protected set; }
        public float FullScreenMapAlphaMin { get; protected set; }
        public int FullScreenMapResolutionHeight { get; protected set; }
        public int FullScreenMapResolutionWidth { get; protected set; }
        public float FullScreenMapScale { get; protected set; }
        public float LowResRevealMultiplier { get; protected set; }
        public ulong MapColorFiller { get; protected set; }
        public ulong MapColorWalkable { get; protected set; }
        public ulong MapColorWall { get; protected set; }
        public float MiniMapAlpha { get; protected set; }
        public int MiniMapResolution { get; protected set; }
        public float CameraAngleX { get; protected set; }
        public float CameraAngleY { get; protected set; }
        public float CameraAngleZ { get; protected set; }
        public float CameraFOV { get; protected set; }
        public float CameraNearPlane { get; protected set; }
        public float FullScreenMapPOISize { get; protected set; }
        public float POIScreenFacingRot { get; protected set; }
        public bool DrawPOIInCanvas { get; protected set; }
        public bool EnableMinimapProjection { get; protected set; }
        public float DefaultZoomMin { get; protected set; }
        public float DefaultZoomMax { get; protected set; }
        public float MiniMapPOISizeMin { get; protected set; }
        public float MiniMapPOISizeMax { get; protected set; }
        public ulong MapColorFillerConsole { get; protected set; }
        public ulong MapColorWalkableConsole { get; protected set; }
        public ulong MapColorWallConsole { get; protected set; }
        public float DefaultZoomConsole { get; protected set; }
        public float DefaultZoomMinConsole { get; protected set; }
        public float DefaultZoomMaxConsole { get; protected set; }
        public float MiniMapPOISizeMinConsole { get; protected set; }
        public float MiniMapPOISizeMaxConsole { get; protected set; }
        public float MiniMapAlphaConsole { get; protected set; }
    }

    public class MetricsFrequencyPrototype : Prototype
    {
        public float SampleRate { get; protected set; }
    }

    public class CameraSettingPrototype : Prototype
    {
        public float DirectionX { get; protected set; }
        public float DirectionY { get; protected set; }
        public float DirectionZ { get; protected set; }
        public float Distance { get; protected set; }
        public float FieldOfView { get; protected set; }
        public float ListenerDistance { get; protected set; }
        public int RotationPitch { get; protected set; }
        public int RotationRoll { get; protected set; }
        public int RotationYaw { get; protected set; }
        public float LookAtOffsetX { get; protected set; }
        public float LookAtOffsetY { get; protected set; }
        public float LookAtOffsetZ { get; protected set; }
        public bool AllowCharacterSpecificZOffset { get; protected set; }
        public bool OrbitalCam { get; protected set; }
        public float OrbitalFocusAngle { get; protected set; }
        public float OrbitalFocusPosX { get; protected set; }
        public float OrbitalFocusPosY { get; protected set; }
    }

    public class CameraSettingCollectionPrototype : Prototype
    {
        public CameraSettingPrototype[] CameraSettings { get; protected set; }
        public CameraSettingPrototype[] CameraSettingsFlying { get; protected set; }
        public int CameraStartingIndex { get; protected set; }
        public bool CameraAllowCustomMaxZoom { get; protected set; }
    }

    public class GlobalPropertiesPrototype : Prototype
    {
        public ulong Properties { get; protected set; }
    }

    public class PowerVisualsGlobalsPrototype : Prototype
    {
        public ulong DailyMissionCompleteClass { get; protected set; }
        public ulong UnlockPetTechR1CommonClass { get; protected set; }
        public ulong UnlockPetTechR2UncommonClass { get; protected set; }
        public ulong UnlockPetTechR3RareClass { get; protected set; }
        public ulong UnlockPetTechR4EpicClass { get; protected set; }
        public ulong UnlockPetTechR5CosmicClass { get; protected set; }
        public ulong LootVaporizedClass { get; protected set; }
        public ulong AchievementUnlockedClass { get; protected set; }
        public ulong OmegaPointGainedClass { get; protected set; }
        public ulong AvatarLeashTeleportClass { get; protected set; }
        public ulong InfinityTimePointEarnedClass { get; protected set; }
        public ulong InfinitySpacePointEarnedClass { get; protected set; }
        public ulong InfinitySoulPointEarnedClass { get; protected set; }
        public ulong InfinityMindPointEarnedClass { get; protected set; }
        public ulong InfinityRealityPointEarnedClass { get; protected set; }
        public ulong InfinityPowerPointEarnedClass { get; protected set; }
    }

    public class RankDefaultEntryPrototype : Prototype
    {
        public ulong Data { get; protected set; }
        public Rank Rank { get; protected set; }
    }

    public class PopulationGlobalsPrototype : Prototype
    {
        public ulong MessageEnemiesGrowStronger { get; protected set; }
        public ulong MessageEnemiesGrowWeaker { get; protected set; }
        public int SpawnMapPoolTickMS { get; protected set; }
        public int SpawnMapLevelTickMS { get; protected set; }
        public float CrowdSupressionRadius { get; protected set; }
        public bool SupressSpawnOnPlayer { get; protected set; }
        public int SpawnMapGimbalRadius { get; protected set; }
        public int SpawnMapHorizon { get; protected set; }
        public float SpawnMapMaxChance { get; protected set; }
        public ulong EmptyPopulation { get; protected set; }
        public ulong TwinEnemyBoost { get; protected set; }
        public int DestructiblesForceSpawnMS { get; protected set; }
        public ulong TwinEnemyCondition { get; protected set; }
        public int SpawnMapHeatPerSecondMax { get; protected set; }
        public int SpawnMapHeatPerSecondMin { get; protected set; }
        public int SpawnMapHeatPerSecondScalar { get; protected set; }
        public ulong TwinEnemyRank { get; protected set; }
        public RankDefaultEntryPrototype[] RankDefaults { get; protected set; }
    }

    public class ClusterConfigurationGlobalsPrototype : Prototype
    {
        public int MinutesToKeepOfflinePlayerGames { get; protected set; }
        public int MinutesToKeepUnusedRegions { get; protected set; }
        public bool HotspotCheckLOSInTown { get; protected set; }
        public int HotspotCheckTargetIntervalMS { get; protected set; }
        public int HotspotCheckTargetTownIntervalMS { get; protected set; }
        public int PartyKickGracePeriodMS { get; protected set; }
        public int QueueReservationGracePeriodMS { get; protected set; }
    }

    public class CombatGlobalsPrototype : Prototype
    {
        public float PowerDmgBonusHardcoreAttenuation { get; protected set; }
        public int MouseHoldStartMoveDelayMeleeMS { get; protected set; }
        public int MouseHoldStartMoveDelayRangedMS { get; protected set; }
        public float CriticalForceApplicationChance { get; protected set; }
        public float CriticalForceApplicationMag { get; protected set; }
        public float EnduranceCostChangePctMin { get; protected set; }
        public EvalPrototype EvalBlockChanceFormula { get; protected set; }
        public ulong EvalInterruptChanceFormula { get; protected set; }
        public ulong EvalNegStatusResistPctFormula { get; protected set; }
        public ulong ChannelInterruptCondition { get; protected set; }
        public EvalPrototype EvalDamageReduction { get; protected set; }
        public EvalPrototype EvalCritChanceFormula { get; protected set; }
        public EvalPrototype EvalSuperCritChanceFormula { get; protected set; }
        public EvalPrototype EvalDamageRatingFormula { get; protected set; }
        public EvalPrototype EvalCritDamageRatingFormula { get; protected set; }
        public EvalPrototype EvalDodgeChanceFormula { get; protected set; }
        public EvalPrototype EvalDamageReductionDefenseOnly { get; protected set; }
        public EvalPrototype EvalDamageReductionForDisplay { get; protected set; }
        public float TravelPowerMaxSpeed { get; protected set; }
        public ulong TUSynergyBonusPerLvl { get; protected set; }
        public ulong TUSynergyBonusPerMaxLvlTU { get; protected set; }
    }

    public class VendorXPCapInfoPrototype : Prototype
    {
        public ulong Vendor { get; protected set; }
        public int Cap { get; protected set; }
        public float WallClockTime24Hr { get; protected set; }
        public Weekday WallClockTimeDay { get; protected set; }
    }

    public class AffixCategoryTableEntryPrototype : Prototype
    {
        public ulong Category { get; protected set; }
        public ulong[] Affixes { get; protected set; }
    }

    public class LootGlobalsPrototype : Prototype
    {
        public ulong LootBonusRarityCurve { get; protected set; }
        public ulong LootBonusSpecialCurve { get; protected set; }
        public ulong LootContainerKeyword { get; protected set; }
        public float LootDropScalar { get; protected set; }
        public int LootInitializationLevelOffset { get; protected set; }
        public ulong LootLevelDistribution { get; protected set; }
        public float LootRarityScalar { get; protected set; }
        public float LootSpecialItemFindScalar { get; protected set; }
        public float LootUnrestedSpecialFindScalar { get; protected set; }
        public float LootUsableByRecipientPercent { get; protected set; }
        public ulong NoLootTable { get; protected set; }
        public ulong SpecialOnKilledLootTable { get; protected set; }
        public int SpecialOnKilledLootCooldownHours { get; protected set; }
        public ulong RarityCosmic { get; protected set; }
        public ulong LootBonusFlatCreditsCurve { get; protected set; }
        public ulong RarityUruForged { get; protected set; }
        public ulong LootTableBlueprint { get; protected set; }
        public ulong RarityUnique { get; protected set; }
        public int LootLevelMaxForDrops { get; protected set; }
        public ulong InsigniaBlueprint { get; protected set; }
        public ulong UniquesBoxCheatItem { get; protected set; }
        public ulong[] EmptySocketAffixes { get; protected set; }
        public ulong GemBlueprint { get; protected set; }
        public VendorXPCapInfoPrototype[] VendorXPCapInfo { get; protected set; }
        public float DropDistanceThreshold { get; protected set; }
        public AffixCategoryTableEntryPrototype[] AffixCategoryTable { get; protected set; }
        public ulong BonusItemFindCurve { get; protected set; }
        public int BonusItemFindNumPointsForBonus { get; protected set; }
        public ulong BonusItemFindLootTable { get; protected set; }
        public float LootCoopPlayerRewardPct { get; protected set; }
        public ulong RarityDefault { get; protected set; }
    }

    public class MatchQueueStringEntryPrototype : Prototype
    {
        public MatchQueueStatus StatusKey { get; protected set; }  // Regions/QueueStatus.type, also appears in the protocol
        public ulong StringLog { get; protected set; }
        public ulong StringStatus { get; protected set; }
    }

    public class TransitionGlobalsPrototype : Prototype
    {
        public RegionPortalControlEntryPrototype[] ControlledRegions { get; protected set; }
        public ulong EnabledState { get; protected set; }
        public ulong DisabledState { get; protected set; }
        public MatchQueueStringEntryPrototype[] QueueStrings { get; protected set; }
        public ulong TransitionEmptyClass { get; protected set; }
    }

    public class KeywordGlobalsPrototype : Prototype
    {
        public ulong PowerKeywordPrototype { get; protected set; }
        public ulong DestructibleKeyword { get; protected set; }
        public ulong PetPowerKeyword { get; protected set; }
        public ulong VacuumableKeyword { get; protected set; }
        public ulong EntityKeywordPrototype { get; protected set; }
        public ulong BodysliderPowerKeyword { get; protected set; }
        public ulong OrbEntityKeyword { get; protected set; }
        public ulong UltimatePowerKeyword { get; protected set; }
        public ulong MeleePowerKeyword { get; protected set; }
        public ulong RangedPowerKeyword { get; protected set; }
        public ulong BasicPowerKeyword { get; protected set; }
        public ulong TeamUpSpecialPowerKeyword { get; protected set; }
        public ulong TeamUpDefaultPowerKeyword { get; protected set; }
        public ulong StealthPowerKeyword { get; protected set; }
        public ulong TeamUpAwayPowerKeyword { get; protected set; }
        public ulong VanityPetKeyword { get; protected set; }
        public ulong ControlPowerKeywordPrototype { get; protected set; }
        public ulong AreaPowerKeyword { get; protected set; }
        public ulong EnergyPowerKeyword { get; protected set; }
        public ulong MentalPowerKeyword { get; protected set; }
        public ulong PhysicalPowerKeyword { get; protected set; }
        public ulong MedKitKeyword { get; protected set; }
        public ulong OrbExperienceEntityKeyword { get; protected set; }
        public ulong TutorialRegionKeyword { get; protected set; }
        public ulong TeamUpKeyword { get; protected set; }
        public ulong MovementPowerKeyword { get; protected set; }
        public ulong ThrownPowerKeyword { get; protected set; }
        public ulong HoloSimKeyword { get; protected set; }
        public ulong ControlledSummonDurationKeyword { get; protected set; }
        public ulong TreasureRoomKeyword { get; protected set; }
        public ulong DangerRoomKeyword { get; protected set; }
        public ulong StealingPowerKeyword { get; protected set; }
        public ulong SummonPowerKeyword { get; protected set; }
    }

    public class CurrencyGlobalsPrototype : Prototype
    {
        public ulong CosmicWorldstones { get; protected set; }
        public ulong Credits { get; protected set; }
        public ulong CubeShards { get; protected set; }
        public ulong EternitySplinters { get; protected set; }
        public ulong EyesOfDemonfire { get; protected set; }
        public ulong HeartsOfDemonfire { get; protected set; }
        public ulong LegendaryMarks { get; protected set; }
        public ulong OmegaFiles { get; protected set; }
        public ulong PvPCrowns { get; protected set; }
        public ulong ResearchDrives { get; protected set; }
        public ulong GenoshaRaid { get; protected set; }
        public ulong DangerRoomMerits { get; protected set; }
        public ulong GazillioniteGs { get; protected set; }
    }

    public class GamepadInputAssetPrototype : Prototype
    {
        public GamepadInput Input { get; protected set; }
        public ulong DualShockPath { get; protected set; }
        public ulong XboxPath { get; protected set; }
    }

    public class GamepadSlotBindingPrototype : Prototype
    {
        public int OrbisSlotNumber { get; protected set; }
        public int PCSlotNumber { get; protected set; }
        public int SlotNumber { get; protected set; }
    }

    public class GamepadGlobalsPrototype : Prototype
    {
        public int GamepadDialogAcceptTimerMS { get; protected set; }
        public float GamepadMaxTargetingRange { get; protected set; }
        public float GamepadTargetingHalfAngle { get; protected set; }
        public float GamepadTargetingDeflectionCost { get; protected set; }
        public float GamepadTargetingPriorityCost { get; protected set; }
        public int UltimateActivationTimeoutMS { get; protected set; }
        public GamepadInputAssetPrototype[] InputAssets { get; protected set; }
        public float GamepadInteractionHalfAngle { get; protected set; }
        public float DisableInteractDangerRadius { get; protected set; }
        public int GamepadInteractRange { get; protected set; }
        public float GamepadInteractionOffset { get; protected set; }
        public float GamepadTargetLockAssistHalfAngle { get; protected set; }
        public float GamepadTargetLockAssistDflctCost { get; protected set; }
        public float GamepadInteractBoundsIncrease { get; protected set; }
        public float GamepadTargetLockDropRadius { get; protected set; }
        public int GamepadTargetLockDropTimeMS { get; protected set; }
        public GamepadSlotBindingPrototype[] GamepadSlotBindings { get; protected set; }
        public float GamepadMeleeMoveIntoRangeDist { get; protected set; }
        public float GamepadMeleeMoveIntoRangeSpeed { get; protected set; }
        public float GamepadAutoTargetLockRadius { get; protected set; }
        public float GamepadDestructTargetDeflctCost { get; protected set; }
        public float GamepadDestructTargetHalfAngle { get; protected set; }
        public float GamepadDestructTargetRange { get; protected set; }
    }

    public class ConsoleGlobalsPrototype : Prototype
    {
        public ulong OrbisDefaultSessionDescription { get; protected set; }
        public ulong OrbisDefaultSessionImage { get; protected set; }
        public int OrbisMaxSessionSize { get; protected set; }
        public int MaxSuggestedPlayers { get; protected set; }
        public ulong OrbisPlayerCameraSettings { get; protected set; }
        public ulong OrbisFriendsInvitationDialogDesc { get; protected set; }
        public int OrbisMaxFriendInvites { get; protected set; }
        public ulong OrbisFriendsSuggestionDialogDesc { get; protected set; }
        public int OrbisMaxFriendSuggestions { get; protected set; }
    }

    public class AvatarOnKilledInfoPrototype : Prototype
    {
        public DeathReleaseBehavior DeathReleaseBehavior { get; protected set; }
        public ulong DeathReleaseButton { get; protected set; }
        public ulong DeathReleaseDialogMessage { get; protected set; }
        public int DeathReleaseTimeoutMS { get; protected set; }
        public ulong ResurrectionDialogMessage { get; protected set; }
        public int ResurrectionTimeoutMS { get; protected set; }
        public int RespawnLockoutMS { get; protected set; }
    }

    public class GlobalEventPrototype : Prototype
    {
        public bool Active { get; protected set; }
        public ulong[] CriteriaList { get; protected set; }
        public GlobalEventCriteriaLogic CriteriaLogic { get; protected set; }
        public ulong DisplayName { get; protected set; }
        public int LeaderboardLength { get; protected set; }
    }

    public class GlobalEventCriteriaPrototype : Prototype
    {
        public ulong DisplayColor { get; protected set; }
        public ulong DisplayName { get; protected set; }
        public int Score { get; protected set; }
        public int ThresholdCount { get; protected set; }
        public ulong DisplayTooltip { get; protected set; }
    }

    public class GlobalEventCriteriaItemCollectPrototype : GlobalEventCriteriaPrototype
    {
        public EntityFilterPrototype ItemFilter { get; protected set; }
    }
}
