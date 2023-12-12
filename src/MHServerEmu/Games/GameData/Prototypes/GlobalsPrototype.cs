
namespace MHServerEmu.Games.GameData.Prototypes
{
    public class GlobalsPrototype : Prototype
    {
        public ulong AdvancementGlobals;
        public ulong AvatarSwapChannelPower;
        public ulong ConnectionMarkerPrototype;
        public ulong DebugGlobals;
        public ulong UIGlobals;
        public ulong DefaultPlayer;
        public ulong DefaultStartTarget;
        public ulong[] PVPAlliances;
        public float HighFlyingHeight;
        public float LowHealthTrigger;
        public float MouseHitCollisionMultiplier;
        public float MouseHitMovingTargetsIncrease;
        public float MouseHitPowerTargetSearchDist;
        public float MouseHitPreferredAddition;
        public float MouseMovementNoPathRadius;
        public ulong MissionGlobals;
        public int TaggingResetDurationMS;
        public int PlayerPartyMaxSize;
        public float NaviBudgetBaseCellSizeWidth;
        public float NaviBudgetBaseCellSizeLength;
        public int NaviBudgetBaseCellMaxPoints;
        public int NaviBudgetBaseCellMaxEdges;
        public ulong[] UIConfigFiles;
        public int InteractRange;
        public ulong CreditsItemPrototype;
        public ulong NegStatusEffectList;
        public ulong PvPPrototype;
        public ulong MissionPrototype;
        public EvalPrototype ItemPriceMultiplierBuyFromVendor;
        public EvalPrototype ItemPriceMultiplierSellToVendor;
        public ModGlobalsPrototype ModGlobals;
        public float MouseMoveDrivePathMaxLengthMult;
        public ulong AudioGlobalEventsClass;
        public ulong MetaGamePrototype;
        public int MobLOSVisUpdatePeriodMS;
        public int MobLOSVisStayVisibleDelayMS;
        public bool MobLOSVisEnabled;
        public ulong BeginPlayAssetTypes;
        public ulong CachedAssetTypes;
        public ulong FileVerificationAssetTypes;
        public ulong LoadingMusic;
        public ulong SystemLocalized;
        public ulong PopulationGlobals;
        public ulong PlayerAlliance;
        public ulong ClusterConfigurationGlobals;
        public ulong DownloadChunks;
        public ulong UIItemInventory;
        public ulong AIGlobals;
        public ulong MusicAssetType;
        public ulong ResurrectionDefaultInfo;
        public ulong PartyJoinPortal;
        public ulong MatchJoinPortal;
        public ulong MovieAssetType;
        public ulong WaypointGraph;
        public ulong WaypointHotspot;
        public float MouseHoldDeadZoneRadius;
        public GlobalPropertiesPrototype Properties;
        public int PlayerGracePeroidInSeconds;
        public ulong CheckpointHotspot;
        public ulong ReturnToHubPower;
        public int DisableEndurRegenOnPowerEndMS;
        public ulong PowerPrototype;
        public ulong WorldEntityPrototype;
        public ulong AreaPrototype;
        public ulong PopulationObjectPrototype;
        public ulong RegionPrototype;
        public ulong AmbientSfxType;
        public ulong CombatGlobals;
        public float OrientForPowerMaxTimeSecs;
        public ulong KismetSequenceEntityPrototype;
        public ulong DynamicArea;
        public ulong ReturnToFieldPower;
        public float AssetCacheCellLoadOutRunSeconds;
        public int AssetCacheMRUSize;
        public int AssetCachePrefetchMRUSize;
        public ulong AvatarSwapInPower;
        public ulong PlayerStartingFaction;
        public ulong VendorBuybackInventory;
        public ulong AnyAlliancePrototype;
        public ulong AnyFriendlyAlliancePrototype;
        public ulong AnyHostileAlliancePrototype;
        public ulong ExperienceBonusCurve;
        public ulong TransitionGlobals;
        public int PlayerGuildMaxSize;
        public bool AutoPartyEnabledInitially;
        public ulong ItemBindingAffix;
        public int InteractFallbackRange;
        public ulong ItemAcquiredThroughMTXStoreAffix;
        public ulong TeleportToPartyMemberPower;
        public ulong AvatarSwapOutPower;
        public int KickIdlePlayerTimeSecs;
        public ulong PlayerCameraSettings;
        public ulong AvatarSynergyCondition;
        public ulong MetaGameLocalized;
        public ulong MetaGameTeamDefault;
        public ulong ItemNoVisualsAffix;
        public int AvatarSynergyConcurrentLimit;
        public ulong LootGlobals;
        public ulong MetaGameTeamBase;
        public ulong AudioGlobals;
        public int PlayerRaidMaxSize;
        public int TimeZone;
        public ulong TeamUpSummonPower;
        public int AssistPvPDurationMS;
        public ulong FulfillmentReceiptPrototype;
        public ulong PetTechVacuumPower;
        public ulong StolenPowerRestrictions;
        public ulong PowerVisualsGlobals;
        public ulong KeywordGlobals;
        public ulong CurrencyGlobals;
        public ulong PointerArrowTemplate;
        public ulong ObjectiveMarkerTemplate;
        public int VaporizedLootLifespanMS;
        public ulong CookedIconAssetTypes;
        public ulong LiveTuneAvatarXPDisplayCondition;
        public ulong LiveTuneCreditsDisplayCondition;
        public ulong LiveTuneRegionXPDisplayCondition;
        public ulong LiveTuneRIFDisplayCondition;
        public ulong LiveTuneSIFDisplayCondition;
        public ulong LiveTuneXPDisplayCondition;
        public ulong ItemLinkInventory;
        public ulong LimitedEditionBlueprint;
        public ulong MobileIconAssetTypes;
        public ulong PetItemBlueprint;
        public ulong AvatarPrototype;
        public int ServerBonusUnlockLevel;
        public ulong GamepadGlobals;
        public ulong CraftingRecipeLibraryInventory;
        public ulong ConditionPrototype;
        public ulong[] LiveTuneServerConditions;
        public ulong DefaultStartingAvatarPrototype;
        public ulong DefaultStartTargetFallbackRegion;
        public ulong DefaultStartTargetPrestigeRegion;
        public ulong DefaultStartTargetStartingRegion;
        public ulong DifficultyGlobals;
        public ulong PublicEventPrototype;
        public ulong AvatarCoopStartPower;
        public ulong AvatarCoopEndPower;
        public DifficultyTierPrototype DifficultyTiers;
        public ulong DefaultLoadingLobbyRegion;
        public ulong DifficultyTierDefault;
        public ulong AvatarHealPower;
        public ulong ConsoleGlobals;
        public ulong TeamUpSynergyCondition;
        public ulong MetricsFrequencyPrototype;
        public ulong ConsumableItemBlueprint;
        public int AvatarCoopInactiveTimeMS;
        public int AvatarCoopInactiveOnDeadBufferMS;
        public GlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GlobalsPrototype), proto); }
    }

    public class LoginRewardPrototype : Prototype
    {
        public int Day;
        public ulong Item;
        public ulong TooltipText;
        public ulong LogoffPanelEntry;
        public LoginRewardPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LoginRewardPrototype), proto); }
    }

    public class PrestigeLevelPrototype : Prototype
    {
        public ulong TextStyle;
        public ulong Reward;
        public PrestigeLevelPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PrestigeLevelPrototype), proto); }
    }

    public class PetTechAffixInfoPrototype : Prototype
    {
        public AffixPosition Position;
        public ulong ItemRarityToConsume;
        public int ItemsRequiredToUnlock;
        public ulong LockedDescriptionText;
        public PetTechAffixInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PetTechAffixInfoPrototype), proto); }
    }

    public class AdvancementGlobalsPrototype : Prototype
    {
        public ulong LevelingCurve;
        public ulong DeathPenaltyCost;
        public ulong ItemEquipRequirementOffset;
        public ulong VendorLevelingCurve;
        public ulong StatsEval;
        public ulong AvatarThrowabilityEval;
        public EvalPrototype VendorLevelingEval;
        public EvalPrototype VendorRollTableLevelEval;
        public float RestedHealthPerMinMult;
        public int PowerBoostMax;
        public PrestigeLevelPrototype PrestigeLevels;
        public ulong ItemAffixLevelingCurve;
        public ulong ExperienceBonusAvatarSynergy;
        public float ExperienceBonusAvatarSynergyMax;
        public int OriginalMaxLevel;
        public ulong ExperienceBonusLevel60Synergy;
        public int TeamUpPowersPerTier;
        public ulong TeamUpPowerTiersCurve;
        public OmegaBonusSetPrototype OmegaBonusSets;
        public int OmegaPointsCap;
        public int OmegaSystemLevelUnlock;
        public PetTechAffixInfoPrototype[] PetTechAffixInfo;
        public ulong PetTechDonationItemPrototype;
        public int AvatarPowerSpecsMax;
        public ulong PctXPFromPrestigeLevelCurve;
        public int StarterAvatarLevelCap;
        public ulong TeamUpLevelingCurve;
        public int TeamUpPowerSpecsMax;
        public ulong PctXPFromLevelDeltaCurve;
        public int InfinitySystemUnlockLevel;
        public long InfinityPointsCapPerGem;
        public InfinityGemSetPrototype InfinityGemSets;
        public long InfinityXPCap;
        public int TravelPowerUnlockLevel;
        public float ExperienceBonusCoop;
        public ulong CoopInactivityExperienceScalar;
        public AdvancementGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AdvancementGlobalsPrototype), proto); }
    }

    public class AIGlobalsPrototype : Prototype
    {
        public ulong LeashReturnHeal;
        public ulong LeashReturnImmunity;
        public ulong LeashingProceduralProfile;
        public int RandomThinkVarianceMS;
        public int ControlledAgentResurrectTimerMS;
        public ulong ControlledAlliance;
        public float OrbAggroRangeMax;
        public ulong OrbAggroRangeBonusCurve;
        public ulong DefaultSimpleNpcBrain;
        public ulong CantBeControlledKeyword;
        public int ControlledAgentSummonDurationMS;
        public AIGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIGlobalsPrototype), proto); }
    }

    public class MusicStatePrototype : Prototype
    {
        public ulong StateGroupName;
        public ulong StateName;
        public MusicStateEndBehavior EndBehavior;
        public MusicStatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MusicStatePrototype), proto); }
    }
    public enum MusicStateEndBehavior
    {
        DoNothing = 0,
        PlayDefaultList = 1,
        StopMusic = 2,
    }
    public class AudioGlobalsPrototype : Prototype
    {
        public int DefaultMemPoolSizeMB;
        public int LowerEngineMemPoolSizeMB;
        public int StreamingMemPoolSizeMB;
        public int MemoryBudgetMB;
        public int MemoryBudgetDevMB;
        public float MaxAnimNotifyRadius;
        public float MaxMocoVolumeDB;
        public float FootstepNotifyMinFpsThreshold;
        public float BossCritBanterHealthPctThreshold;
        public int LongDownTimeInDaysThreshold;
        public float EncounterCheckRadius;
        public AudioGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AudioGlobalsPrototype), proto); }
    }

    public class DebugGlobalsPrototype : Prototype
    {
        public ulong CreateEntityShortcutEntity;
        public ulong DynamicRegion;
        public float HardModeMobDmgBuff;
        public float HardModeMobHealthBuff;
        public float HardModeMobMoveSpdBuff;
        public float HardModePlayerEnduranceCostDebuff;
        public ulong PowersArtModeEntity;
        public int StartingLevelMobs;
        public ulong TransitionRef;
        public ulong CreateLootDummyEntity;
        public ulong MapErrorMapInfo;
        public bool IgnoreDeathPenalty;
        public bool TrashedItemsDropInWorld;
        public ulong PAMEnemyAlliance;
        public EvalPrototype DebugEval;
        public EvalPrototype DebugEvalUnitTest;
        public BotSettingsPrototype BotSettings;
        public ulong ReplacementTestingResultItem;
        public ulong ReplacementTestingTriggerItem;
        public ulong VendorEternitySplinterLoot;
        public DebugGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DebugGlobalsPrototype), proto); }
    }

    public class CharacterSheetDetailedStatPrototype : Prototype
    {
        public EvalPrototype Expression;
        public ulong ExpressionExt;
        public ulong Format;
        public ulong Label;
        public ulong Tooltip;
        public ulong Icon;
        public CharacterSheetDetailedStatPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CharacterSheetDetailedStatPrototype), proto); }
    }

    public class HelpGameTermPrototype : Prototype
    {
        public ulong Name;
        public ulong Description;
        public HelpGameTermPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HelpGameTermPrototype), proto); }
    }
    public enum CoopOp
    {
        StartForSlot = 0,
        EndForSlot = 1,
    }
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
    public class CoopOpUIDataEntryPrototype : Prototype
    {
        public CoopOp Op;
        public CoopOpResult Result;
        public ulong SystemMessage;
        public ulong SystemMessageTemplate;
        public ulong BannerMessage;
        public CoopOpUIDataEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CoopOpUIDataEntryPrototype), proto); }
    }

    public class HelpTextPrototype : Prototype
    {
        public ulong GeneralControls;
        public HelpGameTermPrototype[] GameTerms;
        public ulong Crafting;
        public ulong EndgamePvE;
        public ulong PvP;
        public ulong Tutorial;
        public HelpTextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HelpTextPrototype), proto); }
    }

    public class AffixRollQualityPrototype : Prototype
    {
        public TextStylePrototype Style;
        public float PercentThreshold;
        public AffixRollQualityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixRollQualityPrototype), proto); }
    }

    public class UIGlobalsPrototype : Prototype
    {
        public ulong MessageDefault;
        public ulong MessageLevelUp;
        public ulong MessageItemError;
        public ulong MessageRegionChange;
        public ulong MessageMissionAccepted;
        public ulong MessageMissionCompleted;
        public ulong MessageMissionFailed;
        public int AvatarSwitchUIDeathDelayMS;
        public ulong UINotificationGlobals;
        public int RosterPageSize;
        public ulong LocalizedInfoDirectory;
        public int TooltipHideDelayMS;
        public ulong MessagePowerError;
        public ulong MessageWaypointError;
        public ulong UIStringGlobals;
        public ulong MessagePartyInvite;
        public ulong MapInfoMissionGiver;
        public ulong MapInfoMissionObjectiveTalk;
        public int NumAvatarsToDisplayInItemUsableLists;
        public ulong LoadingScreens;
        public int ChatFadeInMS;
        public int ChatBeginFadeOutMS;
        public int ChatFadeOutMS;
        public ulong MessageWaypointUnlocked;
        public ulong MessagePowerUnlocked;
        public ulong UIMapGlobals;
        public ulong TextStyleCurrentlyEquipped;
        public int ChatTextFadeOutMS;
        public int ChatTextHistoryMax;
        public ulong KeywordFemale;
        public ulong KeywordMale;
        public ulong TextStylePowerUpgradeImprovement;
        public ulong TextStylePowerUpgradeNoImprovement;
        public ulong LoadingScreenIntraRegion;
        public ulong TextStyleVendorPriceCanBuy;
        public ulong TextStyleVendorPriceCantBuy;
        public ulong TextStyleItemRestrictionFailure;
        public int CostumeClosetNumAvatarsVisible;
        public int CostumeClosetNumCostumesVisible;
        public ulong MessagePowerErrorDoNotQueue;
        public ulong TextStylePvPShopPurchased;
        public ulong TextStylePvPShopUnpurchased;
        public ulong MessagePowerPointsAwarded;
        public ulong MapInfoMissionObjectiveUse;
        public ulong TextStyleMissionRewardFloaty;
        public ulong PowerTooltipBodyCurRank0Unlkd;
        public ulong PowerTooltipBodyCurRankLocked;
        public ulong PowerTooltipBodyCurRank1AndUp;
        public ulong PowerTooltipBodyNextRank1First;
        public ulong PowerTooltipBodyNextRank2AndUp;
        public ulong PowerTooltipHeader;
        public ulong MapInfoFlavorNPC;
        public int TooltipSpawnHideDelayMS;
        public int KioskIdleResetTimeSec;
        public ulong KioskSizzleMovie;
        public int KioskSizzleMovieStartTimeSec;
        public ulong MapInfoHealer;
        public ulong TextStyleOpenMission;
        public ulong MapInfoPartyMember;
        public int LoadingScreenTipTimeIntervalMS;
        public ulong TextStyleKillRewardFloaty;
        public ulong TextStyleAvatarOverheadNormal;
        public ulong TextStyleAvatarOverheadParty;
        public CharacterSheetDetailedStatPrototype[] CharacterSheetDetailedStats;
        public ulong PowerProgTableTabRefTab1;
        public ulong PowerProgTableTabRefTab2;
        public ulong PowerProgTableTabRefTab3;
        public float ScreenEdgeArrowRange;
        public ulong HelpText;
        public ulong MessagePvPFactionPortalFail;
        public ulong PropertyTooltipTextOverride;
        public ulong MessagePvPDisabledPortalFail;
        public ulong MessageStatProgression;
        public ulong MessagePvPPartyPortalFail;
        public ulong TextStyleMissionHudOpenMission;
        public ulong MapInfoAvatarDefeated;
        public ulong MapInfoPartyMemberDefeated;
        public ulong MessageGuildInvite;
        public ulong MapInfoMissionObjectiveMob;
        public ulong MapInfoMissionObjectivePortal;
        public ulong CinematicsListLoginScreen;
        public ulong TextStyleGuildLeader;
        public ulong TextStyleGuildOfficer;
        public ulong TextStyleGuildMember;
        public AffixDisplaySlotPrototype[] CostumeAffixDisplaySlots;
        public ulong MessagePartyError;
        public ulong MessageRegionRestricted;
        public ulong CreditsMovies;
        public ulong MessageMetaGameDefault;
        public ulong MessagePartyPvPPortalFail;
        public int ChatNewMsgDarkenBgMS;
        public ulong TextStyleKillZeroRewardFloaty;
        public ulong MessageAvatarSwitchError;
        public ulong TextStyleItemBlessed;
        public ulong TextStyleItemAffixLocked;
        public ulong MessageAlreadyInQueue;
        public ulong MessageOnlyPartyLeaderCanQueue;
        public ulong MessageTeleportTargetIsInMatch;
        public ulong PowerGrantItemTutorialTip;
        public ulong MessagePrivateDisallowedInRaid;
        public ulong MessageQueueNotAvailableInRaid;
        public ulong PowerTooltipBodyNextRank1Antireq;
        public ulong CosmicEquippedTutorialTip;
        public ulong MessageRegionDisabledPortalFail;
        public CharacterSheetDetailedStatPrototype[] TeamUpDetailedStats;
        public ulong MessageOmegaPointsAwarded;
        public ulong MetaGameWidgetMissionName;
        public UIConditionType[] BuffPageOrder;
        public ObjectiveTrackerPageType[] ObjectiveTrackerPageOrder;
        public ulong VanityTitleNoTitle;
        public ulong MessageStealablePowerOccupied;
        public ulong MessageStolenPowerDuplicate;
        public ulong CurrencyDisplayList;
        public ulong CinematicOpener;
        public ulong MessageCantQueueInQueueRegion;
        public int LogoffPanelStoryMissionLevelCap;
        public StoreCategoryPrototype[] MTXStoreCategories;
        public int GiftingAccessMinPlayerLevel;
        public ulong AffixRollRangeTooltipText;
        public ulong[] UISystemLockList;
        public ulong MessageUISystemUnlocked;
        public ulong TooltipInsigniaTeamAffiliations;
        public ulong PowerTooltipBodySpecLocked;
        public ulong PowerTooltipBodySpecUnlocked;
        public ulong PropertyValuePercentFormat;
        public ulong AffixStatDiffPositiveStyle;
        public ulong AffixStatDiffNegativeStyle;
        public ulong AffixStatDiffTooltipText;
        public ulong AffixStatDiffNeutralStyle;
        public ulong AffixStatFoundAffixStyle;
        public ulong[] StashTabCustomIcons;
        public ulong PropertyValueDefaultFormat;
        public ulong[] ItemSortCategoryList;
        public ulong[] ItemSortSubCategoryList;
        public AffixRollQualityPrototype[] AffixRollRangeRollQuality;
        public ulong RadialMenuEntriesList;
        public ulong TextStylePowerChargesEmpty;
        public ulong TextStylePowerChargesFull;
        public ulong MessageLeaderboardRewarded;
        public ulong GamepadIconDonateAction;
        public ulong GamepadIconDropAction;
        public ulong GamepadIconEquipAction;
        public ulong GamepadIconMoveAction;
        public ulong GamepadIconSelectAction;
        public ulong GamepadIconSellAction;
        public ulong ConsoleRadialMenuEntriesList;
        public CoopOpUIDataEntryPrototype[] CoopOpUIDatas;
        public ulong MessageOpenMissionEntered;
        public ulong MessageInfinityPointsAwarded;
        public ulong PowerTooltipBodyTalentLocked;
        public ulong PowerTooltipBodyTalentUnlocked;
        public ulong[] AffixTooltipOrder;
        public ulong PowerTooltipBodyCurRank1Only;
        public int InfinityMaxRanksHideThreshold;
        public ulong MessagePlayingAtLevelCap;
        public ulong GamepadIconRankDownAction;
        public ulong GamepadIconRankUpAction;
        public ulong MessageTeamUpDisabledCoop;
        public ulong MessageStolenPowerAvailable;
        public ulong BIFRewardMessage;
        public ulong PowerTooltipBodyTeamUpLocked;
        public ulong PowerTooltipBodyTeamUpUnlocked;
        public int InfinityNotificationThreshold;
        public ulong HelpTextConsole;
        public ulong MessageRegionNotDownloaded;
        public UIGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIGlobalsPrototype), proto); }
    }

    public enum ObjectiveTrackerPageType
    {
        SharedQuests = 0,
        EventMissions = 1,
        LegendaryMissions = 2,
        MetaGameMissions = 3,
        OpenMissions = 4,
        StoryMissions = 5,
    }

    public class UINotificationGlobalsPrototype : Prototype
    {
        public ulong NotificationPartyInvite;
        public ulong NotificationLevelUp;
        public ulong NotificationServerMessage;
        public ulong NotificationRemoteMission;
        public ulong NotificationMissionUpdate;
        public ulong NotificationMatchInvite;
        public ulong NotificationMatchQueue;
        public ulong NotificationMatchGroupInvite;
        public ulong NotificationPvPShop;
        public ulong NotificationPowerPointsAwarded;
        public int NotificationPartyAIAggroRange;
        public ulong NotificationOfferingUI;
        public ulong NotificationGuildInvite;
        public ulong NotificationMetaGameInfo;
        public ulong NotificationLegendaryMission;
        public ulong NotificationMatchPending;
        public ulong NotificationMatchGroupPending;
        public ulong NotificationMatchWaitlisted;
        public ulong NotificationLegendaryQuestShare;
        public ulong NotificationSynergyPoints;
        public ulong NotificationPvPScoreboard;
        public ulong NotificationOmegaPoints;
        public ulong NotificationTradeInvite;
        public ulong NotificationMatchLocked;
        public ulong NotificationLoginReward;
        public ulong NotificationMatchGracePeriod;
        public ulong NotificationPartyKickGracePeriod;
        public ulong NotificationGiftReceived;
        public ulong NotificationLeaderboardRewarded;
        public ulong NotificationCouponReceived;
        public ulong NotificationPublicEvent;
        public UINotificationGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UINotificationGlobalsPrototype), proto); }
    }

    public class UIMapGlobalsPrototype : Prototype
    {
        public float DefaultRevealRadius;
        public float DefaultZoom;
        public float FullScreenMapAlphaMax;
        public float FullScreenMapAlphaMin;
        public int FullScreenMapResolutionHeight;
        public int FullScreenMapResolutionWidth;
        public float FullScreenMapScale;
        public float LowResRevealMultiplier;
        public ulong MapColorFiller;
        public ulong MapColorWalkable;
        public ulong MapColorWall;
        public float MiniMapAlpha;
        public int MiniMapResolution;
        public float CameraAngleX;
        public float CameraAngleY;
        public float CameraAngleZ;
        public float CameraFOV;
        public float CameraNearPlane;
        public float FullScreenMapPOISize;
        public float POIScreenFacingRot;
        public bool DrawPOIInCanvas;
        public bool EnableMinimapProjection;
        public float DefaultZoomMin;
        public float DefaultZoomMax;
        public float MiniMapPOISizeMin;
        public float MiniMapPOISizeMax;
        public ulong MapColorFillerConsole;
        public ulong MapColorWalkableConsole;
        public ulong MapColorWallConsole;
        public float DefaultZoomConsole;
        public float DefaultZoomMinConsole;
        public float DefaultZoomMaxConsole;
        public float MiniMapPOISizeMinConsole;
        public float MiniMapPOISizeMaxConsole;
        public float MiniMapAlphaConsole;
        public UIMapGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UIMapGlobalsPrototype), proto); }
    }

    public class MetricsFrequencyPrototype : Prototype
    {
        public float SampleRate;
        public MetricsFrequencyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetricsFrequencyPrototype), proto); }
    }

    public class CameraSettingPrototype : Prototype
    {
        public float DirectionX;
        public float DirectionY;
        public float DirectionZ;
        public float Distance;
        public float FieldOfView;
        public float ListenerDistance;
        public int RotationPitch;
        public int RotationRoll;
        public int RotationYaw;
        public float LookAtOffsetX;
        public float LookAtOffsetY;
        public float LookAtOffsetZ;
        public bool AllowCharacterSpecificZOffset;
        public bool OrbitalCam;
        public float OrbitalFocusAngle;
        public float OrbitalFocusPosX;
        public float OrbitalFocusPosY;
        public CameraSettingPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CameraSettingPrototype), proto); }
    }

    public class CameraSettingCollectionPrototype : Prototype
    {
        public CameraSettingPrototype[] CameraSettings;
        public CameraSettingPrototype[] CameraSettingsFlying;
        public int CameraStartingIndex;
        public bool CameraAllowCustomMaxZoom;
        public CameraSettingCollectionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CameraSettingCollectionPrototype), proto); }
    }

    public class GlobalPropertiesPrototype : Prototype
    {
        public ulong Properties;
        public GlobalPropertiesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GlobalPropertiesPrototype), proto); }
    }

    public class PowerVisualsGlobalsPrototype : Prototype
    {
        public ulong DailyMissionCompleteClass;
        public ulong UnlockPetTechR1CommonClass;
        public ulong UnlockPetTechR2UncommonClass;
        public ulong UnlockPetTechR3RareClass;
        public ulong UnlockPetTechR4EpicClass;
        public ulong UnlockPetTechR5CosmicClass;
        public ulong LootVaporizedClass;
        public ulong AchievementUnlockedClass;
        public ulong OmegaPointGainedClass;
        public ulong AvatarLeashTeleportClass;
        public ulong InfinityTimePointEarnedClass;
        public ulong InfinitySpacePointEarnedClass;
        public ulong InfinitySoulPointEarnedClass;
        public ulong InfinityMindPointEarnedClass;
        public ulong InfinityRealityPointEarnedClass;
        public ulong InfinityPowerPointEarnedClass;
        public PowerVisualsGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PowerVisualsGlobalsPrototype), proto); }
    }

    public class RankDefaultEntryPrototype : Prototype
    {
        public ulong Data;
        public Rank Rank;
        public RankDefaultEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RankDefaultEntryPrototype), proto); }
    }

    public class PopulationGlobalsPrototype : Prototype
    {
        public ulong MessageEnemiesGrowStronger;
        public ulong MessageEnemiesGrowWeaker;
        public int SpawnMapPoolTickMS;
        public int SpawnMapLevelTickMS;
        public float CrowdSupressionRadius;
        public bool SupressSpawnOnPlayer;
        public int SpawnMapGimbalRadius;
        public int SpawnMapHorizon;
        public float SpawnMapMaxChance;
        public ulong EmptyPopulation;
        public ulong TwinEnemyBoost;
        public int DestructiblesForceSpawnMS;
        public ulong TwinEnemyCondition;
        public int SpawnMapHeatPerSecondMax;
        public int SpawnMapHeatPerSecondMin;
        public int SpawnMapHeatPerSecondScalar;
        public ulong TwinEnemyRank;
        public RankDefaultEntryPrototype[] RankDefaults;
        public PopulationGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PopulationGlobalsPrototype), proto); }
    }

    public class ClusterConfigurationGlobalsPrototype : Prototype
    {
        public int MinutesToKeepOfflinePlayerGames;
        public int MinutesToKeepUnusedRegions;
        public bool HotspotCheckLOSInTown;
        public int HotspotCheckTargetIntervalMS;
        public int HotspotCheckTargetTownIntervalMS;
        public int PartyKickGracePeriodMS;
        public int QueueReservationGracePeriodMS;
        public ClusterConfigurationGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ClusterConfigurationGlobalsPrototype), proto); }
    }

    public class CombatGlobalsPrototype : Prototype
    {
        public float PowerDmgBonusHardcoreAttenuation;
        public int MouseHoldStartMoveDelayMeleeMS;
        public int MouseHoldStartMoveDelayRangedMS;
        public float CriticalForceApplicationChance;
        public float CriticalForceApplicationMag;
        public float EnduranceCostChangePctMin;
        public EvalPrototype EvalBlockChanceFormula;
        public ulong EvalInterruptChanceFormula;
        public ulong EvalNegStatusResistPctFormula;
        public ulong ChannelInterruptCondition;
        public EvalPrototype EvalDamageReduction;
        public EvalPrototype EvalCritChanceFormula;
        public EvalPrototype EvalSuperCritChanceFormula;
        public EvalPrototype EvalDamageRatingFormula;
        public EvalPrototype EvalCritDamageRatingFormula;
        public EvalPrototype EvalDodgeChanceFormula;
        public EvalPrototype EvalDamageReductionDefenseOnly;
        public EvalPrototype EvalDamageReductionForDisplay;
        public float TravelPowerMaxSpeed;
        public ulong TUSynergyBonusPerLvl;
        public ulong TUSynergyBonusPerMaxLvlTU;
        public CombatGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CombatGlobalsPrototype), proto); }
    }

    public class VendorXPCapInfoPrototype : Prototype
    {
        public ulong Vendor;
        public int Cap;
        public float WallClockTime24Hr;
        public WeekdayEnum WallClockTimeDay;
        public VendorXPCapInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(VendorXPCapInfoPrototype), proto); }
    }

    public class AffixCategoryTableEntryPrototype : Prototype
    {
        public ulong Category;
        public ulong[] Affixes;
        public AffixCategoryTableEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AffixCategoryTableEntryPrototype), proto); }
    }

    public class LootGlobalsPrototype : Prototype
    {
        public ulong LootBonusRarityCurve;
        public ulong LootBonusSpecialCurve;
        public ulong LootContainerKeyword;
        public float LootDropScalar;
        public int LootInitializationLevelOffset;
        public ulong LootLevelDistribution;
        public float LootRarityScalar;
        public float LootSpecialItemFindScalar;
        public float LootUnrestedSpecialFindScalar;
        public float LootUsableByRecipientPercent;
        public ulong NoLootTable;
        public ulong SpecialOnKilledLootTable;
        public int SpecialOnKilledLootCooldownHours;
        public ulong RarityCosmic;
        public ulong LootBonusFlatCreditsCurve;
        public ulong RarityUruForged;
        public ulong LootTableBlueprint;
        public ulong RarityUnique;
        public int LootLevelMaxForDrops;
        public ulong InsigniaBlueprint;
        public ulong UniquesBoxCheatItem;
        public ulong[] EmptySocketAffixes;
        public ulong GemBlueprint;
        public VendorXPCapInfoPrototype[] VendorXPCapInfo;
        public float DropDistanceThreshold;
        public AffixCategoryTableEntryPrototype[] AffixCategoryTable;
        public ulong BonusItemFindCurve;
        public int BonusItemFindNumPointsForBonus;
        public ulong BonusItemFindLootTable;
        public float LootCoopPlayerRewardPct;
        public ulong RarityDefault;
        public LootGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LootGlobalsPrototype), proto); }
    }

    public class MatchQueueStringEntryPrototype : Prototype
    {
        public MatchQueueStatus StatusKey;
        public ulong StringLog;
        public ulong StringStatus;
        public MatchQueueStringEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MatchQueueStringEntryPrototype), proto); }
    }
    public enum MatchQueueStatus
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
    public class TransitionGlobalsPrototype : Prototype
    {
        public RegionPortalControlEntryPrototype[] ControlledRegions;
        public ulong EnabledState;
        public ulong DisabledState;
        public MatchQueueStringEntryPrototype[] QueueStrings;
        public ulong TransitionEmptyClass;
        public TransitionGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TransitionGlobalsPrototype), proto); }
    }

    public class KeywordGlobalsPrototype : Prototype
    {
        public ulong PowerKeywordPrototype;
        public ulong DestructibleKeyword;
        public ulong PetPowerKeyword;
        public ulong VacuumableKeyword;
        public ulong EntityKeywordPrototype;
        public ulong BodysliderPowerKeyword;
        public ulong OrbEntityKeyword;
        public ulong UltimatePowerKeyword;
        public ulong MeleePowerKeyword;
        public ulong RangedPowerKeyword;
        public ulong BasicPowerKeyword;
        public ulong TeamUpSpecialPowerKeyword;
        public ulong TeamUpDefaultPowerKeyword;
        public ulong StealthPowerKeyword;
        public ulong TeamUpAwayPowerKeyword;
        public ulong VanityPetKeyword;
        public ulong ControlPowerKeywordPrototype;
        public ulong AreaPowerKeyword;
        public ulong EnergyPowerKeyword;
        public ulong MentalPowerKeyword;
        public ulong PhysicalPowerKeyword;
        public ulong MedKitKeyword;
        public ulong OrbExperienceEntityKeyword;
        public ulong TutorialRegionKeyword;
        public ulong TeamUpKeyword;
        public ulong MovementPowerKeyword;
        public ulong ThrownPowerKeyword;
        public ulong HoloSimKeyword;
        public ulong ControlledSummonDurationKeyword;
        public ulong TreasureRoomKeyword;
        public ulong DangerRoomKeyword;
        public ulong StealingPowerKeyword;
        public ulong SummonPowerKeyword;
        public KeywordGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(KeywordGlobalsPrototype), proto); }
    }

    public class CurrencyGlobalsPrototype : Prototype
    {
        public ulong CosmicWorldstones;
        public ulong Credits;
        public ulong CubeShards;
        public ulong EternitySplinters;
        public ulong EyesOfDemonfire;
        public ulong HeartsOfDemonfire;
        public ulong LegendaryMarks;
        public ulong OmegaFiles;
        public ulong PvPCrowns;
        public ulong ResearchDrives;
        public ulong GenoshaRaid;
        public ulong DangerRoomMerits;
        public ulong GazillioniteGs;
        public CurrencyGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CurrencyGlobalsPrototype), proto); }
    }

    public class GamepadInputAssetPrototype : Prototype
    {
        public GamepadInput Input;
        public ulong DualShockPath;
        public ulong XboxPath;
        public GamepadInputAssetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GamepadInputAssetPrototype), proto); }
    }
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

    public class GamepadSlotBindingPrototype : Prototype
    {
        public int OrbisSlotNumber;
        public int PCSlotNumber;
        public int SlotNumber;
        public GamepadSlotBindingPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GamepadSlotBindingPrototype), proto); }
    }

    public class GamepadGlobalsPrototype : Prototype
    {
        public int GamepadDialogAcceptTimerMS;
        public float GamepadMaxTargetingRange;
        public float GamepadTargetingHalfAngle;
        public float GamepadTargetingDeflectionCost;
        public float GamepadTargetingPriorityCost;
        public int UltimateActivationTimeoutMS;
        public GamepadInputAssetPrototype[] InputAssets;
        public float GamepadInteractionHalfAngle;
        public float DisableInteractDangerRadius;
        public int GamepadInteractRange;
        public float GamepadInteractionOffset;
        public float GamepadTargetLockAssistHalfAngle;
        public float GamepadTargetLockAssistDflctCost;
        public float GamepadInteractBoundsIncrease;
        public float GamepadTargetLockDropRadius;
        public int GamepadTargetLockDropTimeMS;
        public GamepadSlotBindingPrototype[] GamepadSlotBindings;
        public float GamepadMeleeMoveIntoRangeDist;
        public float GamepadMeleeMoveIntoRangeSpeed;
        public float GamepadAutoTargetLockRadius;
        public float GamepadDestructTargetDeflctCost;
        public float GamepadDestructTargetHalfAngle;
        public float GamepadDestructTargetRange;
        public GamepadGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GamepadGlobalsPrototype), proto); }
    }

    public class ConsoleGlobalsPrototype : Prototype
    {
        public ulong OrbisDefaultSessionDescription;
        public ulong OrbisDefaultSessionImage;
        public int OrbisMaxSessionSize;
        public int MaxSuggestedPlayers;
        public ulong OrbisPlayerCameraSettings;
        public ulong OrbisFriendsInvitationDialogDesc;
        public int OrbisMaxFriendInvites;
        public ulong OrbisFriendsSuggestionDialogDesc;
        public int OrbisMaxFriendSuggestions;
        public ConsoleGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ConsoleGlobalsPrototype), proto); }
    }

    public class AvatarOnKilledInfoPrototype : Prototype
    {
        public DeathReleaseBehavior DeathReleaseBehavior;
        public ulong DeathReleaseButton;
        public ulong DeathReleaseDialogMessage;
        public int DeathReleaseTimeoutMS;
        public ulong ResurrectionDialogMessage;
        public int ResurrectionTimeoutMS;
        public int RespawnLockoutMS;
        public AvatarOnKilledInfoPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AvatarOnKilledInfoPrototype), proto); }
    }
    public enum DeathReleaseBehavior
    {
        ReturnToWaypoint = 0,
        ReturnToCheckpoint = 1,
    }

    public class GlobalEventPrototype : Prototype
    {
        public bool Active;
        public ulong CriteriaList;
        public GlobalEventCriteriaLogic CriteriaLogic;
        public ulong DisplayName;
        public int LeaderboardLength;
        public GlobalEventPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GlobalEventPrototype), proto); }
    }
    public enum GlobalEventCriteriaLogic
    {
        And = 0,
        Or = 1,
    }

    public class GlobalEventCriteriaPrototype : Prototype
    {
        public ulong DisplayColor;
        public ulong DisplayName;
        public int Score;
        public int ThresholdCount;
        public ulong DisplayTooltip;
        public GlobalEventCriteriaPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GlobalEventCriteriaPrototype), proto); }
    }

    public class GlobalEventCriteriaItemCollectPrototype : GlobalEventCriteriaPrototype
    {
        public EntityFilterPrototype ItemFilter;
        public GlobalEventCriteriaItemCollectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GlobalEventCriteriaItemCollectPrototype), proto); }
    }
}
