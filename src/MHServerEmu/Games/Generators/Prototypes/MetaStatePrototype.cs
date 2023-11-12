using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class MetaStatePrototype : Prototype
    {
        public DesignWorkflowState DesignState;
        public DesignWorkflowState DesignStatePS4;
        public DesignWorkflowState DesignStateXboxOne;
        public ulong[] Groups;
        public ulong[] PreventGroups;
        public ulong[] PreventStates;
        public ulong CooldownMS;
        public EvalPrototype EvalCanActivate;
        public ulong[] RemoveGroups;
        public ulong[] RemoveStates;
        public ulong[] SubStates;
        public ulong UIWidget;
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

    public enum DesignWorkflowState {
	    None = 0,
	    NotInGame = 1,
	    DevelopmentOnly = 2,
	    Live = 4,
    }

    public class MetaStateMissionActivatePrototype : MetaStatePrototype
    {
        public int DeactivateOnMissionCompDelayMS;
        public ulong Mission;
        public ulong[] PopulationAreaRestriction;
        public PopulationRequiredObjectPrototype[] PopulationObjects;
        public bool RemovePopulationOnDeactivate;
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
        public int DeactivateOnMissionCompDelayMS;
        public ulong[] PopulationAreaRestriction;
        public bool RemovePopulationOnDeactivate;
        public MetaMissionEntryPrototype[] Sequence;
        public int SequenceAdvanceDelayMS;
        public ulong[] OnMissionCompletedApplyStates;
        public ulong[] OnMissionFailedApplyStates;
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
}
