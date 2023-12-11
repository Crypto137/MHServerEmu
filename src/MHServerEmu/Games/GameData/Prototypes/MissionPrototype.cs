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
        public int MissionLevelLowerBoundsOffset { get; set; }
        public int MissionLevelUpperBoundsOffset { get; set; }
        public ulong OpenMissionContributionReward { get; set; }
        public ulong InitialChapter { get; set; }
        public BannerMessagePrototype InventoryFullMessage { get; set; }
        public ulong InitialStoryWarp { get; set; }
        public ulong MigrationStoryEndMission { get; set; }
        public int LegendaryMissionLevelUnlock { get; set; }
        public ulong LegendaryChapter { get; set; }
        public ulong LegendaryMissionPlaceholder { get; set; }
        public EvalPrototype LegendaryRerollCost { get; set; }
        public ulong LegendaryMissionLogTooltip { get; set; }
        public ulong LoreChapter { get; set; }
        public DailyMissionBannerImageType DailyMissionBannerFriday { get; set; }
        public DailyMissionBannerImageType DailyMissionBannerMonday { get; set; }
        public DailyMissionBannerImageType DailyMissionBannerSaturday { get; set; }
        public DailyMissionBannerImageType DailyMissionBannerSunday { get; set; }
        public DailyMissionBannerImageType DailyMissionBannerThursday { get; set; }
        public DailyMissionBannerImageType DailyMissionBannerTuesday { get; set; }
        public DailyMissionBannerImageType DailyMissionBannerWednesday { get; set; }
        public ulong EventMissionsChapter { get; set; }
        public ulong AccountMissionsChapter { get; set; }
    }

    public class MissionTypePrototype : Prototype
    {
        public ulong Name { get; set; }
        public int Priority { get; set; }
        public ulong EdgeIcon { get; set; }
        public ulong MapIcon { get; set; }
    }

    public class MissionItemDropEntryPrototype : Prototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
        public ulong LootTablePrototype { get; set; }
    }

    public class MissionPopulationEntryPrototype : Prototype
    {
        public long Count { get; set; }
        public PopulationObjectPrototype Population { get; set; }
        public ulong[] RestrictToAreas { get; set; }
        public RegionPrototype RestrictToRegions { get; set; }
        public RegionPrototype RestrictToRegionsExclude { get; set; }
        public bool RestrictToRegionsIncludeChildren { get; set; }
        public ulong[] RestrictToCells { get; set; }
        public ulong RestrictToDifficultyMin { get; set; }
        public ulong RestrictToDifficultyMax { get; set; }
    }

    public class MissionDialogTextPrototype : Prototype
    {
        public ulong Text { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
        public DialogStyle DialogStyle { get; set; }
    }

    public class MissionObjectiveHintPrototype : Prototype
    {
        public EntityFilterPrototype PlayerStateFilter { get; set; }
        public EntityFilterPrototype TargetEntity { get; set; }
        public ulong TargetArea { get; set; }
        public ulong TargetRegion { get; set; }
    }

    public class MissionObjectivePrototype : Prototype
    {
        public MissionDialogTextPrototype[] DialogText { get; set; }
        public MissionConditionListPrototype FailureConditions { get; set; }
        public MissionItemDropEntryPrototype[] ItemDrops { get; set; }
        public bool ItemDropsCleanupRemaining { get; set; }
        public ulong Name { get; set; }
        public MissionActionPrototype[] OnFailActions { get; set; }
        public MissionActionPrototype[] OnStartActions { get; set; }
        public MissionActionPrototype[] OnSuccessActions { get; set; }
        public MissionConditionListPrototype ActivateConditions { get; set; }
        public MissionConditionListPrototype SuccessConditions { get; set; }
        public MissionTimeExpiredResult TimeExpiredResult { get; set; }
        public long TimeLimitSeconds { get; set; }
        public InteractionSpecPrototype[] InteractionsWhenActive { get; set; }
        public InteractionSpecPrototype[] InteractionsWhenComplete { get; set; }
        public ulong TextWhenCompleted { get; set; }
        public ulong TextWhenUpdated { get; set; }
        public bool ShowInMissionLog { get; set; }
        public bool Required { get; set; }
        public bool ShowNotificationIcon { get; set; }
        public bool Checkpoint { get; set; }
        public bool ShowInMissionTracker { get; set; }
        public ulong MissionLogAppendWhenActive { get; set; }
        public bool PlayerHUDShowObjsOnMap { get; set; }
        public bool PlayerHUDShowObjsOnMapNoPing { get; set; }
        public bool PlayerHUDShowObjsOnScreenEdge { get; set; }
        public bool PlayerHUDShowObjsOnEntityFloor { get; set; }
        public int PlayerHUDObjectiveArrowDistOvrde { get; set; }
        public MissionObjectiveHintPrototype[] ObjectiveHints { get; set; }
        public bool ShowCountInUI { get; set; }
        public bool ShowTimerInUI { get; set; }
        public float Order { get; set; }
        public ulong MetaGameWidget { get; set; }
        public ulong MetaGameWidgetFail { get; set; }
        public bool FailureFailsMission { get; set; }
        public bool ShowFailCountInUI { get; set; }
        public ulong TextWhenFailed { get; set; }
        public ulong TextWhenFailUpdated { get; set; }
        public EvalPrototype TimeLimitSecondsEval { get; set; }
        public LootTablePrototype[] Rewards { get; set; }
        public int CounterType { get; set; }
        public ulong MetaGameDetails { get; set; }
        public int MetaGameDetailsDelayMS { get; set; }
        public ulong MetaGameDetailsNPCIconPath { get; set; }
        public ulong LogoffEntryDisplayIfNotComplete { get; set; }
        public ulong MissionLogObjectiveHint { get; set; }
        public ulong MusicState { get; set; }
        public MissionDialogTextPrototype[] DialogTextWhenCompleted { get; set; }
        public MissionDialogTextPrototype[] DialogTextWhenFailed { get; set; }
        public InteractionSpecPrototype[] InteractionsWhenFailed { get; set; }
        public bool PlayerHUDShowObjsOnEntityAbove { get; set; }
        public MissionActionPrototype[] OnAvailableActions { get; set; }
    }

    public class MissionNamedObjectivePrototype : MissionObjectivePrototype
    {
        public long ObjectiveID { get; set; }
        public bool SendMetricEvents { get; set; }
    }

    public class OpenMissionRewardEntryPrototype : Prototype
    {
        public ulong ChestEntity { get; set; }
        public double ContributionPercentage { get; set; }
        public ulong[] Rewards { get; set; }
    }

    public class MissionPrototype : Prototype
    {
        public MissionConditionListPrototype ActivateConditions { get; set; }
        public ulong Chapter { get; set; }
        public MissionDialogTextPrototype[] DialogText { get; set; }
        public MissionConditionListPrototype FailureConditions { get; set; }
        public long Level { get; set; }
        public ulong MissionLogDescription { get; set; }
        public ulong Name { get; set; }
        public MissionObjectivePrototype[] Objectives { get; set; }
        public MissionActionPrototype[] OnFailActions { get; set; }
        public MissionActionPrototype[] OnStartActions { get; set; }
        public MissionActionPrototype[] OnSuccessActions { get; set; }
        public MissionPopulationEntryPrototype[] PopulationSpawns { get; set; }
        public MissionConditionListPrototype PrereqConditions { get; set; }
        public bool Repeatable { get; set; }
        public LootTablePrototype[] Rewards { get; set; }
        public MissionTimeExpiredResult TimeExpiredResult { get; set; }
        public long TimeLimitSeconds { get; set; }
        public InteractionSpecPrototype[] InteractionsWhenActive { get; set; }
        public InteractionSpecPrototype[] InteractionsWhenComplete { get; set; }
        public ulong TextWhenActivated { get; set; }
        public ulong TextWhenCompleted { get; set; }
        public ulong TextWhenFailed { get; set; }
        public bool ShowInteractIndicators { get; set; }
        public bool ShowBannerMessages { get; set; }
        public bool ShowInMissionLogDEPRECATED { get; set; }
        public bool ShowNotificationIcon { get; set; }
        public int SortOrder { get; set; }
        public MissionConditionListPrototype ActivateNowConditions { get; set; }
        public MissionShowInTracker ShowInMissionTracker { get; set; }
        public ulong ResetsWithRegion { get; set; }
        public ulong MissionLogDescriptionComplete { get; set; }
        public bool PlayerHUDShowObjs { get; set; }
        public bool PlayerHUDShowObjsOnMap { get; set; }
        public bool PlayerHUDShowObjsOnMapNoPing { get; set; }
        public bool PlayerHUDShowObjsOnScreenEdge { get; set; }
        public bool PlayerHUDShowObjsOnEntityFloor { get; set; }
        public bool PlayerHUDShowObjsNoActivateCond { get; set; }
        public DesignWorkflowState DesignState { get; set; }
        public long ResetTimeSeconds { get; set; }
        public bool ShowInMissionTrackerFilterByChap { get; set; }
        public bool ShowMapPingOnPortals { get; set; }
        public bool PopulationRequired { get; set; }
        public bool SaveStatePerAvatar { get; set; }
        public MissionTrackerFilterType ShowInMissionTrackerFilterType { get; set; }
        public int Version { get; set; }
        public bool DEPRewardLevelBasedOnAvatarLevel { get; set; }
        public ulong MissionLogHint { get; set; }
        public ulong LootCooldownChannel { get; set; }
        public ulong MetaGameDetails { get; set; }
        public int MetaGameDetailsDelayMS { get; set; }
        public bool ShowTimerInUI { get; set; }
        public ulong MetaGameDetailsNPCIconPath { get; set; }
        public bool DropLootOnGround { get; set; }
        public ulong[] Keywords { get; set; }
        public ulong[] RegionRestrictionKeywords { get; set; }
        public bool ForceTrackerPageOnStart { get; set; }
        public ulong MusicState { get; set; }
        public MissionDialogTextPrototype[] DialogTextWhenCompleted { get; set; }
        public MissionDialogTextPrototype[] DialogTextWhenFailed { get; set; }
        public InteractionSpecPrototype[] InteractionsWhenFailed { get; set; }
        public bool PlayerHUDShowObjsOnEntityAbove { get; set; }
        public ulong MissionType { get; set; }
        public MissionShowInLog ShowInMissionLog { get; set; }
        public bool SuspendIfNoMatchingKeyword { get; set; }
        public MissionActionPrototype[] OnAvailableActions { get; set; }
        public MissionConditionListPrototype CompleteNowConditions { get; set; }
        public LootTablePrototype[] CompleteNowRewards { get; set; }
        public DesignWorkflowState DesignStatePS4 { get; set; }
        public DesignWorkflowState DesignStateXboxOne { get; set; }
    }

    public class OpenMissionPrototype : MissionPrototype
    {
        public bool ParticipationBasedOnAreaCell { get; set; }
        public OpenMissionRewardEntryPrototype[] RewardsByContribution { get; set; }
        public StoryNotificationPrototype StoryNotification { get; set; }
        public RegionPrototype ActiveInRegions { get; set; }
        public bool ActiveInRegionsIncludeChildren { get; set; }
        public RegionPrototype ActiveInRegionsExclude { get; set; }
        public ulong[] ActiveInAreas { get; set; }
        public ulong[] ActiveInCells { get; set; }
        public bool ResetWhenUnsimulated { get; set; }
        public double MinimumContributionForCredit { get; set; }
        public bool RespawnInPlace { get; set; }
        public double ParticipantTimeoutInSeconds { get; set; }
        public bool RespawnOnRestart { get; set; }
        public int IdleTimeoutSeconds { get; set; }
        public double ParticipationContributionValue { get; set; }
        public long AchievementTimeLimitSeconds { get; set; }
        public bool ShowToastMessages { get; set; }
    }

    public class LegendaryMissionCategoryPrototype : Prototype
    {
        public ulong Name { get; set; }
        public int Weight { get; set; }
        public int BlacklistLength { get; set; }
    }

    public class LegendaryMissionPrototype : MissionPrototype
    {
        public EvalPrototype EvalCanStart { get; set; }
        public ulong Category { get; set; }
    }

    public class DailyMissionPrototype : MissionPrototype
    {
        public Weekday Day { get; set; }
        public DailyMissionType Type { get; set; }
        public ulong Image { get; set; }
        public DailyMissionResetFrequency ResetFrequency { get; set; }
    }

    public class AdvancedMissionCategoryPrototype : LegendaryMissionCategoryPrototype
    {
        public Weekday WeeklyResetDay { get; set; }
        public AdvancedMissionFrequencyType MissionType { get; set; }
        public ulong CategoryLabel { get; set; }
    }

    public class AdvancedMissionPrototype : MissionPrototype
    {
        public ulong CategoryType { get; set; }
        public ulong ReputationExperienceType { get; set; }
    }
}
