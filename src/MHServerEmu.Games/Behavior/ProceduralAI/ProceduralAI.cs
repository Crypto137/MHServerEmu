using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.ProceduralAI
{
    public class ProceduralAI
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public const short MaxConcurrentStates = 2;
        private AIController _owningController;
        private short _curState;
        private PState[] _states = new PState[MaxConcurrentStates];

        public void StopOwnerLocomotor()
        {
            var agent = _owningController.Owner;
            if (agent != null)
                if (agent.IsInWorld)
                    agent.Locomotor?.Stop();
        }

        public StaticBehaviorReturnType HandleContext(IAIState state, IStateContext stateContext, ProceduralContextPrototype proceduralContext)
        {
            if (state == null || stateContext == null || _curState < 0 || _curState >= MaxConcurrentStates)
                return StaticBehaviorReturnType.Failed;

            StaticBehaviorReturnType result;
            if (_states[_curState].State == state)
                result = state.Update(stateContext);
            else if (ValidateState(state, stateContext))
                result = SwitchProceduralState(state, stateContext, StaticBehaviorReturnType.Interrupted, proceduralContext);
            else
                return StaticBehaviorReturnType.Failed;

            if (result == StaticBehaviorReturnType.Completed || result == StaticBehaviorReturnType.Failed)
                SwitchProceduralState(null, stateContext, result, null);

            return result;
        }

        private StaticBehaviorReturnType SwitchProceduralState(IAIState state, IStateContext stateContext, StaticBehaviorReturnType returnType, ProceduralContextPrototype proceduralContext)
        {
            throw new NotImplementedException();
        }

        private bool ValidateState(IAIState state, IStateContext stateContext)
        {
            if (_owningController.IsOwnerValid()) 
                return state.Validate(stateContext);
            
            Logger.Warn($"[{_owningController.Owner}] is trying to switch states but the agent is not valid anymore." +
                "An engineer needs to make sure that whatever behavior logic is called before this state,"+
                "handles invalidation of the owner gracefully and ceases to continue executing behavior logic");
            return false;
        }
    }

    public struct PState
    {
        public IAIState State;
        public ProceduralContextPrototype contextProto;
    }
}
