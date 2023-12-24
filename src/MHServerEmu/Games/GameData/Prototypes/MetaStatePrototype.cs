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
        public DesignWorkflowState DesignState { get; private set; }
        public ulong[] Groups { get; private set; }
        public ulong[] PreventGroups { get; private set; }
        public ulong[] PreventStates { get; private set; }
        public long CooldownMS { get; private set; }
        public EvalPrototype EvalCanActivate { get; private set; }
        public ulong[] RemoveGroups { get; private set; }
        public ulong[] RemoveStates { get; private set; }
        public ulong[] SubStates { get; private set; }
        public ulong UIWidget { get; private set; }
        public DesignWorkflowState DesignStatePS4 { get; private set; }
        public DesignWorkflowState DesignStateXboxOne { get; private set; }
    }

    public class MetaStateMissionActivatePrototype : MetaStatePrototype
    {
        public ulong Mission { get; private set; }
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; private set; }
        public ulong[] PopulationAreaRestriction { get; private set; }
        public bool RemovePopulationOnDeactivate { get; private set; }
        public int DeactivateOnMissionCompDelayMS { get; private set; }
        public ulong[] OnMissionCompletedApplyStates { get; private set; }
        public ulong[] OnMissionFailedApplyStates { get; private set; }
    }

    public class MetaMissionEntryPrototype : Prototype
    {
        public ulong Mission { get; private set; }
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; private set; }
        public ulong[] PopulationAreaRestriction { get; private set; }
    }

    public class MetaStateMissionSequencerPrototype : MetaStatePrototype
    {
        public MetaMissionEntryPrototype[] Sequence { get; private set; }
        public ulong[] PopulationAreaRestriction { get; private set; }
        public bool RemovePopulationOnDeactivate { get; private set; }
        public int DeactivateOnMissionCompDelayMS { get; private set; }
        public ulong[] OnMissionCompletedApplyStates { get; private set; }
        public ulong[] OnMissionFailedApplyStates { get; private set; }
        public int SequenceAdvanceDelayMS { get; private set; }
        public ulong OnSequenceCompleteSetMode { get; private set; }
    }

    public class WeightedPrototypeDataRefPrototype : Prototype
    {
        public ulong Ref { get; private set; }
        public int Weight { get; private set; }
    }

    public class MetaStateWaveInstancePrototype : MetaStatePrototype
    {
        public ulong[] States { get; private set; }
        public WeightedPrototypeDataRefPrototype[] StatesWeighted { get; private set; }
        public int StatePickIntervalMS { get; private set; }
    }

    public class MetaStateScoringEventTimerEndPrototype : MetaStatePrototype
    {
        public ulong Timer { get; private set; }
    }

    public class MetaStateScoringEventTimerStartPrototype : MetaStatePrototype
    {
        public ulong Timer { get; private set; }
    }

    public class MetaStateScoringEventTimerStopPrototype : MetaStatePrototype
    {
        public ulong Timer { get; private set; }
    }

    public class MetaStateLimitPlayerDeathsPrototype : MetaStatePrototype
    {
        public int PlayerDeathLimit { get; private set; }
        public int FailMode { get; private set; }
        public ulong DeathUINotification { get; private set; }
        public bool FailOnAllPlayersDead { get; private set; }
        public bool BlacklistDeadPlayers { get; private set; }
        public ulong DeathLimitUINotification { get; private set; }
        public bool StayInModeOnFail { get; private set; }
        public bool UseRegionDeathCount { get; private set; }
    }

    public class MetaStateLimitPlayerDeathsPerMissionPrototype : MetaStateLimitPlayerDeathsPrototype
    {
    }

    public class MetaStateShutdownPrototype : MetaStatePrototype
    {
        public int ShutdownDelayMS { get; private set; }
        public int TeleportDelayMS { get; private set; }
        public DialogPrototype TeleportDialog { get; private set; }
        public bool TeleportIsEndlessDown { get; private set; }
        public ulong TeleportTarget { get; private set; }
        public ulong TeleportButtonWidget { get; private set; }
        public ulong ReadyCheckWidget { get; private set; }
    }

    public class MetaStatePopulationMaintainPrototype : MetaStatePrototype
    {
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; private set; }
        public ulong[] RestrictToAreas { get; private set; }
        public ulong[] RestrictToCells { get; private set; }
        public int RespawnDelayMS { get; private set; }
        public bool Respawn { get; private set; }
        public bool RemovePopObjectsOnSpawnFail { get; private set; }
    }

    public class MetaStateMissionStateListenerPrototype : MetaStatePrototype
    {
        public ulong CompleteMissions { get; private set; }
        public int CompleteMode { get; private set; }
        public ulong FailMissions { get; private set; }
        public int FailMode { get; private set; }
    }

    public class MetaStateEntityModifierPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
        public EvalPrototype Eval { get; private set; }
    }

    public class MetaStateEntityEventCounterPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class MetaStateMissionProgressionPrototype : MetaStatePrototype
    {
        public ulong[] StatesProgression { get; private set; }
        public int BeforeFirstStateDelayMS { get; private set; }
        public int BetweenStatesIntervalMS { get; private set; }
        public long ProgressionStateTimeoutSecs { get; private set; }
        public bool SaveProgressionStateToDb { get; private set; }
    }

    public class MetaStateRegionPlayerAccessPrototype : MetaStatePrototype
    {
        public RegionPlayerAccess Access { get; private set; }
        public EvalPrototype EvalTrigger { get; private set; }
    }

    public class MetaStateStartTargetOverridePrototype : MetaStatePrototype
    {
        public bool OnRemoveClearOverride { get; private set; }
        public ulong StartTarget { get; private set; }
    }

    public class MetaStateCombatQueueLockoutPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
        public bool UnlockOnCombatExit { get; private set; }
    }

    public class MetaStateTrackRegionScorePrototype : MetaStatePrototype
    {
        public int ScoreThreshold { get; private set; }
        public ulong[] MissionsToComplete { get; private set; }
        public ulong[] MissionsToFail { get; private set; }
        public ulong[] MissionsToDeactivate { get; private set; }
        public int NextMode { get; private set; }
        public ulong ScoreCurveForMobRank { get; private set; }
        public ulong ScoreCurveForMobLevel { get; private set; }
        public bool RemoveStateOnScoreThreshold { get; private set; }
        public ulong[] OnScoreThresholdApplyStates { get; private set; }
        public ulong[] OnScoreThresholdRemoveStates { get; private set; }
    }

    public class MetaStateTimedBonusEntryPrototype : Prototype
    {
        public ulong[] MissionsToWatch { get; private set; }
        public ulong MissionTrackerText { get; private set; }
        public bool RemoveStateOnSuccess { get; private set; }
        public bool RemoveStateOnFail { get; private set; }
        public MissionActionPrototype[] ActionsOnSuccess { get; private set; }
        public MissionActionPrototype[] ActionsOnFail { get; private set; }
        public long TimerForEntryMS { get; private set; }
        public ulong UIWidget { get; private set; }
    }

    public class MetaStateTimedBonusPrototype : MetaStatePrototype
    {
        public MetaStateTimedBonusEntryPrototype[] Entries { get; private set; }
    }

    public class MetaStateMissionRestartPrototype : MetaStatePrototype
    {
        public ulong[] MissionsToRestart { get; private set; }
    }
}
