
namespace MHServerEmu.Games.GameData.Prototypes
{
    public class MetaStatePrototype : Prototype
    {
        public DesignWorkflowState DesignState;
        public ulong[] Groups;
        public ulong[] PreventGroups;
        public ulong[] PreventStates;
        public long CooldownMS;
        public EvalPrototype EvalCanActivate;
        public ulong[] RemoveGroups;
        public ulong[] RemoveStates;
        public ulong[] SubStates;
        public ulong UIWidget;
        public DesignWorkflowState DesignStatePS4;
        public DesignWorkflowState DesignStateXboxOne;

        public MetaStatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStatePrototype), proto); }

        public virtual bool CanApplyState()
        {
            var designState = DesignState;
            /*
              if (GameDatabase.Settings.TargetingPS4Data)
                  designState = DesignStatePS4;
              else if (GameDatabase.Settings.TargetingXboxOneData)
                  designState = DesignStateXboxOne;*/

            return designState == DesignWorkflowState.Live;//GameDatabase.DesignStateOk(designState);
        }

    }

    public enum DesignWorkflowState
    {
        None = 0,
        NotInGame = 1,
        DevelopmentOnly = 2,
        Live = 4,
    }

    public class MetaStateMissionActivatePrototype : MetaStatePrototype
    {
        public ulong Mission;
        public PopulationRequiredObjectPrototype[] PopulationObjects;
        public ulong[] PopulationAreaRestriction;
        public bool RemovePopulationOnDeactivate;
        public int DeactivateOnMissionCompDelayMS;
        public ulong[] OnMissionCompletedApplyStates;
        public ulong[] OnMissionFailedApplyStates;
        public MetaStateMissionActivatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateMissionActivatePrototype), proto); }

        public override bool CanApplyState()
        {
            // TODO
            return true;
        }
    }

    public class MetaMissionEntryPrototype : Prototype
    {
        public ulong Mission;
        public PopulationRequiredObjectPrototype[] PopulationObjects;
        public ulong[] PopulationAreaRestriction;
        public MetaMissionEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaMissionEntryPrototype), proto); }

    }

    public class MetaStateMissionSequencerPrototype : MetaStatePrototype
    {
        public MetaMissionEntryPrototype[] Sequence;
        public ulong[] PopulationAreaRestriction;
        public bool RemovePopulationOnDeactivate;
        public int DeactivateOnMissionCompDelayMS;
        public ulong[] OnMissionCompletedApplyStates;
        public ulong[] OnMissionFailedApplyStates;
        public int SequenceAdvanceDelayMS;
        public ulong OnSequenceCompleteSetMode;
        public MetaStateMissionSequencerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateMissionSequencerPrototype), proto); }

        public override bool CanApplyState()
        {
            // TODO
            return true;
        }
    }

    public class WeightedPrototypeDataRefPrototype : Prototype
    {
        public ulong Ref;
        public int Weight;
        public WeightedPrototypeDataRefPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WeightedPrototypeDataRefPrototype), proto); }
    }

    public class MetaStateWaveInstancePrototype : MetaStatePrototype
    {
        public ulong[] States;
        public WeightedPrototypeDataRefPrototype[] StatesWeighted;
        public int StatePickIntervalMS;
        public MetaStateWaveInstancePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateWaveInstancePrototype), proto); }
    }

    public class MetaStateScoringEventTimerEndPrototype : MetaStatePrototype
    {
        public ulong Timer;
        public MetaStateScoringEventTimerEndPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateScoringEventTimerEndPrototype), proto); }
    }

    public class MetaStateScoringEventTimerStartPrototype : MetaStatePrototype
    {
        public ulong Timer;
        public MetaStateScoringEventTimerStartPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateScoringEventTimerStartPrototype), proto); }
    }

    public class MetaStateScoringEventTimerStopPrototype : MetaStatePrototype
    {
        public ulong Timer;
        public MetaStateScoringEventTimerStopPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateScoringEventTimerStopPrototype), proto); }
    }

    public class MetaStateLimitPlayerDeathsPrototype : MetaStatePrototype
    {
        public int PlayerDeathLimit;
        public int FailMode;
        public ulong DeathUINotification;
        public bool FailOnAllPlayersDead;
        public bool BlacklistDeadPlayers;
        public ulong DeathLimitUINotification;
        public bool StayInModeOnFail;
        public bool UseRegionDeathCount;
        public MetaStateLimitPlayerDeathsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateLimitPlayerDeathsPrototype), proto); }
    }

    public class MetaStateLimitPlayerDeathsPerMissionPrototype : MetaStateLimitPlayerDeathsPrototype
    {
        public MetaStateLimitPlayerDeathsPerMissionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateLimitPlayerDeathsPerMissionPrototype), proto); }
    }

    public class MetaStateShutdownPrototype : MetaStatePrototype
    {
        public int ShutdownDelayMS;
        public int TeleportDelayMS;
        public DialogPrototype TeleportDialog;
        public bool TeleportIsEndlessDown;
        public ulong TeleportTarget;
        public ulong TeleportButtonWidget;
        public ulong ReadyCheckWidget;
        public MetaStateShutdownPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateShutdownPrototype), proto); }
    }

    public class MetaStatePopulationMaintainPrototype : MetaStatePrototype
    {
        public PopulationRequiredObjectPrototype[] PopulationObjects;
        public ulong[] RestrictToAreas;
        public ulong[] RestrictToCells;
        public int RespawnDelayMS;
        public bool Respawn;
        public bool RemovePopObjectsOnSpawnFail;
        public MetaStatePopulationMaintainPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStatePopulationMaintainPrototype), proto); }
    }

    public class MetaStateMissionStateListenerPrototype : MetaStatePrototype
    {
        public ulong[] CompleteMissions;
        public int CompleteMode;
        public ulong[] FailMissions;
        public int FailMode;
        public MetaStateMissionStateListenerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateMissionStateListenerPrototype), proto); }
    }

    public class MetaStateEntityModifierPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter;
        public EvalPrototype Eval;
        public MetaStateEntityModifierPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateEntityModifierPrototype), proto); }
    }

    public class MetaStateEntityEventCounterPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter;
        public MetaStateEntityEventCounterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateEntityEventCounterPrototype), proto); }
    }

    public class MetaStateMissionProgressionPrototype : MetaStatePrototype
    {
        public ulong[] StatesProgression;
        public int BeforeFirstStateDelayMS;
        public int BetweenStatesIntervalMS;
        public long ProgressionStateTimeoutSecs;
        public bool SaveProgressionStateToDb;
        public MetaStateMissionProgressionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateMissionProgressionPrototype), proto); }
    }

    public enum RegionPlayerAccess
    {
        Open = 1,
        InviteOnly = 2,
        Locked = 4,
        Closed = 5,
    }

    public class MetaStateRegionPlayerAccessPrototype : MetaStatePrototype
    {
        public RegionPlayerAccess Access;
        public EvalPrototype EvalTrigger;
        public MetaStateRegionPlayerAccessPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateRegionPlayerAccessPrototype), proto); }
    }

    public class MetaStateStartTargetOverridePrototype : MetaStatePrototype
    {
        public bool OnRemoveClearOverride;
        public ulong StartTarget;
        public MetaStateStartTargetOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateStartTargetOverridePrototype), proto); }
    }

    public class MetaStateCombatQueueLockoutPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter;
        public bool UnlockOnCombatExit;
        public MetaStateCombatQueueLockoutPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateCombatQueueLockoutPrototype), proto); }
    }

    public class MetaStateTrackRegionScorePrototype : MetaStatePrototype
    {
        public int ScoreThreshold;
        public ulong[] MissionsToComplete;
        public ulong[] MissionsToFail;
        public ulong[] MissionsToDeactivate;
        public int NextMode;
        public ulong ScoreCurveForMobRank;
        public ulong ScoreCurveForMobLevel;
        public bool RemoveStateOnScoreThreshold;
        public ulong[] OnScoreThresholdApplyStates;
        public ulong[] OnScoreThresholdRemoveStates;
        public MetaStateTrackRegionScorePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateTrackRegionScorePrototype), proto); }
    }

    public class MetaStateTimedBonusEntryPrototype : Prototype
    {
        public ulong[] MissionsToWatch;
        public ulong MissionTrackerText;
        public bool RemoveStateOnSuccess;
        public bool RemoveStateOnFail;
        public MissionActionPrototype[] ActionsOnSuccess;
        public MissionActionPrototype[] ActionsOnFail;
        public long TimerForEntryMS;
        public ulong UIWidget;
        public MetaStateTimedBonusEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateTimedBonusEntryPrototype), proto); }
    }

    public class MetaStateTimedBonusPrototype : MetaStatePrototype
    {
        public new MetaStateTimedBonusEntryPrototype[] Entries;
        public MetaStateTimedBonusPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateTimedBonusPrototype), proto); }
    }

    public class MetaStateMissionRestartPrototype : MetaStatePrototype
    {
        public ulong[] MissionsToRestart;
        public MetaStateMissionRestartPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MetaStateMissionRestartPrototype), proto); }
    }
}
