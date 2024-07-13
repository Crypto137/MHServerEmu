using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior.ProceduralAI
{
    public class ProceduralAI
    {
        public static readonly Logger Logger = LogManager.CreateLogger();

        public const short MaxConcurrentStates = 2;
        private Game _game;
        private readonly AIController _owningController;
        private short _curState;
        private readonly PState[] _states;
        private ProfilePtr _proceduralPtr;
        public ProceduralAIProfilePrototype Behavior { get => _proceduralPtr.Profile; }
        public BrainPrototype BrainPrototype { get; private set; }
        public ulong LastThinkQTime { get; set; }
        public uint ThinkCountPerFrame { get; set; }

        private ProfilePtr _partialOverridePtr;
        public ProceduralAIProfilePrototype PartialOverrideBehavior { get => _partialOverridePtr.Profile; }

        private ProfilePtr _fullOverridePtr;
        public ProceduralAIProfilePrototype FullOverrideBehavior { get => _fullOverridePtr.Profile; }
        public StaticBehaviorReturnType LastPowerResult { get; set; }

        public ProceduralAI(Game game, AIController owningController)
        {
            _game = game;
            _owningController = owningController;
            _proceduralPtr = new ();
            _partialOverridePtr = new();
            _fullOverridePtr = new();
            _curState = 0;
            _states = new PState[MaxConcurrentStates];
        }

        public void Initialize(BehaviorProfilePrototype profile)
        {
            PrototypeId brainRef;
            if (profile.Brain != PrototypeId.Invalid)
                brainRef = profile.Brain;
            else                
                brainRef = _owningController.Blackboard.PropertyCollection[PropertyEnum.AIFullOverride];
            
            Agent agent = _owningController.Owner;
            InitPartialOverride(agent);
            var proceduralProfile = brainRef.As<ProceduralAIProfilePrototype>();
            BrainPrototype = proceduralProfile;
            InitProceduralProfile(ref _proceduralPtr, proceduralProfile);
        }

        private bool InitProceduralProfile(ref ProfilePtr current, ProceduralAIProfilePrototype profile)
        {
            Agent agent = _owningController.Owner;
            if (agent == null || profile == null) return false;

            current.Profile = profile;

            if (agent.IsControlledEntity)
            {
                ulong masterAvatarDbGuid = agent.Properties[PropertyEnum.AIMasterAvatarDbGuid];
                if (masterAvatarDbGuid != 0)
                {
                    var avatar = _game.EntityManager.GetEntityByDbGuid<Player>(masterAvatarDbGuid); // TODO AvatarDB
                    if (avatar != null)
                        _owningController.Blackboard.PropertyCollection[PropertyEnum.AIAssistedEntityID] = avatar.Id;
                }
            }

            _proceduralPtr.Profile.Init(agent); // Init Powers for agent
            return true;
        }

        private void InitPartialOverride(Agent agent)
        {
            ClearOverrideBehavior(OverrideType.Partial);
            PrototypeId overrideRef = agent.Properties[PropertyEnum.AIPartialOverride];
            if (overrideRef != PrototypeId.Invalid) 
            {
                var profile = overrideRef.As<ProceduralAIProfilePrototype>();
                if (profile != null)
                    SetOverride(profile, OverrideType.Partial);
            }
        }

        public void StopOwnerLocomotor()
        {
            var agent = _owningController.Owner;
            if (agent != null)
                if (agent.IsInWorld)
                    agent.Locomotor?.Stop();
        }

        public StaticBehaviorReturnType HandleContext(IAIState state, in IStateContext stateContext, ProceduralContextPrototype proceduralContext)
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

        public void StartState(IAIState state, in IStateContext stateContext, ProceduralContextPrototype proceduralContext)
        {
            if ( _curState < 0 || _curState >= MaxConcurrentStates) return;

            _states[_curState] = new(state, proceduralContext);
            state.Start(stateContext);
            proceduralContext?.OnStart(_owningController, _proceduralPtr.Profile);
        }

        public  StaticBehaviorReturnType SwitchProceduralState(IAIState state, in IStateContext stateContext, StaticBehaviorReturnType currentBehaviorState, ProceduralContextPrototype proceduralContext = null)
        {
            /* pointless check
            AIController ownerController = stateContext.OwnerController;
            Agent ownerAgent = null;
            BehaviorBlackboard blackboard = null;
            bool usePowerTargetPos = false;

            if (ownerController != null)
            {
                ownerAgent = ownerController.Owner;
                blackboard = ownerController.Blackboard;
                usePowerTargetPos = blackboard.UsePowerTargetPos != Vector3.Zero;
            }*/

            EndStateAndLowerStates(currentBehaviorState);

            if (state != null)
            {
                /* impossible compare
                if (usePowerTargetPos && ownerController != null && blackboard != null && blackboard.UsePowerTargetPos == Vector3.Zero)
                {
                    PrototypeId activePowerRef = PrototypeId.Invalid;
                    PrototypeId summonEntityOverrideRef = PrototypeId.Invalid;
                    if (ownerAgent != null)
                    {
                        activePowerRef = ownerAgent.ActivePowerRef;
                        summonEntityOverrideRef = ownerAgent.Properties[PropertyEnum.SummonEntityOverrideRef];
                    }
                    int aiPowerStartedCount = blackboard.PropertyCollection.NumPropertiesInRange(PropertyEnum.AIPowerStarted);
                    Logger.Warn($"SwitchProceduralState stomped target position for power prior to startState? " +
                        $"CurStateIdx=[{_curState}] " +
                        $"AIPowerStarted=[{GameDatabase.GetPrototypeName(ownerController.ActivePowerRef)}] " +
                        $"AIPowerStartedCount=[{aiPowerStartedCount}] " +
                        $"CurrentBehaviorState=[{currentBehaviorState}] " +
                        $"AgentActivePower=[{GameDatabase.GetPrototypeName(activePowerRef)}] " +
                        $"SummonEntityOverride=[{GameDatabase.GetPrototypeName(summonEntityOverrideRef)}] " +
                        $"LastPowerActivated=[{GameDatabase.GetPrototypeName(blackboard.PropertyCollection[PropertyEnum.AILastPowerActivated])}] " +
                        $"TargetId=[{blackboard.PropertyCollection[PropertyEnum.AIUsePowerTargetID]}] " +
                        $"OwnerAgent=[{ownerAgent}]");                 
                }*/

                StartState(state, stateContext, proceduralContext);
                return state.Update(stateContext);
            }

            return StaticBehaviorReturnType.None; 
        }

        private void EndStateAndLowerStates(StaticBehaviorReturnType endState)
        {
            for (int i = _curState; i < MaxConcurrentStates; ++i)
            {
                _states[i].ProceduralContext?.OnEnd(_owningController, _proceduralPtr.Profile);
                _states[i].State?.End(_owningController, endState);
                _states[i] = new PState();
            }
        }

        public bool ValidateContext(IAIState state, in IStateContext stateContext)
        {
            if (state == null || stateContext == null ) return false;
            return ValidateState(state, stateContext);
        }

        private bool ValidateState(IAIState state, in IStateContext stateContext)
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

        public void PushSubstate()
        {
            if (_curState < MaxConcurrentStates - 1) _curState++;
        }

        public void PopSubstate()
        {
            if (_curState > 0) _curState--;
        }

        public void SetOverride(ProceduralAIProfilePrototype profile, OverrideType overrideType)
        {
            if (profile == null)
            {
                ClearOverrideBehavior(overrideType);
                return;
            }

            BehaviorBlackboard blackboard = _owningController.Blackboard;
            blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AICustomOverrideStateVal1);
            StopOwnerLocomotor();
            ProfilePtr tempOverride = GetOverrideByType(overrideType);
            if (tempOverride == null) return;
            if (tempOverride.Profile == profile)
            {
                Logger.Warn($"Trying to override a profile with itself: {profile}");
                return;
            }

            if (InitProceduralProfile(ref tempOverride, profile))
            {
                SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
                _owningController.OnAIBehaviorChange();
            }
        }

        private ProfilePtr GetOverrideByType(OverrideType overrideType)
        {
            return overrideType switch
            {
                OverrideType.Full => _fullOverridePtr,
                OverrideType.Partial => _partialOverridePtr,
                _ => null
            };
        }

        public void ClearOverrideBehavior(OverrideType overrideType)
        {
            StopOwnerLocomotor();
            SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
            ProfilePtr tempOverride = GetOverrideByType(overrideType);
            if (tempOverride.Profile != null)
            {
                tempOverride.Profile = null;
                _owningController.OnAIBehaviorChange();
            }
        }

        public void Think()
        {
            _proceduralPtr.Profile?.Think(_owningController);
        }

        public void ProcessInterrupts(BehaviorInterruptType interrupt)
        {
            _proceduralPtr.Profile?.ProcessInterrupts(_owningController, interrupt);
        }

        public void OnOwnerExitWorld()
        {
            _proceduralPtr.Profile?.OnOwnerExitWorld(_owningController);
        }

        public void OnOwnerKilled()
        {
            SwitchProceduralState(null, null, StaticBehaviorReturnType.Interrupted);
            _proceduralPtr.Profile?.OnOwnerKilled(_owningController);
            StopOwnerLocomotor();
        }

        public void OnAllyGotKilled()
        {
            _proceduralPtr.Profile?.OnOwnerAllyDeath(_owningController);
            _owningController.Owner?.TriggerEntityActionEvent(EntitySelectorActionEventType.OnAllyGotKilled);
        }

        public void OnOwnerTargetSwitch(ulong oldTarget, ulong newTarget)
        {
            _proceduralPtr.Profile?.OnOwnerTargetSwitch(_owningController, oldTarget, newTarget);
        }

        public void OnOwnerOverlapBegin(WorldEntity attacker)
        {
            _proceduralPtr.Profile?.OnOwnerOverlapBegin(_owningController, attacker);
        }

        public void OnEntityDeadEvent(in EntityDeadGameEvent deadEvent)
        {
            _proceduralPtr.Profile?.OnEntityDeadEvent(_owningController, deadEvent);
        }

        public void OnAIBroadcastBlackboardEvent(in AIBroadcastBlackboardGameEvent broadcastEvent)
        {
            _proceduralPtr.Profile?.OnAIBroadcastBlackboardEvent(_owningController, broadcastEvent);
        }

        public void OnPlayerInteractEvent(in PlayerInteractGameEvent broadcastEvent)
        {
            _proceduralPtr.Profile?.OnPlayerInteractEvent(_owningController, broadcastEvent);
        }

        public void OnEntityAggroedEvent(in EntityAggroedGameEvent broadcastEvent)
        {
            _proceduralPtr.Profile?.OnEntityAggroedEvent(_owningController, broadcastEvent);
        }

        public void OnMissileReturnEvent()
        {
            _proceduralPtr.Profile?.OnMissileReturnEvent(_owningController);
        }

        public void OnSetSimulated(bool simulated)
        {
            _proceduralPtr.Profile?.OnSetSimulated(_owningController, simulated);
        }

        public void OnPropertyChange(PropertyId id, PropertyValue newValue, PropertyValue oldValue, SetPropertyFlags flags)
        {
            switch (id.Enum)
            {
                case PropertyEnum.AIFullOverride:
                case PropertyEnum.AIPartialOverride:

                    if (BrainPrototype == null) return;
                    OverrideType overrideType = id.Enum == PropertyEnum.AIFullOverride ? OverrideType.Full : OverrideType.Partial;
                    if (newValue.ToPrototypeId() != PrototypeId.Invalid)
                    {
                        var profile = GameDatabase.GetPrototype<ProceduralAIProfilePrototype>(newValue.ToPrototypeId());
                        if (profile == null) return;
                        SetOverride(profile, overrideType);
                    }
                    else
                        ClearOverrideBehavior(overrideType);

                    break;
            }
        }
    }

    public enum OverrideType
    {
        Full,
        Partial
    }

    public class ProfilePtr
    {
        public ProceduralAIProfilePrototype Profile;
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
