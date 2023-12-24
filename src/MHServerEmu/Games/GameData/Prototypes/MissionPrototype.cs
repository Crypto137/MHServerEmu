using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum DailyMissionBannerImageType     // UI/Types/DailyMissionBannerType.type
    {
        Asgard = 0,
        CastleAndMoon = 1,
        Cave = 2,
        CityNight = 3,
        CityNight_v2 = 4,
        MetalFloor = 5,
        Nightclub = 6,
        Norway = 7,
        Odin = 8,
        RedMoon = 9,
        Space = 10,
        TowerBaseSunset = 11,
    }

    [AssetEnum]
    public enum MissionTimeExpiredResult        // Missions/Types/OnTimeExpired.type
    {
        Invalid = 0,
        Complete = 1,
        Fail = 2,
    }

    [AssetEnum]
    public enum MissionShowInTracker            // Missions/Types/ShowInTracker.type
    {
        Never = 0,
        IfObjectivesVisible = 1,
        Always = 2,
    }

    [AssetEnum]
    public enum MissionShowInLog                // Missions/Types/ShowInMissionLog.type
    {
        Never = 0,
        OnlyWhenActive = 1,
        Always = 2,
    }

    [AssetEnum]
    public enum DailyMissionType
    {
        Patrol = 0,
        Survival = 1,
        Terminal = 2,
    }

    [AssetEnum]
    public enum DailyMissionResetFrequency
    {
        Daily = 0,
        Weekly = 1,
    }

    [AssetEnum]
    public enum AdvancedMissionFrequencyType
    {
        Invalid = 0,
        Repeatable = 1,
        Daily = 2,
        Weekly = 3,
    }

    #endregion

    public class MissionGlobalsPrototype : Prototype
    {
        public int MissionLevelLowerBoundsOffset { get; private set; }
        public int MissionLevelUpperBoundsOffset { get; private set; }
        public ulong OpenMissionContributionReward { get; private set; }
        public ulong InitialChapter { get; private set; }
        public BannerMessagePrototype InventoryFullMessage { get; private set; }
        public ulong InitialStoryWarp { get; private set; }
        public ulong MigrationStoryEndMission { get; private set; }
        public int LegendaryMissionLevelUnlock { get; private set; }
        public ulong LegendaryChapter { get; private set; }
        public ulong LegendaryMissionPlaceholder { get; private set; }
        public EvalPrototype LegendaryRerollCost { get; private set; }
        public ulong LegendaryMissionLogTooltip { get; private set; }
        public ulong LoreChapter { get; private set; }
        public DailyMissionBannerImageType DailyMissionBannerFriday { get; private set; }
        public DailyMissionBannerImageType DailyMissionBannerMonday { get; private set; }
        public DailyMissionBannerImageType DailyMissionBannerSaturday { get; private set; }
        public DailyMissionBannerImageType DailyMissionBannerSunday { get; private set; }
        public DailyMissionBannerImageType DailyMissionBannerThursday { get; private set; }
        public DailyMissionBannerImageType DailyMissionBannerTuesday { get; private set; }
        public DailyMissionBannerImageType DailyMissionBannerWednesday { get; private set; }
        public ulong EventMissionsChapter { get; private set; }
        public ulong AccountMissionsChapter { get; private set; }
    }

    public class MissionTypePrototype : Prototype
    {
        public ulong Name { get; private set; }
        public int Priority { get; private set; }
        public ulong EdgeIcon { get; private set; }
        public ulong MapIcon { get; private set; }
    }

    public class MissionItemDropEntryPrototype : Prototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
        public ulong LootTablePrototype { get; private set; }
    }

    public class MissionPopulationEntryPrototype : Prototype
    {
        public long Count { get; private set; }
        public PopulationObjectPrototype Population { get; private set; }
        public ulong[] RestrictToAreas { get; private set; }
        public RegionPrototype RestrictToRegions { get; private set; }
        public RegionPrototype RestrictToRegionsExclude { get; private set; }
        public bool RestrictToRegionsIncludeChildren { get; private set; }
        public ulong[] RestrictToCells { get; private set; }
        public ulong RestrictToDifficultyMin { get; private set; }
        public ulong RestrictToDifficultyMax { get; private set; }
    }

    public class MissionDialogTextPrototype : Prototype
    {
        public ulong Text { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
        public DialogStyle DialogStyle { get; private set; }
    }

    public class MissionObjectiveHintPrototype : Prototype
    {
        public EntityFilterPrototype PlayerStateFilter { get; private set; }
        public EntityFilterPrototype TargetEntity { get; private set; }
        public ulong TargetArea { get; private set; }
        public ulong TargetRegion { get; private set; }
    }

    public class MissionObjectivePrototype : Prototype
    {
        public MissionDialogTextPrototype[] DialogText { get; private set; }
        public MissionConditionListPrototype FailureConditions { get; private set; }
        public MissionItemDropEntryPrototype[] ItemDrops { get; private set; }
        public bool ItemDropsCleanupRemaining { get; private set; }
        public ulong Name { get; private set; }
        public MissionActionPrototype[] OnFailActions { get; private set; }
        public MissionActionPrototype[] OnStartActions { get; private set; }
        public MissionActionPrototype[] OnSuccessActions { get; private set; }
        public MissionConditionListPrototype ActivateConditions { get; private set; }
        public MissionConditionListPrototype SuccessConditions { get; private set; }
        public MissionTimeExpiredResult TimeExpiredResult { get; private set; }
        public long TimeLimitSeconds { get; private set; }
        public InteractionSpecPrototype[] InteractionsWhenActive { get; private set; }
        public InteractionSpecPrototype[] InteractionsWhenComplete { get; private set; }
        public ulong TextWhenCompleted { get; private set; }
        public ulong TextWhenUpdated { get; private set; }
        public bool ShowInMissionLog { get; private set; }
        public bool Required { get; private set; }
        public bool ShowNotificationIcon { get; private set; }
        public bool Checkpoint { get; private set; }
        public bool ShowInMissionTracker { get; private set; }
        public ulong MissionLogAppendWhenActive { get; private set; }
        public bool PlayerHUDShowObjsOnMap { get; private set; }
        public bool PlayerHUDShowObjsOnMapNoPing { get; private set; }
        public bool PlayerHUDShowObjsOnScreenEdge { get; private set; }
        public bool PlayerHUDShowObjsOnEntityFloor { get; private set; }
        public int PlayerHUDObjectiveArrowDistOvrde { get; private set; }
        public MissionObjectiveHintPrototype[] ObjectiveHints { get; private set; }
        public bool ShowCountInUI { get; private set; }
        public bool ShowTimerInUI { get; private set; }
        public float Order { get; private set; }
        public ulong MetaGameWidget { get; private set; }
        public ulong MetaGameWidgetFail { get; private set; }
        public bool FailureFailsMission { get; private set; }
        public bool ShowFailCountInUI { get; private set; }
        public ulong TextWhenFailed { get; private set; }
        public ulong TextWhenFailUpdated { get; private set; }
        public EvalPrototype TimeLimitSecondsEval { get; private set; }
        public LootTablePrototype[] Rewards { get; private set; }
        public int CounterType { get; private set; }
        public ulong MetaGameDetails { get; private set; }
        public int MetaGameDetailsDelayMS { get; private set; }
        public ulong MetaGameDetailsNPCIconPath { get; private set; }
        public ulong LogoffEntryDisplayIfNotComplete { get; private set; }
        public ulong MissionLogObjectiveHint { get; private set; }
        public ulong MusicState { get; private set; }
        public MissionDialogTextPrototype[] DialogTextWhenCompleted { get; private set; }
        public MissionDialogTextPrototype[] DialogTextWhenFailed { get; private set; }
        public InteractionSpecPrototype[] InteractionsWhenFailed { get; private set; }
        public bool PlayerHUDShowObjsOnEntityAbove { get; private set; }
        public MissionActionPrototype[] OnAvailableActions { get; private set; }
    }

    public class MissionNamedObjectivePrototype : MissionObjectivePrototype
    {
        public long ObjectiveID { get; private set; }
        public bool SendMetricEvents { get; private set; }
    }

    public class OpenMissionRewardEntryPrototype : Prototype
    {
        public ulong ChestEntity { get; private set; }
        public double ContributionPercentage { get; private set; }
        public ulong[] Rewards { get; private set; }
    }

    public class MissionPrototype : Prototype
    {
        public MissionConditionListPrototype ActivateConditions { get; private set; }
        public ulong Chapter { get; private set; }
        public MissionDialogTextPrototype[] DialogText { get; private set; }
        public MissionConditionListPrototype FailureConditions { get; private set; }
        public long Level { get; private set; }
        public ulong MissionLogDescription { get; private set; }
        public ulong Name { get; private set; }
        public MissionObjectivePrototype[] Objectives { get; private set; }
        public MissionActionPrototype[] OnFailActions { get; private set; }
        public MissionActionPrototype[] OnStartActions { get; private set; }
        public MissionActionPrototype[] OnSuccessActions { get; private set; }
        public MissionPopulationEntryPrototype[] PopulationSpawns { get; private set; }
        public MissionConditionListPrototype PrereqConditions { get; private set; }
        public bool Repeatable { get; private set; }
        public LootTablePrototype[] Rewards { get; private set; }
        public MissionTimeExpiredResult TimeExpiredResult { get; private set; }
        public long TimeLimitSeconds { get; private set; }
        public InteractionSpecPrototype[] InteractionsWhenActive { get; private set; }
        public InteractionSpecPrototype[] InteractionsWhenComplete { get; private set; }
        public ulong TextWhenActivated { get; private set; }
        public ulong TextWhenCompleted { get; private set; }
        public ulong TextWhenFailed { get; private set; }
        public bool ShowInteractIndicators { get; private set; }
        public bool ShowBannerMessages { get; private set; }
        public bool ShowInMissionLogDEPRECATED { get; private set; }
        public bool ShowNotificationIcon { get; private set; }
        public int SortOrder { get; private set; }
        public MissionConditionListPrototype ActivateNowConditions { get; private set; }
        public MissionShowInTracker ShowInMissionTracker { get; private set; }
        public ulong ResetsWithRegion { get; private set; }
        public ulong MissionLogDescriptionComplete { get; private set; }
        public bool PlayerHUDShowObjs { get; private set; }
        public bool PlayerHUDShowObjsOnMap { get; private set; }
        public bool PlayerHUDShowObjsOnMapNoPing { get; private set; }
        public bool PlayerHUDShowObjsOnScreenEdge { get; private set; }
        public bool PlayerHUDShowObjsOnEntityFloor { get; private set; }
        public bool PlayerHUDShowObjsNoActivateCond { get; private set; }
        public DesignWorkflowState DesignState { get; private set; }
        public long ResetTimeSeconds { get; private set; }
        public bool ShowInMissionTrackerFilterByChap { get; private set; }
        public bool ShowMapPingOnPortals { get; private set; }
        public bool PopulationRequired { get; private set; }
        public bool SaveStatePerAvatar { get; private set; }
        public MissionTrackerFilterType ShowInMissionTrackerFilterType { get; private set; }
        public int Version { get; private set; }
        public bool DEPRewardLevelBasedOnAvatarLevel { get; private set; }
        public ulong MissionLogHint { get; private set; }
        public ulong LootCooldownChannel { get; private set; }
        public ulong MetaGameDetails { get; private set; }
        public int MetaGameDetailsDelayMS { get; private set; }
        public bool ShowTimerInUI { get; private set; }
        public ulong MetaGameDetailsNPCIconPath { get; private set; }
        public bool DropLootOnGround { get; private set; }
        public ulong[] Keywords { get; private set; }
        public ulong[] RegionRestrictionKeywords { get; private set; }
        public bool ForceTrackerPageOnStart { get; private set; }
        public ulong MusicState { get; private set; }
        public MissionDialogTextPrototype[] DialogTextWhenCompleted { get; private set; }
        public MissionDialogTextPrototype[] DialogTextWhenFailed { get; private set; }
        public InteractionSpecPrototype[] InteractionsWhenFailed { get; private set; }
        public bool PlayerHUDShowObjsOnEntityAbove { get; private set; }
        public ulong MissionType { get; private set; }
        public MissionShowInLog ShowInMissionLog { get; private set; }
        public bool SuspendIfNoMatchingKeyword { get; private set; }
        public MissionActionPrototype[] OnAvailableActions { get; private set; }
        public MissionConditionListPrototype CompleteNowConditions { get; private set; }
        public LootTablePrototype[] CompleteNowRewards { get; private set; }
        public DesignWorkflowState DesignStatePS4 { get; private set; }
        public DesignWorkflowState DesignStateXboxOne { get; private set; }
    }

    public class OpenMissionPrototype : MissionPrototype
    {
        public bool ParticipationBasedOnAreaCell { get; private set; }
        public OpenMissionRewardEntryPrototype[] RewardsByContribution { get; private set; }
        public StoryNotificationPrototype StoryNotification { get; private set; }
        public RegionPrototype ActiveInRegions { get; private set; }
        public bool ActiveInRegionsIncludeChildren { get; private set; }
        public RegionPrototype ActiveInRegionsExclude { get; private set; }
        public ulong[] ActiveInAreas { get; private set; }
        public ulong[] ActiveInCells { get; private set; }
        public bool ResetWhenUnsimulated { get; private set; }
        public double MinimumContributionForCredit { get; private set; }
        public bool RespawnInPlace { get; private set; }
        public double ParticipantTimeoutInSeconds { get; private set; }
        public bool RespawnOnRestart { get; private set; }
        public int IdleTimeoutSeconds { get; private set; }
        public double ParticipationContributionValue { get; private set; }
        public long AchievementTimeLimitSeconds { get; private set; }
        public bool ShowToastMessages { get; private set; }
    }

    public class LegendaryMissionCategoryPrototype : Prototype
    {
        public ulong Name { get; private set; }
        public int Weight { get; private set; }
        public int BlacklistLength { get; private set; }
    }

    public class LegendaryMissionPrototype : MissionPrototype
    {
        public EvalPrototype EvalCanStart { get; private set; }
        public ulong Category { get; private set; }
    }

    public class DailyMissionPrototype : MissionPrototype
    {
        public Weekday Day { get; private set; }
        public DailyMissionType Type { get; private set; }
        public ulong Image { get; private set; }
        public DailyMissionResetFrequency ResetFrequency { get; private set; }
    }

    public class AdvancedMissionCategoryPrototype : LegendaryMissionCategoryPrototype
    {
        public Weekday WeeklyResetDay { get; private set; }
        public AdvancedMissionFrequencyType MissionType { get; private set; }
        public ulong CategoryLabel { get; private set; }
    }

    public class AdvancedMissionPrototype : MissionPrototype
    {
        public ulong CategoryType { get; private set; }
        public ulong ReputationExperienceType { get; private set; }
    }
}
