using System.Net;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum DesignWorkflowState
    {
        None = 0,
        NotInGame = 1,
        DevelopmentOnly = 2,
        Live = 4,
    }

    public enum RegionPlayerAccess
    {
        Open = 1,
        InviteOnly = 2,
        Locked = 4,
        Closed = 5,
    }

    #endregion

    public class MetaStatePrototype : Prototype
    {
        public DesignWorkflowState DesignState { get; set; }
        public ulong[] Groups { get; set; }
        public ulong[] PreventGroups { get; set; }
        public ulong[] PreventStates { get; set; }
        public long CooldownMS { get; set; }
        public EvalPrototype EvalCanActivate { get; set; }
        public ulong[] RemoveGroups { get; set; }
        public ulong[] RemoveStates { get; set; }
        public ulong[] SubStates { get; set; }
        public ulong UIWidget { get; set; }
        public DesignWorkflowState DesignStatePS4 { get; set; }
        public DesignWorkflowState DesignStateXboxOne { get; set; }
    }

    public class MetaStateMissionActivatePrototype : MetaStatePrototype
    {
        public ulong Mission { get; set; }
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; set; }
        public ulong[] PopulationAreaRestriction { get; set; }
        public bool RemovePopulationOnDeactivate { get; set; }
        public int DeactivateOnMissionCompDelayMS { get; set; }
        public ulong[] OnMissionCompletedApplyStates { get; set; }
        public ulong[] OnMissionFailedApplyStates { get; set; }
    }

    public class MetaMissionEntryPrototype : Prototype
    {
        public ulong Mission { get; set; }
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; set; }
        public ulong[] PopulationAreaRestriction { get; set; }
    }

    public class MetaStateMissionSequencerPrototype : MetaStatePrototype
    {
        public MetaMissionEntryPrototype[] Sequence { get; set; }
        public ulong[] PopulationAreaRestriction { get; set; }
        public bool RemovePopulationOnDeactivate { get; set; }
        public int DeactivateOnMissionCompDelayMS { get; set; }
        public ulong[] OnMissionCompletedApplyStates { get; set; }
        public ulong[] OnMissionFailedApplyStates { get; set; }
        public int SequenceAdvanceDelayMS { get; set; }
        public ulong OnSequenceCompleteSetMode { get; set; }
    }

    public class WeightedPrototypeDataRefPrototype : Prototype
    {
        public ulong Ref { get; set; }
        public int Weight { get; set; }
    }

    public class MetaStateWaveInstancePrototype : MetaStatePrototype
    {
        public ulong[] States { get; set; }
        public WeightedPrototypeDataRefPrototype[] StatesWeighted { get; set; }
        public int StatePickIntervalMS { get; set; }
    }

    public class MetaStateScoringEventTimerEndPrototype : MetaStatePrototype
    {
        public ulong Timer { get; set; }
    }

    public class MetaStateScoringEventTimerStartPrototype : MetaStatePrototype
    {
        public ulong Timer { get; set; }
    }

    public class MetaStateScoringEventTimerStopPrototype : MetaStatePrototype
    {
        public ulong Timer { get; set; }
    }

    public class MetaStateLimitPlayerDeathsPrototype : MetaStatePrototype
    {
        public int PlayerDeathLimit { get; set; }
        public int FailMode { get; set; }
        public ulong DeathUINotification { get; set; }
        public bool FailOnAllPlayersDead { get; set; }
        public bool BlacklistDeadPlayers { get; set; }
        public ulong DeathLimitUINotification { get; set; }
        public bool StayInModeOnFail { get; set; }
        public bool UseRegionDeathCount { get; set; }
    }

    public class MetaStateLimitPlayerDeathsPerMissionPrototype : MetaStateLimitPlayerDeathsPrototype
    {
    }

    public class MetaStateShutdownPrototype : MetaStatePrototype
    {
        public int ShutdownDelayMS { get; set; }
        public int TeleportDelayMS { get; set; }
        public DialogPrototype TeleportDialog { get; set; }
        public bool TeleportIsEndlessDown { get; set; }
        public ulong TeleportTarget { get; set; }
        public ulong TeleportButtonWidget { get; set; }
        public ulong ReadyCheckWidget { get; set; }
    }

    public class MetaStatePopulationMaintainPrototype : MetaStatePrototype
    {
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; set; }
        public ulong[] RestrictToAreas { get; set; }
        public ulong[] RestrictToCells { get; set; }
        public int RespawnDelayMS { get; set; }
        public bool Respawn { get; set; }
        public bool RemovePopObjectsOnSpawnFail { get; set; }
    }

    public class MetaStateMissionStateListenerPrototype : MetaStatePrototype
    {
        public ulong CompleteMissions { get; set; }
        public int CompleteMode { get; set; }
        public ulong FailMissions { get; set; }
        public int FailMode { get; set; }
    }

    public class MetaStateEntityModifierPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
        public EvalPrototype Eval { get; set; }
    }

    public class MetaStateEntityEventCounterPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class MetaStateMissionProgressionPrototype : MetaStatePrototype
    {
        public ulong[] StatesProgression { get; set; }
        public int BeforeFirstStateDelayMS { get; set; }
        public int BetweenStatesIntervalMS { get; set; }
        public long ProgressionStateTimeoutSecs { get; set; }
        public bool SaveProgressionStateToDb { get; set; }
    }

    public class MetaStateRegionPlayerAccessPrototype : MetaStatePrototype
    {
        public RegionPlayerAccess Access { get; set; }
        public EvalPrototype EvalTrigger { get; set; }
    }

    public class MetaStateStartTargetOverridePrototype : MetaStatePrototype
    {
        public bool OnRemoveClearOverride { get; set; }
        public ulong StartTarget { get; set; }
    }

    public class MetaStateCombatQueueLockoutPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
        public bool UnlockOnCombatExit { get; set; }
    }

    public class MetaStateTrackRegionScorePrototype : MetaStatePrototype
    {
        public int ScoreThreshold { get; set; }
        public ulong[] MissionsToComplete { get; set; }
        public ulong[] MissionsToFail { get; set; }
        public ulong[] MissionsToDeactivate { get; set; }
        public int NextMode { get; set; }
        public ulong ScoreCurveForMobRank { get; set; }
        public ulong ScoreCurveForMobLevel { get; set; }
        public bool RemoveStateOnScoreThreshold { get; set; }
        public ulong[] OnScoreThresholdApplyStates { get; set; }
        public ulong[] OnScoreThresholdRemoveStates { get; set; }
    }

    public class MetaStateTimedBonusEntryPrototype : Prototype
    {
        public ulong[] MissionsToWatch { get; set; }
        public ulong MissionTrackerText { get; set; }
        public bool RemoveStateOnSuccess { get; set; }
        public bool RemoveStateOnFail { get; set; }
        public MissionActionPrototype[] ActionsOnSuccess { get; set; }
        public MissionActionPrototype[] ActionsOnFail { get; set; }
        public long TimerForEntryMS { get; set; }
        public ulong UIWidget { get; set; }
    }

    public class MetaStateTimedBonusPrototype : MetaStatePrototype
    {
        public MetaStateTimedBonusEntryPrototype[] Entries { get; set; }
    }

    public class MetaStateMissionRestartPrototype : MetaStatePrototype
    {
        public ulong[] MissionsToRestart { get; set; }
    }
}
