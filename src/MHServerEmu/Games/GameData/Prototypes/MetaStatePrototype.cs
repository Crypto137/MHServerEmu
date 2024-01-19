using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum DesignWorkflowState
    {
        None = 0,
        NotInGame = 1,
        DevelopmentOnly = 2,
        Live = 4,
    }

    [AssetEnum]
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
        public DesignWorkflowState DesignState { get; protected set; }
        public ulong[] Groups { get; protected set; }
        public ulong[] PreventGroups { get; protected set; }
        public ulong[] PreventStates { get; protected set; }
        public long CooldownMS { get; protected set; }
        public EvalPrototype EvalCanActivate { get; protected set; }
        public ulong[] RemoveGroups { get; protected set; }
        public ulong[] RemoveStates { get; protected set; }
        public ulong[] SubStates { get; protected set; }
        public ulong UIWidget { get; protected set; }
        public DesignWorkflowState DesignStatePS4 { get; protected set; }
        public DesignWorkflowState DesignStateXboxOne { get; protected set; }
    }

    public class MetaStateMissionActivatePrototype : MetaStatePrototype
    {
        public ulong Mission { get; protected set; }
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; protected set; }
        public ulong[] PopulationAreaRestriction { get; protected set; }
        public bool RemovePopulationOnDeactivate { get; protected set; }
        public int DeactivateOnMissionCompDelayMS { get; protected set; }
        public ulong[] OnMissionCompletedApplyStates { get; protected set; }
        public ulong[] OnMissionFailedApplyStates { get; protected set; }
    }

    public class MetaMissionEntryPrototype : Prototype
    {
        public ulong Mission { get; protected set; }
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; protected set; }
        public ulong[] PopulationAreaRestriction { get; protected set; }
    }

    public class MetaStateMissionSequencerPrototype : MetaStatePrototype
    {
        public MetaMissionEntryPrototype[] Sequence { get; protected set; }
        public ulong[] PopulationAreaRestriction { get; protected set; }
        public bool RemovePopulationOnDeactivate { get; protected set; }
        public int DeactivateOnMissionCompDelayMS { get; protected set; }
        public ulong[] OnMissionCompletedApplyStates { get; protected set; }
        public ulong[] OnMissionFailedApplyStates { get; protected set; }
        public int SequenceAdvanceDelayMS { get; protected set; }
        public ulong OnSequenceCompleteSetMode { get; protected set; }
    }

    public class WeightedPrototypeDataRefPrototype : Prototype
    {
        public ulong Ref { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class MetaStateWaveInstancePrototype : MetaStatePrototype
    {
        public ulong[] States { get; protected set; }
        public WeightedPrototypeDataRefPrototype[] StatesWeighted { get; protected set; }
        public int StatePickIntervalMS { get; protected set; }
    }

    public class MetaStateScoringEventTimerEndPrototype : MetaStatePrototype
    {
        public ulong Timer { get; protected set; }
    }

    public class MetaStateScoringEventTimerStartPrototype : MetaStatePrototype
    {
        public ulong Timer { get; protected set; }
    }

    public class MetaStateScoringEventTimerStopPrototype : MetaStatePrototype
    {
        public ulong Timer { get; protected set; }
    }

    public class MetaStateLimitPlayerDeathsPrototype : MetaStatePrototype
    {
        public int PlayerDeathLimit { get; protected set; }
        public int FailMode { get; protected set; }
        public ulong DeathUINotification { get; protected set; }
        public bool FailOnAllPlayersDead { get; protected set; }
        public bool BlacklistDeadPlayers { get; protected set; }
        public ulong DeathLimitUINotification { get; protected set; }
        public bool StayInModeOnFail { get; protected set; }
        public bool UseRegionDeathCount { get; protected set; }
    }

    public class MetaStateLimitPlayerDeathsPerMissionPrototype : MetaStateLimitPlayerDeathsPrototype
    {
    }

    public class MetaStateShutdownPrototype : MetaStatePrototype
    {
        public int ShutdownDelayMS { get; protected set; }
        public int TeleportDelayMS { get; protected set; }
        public DialogPrototype TeleportDialog { get; protected set; }
        public bool TeleportIsEndlessDown { get; protected set; }
        public ulong TeleportTarget { get; protected set; }
        public ulong TeleportButtonWidget { get; protected set; }
        public ulong ReadyCheckWidget { get; protected set; }
    }

    public class MetaStatePopulationMaintainPrototype : MetaStatePrototype
    {
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; protected set; }
        public ulong[] RestrictToAreas { get; protected set; }
        public ulong[] RestrictToCells { get; protected set; }
        public int RespawnDelayMS { get; protected set; }
        public bool Respawn { get; protected set; }
        public bool RemovePopObjectsOnSpawnFail { get; protected set; }
    }

    public class MetaStateMissionStateListenerPrototype : MetaStatePrototype
    {
        public ulong[] CompleteMissions { get; protected set; }
        public int CompleteMode { get; protected set; }
        public ulong[] FailMissions { get; protected set; }
        public int FailMode { get; protected set; }
    }

    public class MetaStateEntityModifierPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public EvalPrototype Eval { get; protected set; }
    }

    public class MetaStateEntityEventCounterPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
    }

    public class MetaStateMissionProgressionPrototype : MetaStatePrototype
    {
        public ulong[] StatesProgression { get; protected set; }
        public int BeforeFirstStateDelayMS { get; protected set; }
        public int BetweenStatesIntervalMS { get; protected set; }
        public long ProgressionStateTimeoutSecs { get; protected set; }
        public bool SaveProgressionStateToDb { get; protected set; }
    }

    public class MetaStateRegionPlayerAccessPrototype : MetaStatePrototype
    {
        public RegionPlayerAccess Access { get; protected set; }
        public EvalPrototype EvalTrigger { get; protected set; }
    }

    public class MetaStateStartTargetOverridePrototype : MetaStatePrototype
    {
        public bool OnRemoveClearOverride { get; protected set; }
        public ulong StartTarget { get; protected set; }
    }

    public class MetaStateCombatQueueLockoutPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public bool UnlockOnCombatExit { get; protected set; }
    }

    public class MetaStateTrackRegionScorePrototype : MetaStatePrototype
    {
        public int ScoreThreshold { get; protected set; }
        public ulong[] MissionsToComplete { get; protected set; }
        public ulong[] MissionsToFail { get; protected set; }
        public ulong[] MissionsToDeactivate { get; protected set; }
        public int NextMode { get; protected set; }
        public ulong ScoreCurveForMobRank { get; protected set; }
        public ulong ScoreCurveForMobLevel { get; protected set; }
        public bool RemoveStateOnScoreThreshold { get; protected set; }
        public ulong[] OnScoreThresholdApplyStates { get; protected set; }
        public ulong[] OnScoreThresholdRemoveStates { get; protected set; }
    }

    public class MetaStateTimedBonusEntryPrototype : Prototype
    {
        public ulong[] MissionsToWatch { get; protected set; }
        public ulong MissionTrackerText { get; protected set; }
        public bool RemoveStateOnSuccess { get; protected set; }
        public bool RemoveStateOnFail { get; protected set; }
        public MissionActionPrototype[] ActionsOnSuccess { get; protected set; }
        public MissionActionPrototype[] ActionsOnFail { get; protected set; }
        public long TimerForEntryMS { get; protected set; }
        public ulong UIWidget { get; protected set; }
    }

    public class MetaStateTimedBonusPrototype : MetaStatePrototype
    {
        public MetaStateTimedBonusEntryPrototype[] Entries { get; protected set; }
    }

    public class MetaStateMissionRestartPrototype : MetaStatePrototype
    {
        public ulong[] MissionsToRestart { get; protected set; }
    }
}
