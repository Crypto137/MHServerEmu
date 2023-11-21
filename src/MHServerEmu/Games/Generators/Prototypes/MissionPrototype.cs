using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class MissionGlobalsPrototype : Prototype
    {
        public int MissionLevelLowerBoundsOffset;
        public int MissionLevelUpperBoundsOffset;
        public ulong OpenMissionContributionReward;
        public ulong InitialChapter;
        public BannerMessagePrototype InventoryFullMessage;
        public ulong InitialStoryWarp;
        public ulong MigrationStoryEndMission;
        public int LegendaryMissionLevelUnlock;
        public ulong LegendaryChapter;
        public ulong LegendaryMissionPlaceholder;
        public EvalPrototype LegendaryRerollCost;
        public ulong LegendaryMissionLogTooltip;
        public ulong LoreChapter;
        public DailyMissionBannerImageType DailyMissionBannerFriday;
        public DailyMissionBannerImageType DailyMissionBannerMonday;
        public DailyMissionBannerImageType DailyMissionBannerSaturday;
        public DailyMissionBannerImageType DailyMissionBannerSunday;
        public DailyMissionBannerImageType DailyMissionBannerThursday;
        public DailyMissionBannerImageType DailyMissionBannerTuesday;
        public DailyMissionBannerImageType DailyMissionBannerWednesday;
        public ulong EventMissionsChapter;
        public ulong AccountMissionsChapter;
        public MissionGlobalsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionGlobalsPrototype), proto); }
    }

    public enum DailyMissionBannerImageType
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

    public class MissionTypePrototype : Prototype
    {
        public ulong Name;
        public int Priority;
        public ulong EdgeIcon;
        public ulong MapIcon;
        public MissionTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionTypePrototype), proto); }
    }

    public class MissionItemDropEntryPrototype : Prototype
    {
        public EntityFilterPrototype EntityFilter;
        public ulong LootTablePrototype;
        public MissionItemDropEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionItemDropEntryPrototype), proto); }
    }

    public class MissionPopulationEntryPrototype : Prototype
    {
        public long Count;
        public PopulationObjectPrototype Population;
        public ulong[] RestrictToAreas;
        public RegionPrototype RestrictToRegions;
        public RegionPrototype RestrictToRegionsExclude;
        public bool RestrictToRegionsIncludeChildren;
        public ulong[] RestrictToCells;
        public ulong RestrictToDifficultyMin;
        public ulong RestrictToDifficultyMax;
        public MissionPopulationEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionPopulationEntryPrototype), proto); }
    }

    public class MissionDialogTextPrototype : Prototype
    {
        public ulong Text;
        public EntityFilterPrototype EntityFilter;
        public DialogStyle DialogStyle;
        public MissionDialogTextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionDialogTextPrototype), proto); }
    }

    public class MissionObjectiveHintPrototype : Prototype
    {
        public EntityFilterPrototype PlayerStateFilter;
        public EntityFilterPrototype TargetEntity;
        public ulong TargetArea;
        public ulong TargetRegion;
        public MissionObjectiveHintPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionObjectiveHintPrototype), proto); }
    }

    public class MissionObjectivePrototype : Prototype
    {
        public MissionDialogTextPrototype[] DialogText;
        public MissionConditionListPrototype FailureConditions;
        public MissionItemDropEntryPrototype[] ItemDrops;
        public bool ItemDropsCleanupRemaining;
        public ulong Name;
        public MissionActionPrototype[] OnFailActions;
        public MissionActionPrototype[] OnStartActions;
        public MissionActionPrototype[] OnSuccessActions;
        public MissionConditionListPrototype ActivateConditions;
        public MissionConditionListPrototype SuccessConditions;
        public MissionTimeExpiredResult TimeExpiredResult;
        public long TimeLimitSeconds;
        public InteractionSpecPrototype[] InteractionsWhenActive;
        public InteractionSpecPrototype[] InteractionsWhenComplete;
        public ulong TextWhenCompleted;
        public ulong TextWhenUpdated;
        public bool ShowInMissionLog;
        public bool Required;
        public bool ShowNotificationIcon;
        public bool Checkpoint;
        public bool ShowInMissionTracker;
        public ulong MissionLogAppendWhenActive;
        public bool PlayerHUDShowObjsOnMap;
        public bool PlayerHUDShowObjsOnMapNoPing;
        public bool PlayerHUDShowObjsOnScreenEdge;
        public bool PlayerHUDShowObjsOnEntityFloor;
        public int PlayerHUDObjectiveArrowDistOvrde;
        public MissionObjectiveHintPrototype[] ObjectiveHints;
        public bool ShowCountInUI;
        public bool ShowTimerInUI;
        public float Order;
        public ulong MetaGameWidget;
        public ulong MetaGameWidgetFail;
        public bool FailureFailsMission;
        public bool ShowFailCountInUI;
        public ulong TextWhenFailed;
        public ulong TextWhenFailUpdated;
        public EvalPrototype TimeLimitSecondsEval;
        public LootTablePrototype[] Rewards;
        public int CounterType;
        public ulong MetaGameDetails;
        public int MetaGameDetailsDelayMS;
        public ulong MetaGameDetailsNPCIconPath;
        public ulong LogoffEntryDisplayIfNotComplete;
        public ulong MissionLogObjectiveHint;
        public ulong MusicState;
        public MissionDialogTextPrototype[] DialogTextWhenCompleted;
        public MissionDialogTextPrototype[] DialogTextWhenFailed;
        public InteractionSpecPrototype[] InteractionsWhenFailed;
        public bool PlayerHUDShowObjsOnEntityAbove;
        public MissionActionPrototype[] OnAvailableActions;
        public MissionObjectivePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionObjectivePrototype), proto); }
    }

    public enum MissionTimeExpiredResult
    {
        Invalid = 0,
        Complete = 1,
        Fail = 2,
    }

    public class MissionNamedObjectivePrototype : MissionObjectivePrototype
    {
        public long ObjectiveID;
        public bool SendMetricEvents;
        public MissionNamedObjectivePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionNamedObjectivePrototype), proto); }
    }

    public class OpenMissionRewardEntryPrototype : Prototype
    {
        public ulong ChestEntity;
        public double ContributionPercentage;
        public ulong[] Rewards;
        public OpenMissionRewardEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OpenMissionRewardEntryPrototype), proto); }
    }

    public class MissionPrototype : Prototype
    {
        public MissionConditionListPrototype ActivateConditions;
        public ulong Chapter;
        public MissionDialogTextPrototype[] DialogText;
        public MissionConditionListPrototype FailureConditions;
        public long Level;
        public ulong MissionLogDescription;
        public ulong Name;
        public MissionObjectivePrototype[] Objectives;
        public MissionActionPrototype[] OnFailActions;
        public MissionActionPrototype[] OnStartActions;
        public MissionActionPrototype[] OnSuccessActions;
        public MissionPopulationEntryPrototype[] PopulationSpawns;
        public MissionConditionListPrototype PrereqConditions;
        public bool Repeatable;
        public LootTablePrototype[] Rewards;
        public MissionTimeExpiredResult TimeExpiredResult;
        public long TimeLimitSeconds;
        public InteractionSpecPrototype[] InteractionsWhenActive;
        public InteractionSpecPrototype[] InteractionsWhenComplete;
        public ulong TextWhenActivated;
        public ulong TextWhenCompleted;
        public ulong TextWhenFailed;
        public bool ShowInteractIndicators;
        public bool ShowBannerMessages;
        public bool ShowInMissionLogDEPRECATED;
        public bool ShowNotificationIcon;
        public int SortOrder;
        public MissionConditionListPrototype ActivateNowConditions;
        public MissionShowInTracker ShowInMissionTracker;
        public ulong ResetsWithRegion;
        public ulong MissionLogDescriptionComplete;
        public bool PlayerHUDShowObjs;
        public bool PlayerHUDShowObjsOnMap;
        public bool PlayerHUDShowObjsOnMapNoPing;
        public bool PlayerHUDShowObjsOnScreenEdge;
        public bool PlayerHUDShowObjsOnEntityFloor;
        public bool PlayerHUDShowObjsNoActivateCond;
        public DesignWorkflowState DesignState;
        public long ResetTimeSeconds;
        public bool ShowInMissionTrackerFilterByChap;
        public bool ShowMapPingOnPortals;
        public bool PopulationRequired;
        public bool SaveStatePerAvatar;
        public MissionTrackerFilterType ShowInMissionTrackerFilterType;
        public int Version;
        public bool DEPRewardLevelBasedOnAvatarLevel;
        public ulong MissionLogHint;
        public ulong LootCooldownChannel;
        public ulong MetaGameDetails;
        public int MetaGameDetailsDelayMS;
        public bool ShowTimerInUI;
        public ulong MetaGameDetailsNPCIconPath;
        public bool DropLootOnGround;
        public ulong[] Keywords;
        public ulong[] RegionRestrictionKeywords;
        public bool ForceTrackerPageOnStart;
        public ulong MusicState;
        public MissionDialogTextPrototype[] DialogTextWhenCompleted;
        public MissionDialogTextPrototype[] DialogTextWhenFailed;
        public InteractionSpecPrototype[] InteractionsWhenFailed;
        public bool PlayerHUDShowObjsOnEntityAbove;
        public ulong MissionType;
        public MissionShowInLog ShowInMissionLog;
        public bool SuspendIfNoMatchingKeyword;
        public MissionActionPrototype[] OnAvailableActions;
        public MissionConditionListPrototype CompleteNowConditions;
        public LootTablePrototype[] CompleteNowRewards;
        public DesignWorkflowState DesignStatePS4;
        public DesignWorkflowState DesignStateXboxOne;
        public MissionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionPrototype), proto); }
    }
    public enum MissionShowInTracker
    {
        Never = 0,
        IfObjectivesVisible = 1,
        Always = 2,
    }
    public enum MissionShowInLog
    {
        Never = 0,
        OnlyWhenActive = 1,
        Always = 2,
    }
    public class OpenMissionPrototype : MissionPrototype
    {
        public bool ParticipationBasedOnAreaCell;
        public OpenMissionRewardEntryPrototype[] RewardsByContribution;
        public StoryNotificationPrototype StoryNotification;
        public RegionPrototype ActiveInRegions;
        public bool ActiveInRegionsIncludeChildren;
        public RegionPrototype ActiveInRegionsExclude;
        public ulong[] ActiveInAreas;
        public ulong[] ActiveInCells;
        public bool ResetWhenUnsimulated;
        public double MinimumContributionForCredit;
        public bool RespawnInPlace;
        public double ParticipantTimeoutInSeconds;
        public bool RespawnOnRestart;
        public int IdleTimeoutSeconds;
        public double ParticipationContributionValue;
        public long AchievementTimeLimitSeconds;
        public bool ShowToastMessages;
        public OpenMissionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OpenMissionPrototype), proto); }
    }

    public class LegendaryMissionCategoryPrototype : Prototype
    {
        public ulong Name;
        public int Weight;
        public int BlacklistLength;
        public LegendaryMissionCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LegendaryMissionCategoryPrototype), proto); }
    }

    public class LegendaryMissionPrototype : MissionPrototype
    {
        public EvalPrototype EvalCanStart;
        public ulong Category;
        public LegendaryMissionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(LegendaryMissionPrototype), proto); }
    }

    public class DailyMissionPrototype : MissionPrototype
    {
        public WeekdayEnum Day;
        public DailyMissionType Type;
        public ulong Image;
        public DailyMissionResetFrequency ResetFrequency;
        public DailyMissionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DailyMissionPrototype), proto); }
    }
    public enum DailyMissionType
    {
        Patrol = 0,
        Survival = 1,
        Terminal = 2,
    }
    public enum DailyMissionResetFrequency
    {
        Daily = 0,
        Weekly = 1,
    }
    public class AdvancedMissionCategoryPrototype : LegendaryMissionCategoryPrototype
    {
        public WeekdayEnum WeeklyResetDay;
        public AdvancedMissionFrequencyType MissionType;
        public ulong CategoryLabel;
        public AdvancedMissionCategoryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AdvancedMissionCategoryPrototype), proto); }
    }
    public enum AdvancedMissionFrequencyType
    {
        Invalid = 0,
        Repeatable = 1,
        Daily = 2,
        Weekly = 3,
    }
    public class AdvancedMissionPrototype : MissionPrototype
    {
        public ulong CategoryType;
        public ulong ReputationExperienceType;
        public AdvancedMissionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AdvancedMissionPrototype), proto); }
    }

}
