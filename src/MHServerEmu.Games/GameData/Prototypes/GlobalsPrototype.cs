using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

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
        // Not found in client
        InactiveForSlot = 0,
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

    // Regions/QueueStatus.type, equivalent to Gazillion::RegionRequestQueueUpdateVar from the protocol.
    // This is used in MatchQueueStringEntryPrototype for assigning locale strings to queue statuses.
    // Use the protocol enum if you need to do something with MatchQueueStatus.
    [AssetEnum((int)Invalid)]
    public enum RegionRequestQueueUpdateVarAssetEnum    
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

    #endregion

    public class GlobalsPrototype : Prototype
    {
        public PrototypeId AdvancementGlobals { get; protected set; }
        public PrototypeId AvatarSwapChannelPower { get; protected set; }
        public PrototypeId ConnectionMarkerPrototype { get; protected set; }
        public PrototypeId DebugGlobals { get; protected set; }
        public PrototypeId UIGlobals { get; protected set; }
        public PrototypeId DefaultPlayer { get; protected set; }
        public PrototypeId DefaultStartTarget { get; protected set; }
        public PrototypeId[] PVPAlliances { get; protected set; }
        public float HighFlyingHeight { get; protected set; }
        public float LowHealthTrigger { get; protected set; }
        public float MouseHitCollisionMultiplier { get; protected set; }
        public float MouseHitMovingTargetsIncrease { get; protected set; }
        public float MouseHitPowerTargetSearchDist { get; protected set; }
        public float MouseHitPreferredAddition { get; protected set; }
        public float MouseMovementNoPathRadius { get; protected set; }
        public PrototypeId MissionGlobals { get; protected set; }
        public int TaggingResetDurationMS { get; protected set; }
        public int PlayerPartyMaxSize { get; protected set; }
        public float NaviBudgetBaseCellSizeWidth { get; protected set; }
        public float NaviBudgetBaseCellSizeLength { get; protected set; }
        public int NaviBudgetBaseCellMaxPoints { get; protected set; }
        public int NaviBudgetBaseCellMaxEdges { get; protected set; }
        public AssetId[] UIConfigFiles { get; protected set; }
        public int InteractRange { get; protected set; }
        public PrototypeId CreditsItemPrototype { get; protected set; }
        public PrototypeId[] NegStatusEffectList { get; protected set; }
        public PrototypeId PvPPrototype { get; protected set; }
        public PrototypeId MissionPrototype { get; protected set; }
        public EvalPrototype ItemPriceMultiplierBuyFromVendor { get; protected set; }
        public EvalPrototype ItemPriceMultiplierSellToVendor { get; protected set; }
        public ModGlobalsPrototype ModGlobals { get; protected set; }
        public float MouseMoveDrivePathMaxLengthMult { get; protected set; }
        public AssetId AudioGlobalEventsClass { get; protected set; }
        public PrototypeId MetaGamePrototype { get; protected set; }
        public int MobLOSVisUpdatePeriodMS { get; protected set; }
        public int MobLOSVisStayVisibleDelayMS { get; protected set; }
        public bool MobLOSVisEnabled { get; protected set; }
        public AssetTypeId[] BeginPlayAssetTypes { get; protected set; }
        public AssetTypeId[] CachedAssetTypes { get; protected set; }
        public AssetTypeId[] FileVerificationAssetTypes { get; protected set; }
        public AssetId LoadingMusic { get; protected set; }
        public LocaleStringId SystemLocalized { get; protected set; }
        public PrototypeId PopulationGlobals { get; protected set; }
        public PrototypeId PlayerAlliance { get; protected set; }
        public PrototypeId ClusterConfigurationGlobals { get; protected set; }
        public PrototypeId DownloadChunks { get; protected set; }
        public PrototypeId UIItemInventory { get; protected set; }
        public PrototypeId AIGlobals { get; protected set; }
        public AssetTypeId MusicAssetType { get; protected set; }
        public PrototypeId ResurrectionDefaultInfo { get; protected set; }
        public PrototypeId PartyJoinPortal { get; protected set; }
        public PrototypeId MatchJoinPortal { get; protected set; }
        public AssetTypeId MovieAssetType { get; protected set; }
        public PrototypeId WaypointGraph { get; protected set; }
        public PrototypeId WaypointHotspot { get; protected set; }
        public float MouseHoldDeadZoneRadius { get; protected set; }
        public GlobalPropertiesPrototype Properties { get; protected set; }
        public int PlayerGracePeroidInSeconds { get; protected set; }
        public PrototypeId CheckpointHotspot { get; protected set; }
        public PrototypeId ReturnToHubPower { get; protected set; }
        public int DisableEndurRegenOnPowerEndMS { get; protected set; }
        public PrototypeId PowerPrototype { get; protected set; }
        public PrototypeId WorldEntityPrototype { get; protected set; }
        public PrototypeId AreaPrototype { get; protected set; }
        public PrototypeId PopulationObjectPrototype { get; protected set; }
        public PrototypeId RegionPrototype { get; protected set; }
        public AssetTypeId AmbientSfxType { get; protected set; }
        public PrototypeId CombatGlobals { get; protected set; }
        public float OrientForPowerMaxTimeSecs { get; protected set; }
        public PrototypeId KismetSequenceEntityPrototype { get; protected set; }
        public PrototypeId DynamicArea { get; protected set; }
        public PrototypeId ReturnToFieldPower { get; protected set; }
        public float AssetCacheCellLoadOutRunSeconds { get; protected set; }
        public int AssetCacheMRUSize { get; protected set; }
        public int AssetCachePrefetchMRUSize { get; protected set; }
        public PrototypeId AvatarSwapInPower { get; protected set; }
        public PrototypeId PlayerStartingFaction { get; protected set; }
        public PrototypeId VendorBuybackInventory { get; protected set; }
        public PrototypeId AnyAlliancePrototype { get; protected set; }
        public PrototypeId AnyFriendlyAlliancePrototype { get; protected set; }
        public PrototypeId AnyHostileAlliancePrototype { get; protected set; }
        public CurveId ExperienceBonusCurve { get; protected set; }
        public PrototypeId TransitionGlobals { get; protected set; }
        public int PlayerGuildMaxSize { get; protected set; }
        public bool AutoPartyEnabledInitially { get; protected set; }
        public PrototypeId ItemBindingAffix { get; protected set; }
        public int InteractFallbackRange { get; protected set; }
        public PrototypeId ItemAcquiredThroughMTXStoreAffix { get; protected set; }
        public PrototypeId TeleportToPartyMemberPower { get; protected set; }
        public PrototypeId AvatarSwapOutPower { get; protected set; }
        public int KickIdlePlayerTimeSecs { get; protected set; }
        public PrototypeId PlayerCameraSettings { get; protected set; }
        public PrototypeId AvatarSynergyCondition { get; protected set; }
        public LocaleStringId MetaGameLocalized { get; protected set; }
        public PrototypeId MetaGameTeamDefault { get; protected set; }
        public PrototypeId ItemNoVisualsAffix { get; protected set; }
        public int AvatarSynergyConcurrentLimit { get; protected set; }
        public PrototypeId LootGlobals { get; protected set; }
        public PrototypeId MetaGameTeamBase { get; protected set; }
        public PrototypeId AudioGlobals { get; protected set; }
        public int PlayerRaidMaxSize { get; protected set; }
        public int TimeZone { get; protected set; }
        public PrototypeId TeamUpSummonPower { get; protected set; }
        public int AssistPvPDurationMS { get; protected set; }
        public PrototypeId FulfillmentReceiptPrototype { get; protected set; }
        public PrototypeId PetTechVacuumPower { get; protected set; }
        public PrototypeId[] StolenPowerRestrictions { get; protected set; }
        public PrototypeId PowerVisualsGlobals { get; protected set; }
        public PrototypeId KeywordGlobals { get; protected set; }
        public PrototypeId CurrencyGlobals { get; protected set; }
        public PrototypeId PointerArrowTemplate { get; protected set; }
        public PrototypeId ObjectiveMarkerTemplate { get; protected set; }
        public int VaporizedLootLifespanMS { get; protected set; }
        public AssetTypeId[] CookedIconAssetTypes { get; protected set; }
        public PrototypeId LiveTuneAvatarXPDisplayCondition { get; protected set; }
        public PrototypeId LiveTuneCreditsDisplayCondition { get; protected set; }
        public PrototypeId LiveTuneRegionXPDisplayCondition { get; protected set; }
        public PrototypeId LiveTuneRIFDisplayCondition { get; protected set; }
        public PrototypeId LiveTuneSIFDisplayCondition { get; protected set; }
        public PrototypeId LiveTuneXPDisplayCondition { get; protected set; }
        public PrototypeId ItemLinkInventory { get; protected set; }
        public PrototypeId LimitedEditionBlueprint { get; protected set; }
        public AssetTypeId[] MobileIconAssetTypes { get; protected set; }
        public PrototypeId PetItemBlueprint { get; protected set; }
        public PrototypeId AvatarPrototype { get; protected set; }
        public int ServerBonusUnlockLevel { get; protected set; }
        public PrototypeId GamepadGlobals { get; protected set; }
        public PrototypeId CraftingRecipeLibraryInventory { get; protected set; }
        public PrototypeId ConditionPrototype { get; protected set; }
        public PrototypeId[] LiveTuneServerConditions { get; protected set; }
        public PrototypeId DefaultStartingAvatarPrototype { get; protected set; }
        public PrototypeId DefaultStartTargetFallbackRegion { get; protected set; }
        public PrototypeId DefaultStartTargetPrestigeRegion { get; protected set; }
        public PrototypeId DefaultStartTargetStartingRegion { get; protected set; }
        public PrototypeId DifficultyGlobals { get; protected set; }
        public PrototypeId PublicEventPrototype { get; protected set; }
        public PrototypeId AvatarCoopStartPower { get; protected set; }
        public PrototypeId AvatarCoopEndPower { get; protected set; }
        public PrototypeId[] DifficultyTiers { get; protected set; }      // VectorPrototypeRefPtr DifficultyTierPrototype
        public PrototypeId DefaultLoadingLobbyRegion { get; protected set; }
        public PrototypeId DifficultyTierDefault { get; protected set; }
        public PrototypeId AvatarHealPower { get; protected set; }
        public PrototypeId ConsoleGlobals { get; protected set; }
        public PrototypeId TeamUpSynergyCondition { get; protected set; }
        public PrototypeId MetricsFrequencyPrototype { get; protected set; }
        public PrototypeId ConsumableItemBlueprint { get; protected set; }
        public int AvatarCoopInactiveTimeMS { get; protected set; }
        public int AvatarCoopInactiveOnDeadBufferMS { get; protected set; }

        //---

        private static readonly Logger Logger = LogManager.CreateLogger();

        private DifficultyTierPrototype[] _difficultyTierProtos;

        [DoNotCopy]
        public AlliancePrototype PlayerAlliancePrototype { get; protected set; }

        [DoNotCopy]
        public PopulationGlobalsPrototype PopulationGlobalsPrototype { get; protected set; }

        public override void PostProcess()
        {
            base.PostProcess();

            if (DifficultyTiers.HasValue())
            {
                _difficultyTierProtos = new DifficultyTierPrototype[(int)DifficultyTier.NumTiers];

                foreach (PrototypeId difficultyTierProtoRef in DifficultyTiers)
                {
                    DifficultyTierPrototype tierPtr = difficultyTierProtoRef.As<DifficultyTierPrototype>();
                    if (tierPtr == null)
                    {
                        Logger.Warn("PostProcess(): tierPtr == null");
                        continue;
                    }

                    int tierAsIndex = (int)tierPtr.Tier;
                    if (tierAsIndex < 0 || tierAsIndex >= _difficultyTierProtos.Length)
                    {
                        Logger.Warn("PostProcess(): tierAsIndex < 0 || tierAsIndex >= _difficultyTierProtos.Length");
                        continue;
                    }

                    _difficultyTierProtos[tierAsIndex] = tierPtr;
                }
            }

            PlayerAlliancePrototype = PlayerAlliance.As<AlliancePrototype>();
            PopulationGlobalsPrototype = PopulationGlobals.As<PopulationGlobalsPrototype>();
        }

        public DifficultyTierPrototype GetDifficultyTierByEnum(DifficultyTier tierEnum)
        {
            int tierAsIndex = (int)tierEnum;
            if (tierAsIndex < 0 || tierAsIndex >= _difficultyTierProtos.Length)
                Logger.WarnReturn<DifficultyTierPrototype>(null, "PostProcess(): tierAsIndex < 0 || tierAsIndex >= _difficultyTierProtos.Length");

            return _difficultyTierProtos[tierAsIndex];
        }
    }

    public class LoginRewardPrototype : Prototype
    {
        public int Day { get; protected set; }
        public PrototypeId Item { get; protected set; }
        public LocaleStringId TooltipText { get; protected set; }
        public PrototypeId LogoffPanelEntry { get; protected set; }
    }

    public class PrestigeLevelPrototype : Prototype
    {
        public PrototypeId TextStyle { get; protected set; }
        public PrototypeId Reward { get; protected set; }
    }

    public class PetTechAffixInfoPrototype : Prototype
    {
        public AffixPosition Position { get; protected set; }
        public PrototypeId ItemRarityToConsume { get; protected set; }
        public int ItemsRequiredToUnlock { get; protected set; }
        public LocaleStringId LockedDescriptionText { get; protected set; }
    }

    public class AdvancementGlobalsPrototype : Prototype
    {
        public CurveId LevelingCurve { get; protected set; }
        public CurveId DeathPenaltyCost { get; protected set; }
        public CurveId ItemEquipRequirementOffset { get; protected set; }
        public CurveId VendorLevelingCurve { get; protected set; }
        public PrototypeId StatsEval { get; protected set; }
        public PrototypeId AvatarThrowabilityEval { get; protected set; }
        public EvalPrototype VendorLevelingEval { get; protected set; }
        public EvalPrototype VendorRollTableLevelEval { get; protected set; }
        public float RestedHealthPerMinMult { get; protected set; }
        public int PowerBoostMax { get; protected set; }
        public PrototypeId[] PrestigeLevels { get; protected set; }   // VectorPrototypeRefPtr PrestigeLevelPrototype
        public CurveId ItemAffixLevelingCurve { get; protected set; }
        public CurveId ExperienceBonusAvatarSynergy { get; protected set; }
        public float ExperienceBonusAvatarSynergyMax { get; protected set; }
        public int OriginalMaxLevel { get; protected set; }
        public CurveId ExperienceBonusLevel60Synergy { get; protected set; }
        public int TeamUpPowersPerTier { get; protected set; }
        public CurveId TeamUpPowerTiersCurve { get; protected set; }
        public PrototypeId[] OmegaBonusSets { get; protected set; }   // VectorPrototypeRefPtr OmegaBonusSetPrototype
        public int OmegaPointsCap { get; protected set; }
        public int OmegaSystemLevelUnlock { get; protected set; }
        public PetTechAffixInfoPrototype[] PetTechAffixInfo { get; protected set; }
        public PrototypeId PetTechDonationItemPrototype { get; protected set; }
        public int AvatarPowerSpecsMax { get; protected set; }
        public CurveId PctXPFromPrestigeLevelCurve { get; protected set; }
        public int StarterAvatarLevelCap { get; protected set; }
        public CurveId TeamUpLevelingCurve { get; protected set; }
        public int TeamUpPowerSpecsMax { get; protected set; }
        public CurveId PctXPFromLevelDeltaCurve { get; protected set; }
        public int InfinitySystemUnlockLevel { get; protected set; }
        public long InfinityPointsCapPerGem { get; protected set; }
        public PrototypeId[] InfinityGemSets { get; protected set; }  // VectorPrototypeRefPtr InfinityGemSetPrototype
        public long InfinityXPCap { get; protected set; }
        public int TravelPowerUnlockLevel { get; protected set; }
        public float ExperienceBonusCoop { get; protected set; }
        public CurveId CoopInactivityExperienceScalar { get; protected set; }

        // ---

        private static readonly Logger Logger = LogManager.CreateLogger();

        public const long InvalidXPRequirement = -1;

        [DoNotCopy]
        public int MaxPrestigeLevel { get => PrestigeLevels.Length; }

        public int GetAvatarLevelCap()
        {
            Curve levelingCurve = GetAvatarLevelingCurve();
            if (levelingCurve == null) return Logger.WarnReturn(0, "GetAvatarLevelCap(): levelingCurve == null");

            return levelingCurve.MaxPosition;
        }

        public int GetTeamUpLevelCap()
        {
            Curve levelingCurve = GetTeamUpLevelingCurve();
            if (levelingCurve == null) return Logger.WarnReturn(0, "GetTeamUpLevelCap(): levelingCurve == null");

            return levelingCurve.MaxPosition;
        }

        public int GetItemAffixLevelCap()
        {
            Curve levelingCurve = GetItemAffixLevelingCurve();
            if (levelingCurve == null) return Logger.WarnReturn(0, "GetItemAffixLevelCap(): levelingCurve == null");

            return levelingCurve.MaxPosition;
        }

        public int GetPrestigeLevelIndex(PrestigeLevelPrototype prestigeLevelProto)
        {
            return GetPrestigeLevelIndex(prestigeLevelProto.DataRef);
        }

        public int GetPrestigeLevelIndex(PrototypeId prestigeLevel)
        {
            if (PrestigeLevels.IsNullOrEmpty()) return 0;

            for (int i = 0; i < MaxPrestigeLevel; i++)
            {
                if (PrestigeLevels[i] == prestigeLevel)
                    return i + 1;
            }

            return 0;
        }

        public long GetAvatarLevelUpXPRequirement(int level)
        {
            if (level < 1)
                return InvalidXPRequirement;

            Curve levelingCurve = GetAvatarLevelingCurve();
            if (levelingCurve == null) return Logger.WarnReturn(InvalidXPRequirement, "GetAvatarLevelUpXPRequirement(): levelingCurve == null");

            return GetLevelUpXPRequirementFromCurve(level, levelingCurve);
        }

        public long GetTeamUpLevelUpXPRequirement(int level)
        {
            if (level < 1)
                return InvalidXPRequirement;

            Curve levelingCurve = GetTeamUpLevelingCurve();
            if (levelingCurve == null) return Logger.WarnReturn(InvalidXPRequirement, "GetTeamUpLevelUpXPRequirement(): levelingCurve == null");

            return GetLevelUpXPRequirementFromCurve(level, levelingCurve);
        }

        public long GetItemAffixLevelUpXPRequirement(int level)
        {
            if (level < 0)
                return InvalidXPRequirement;

            Curve levelingCurve = GetItemAffixLevelingCurve();
            if (levelingCurve == null) return Logger.WarnReturn(InvalidXPRequirement, "GetItemAffixLevelUpXPRequirement(): levelingCurve == null");

            return GetLevelUpXPRequirementFromCurve(level, levelingCurve);
        }

        private static long GetLevelUpXPRequirementFromCurve(int level, Curve curve)
        {
            if (level < curve.MinPosition || level > curve.MaxPosition)
                return InvalidXPRequirement;

            return curve.GetInt64At(level);
        }

        private Curve GetAvatarLevelingCurve()
        {
            return CurveDirectory.Instance.GetCurve(LevelingCurve);
        }

        private Curve GetTeamUpLevelingCurve()
        {
            return CurveDirectory.Instance.GetCurve(TeamUpLevelingCurve);
        }

        private Curve GetItemAffixLevelingCurve()
        {
            return CurveDirectory.Instance.GetCurve(ItemAffixLevelingCurve);
        }
    }

    public class AIGlobalsPrototype : Prototype
    {
        public PrototypeId LeashReturnHeal { get; protected set; }
        public PrototypeId LeashReturnImmunity { get; protected set; }
        public PrototypeId LeashingProceduralProfile { get; protected set; }
        public int RandomThinkVarianceMS { get; protected set; }
        public int ControlledAgentResurrectTimerMS { get; protected set; }
        public PrototypeId ControlledAlliance { get; protected set; }
        public float OrbAggroRangeMax { get; protected set; }
        public CurveId OrbAggroRangeBonusCurve { get; protected set; }
        public PrototypeId DefaultSimpleNpcBrain { get; protected set; }
        public PrototypeId CantBeControlledKeyword { get; protected set; }
        public int ControlledAgentSummonDurationMS { get; protected set; }
    }

    public class MusicStatePrototype : Prototype
    {
        public AssetId StateGroupName { get; protected set; }
        public AssetId StateName { get; protected set; }
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
        public PrototypeId CreateEntityShortcutEntity { get; protected set; }
        public PrototypeId DynamicRegion { get; protected set; }
        public float HardModeMobDmgBuff { get; protected set; }
        public float HardModeMobHealthBuff { get; protected set; }
        public float HardModeMobMoveSpdBuff { get; protected set; }
        public float HardModePlayerEnduranceCostDebuff { get; protected set; }
        public PrototypeId PowersArtModeEntity { get; protected set; }
        public int StartingLevelMobs { get; protected set; }
        public PrototypeId TransitionRef { get; protected set; }
        public PrototypeId CreateLootDummyEntity { get; protected set; }
        public PrototypeId MapErrorMapInfo { get; protected set; }
        public bool IgnoreDeathPenalty { get; protected set; }
        public bool TrashedItemsDropInWorld { get; protected set; }
        public PrototypeId PAMEnemyAlliance { get; protected set; }
        public EvalPrototype DebugEval { get; protected set; }
        public EvalPrototype DebugEvalUnitTest { get; protected set; }
        public BotSettingsPrototype BotSettings { get; protected set; }
        public PrototypeId ReplacementTestingResultItem { get; protected set; }
        public PrototypeId ReplacementTestingTriggerItem { get; protected set; }
        public PrototypeId VendorEternitySplinterLoot { get; protected set; }
    }

    public class CharacterSheetDetailedStatPrototype : Prototype
    {
        public EvalPrototype Expression { get; protected set; }
        public PrototypeId ExpressionExt { get; protected set; }
        public LocaleStringId Format { get; protected set; }
        public LocaleStringId Label { get; protected set; }
        public LocaleStringId Tooltip { get; protected set; }
        public AssetId Icon { get; protected set; }
    }

    public class HelpGameTermPrototype : Prototype
    {
        public LocaleStringId Name { get; protected set; }
        public LocaleStringId Description { get; protected set; }
    }

    public class CoopOpUIDataEntryPrototype : Prototype
    {
        public CoopOp Op { get; protected set; }
        public CoopOpResult Result { get; protected set; }
        public LocaleStringId SystemMessage { get; protected set; }
        public PrototypeId SystemMessageTemplate { get; protected set; }
        public PrototypeId BannerMessage { get; protected set; }
    }

    public class HelpTextPrototype : Prototype
    {
        public LocaleStringId GeneralControls { get; protected set; }
        public HelpGameTermPrototype[] GameTerms { get; protected set; }
        public LocaleStringId Crafting { get; protected set; }
        public LocaleStringId EndgamePvE { get; protected set; }
        public LocaleStringId PvP { get; protected set; }
        public LocaleStringId Tutorial { get; protected set; }
    }

    public class AffixRollQualityPrototype : Prototype
    {
        public TextStylePrototype Style { get; protected set; }
        public float PercentThreshold { get; protected set; }
    }

    public class UIGlobalsPrototype : Prototype
    {
        public PrototypeId MessageDefault { get; protected set; }
        public PrototypeId MessageLevelUp { get; protected set; }
        public PrototypeId MessageItemError { get; protected set; }
        public PrototypeId MessageRegionChange { get; protected set; }
        public PrototypeId MessageMissionAccepted { get; protected set; }
        public PrototypeId MessageMissionCompleted { get; protected set; }
        public PrototypeId MessageMissionFailed { get; protected set; }
        public int AvatarSwitchUIDeathDelayMS { get; protected set; }
        public PrototypeId UINotificationGlobals { get; protected set; }
        public int RosterPageSize { get; protected set; }
        public AssetId LocalizedInfoDirectory { get; protected set; }
        public int TooltipHideDelayMS { get; protected set; }
        public PrototypeId MessagePowerError { get; protected set; }
        public PrototypeId MessageWaypointError { get; protected set; }
        public PrototypeId UIStringGlobals { get; protected set; }
        public PrototypeId MessagePartyInvite { get; protected set; }
        public PrototypeId MapInfoMissionGiver { get; protected set; }
        public PrototypeId MapInfoMissionObjectiveTalk { get; protected set; }
        public int NumAvatarsToDisplayInItemUsableLists { get; protected set; }
        public PrototypeId[] LoadingScreens { get; protected set; }
        public int ChatFadeInMS { get; protected set; }
        public int ChatBeginFadeOutMS { get; protected set; }
        public int ChatFadeOutMS { get; protected set; }
        public PrototypeId MessageWaypointUnlocked { get; protected set; }
        public PrototypeId MessagePowerUnlocked { get; protected set; }
        public PrototypeId UIMapGlobals { get; protected set; }
        public PrototypeId TextStyleCurrentlyEquipped { get; protected set; }
        public int ChatTextFadeOutMS { get; protected set; }
        public int ChatTextHistoryMax { get; protected set; }
        public PrototypeId KeywordFemale { get; protected set; }
        public PrototypeId KeywordMale { get; protected set; }
        public PrototypeId TextStylePowerUpgradeImprovement { get; protected set; }
        public PrototypeId TextStylePowerUpgradeNoImprovement { get; protected set; }
        public PrototypeId LoadingScreenIntraRegion { get; protected set; }
        public PrototypeId TextStyleVendorPriceCanBuy { get; protected set; }
        public PrototypeId TextStyleVendorPriceCantBuy { get; protected set; }
        public PrototypeId TextStyleItemRestrictionFailure { get; protected set; }
        public int CostumeClosetNumAvatarsVisible { get; protected set; }
        public int CostumeClosetNumCostumesVisible { get; protected set; }
        public PrototypeId MessagePowerErrorDoNotQueue { get; protected set; }
        public PrototypeId TextStylePvPShopPurchased { get; protected set; }
        public PrototypeId TextStylePvPShopUnpurchased { get; protected set; }
        public PrototypeId MessagePowerPointsAwarded { get; protected set; }
        public PrototypeId MapInfoMissionObjectiveUse { get; protected set; }
        public PrototypeId TextStyleMissionRewardFloaty { get; protected set; }
        public PrototypeId PowerTooltipBodyCurRank0Unlkd { get; protected set; }
        public PrototypeId PowerTooltipBodyCurRankLocked { get; protected set; }
        public PrototypeId PowerTooltipBodyCurRank1AndUp { get; protected set; }
        public PrototypeId PowerTooltipBodyNextRank1First { get; protected set; }
        public PrototypeId PowerTooltipBodyNextRank2AndUp { get; protected set; }
        public PrototypeId PowerTooltipHeader { get; protected set; }
        public PrototypeId MapInfoFlavorNPC { get; protected set; }
        public int TooltipSpawnHideDelayMS { get; protected set; }
        public int KioskIdleResetTimeSec { get; protected set; }
        public PrototypeId KioskSizzleMovie { get; protected set; }
        public int KioskSizzleMovieStartTimeSec { get; protected set; }
        public PrototypeId MapInfoHealer { get; protected set; }
        public PrototypeId TextStyleOpenMission { get; protected set; }
        public PrototypeId MapInfoPartyMember { get; protected set; }
        public int LoadingScreenTipTimeIntervalMS { get; protected set; }
        public PrototypeId TextStyleKillRewardFloaty { get; protected set; }
        public PrototypeId TextStyleAvatarOverheadNormal { get; protected set; }
        public PrototypeId TextStyleAvatarOverheadParty { get; protected set; }
        public CharacterSheetDetailedStatPrototype[] CharacterSheetDetailedStats { get; protected set; }
        public PrototypeId PowerProgTableTabRefTab1 { get; protected set; }
        public PrototypeId PowerProgTableTabRefTab2 { get; protected set; }
        public PrototypeId PowerProgTableTabRefTab3 { get; protected set; }
        public float ScreenEdgeArrowRange { get; protected set; }
        public PrototypeId HelpText { get; protected set; }
        public PrototypeId MessagePvPFactionPortalFail { get; protected set; }
        public PrototypeId PropertyTooltipTextOverride { get; protected set; }
        public PrototypeId MessagePvPDisabledPortalFail { get; protected set; }
        public PrototypeId MessageStatProgression { get; protected set; }
        public PrototypeId MessagePvPPartyPortalFail { get; protected set; }
        public PrototypeId TextStyleMissionHudOpenMission { get; protected set; }
        public PrototypeId MapInfoAvatarDefeated { get; protected set; }
        public PrototypeId MapInfoPartyMemberDefeated { get; protected set; }
        public PrototypeId MessageGuildInvite { get; protected set; }
        public PrototypeId MapInfoMissionObjectiveMob { get; protected set; }
        public PrototypeId MapInfoMissionObjectivePortal { get; protected set; }
        public PrototypeId CinematicsListLoginScreen { get; protected set; }
        public PrototypeId TextStyleGuildLeader { get; protected set; }
        public PrototypeId TextStyleGuildOfficer { get; protected set; }
        public PrototypeId TextStyleGuildMember { get; protected set; }
        public AffixDisplaySlotPrototype[] CostumeAffixDisplaySlots { get; protected set; }
        public PrototypeId MessagePartyError { get; protected set; }
        public PrototypeId MessageRegionRestricted { get; protected set; }
        public PrototypeId[] CreditsMovies { get; protected set; }
        public PrototypeId MessageMetaGameDefault { get; protected set; }
        public PrototypeId MessagePartyPvPPortalFail { get; protected set; }
        public int ChatNewMsgDarkenBgMS { get; protected set; }
        public PrototypeId TextStyleKillZeroRewardFloaty { get; protected set; }
        public PrototypeId MessageAvatarSwitchError { get; protected set; }
        public PrototypeId TextStyleItemBlessed { get; protected set; }
        public PrototypeId TextStyleItemAffixLocked { get; protected set; }
        public PrototypeId MessageAlreadyInQueue { get; protected set; }
        public PrototypeId MessageOnlyPartyLeaderCanQueue { get; protected set; }
        public PrototypeId MessageTeleportTargetIsInMatch { get; protected set; }
        public PrototypeId PowerGrantItemTutorialTip { get; protected set; }
        public PrototypeId MessagePrivateDisallowedInRaid { get; protected set; }
        public PrototypeId MessageQueueNotAvailableInRaid { get; protected set; }
        public PrototypeId PowerTooltipBodyNextRank1Antireq { get; protected set; }
        public PrototypeId CosmicEquippedTutorialTip { get; protected set; }
        public PrototypeId MessageRegionDisabledPortalFail { get; protected set; }
        public CharacterSheetDetailedStatPrototype[] TeamUpDetailedStats { get; protected set; }
        public PrototypeId MessageOmegaPointsAwarded { get; protected set; }
        public PrototypeId MetaGameWidgetMissionName { get; protected set; }
        public UIConditionType[] BuffPageOrder { get; protected set; }
        public ObjectiveTrackerPageType[] ObjectiveTrackerPageOrder { get; protected set; }
        public PrototypeId VanityTitleNoTitle { get; protected set; }
        public PrototypeId MessageStealablePowerOccupied { get; protected set; }
        public PrototypeId MessageStolenPowerDuplicate { get; protected set; }
        public PrototypeId[] CurrencyDisplayList { get; protected set; }
        public PrototypeId CinematicOpener { get; protected set; }
        public PrototypeId MessageCantQueueInQueueRegion { get; protected set; }
        public int LogoffPanelStoryMissionLevelCap { get; protected set; }
        public StoreCategoryPrototype[] MTXStoreCategories { get; protected set; }
        public int GiftingAccessMinPlayerLevel { get; protected set; }
        public PrototypeId AffixRollRangeTooltipText { get; protected set; }
        public PrototypeId[] UISystemLockList { get; protected set; }
        public PrototypeId MessageUISystemUnlocked { get; protected set; }
        public PrototypeId TooltipInsigniaTeamAffiliations { get; protected set; }
        public PrototypeId PowerTooltipBodySpecLocked { get; protected set; }
        public PrototypeId PowerTooltipBodySpecUnlocked { get; protected set; }
        public PrototypeId PropertyValuePercentFormat { get; protected set; }
        public PrototypeId AffixStatDiffPositiveStyle { get; protected set; }
        public PrototypeId AffixStatDiffNegativeStyle { get; protected set; }
        public PrototypeId AffixStatDiffTooltipText { get; protected set; }
        public PrototypeId AffixStatDiffNeutralStyle { get; protected set; }
        public PrototypeId AffixStatFoundAffixStyle { get; protected set; }
        public AssetId[] StashTabCustomIcons { get; protected set; }
        public PrototypeId PropertyValueDefaultFormat { get; protected set; }
        public PrototypeId[] ItemSortCategoryList { get; protected set; }
        public PrototypeId[] ItemSortSubCategoryList { get; protected set; }
        public AffixRollQualityPrototype[] AffixRollRangeRollQuality { get; protected set; }
        public PrototypeId[] RadialMenuEntriesList { get; protected set; }
        public PrototypeId TextStylePowerChargesEmpty { get; protected set; }
        public PrototypeId TextStylePowerChargesFull { get; protected set; }
        public PrototypeId MessageLeaderboardRewarded { get; protected set; }
        public PrototypeId GamepadIconDonateAction { get; protected set; }
        public PrototypeId GamepadIconDropAction { get; protected set; }
        public PrototypeId GamepadIconEquipAction { get; protected set; }
        public PrototypeId GamepadIconMoveAction { get; protected set; }
        public PrototypeId GamepadIconSelectAction { get; protected set; }
        public PrototypeId GamepadIconSellAction { get; protected set; }
        public PrototypeId[] ConsoleRadialMenuEntriesList { get; protected set; }
        public CoopOpUIDataEntryPrototype[] CoopOpUIDatas { get; protected set; }
        public PrototypeId MessageOpenMissionEntered { get; protected set; }
        public PrototypeId MessageInfinityPointsAwarded { get; protected set; }
        public PrototypeId PowerTooltipBodyTalentLocked { get; protected set; }
        public PrototypeId PowerTooltipBodyTalentUnlocked { get; protected set; }
        public PrototypeId[] AffixTooltipOrder { get; protected set; }
        public PrototypeId PowerTooltipBodyCurRank1Only { get; protected set; }
        public int InfinityMaxRanksHideThreshold { get; protected set; }
        public PrototypeId MessagePlayingAtLevelCap { get; protected set; }
        public PrototypeId GamepadIconRankDownAction { get; protected set; }
        public PrototypeId GamepadIconRankUpAction { get; protected set; }
        public PrototypeId MessageTeamUpDisabledCoop { get; protected set; }
        public PrototypeId MessageStolenPowerAvailable { get; protected set; }
        public PrototypeId BIFRewardMessage { get; protected set; }
        public PrototypeId PowerTooltipBodyTeamUpLocked { get; protected set; }
        public PrototypeId PowerTooltipBodyTeamUpUnlocked { get; protected set; }
        public int InfinityNotificationThreshold { get; protected set; }
        public PrototypeId HelpTextConsole { get; protected set; }
        public PrototypeId MessageRegionNotDownloaded { get; protected set; }
    }

    public class UINotificationGlobalsPrototype : Prototype
    {
        public PrototypeId NotificationPartyInvite { get; protected set; }
        public PrototypeId NotificationLevelUp { get; protected set; }
        public PrototypeId NotificationServerMessage { get; protected set; }
        public PrototypeId NotificationRemoteMission { get; protected set; }
        public PrototypeId NotificationMissionUpdate { get; protected set; }
        public PrototypeId NotificationMatchInvite { get; protected set; }
        public PrototypeId NotificationMatchQueue { get; protected set; }
        public PrototypeId NotificationMatchGroupInvite { get; protected set; }
        public PrototypeId NotificationPvPShop { get; protected set; }
        public PrototypeId NotificationPowerPointsAwarded { get; protected set; }
        public int NotificationPartyAIAggroRange { get; protected set; }
        public PrototypeId NotificationOfferingUI { get; protected set; }
        public PrototypeId NotificationGuildInvite { get; protected set; }
        public PrototypeId NotificationMetaGameInfo { get; protected set; }
        public PrototypeId NotificationLegendaryMission { get; protected set; }
        public PrototypeId NotificationMatchPending { get; protected set; }
        public PrototypeId NotificationMatchGroupPending { get; protected set; }
        public PrototypeId NotificationMatchWaitlisted { get; protected set; }
        public PrototypeId NotificationLegendaryQuestShare { get; protected set; }
        public PrototypeId NotificationSynergyPoints { get; protected set; }
        public PrototypeId NotificationPvPScoreboard { get; protected set; }
        public PrototypeId NotificationOmegaPoints { get; protected set; }
        public PrototypeId NotificationTradeInvite { get; protected set; }
        public PrototypeId NotificationMatchLocked { get; protected set; }
        public PrototypeId NotificationLoginReward { get; protected set; }
        public PrototypeId NotificationMatchGracePeriod { get; protected set; }
        public PrototypeId NotificationPartyKickGracePeriod { get; protected set; }
        public PrototypeId NotificationGiftReceived { get; protected set; }
        public PrototypeId NotificationLeaderboardRewarded { get; protected set; }
        public PrototypeId NotificationCouponReceived { get; protected set; }
        public PrototypeId NotificationPublicEvent { get; protected set; }
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
        public AssetId MapColorFiller { get; protected set; }
        public AssetId MapColorWalkable { get; protected set; }
        public AssetId MapColorWall { get; protected set; }
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
        public AssetId MapColorFillerConsole { get; protected set; }
        public AssetId MapColorWalkableConsole { get; protected set; }
        public AssetId MapColorWallConsole { get; protected set; }
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

        [DoNotCopy]
        public int MetricsFrequencyPrototypeEnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();
            MetricsFrequencyPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetMetricsFrequencyBlueprintDataRef());
        }
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
        public PrototypePropertyCollection Properties { get; protected set; }
    }

    public class PowerVisualsGlobalsPrototype : Prototype
    {
        public AssetId DailyMissionCompleteClass { get; protected set; }
        public AssetId UnlockPetTechR1CommonClass { get; protected set; }
        public AssetId UnlockPetTechR2UncommonClass { get; protected set; }
        public AssetId UnlockPetTechR3RareClass { get; protected set; }
        public AssetId UnlockPetTechR4EpicClass { get; protected set; }
        public AssetId UnlockPetTechR5CosmicClass { get; protected set; }
        public AssetId LootVaporizedClass { get; protected set; }
        public AssetId AchievementUnlockedClass { get; protected set; }
        public AssetId OmegaPointGainedClass { get; protected set; }
        public AssetId AvatarLeashTeleportClass { get; protected set; }
        public AssetId InfinityTimePointEarnedClass { get; protected set; }
        public AssetId InfinitySpacePointEarnedClass { get; protected set; }
        public AssetId InfinitySoulPointEarnedClass { get; protected set; }
        public AssetId InfinityMindPointEarnedClass { get; protected set; }
        public AssetId InfinityRealityPointEarnedClass { get; protected set; }
        public AssetId InfinityPowerPointEarnedClass { get; protected set; }
    }

    public class RankDefaultEntryPrototype : Prototype
    {
        public PrototypeId Data { get; protected set; }
        public Rank Rank { get; protected set; }
    }

    public class PopulationGlobalsPrototype : Prototype
    {
        public LocaleStringId MessageEnemiesGrowStronger { get; protected set; }
        public LocaleStringId MessageEnemiesGrowWeaker { get; protected set; }
        public int SpawnMapPoolTickMS { get; protected set; }
        public int SpawnMapLevelTickMS { get; protected set; }
        public float CrowdSupressionRadius { get; protected set; }
        public bool SupressSpawnOnPlayer { get; protected set; }
        public int SpawnMapGimbalRadius { get; protected set; }
        public int SpawnMapHorizon { get; protected set; }
        public float SpawnMapMaxChance { get; protected set; }
        public PrototypeId EmptyPopulation { get; protected set; }
        public PrototypeId TwinEnemyBoost { get; protected set; }
        public int DestructiblesForceSpawnMS { get; protected set; }
        public PrototypeId TwinEnemyCondition { get; protected set; }
        public int SpawnMapHeatPerSecondMax { get; protected set; }
        public int SpawnMapHeatPerSecondMin { get; protected set; }
        public int SpawnMapHeatPerSecondScalar { get; protected set; }
        public PrototypeId TwinEnemyRank { get; protected set; }
        public RankDefaultEntryPrototype[] RankDefaults { get; protected set; }

        private RankPrototype[] _rankEnumToProto;

        public override void PostProcess()
        {
            base.PostProcess();

            if (RankDefaults.IsNullOrEmpty()) return;

            _rankEnumToProto = new RankPrototype[(int)Rank.Max];
            foreach (var rankEntry in RankDefaults)
            {
                int rankAsIndex = (int)rankEntry.Rank;
                if (rankAsIndex < 0 || rankAsIndex >= (int)Rank.Max) continue;
                _rankEnumToProto[rankAsIndex] = rankEntry.Data.As<RankPrototype>();
            }
        }

        public RankPrototype GetRankByEnum(Rank rank)
        {
            int rankAsIndex = (int)rank;
            if (rankAsIndex < 0 || rankAsIndex >= (int)Rank.Max) return null;
            return _rankEnumToProto[rankAsIndex];
        }
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
        public PrototypeId EvalInterruptChanceFormula { get; protected set; }
        public PrototypeId EvalNegStatusResistPctFormula { get; protected set; }
        public PrototypeId ChannelInterruptCondition { get; protected set; }
        public EvalPrototype EvalDamageReduction { get; protected set; }
        public EvalPrototype EvalCritChanceFormula { get; protected set; }
        public EvalPrototype EvalSuperCritChanceFormula { get; protected set; }
        public EvalPrototype EvalDamageRatingFormula { get; protected set; }
        public EvalPrototype EvalCritDamageRatingFormula { get; protected set; }
        public EvalPrototype EvalDodgeChanceFormula { get; protected set; }
        public EvalPrototype EvalDamageReductionDefenseOnly { get; protected set; }
        public EvalPrototype EvalDamageReductionForDisplay { get; protected set; }
        public float TravelPowerMaxSpeed { get; protected set; }
        public PrototypeId TUSynergyBonusPerLvl { get; protected set; }
        public PrototypeId TUSynergyBonusPerMaxLvlTU { get; protected set; }

        public float GetHardcoreAttenuationFactor(PropertyCollection properties)
        {
            int numberOfDeaths = properties[PropertyEnum.NumberOfDeaths];
            return Math.Clamp(1f - (PowerDmgBonusHardcoreAttenuation * numberOfDeaths), 0f, 1f);
        }
    }

    public class VendorXPCapInfoPrototype : Prototype
    {
        public PrototypeId Vendor { get; protected set; }
        public int Cap { get; protected set; }
        public float WallClockTime24Hr { get; protected set; }
        public Weekday WallClockTimeDay { get; protected set; }
    }

    public class AffixCategoryTableEntryPrototype : Prototype
    {
        public PrototypeId Category { get; protected set; }
        public PrototypeId[] Affixes { get; protected set; }
    }

    public class LootGlobalsPrototype : Prototype
    {
        public CurveId LootBonusRarityCurve { get; protected set; }
        public CurveId LootBonusSpecialCurve { get; protected set; }
        public PrototypeId LootContainerKeyword { get; protected set; }
        public float LootDropScalar { get; protected set; }
        public int LootInitializationLevelOffset { get; protected set; }
        public CurveId LootLevelDistribution { get; protected set; }
        public float LootRarityScalar { get; protected set; }
        public float LootSpecialItemFindScalar { get; protected set; }
        public float LootUnrestedSpecialFindScalar { get; protected set; }
        public float LootUsableByRecipientPercent { get; protected set; }
        public PrototypeId NoLootTable { get; protected set; }
        public PrototypeId SpecialOnKilledLootTable { get; protected set; }
        public int SpecialOnKilledLootCooldownHours { get; protected set; }
        public PrototypeId RarityCosmic { get; protected set; }
        public CurveId LootBonusFlatCreditsCurve { get; protected set; }
        public PrototypeId RarityUruForged { get; protected set; }
        public PrototypeId LootTableBlueprint { get; protected set; }
        public PrototypeId RarityUnique { get; protected set; }
        public int LootLevelMaxForDrops { get; protected set; }
        public PrototypeId InsigniaBlueprint { get; protected set; }
        public PrototypeId UniquesBoxCheatItem { get; protected set; }
        public PrototypeId[] EmptySocketAffixes { get; protected set; }
        public PrototypeId GemBlueprint { get; protected set; }
        public VendorXPCapInfoPrototype[] VendorXPCapInfo { get; protected set; }
        public float DropDistanceThreshold { get; protected set; }
        public AffixCategoryTableEntryPrototype[] AffixCategoryTable { get; protected set; }
        public CurveId BonusItemFindCurve { get; protected set; }
        public int BonusItemFindNumPointsForBonus { get; protected set; }
        public PrototypeId BonusItemFindLootTable { get; protected set; }
        public float LootCoopPlayerRewardPct { get; protected set; }
        public PrototypeId RarityDefault { get; protected set; }
    }

    public class MatchQueueStringEntryPrototype : Prototype
    {
        public RegionRequestQueueUpdateVarAssetEnum StatusKey { get; protected set; }  // Regions/QueueStatus.type
        public LocaleStringId StringLog { get; protected set; }
        public LocaleStringId StringStatus { get; protected set; }
    }

    public class TransitionGlobalsPrototype : Prototype
    {
        public RegionPortalControlEntryPrototype[] ControlledRegions { get; protected set; }
        public PrototypeId EnabledState { get; protected set; }
        public PrototypeId DisabledState { get; protected set; }
        public MatchQueueStringEntryPrototype[] QueueStrings { get; protected set; }
        public AssetId TransitionEmptyClass { get; protected set; }
    }

    public class KeywordGlobalsPrototype : Prototype
    {
        public PrototypeId PowerKeywordPrototype { get; protected set; }
        public PrototypeId DestructibleKeyword { get; protected set; }
        public PrototypeId PetPowerKeyword { get; protected set; }
        public PrototypeId VacuumableKeyword { get; protected set; }
        public PrototypeId EntityKeywordPrototype { get; protected set; }
        public PrototypeId BodysliderPowerKeyword { get; protected set; }
        public PrototypeId OrbEntityKeyword { get; protected set; }
        public PrototypeId UltimatePowerKeyword { get; protected set; }
        public PrototypeId MeleePowerKeyword { get; protected set; }
        public PrototypeId RangedPowerKeyword { get; protected set; }
        public PrototypeId BasicPowerKeyword { get; protected set; }
        public PrototypeId TeamUpSpecialPowerKeyword { get; protected set; }
        public PrototypeId TeamUpDefaultPowerKeyword { get; protected set; }
        public PrototypeId StealthPowerKeyword { get; protected set; }
        public PrototypeId TeamUpAwayPowerKeyword { get; protected set; }
        public PrototypeId VanityPetKeyword { get; protected set; }
        public PrototypeId ControlPowerKeywordPrototype { get; protected set; }
        public PrototypeId AreaPowerKeyword { get; protected set; }
        public PrototypeId EnergyPowerKeyword { get; protected set; }
        public PrototypeId MentalPowerKeyword { get; protected set; }
        public PrototypeId PhysicalPowerKeyword { get; protected set; }
        public PrototypeId MedKitKeyword { get; protected set; }
        public PrototypeId OrbExperienceEntityKeyword { get; protected set; }
        public PrototypeId TutorialRegionKeyword { get; protected set; }
        public PrototypeId TeamUpKeyword { get; protected set; }
        public PrototypeId MovementPowerKeyword { get; protected set; }
        public PrototypeId ThrownPowerKeyword { get; protected set; }
        public PrototypeId HoloSimKeyword { get; protected set; }
        public PrototypeId ControlledSummonDurationKeyword { get; protected set; }
        public PrototypeId TreasureRoomKeyword { get; protected set; }
        public PrototypeId DangerRoomKeyword { get; protected set; }
        public PrototypeId StealingPowerKeyword { get; protected set; }
        public PrototypeId SummonPowerKeyword { get; protected set; }
    }

    public class CurrencyGlobalsPrototype : Prototype
    {
        public PrototypeId CosmicWorldstones { get; protected set; }
        public PrototypeId Credits { get; protected set; }
        public PrototypeId CubeShards { get; protected set; }
        public PrototypeId EternitySplinters { get; protected set; }
        public PrototypeId EyesOfDemonfire { get; protected set; }
        public PrototypeId HeartsOfDemonfire { get; protected set; }
        public PrototypeId LegendaryMarks { get; protected set; }
        public PrototypeId OmegaFiles { get; protected set; }
        public PrototypeId PvPCrowns { get; protected set; }
        public PrototypeId ResearchDrives { get; protected set; }
        public PrototypeId GenoshaRaid { get; protected set; }
        public PrototypeId DangerRoomMerits { get; protected set; }
        public PrototypeId GazillioniteGs { get; protected set; }
    }

    public class GamepadInputAssetPrototype : Prototype
    {
        public GamepadInput Input { get; protected set; }
        public AssetId DualShockPath { get; protected set; }
        public AssetId XboxPath { get; protected set; }
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
        public LocaleStringId OrbisDefaultSessionDescription { get; protected set; }
        public PrototypeId OrbisDefaultSessionImage { get; protected set; }
        public int OrbisMaxSessionSize { get; protected set; }
        public int MaxSuggestedPlayers { get; protected set; }
        public PrototypeId OrbisPlayerCameraSettings { get; protected set; }
        public LocaleStringId OrbisFriendsInvitationDialogDesc { get; protected set; }
        public int OrbisMaxFriendInvites { get; protected set; }
        public LocaleStringId OrbisFriendsSuggestionDialogDesc { get; protected set; }
        public int OrbisMaxFriendSuggestions { get; protected set; }
    }

    public class AvatarOnKilledInfoPrototype : Prototype
    {
        public DeathReleaseBehavior DeathReleaseBehavior { get; protected set; }
        public LocaleStringId DeathReleaseButton { get; protected set; }
        public LocaleStringId DeathReleaseDialogMessage { get; protected set; }
        public int DeathReleaseTimeoutMS { get; protected set; }
        public LocaleStringId ResurrectionDialogMessage { get; protected set; }
        public int ResurrectionTimeoutMS { get; protected set; }
        public int RespawnLockoutMS { get; protected set; }
    }

    public class GlobalEventPrototype : Prototype
    {
        public bool Active { get; protected set; }
        public PrototypeId[] CriteriaList { get; protected set; }
        public GlobalEventCriteriaLogic CriteriaLogic { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public int LeaderboardLength { get; protected set; }
    }

    public class GlobalEventCriteriaPrototype : Prototype
    {
        public AssetId DisplayColor { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public int Score { get; protected set; }
        public int ThresholdCount { get; protected set; }
        public LocaleStringId DisplayTooltip { get; protected set; }
    }

    public class GlobalEventCriteriaItemCollectPrototype : GlobalEventCriteriaPrototype
    {
        public EntityFilterPrototype ItemFilter { get; protected set; }
    }
}
