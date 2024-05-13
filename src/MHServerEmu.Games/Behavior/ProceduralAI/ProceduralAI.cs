using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior.ProceduralAI
{
    public class ProceduralAI
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        public const short MaxConcurrentStates = 2;
        private AIController _owningController;
        private short _curState;
        private PState[] _states = new PState[MaxConcurrentStates];
        private ProceduralAIProfilePrototype _procedurealProfile;

        public BrainPrototype BrainPrototype { get; private set; }

        private Game _game;

        public ulong LastThinkQTime { get; set; }
        public uint ThinkCountPerFrame { get; set; }
        public ProceduralAIProfilePrototype PartialOverrideBehavior { get; private set; }
        public StaticBehaviorReturnType LastPowerResult { get; internal set; }
        public ProceduralAIProfilePrototype FullOverrideBehavior { get; internal set; }

        public ProceduralAI(Game game, AIController owningController)
        {
            _game = game;
            _owningController = owningController;
        }

        public void Initialize(BehaviorProfilePrototype profile)
        {
            PrototypeId brainRef;
            if (profile.Brain != PrototypeId.Invalid)
                brainRef = profile.Brain;
            else                
                brainRef = _owningController.Blackboard.PropertyCollection[PropertyEnum.AIFullOverride];
            
            Agent agent = _owningController.Owner;
            // TODO Init PropertyEnum.AIPartialOverride;
            // GetOverrideByType

            _procedurealProfile = brainRef.As<ProceduralAIProfilePrototype>();
            BrainPrototype = _procedurealProfile;

            if (agent.IsControlledEntity)
            {
                ulong masterAvatarDbGuid = agent.Properties[PropertyEnum.AIMasterAvatarDbGuid];
                if (masterAvatarDbGuid != 0)
                {
                    // TODO Set PropertyEnum.AIAssistedEntityID = DBAvatarId
                }               
            }

            _procedurealProfile.Init(agent); // Init Powers for agent
        }

        public void StopOwnerLocomotor()
        {
            var agent = _owningController.Owner;
            if (agent != null)
                if (agent.IsInWorld)
                    agent.Locomotor?.Stop();
        }

        public StaticBehaviorReturnType HandleContext(IAIState state, ref IStateContext stateContext, ProceduralContextPrototype proceduralContext)
        {
            if (state == null || stateContext == null || _curState < 0 || _curState >= MaxConcurrentStates)
                return StaticBehaviorReturnType.Failed;

            StaticBehaviorReturnType result;
            if (_states[_curState].State == state)
                result = state.Update(stateContext);
            else if (ValidateState(state, ref stateContext))
                result = SwitchProceduralState(state, ref stateContext, StaticBehaviorReturnType.Interrupted, proceduralContext);
            else
                return StaticBehaviorReturnType.Failed;

            if (result == StaticBehaviorReturnType.Completed || result == StaticBehaviorReturnType.Failed)
                SwitchProceduralState(null, ref stateContext, result, null);

            return result;
        }
        public void StartState(IAIState state, ref IStateContext stateContext, ProceduralContextPrototype proceduralContext)
        {
            if ( _curState < 0 || _curState >= MaxConcurrentStates) return;

            _states[_curState] = new(state, proceduralContext);
            state.Start(stateContext);
            proceduralContext?.OnStart(_owningController, _procedurealProfile);
        }

        public  StaticBehaviorReturnType SwitchProceduralState(IAIState state, ref IStateContext stateContext, StaticBehaviorReturnType returnType, ProceduralContextPrototype proceduralContext = null)
        {
            throw new NotImplementedException();
        }

        public bool ValidateContext(IAIState state, ref IStateContext stateContext)
        {
            if (state == null || stateContext == null ) return false;
            return ValidateState(state, ref stateContext);
        }

        private bool ValidateState(IAIState state, ref IStateContext stateContext)
        {
            if (_owningController.IsOwnerValid()) 
                return state.Validate(stateContext);
            
            Logger.Warn($"[{_owningController.Owner}] is trying to switch states but the agent is not valid anymore." +
                "An engineer needs to make sure that whatever behavior logic is called before this state,"+
                "handles invalidation of the owner gracefully and ceases to continue executing behavior logic");
            return false;
        }

        public IAIState GetState(int stateIndex)
        {
            if (stateIndex < 0 || stateIndex >= MaxConcurrentStates) return null;
            return _states[stateIndex].State;
        }

        internal void PushSubstate()
        {
            throw new NotImplementedException();
        }

        internal void PopSubstate()
        {
            throw new NotImplementedException();
        }

    }

    public struct PState
    {
        public IAIState State;
        public ProceduralContextPrototype ProceduralContext;

        public PState(IAIState state, ProceduralContextPrototype proceduralContext)
        {
            State = state;
            ProceduralContext = proceduralContext;
        }
    }
}
