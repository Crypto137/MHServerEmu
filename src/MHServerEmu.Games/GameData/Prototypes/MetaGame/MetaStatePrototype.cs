using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.MetaGames.MetaStates;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)None)]
    public enum DesignWorkflowState
    {
        None = 0,
        NotInGame = 1,
        DevelopmentOnly = 2,
        Live = 4,
    }

    [AssetEnum((int)Open)]
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
        public AssetId[] Groups { get; protected set; }
        public AssetId[] PreventGroups { get; protected set; }
        public PrototypeId[] PreventStates { get; protected set; }
        public long CooldownMS { get; protected set; }
        public EvalPrototype EvalCanActivate { get; protected set; }
        public AssetId[] RemoveGroups { get; protected set; }
        public PrototypeId[] RemoveStates { get; protected set; }
        public PrototypeId[] SubStates { get; protected set; }
        public PrototypeId UIWidget { get; protected set; }
        public DesignWorkflowState DesignStatePS4 { get; protected set; }
        public DesignWorkflowState DesignStateXboxOne { get; protected set; }

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

        public virtual MetaState AllocateState(MetaGame metaGame)
        {
            return null;
        }
    }

    public class MetaStateMissionActivatePrototype : MetaStatePrototype
    {
        public PrototypeId Mission { get; protected set; }
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; protected set; }
        public PrototypeId[] PopulationAreaRestriction { get; protected set; }
        public bool RemovePopulationOnDeactivate { get; protected set; }
        public int DeactivateOnMissionCompDelayMS { get; protected set; }
        public PrototypeId[] OnMissionCompletedApplyStates { get; protected set; }
        public PrototypeId[] OnMissionFailedApplyStates { get; protected set; }

        public override bool CanApplyState()
        {
            bool canApply = base.CanApplyState();
            if (canApply)
            {
                var missionProto = GameDatabase.GetPrototype<MissionPrototype>(Mission);
                if (missionProto != null)
                    canApply &= missionProto.ApprovedForUse() && missionProto.IsLiveTuningEnabled();
            }
            return canApply;
        }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateMissionActivate(metaGame, this);
        }
    }

    public class MetaMissionEntryPrototype : Prototype
    {
        public PrototypeId Mission { get; protected set; }
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; protected set; }
        public PrototypeId[] PopulationAreaRestriction { get; protected set; }
    }

    public class MetaStateMissionSequencerPrototype : MetaStatePrototype
    {
        public MetaMissionEntryPrototype[] Sequence { get; protected set; }
        public PrototypeId[] PopulationAreaRestriction { get; protected set; }
        public bool RemovePopulationOnDeactivate { get; protected set; }
        public int DeactivateOnMissionCompDelayMS { get; protected set; }
        public PrototypeId[] OnMissionCompletedApplyStates { get; protected set; }
        public PrototypeId[] OnMissionFailedApplyStates { get; protected set; }
        public int SequenceAdvanceDelayMS { get; protected set; }
        public PrototypeId OnSequenceCompleteSetMode { get; protected set; }

        public override bool CanApplyState()
        {
            bool canApply = base.CanApplyState();
            if (canApply && Sequence.HasValue())
                foreach (var entryProto in Sequence)
                {
                    var missionProto = GameDatabase.GetPrototype<MissionPrototype>(entryProto.Mission);
                    if (missionProto != null)
                        canApply &= missionProto.ApprovedForUse() && missionProto.IsLiveTuningEnabled();
                }
            return canApply;
        }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateMissionSequencer(metaGame, this);
        }
    }

    public class WeightedPrototypeDataRefPrototype : Prototype
    {
        public PrototypeId Ref { get; protected set; }
        public int Weight { get; protected set; }
    }

    public class MetaStateWaveInstancePrototype : MetaStatePrototype
    {
        public PrototypeId[] States { get; protected set; }
        public WeightedPrototypeDataRefPrototype[] StatesWeighted { get; protected set; }
        public int StatePickIntervalMS { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateWaveInstance(metaGame, this);
        }
    }

    public class MetaStateScoringEventTimerEndPrototype : MetaStatePrototype
    {
        public PrototypeId Timer { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateScoringEventTimerEnd(metaGame, this);
        }
    }

    public class MetaStateScoringEventTimerStartPrototype : MetaStatePrototype
    {
        public PrototypeId Timer { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateScoringEventTimerStart(metaGame, this);
        }
    }

    public class MetaStateScoringEventTimerStopPrototype : MetaStatePrototype
    {
        public PrototypeId Timer { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateScoringEventTimerStop(metaGame, this);
        }
    }

    public class MetaStateLimitPlayerDeathsPrototype : MetaStatePrototype
    {
        public int PlayerDeathLimit { get; protected set; }
        public int FailMode { get; protected set; }
        public PrototypeId DeathUINotification { get; protected set; }
        public bool FailOnAllPlayersDead { get; protected set; }
        public bool BlacklistDeadPlayers { get; protected set; }
        public PrototypeId DeathLimitUINotification { get; protected set; }
        public bool StayInModeOnFail { get; protected set; }
        public bool UseRegionDeathCount { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateLimitPlayerDeaths(metaGame, this);
        }
    }

    public class MetaStateLimitPlayerDeathsPerMissionPrototype : MetaStateLimitPlayerDeathsPrototype
    {
        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateLimitPlayerDeathsPerMission(metaGame, this);
        }
    }

    public class MetaStateShutdownPrototype : MetaStatePrototype
    {
        public int ShutdownDelayMS { get; protected set; }
        public int TeleportDelayMS { get; protected set; }
        public DialogPrototype TeleportDialog { get; protected set; }
        public bool TeleportIsEndlessDown { get; protected set; }
        public PrototypeId TeleportTarget { get; protected set; }
        public PrototypeId TeleportButtonWidget { get; protected set; }
        public PrototypeId ReadyCheckWidget { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateShutdown(metaGame, this);
        }
    }

    public class MetaStatePopulationMaintainPrototype : MetaStatePrototype
    {
        public PopulationRequiredObjectPrototype[] PopulationObjects { get; protected set; }
        public PrototypeId[] RestrictToAreas { get; protected set; }
        public AssetId[] RestrictToCells { get; protected set; }
        public int RespawnDelayMS { get; protected set; }
        public bool Respawn { get; protected set; }
        public bool RemovePopObjectsOnSpawnFail { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStatePopulationMaintain(metaGame, this);
        }
    }

    public class MetaStateMissionStateListenerPrototype : MetaStatePrototype
    {
        public PrototypeId[] CompleteMissions { get; protected set; }
        public int CompleteMode { get; protected set; }
        public PrototypeId[] FailMissions { get; protected set; }
        public int FailMode { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateMissionStateListener(metaGame, this);
        }
    }

    public class MetaStateEntityModifierPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public EvalPrototype Eval { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateEntityModifier(metaGame, this);
        }
    }

    public class MetaStateEntityEventCounterPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateEntityEventCounter(metaGame, this);
        }
    }

    public class MetaStateMissionProgressionPrototype : MetaStatePrototype
    {
        public PrototypeId[] StatesProgression { get; protected set; }
        public int BeforeFirstStateDelayMS { get; protected set; }
        public int BetweenStatesIntervalMS { get; protected set; }
        public long ProgressionStateTimeoutSecs { get; protected set; }
        public bool SaveProgressionStateToDb { get; protected set; }

        public PrototypeId NextState(PrototypeId stateRef)
        {
            if (StatesProgression.IsNullOrEmpty()) return PrototypeId.Invalid;
            if (stateRef == PrototypeId.Invalid) return StatesProgression[0];

            for (int i = 0; i < StatesProgression.Length; i++)
                if (StatesProgression[i] == stateRef)
                {
                    if (i + 1 < StatesProgression.Length) 
                        return StatesProgression[i + 1];
                    else break;
                }
            return PrototypeId.Invalid;
        }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateMissionProgression(metaGame, this);
        }
    }

    public class MetaStateRegionPlayerAccessPrototype : MetaStatePrototype
    {
        public RegionPlayerAccess Access { get; protected set; }
        public EvalPrototype EvalTrigger { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateRegionPlayerAccess(metaGame, this);
        }
    }

    public class MetaStateStartTargetOverridePrototype : MetaStatePrototype
    {
        public bool OnRemoveClearOverride { get; protected set; }
        public PrototypeId StartTarget { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateStartTargetOverride(metaGame, this);
        }
    }

    public class MetaStateCombatQueueLockoutPrototype : MetaStatePrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public bool UnlockOnCombatExit { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateCombatQueueLockout(metaGame, this);
        }
    }

    public class MetaStateTrackRegionScorePrototype : MetaStatePrototype
    {
        public int ScoreThreshold { get; protected set; }
        public PrototypeId[] MissionsToComplete { get; protected set; }
        public PrototypeId[] MissionsToFail { get; protected set; }
        public PrototypeId[] MissionsToDeactivate { get; protected set; }
        public int NextMode { get; protected set; }
        public CurveId ScoreCurveForMobRank { get; protected set; }
        public CurveId ScoreCurveForMobLevel { get; protected set; }
        public bool RemoveStateOnScoreThreshold { get; protected set; }
        public PrototypeId[] OnScoreThresholdApplyStates { get; protected set; }
        public PrototypeId[] OnScoreThresholdRemoveStates { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateTrackRegionScore(metaGame, this);
        }
    }

    public class MetaStateTimedBonusEntryPrototype : Prototype
    {
        public PrototypeId[] MissionsToWatch { get; protected set; }
        public LocaleStringId MissionTrackerText { get; protected set; }
        public bool RemoveStateOnSuccess { get; protected set; }
        public bool RemoveStateOnFail { get; protected set; }
        public MissionActionPrototype[] ActionsOnSuccess { get; protected set; }
        public MissionActionPrototype[] ActionsOnFail { get; protected set; }
        public long TimerForEntryMS { get; protected set; }
        public PrototypeId UIWidget { get; protected set; }
    }

    public class MetaStateTimedBonusPrototype : MetaStatePrototype
    {
        public MetaStateTimedBonusEntryPrototype[] Entries { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateTimedBonus(metaGame, this);
        }
    }

    public class MetaStateMissionRestartPrototype : MetaStatePrototype
    {
        public PrototypeId[] MissionsToRestart { get; protected set; }

        public override MetaState AllocateState(MetaGame metaGame)
        {
            return new MetaStateMissionRestart(metaGame, this);
        }
    }
}
