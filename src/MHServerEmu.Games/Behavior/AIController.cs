using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Generators.Population;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior
{
    public class AIController
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public Agent Owner { get; private set; }
        public Game Game { get; private set; }
        public ProceduralAI.ProceduralAI Brain { get; private set; }
        public BehaviorSensorySystem Senses { get; private set; }
        public BehaviorBlackboard Blackboard { get; private set; }   
        public bool IsEnabled { get; private set; }
        public WorldEntity TargetEntity => Senses.GetCurrentTarget();
        public WorldEntity InteractEntity => GetInteractEntityHelper();
        public WorldEntity AssistedEntity => GetAssistedEntityHelper();

        public AIController(Game game, Agent owner)
        {
            Game = game;
            Owner = owner;
            Senses = new ();
            Blackboard = new (owner);
            Brain = new (game, this); 
        }

        public bool Initialize(BehaviorProfilePrototype profile, SpawnSpec spec, PropertyCollection collection)
        {
            Senses.Initialize(this, profile, spec);
            Blackboard.Initialize(profile, spec, collection);
            Brain.Initialize(profile);
            return true;
        }

        public bool IsOwnerValid()
        {
            if (Owner != null && Owner.IsInWorld && Owner.IsSimulated 
                && Owner.TestStatus(EntityStatus.PendingDestroy) == false 
                && Owner.TestStatus(EntityStatus.Destroyed) == false)
                return true;
            
            return false;
        }

        public bool GetDesiredIsWalkingState(MovementSpeedOverride speedOverride)
        {
            var locomotor = Owner?.Locomotor;            
            if (locomotor != null && locomotor.SupportsWalking)
            {
                if (speedOverride == MovementSpeedOverride.Walk)               
                    return true;
                 else if (speedOverride == MovementSpeedOverride.Default) 
                    return Senses.GetCurrentTarget() == null;
            }
            else
            {
                if (speedOverride == MovementSpeedOverride.Walk)
                    Logger.Warn("An AI agent's behavior has a movement context with a MovementSpeed of Walk, " +
                        $"but the agent's Locomotor doesn't support Walking.\nAgent [{Owner}]");
            }            
            return false;
        }
        
        private WorldEntity GetInteractEntityHelper()
        {
            throw new NotImplementedException();
        }
        
        private WorldEntity GetAssistedEntityHelper()
        {
            throw new NotImplementedException();
        }

        public float AggroRangeAlly 
        { 
            get => 
                Blackboard.PropertyCollection.HasProperty(PropertyEnum.AIAggroRangeOverrideAlly) ?
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeOverrideAlly] :
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeAlly];
        }

        public float AggroRangeHostile 
        { 
            get => 
                Blackboard.PropertyCollection.HasProperty(PropertyEnum.AIAggroRangeOverrideHostile) ?
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeOverrideHostile] : 
                Blackboard.PropertyCollection[PropertyEnum.AIAggroRangeHostile]; 
        }
        public PrototypeId ActivePowerRef { get; internal set; }

        public void OnAIActivated()
        {
            OnAIEnabled();
        }

        public void OnAIEnteredWorld()
        {
            OnAIEnabled();
        }

        public void OnAIAllianceChange()
        {
            throw new NotImplementedException();
        }

        private void ScheduleAIThinkEvent(TimeSpan timeSpan, bool useGlobalThinkVariance, bool ignoreActivePower)
        {
            throw new NotImplementedException();
            // HasNotExceededMaxThinksPerFrame
            // AIThinkEvent
        }

        private void ClearScheduledThinkEvent()
        {
            throw new NotImplementedException();
        }

        public void SetIsEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (enabled)
                OnAIEnabled();
            else
                OnAIDisabled();
        }

        private void OnAIEnabled()
        {
            ScheduleAIThinkEvent(TimeSpan.FromMilliseconds(0), true, false);
        }        

        private void OnAIDisabled()
        {
            ClearScheduledThinkEvent();
        }

        public void OnAIExitedWorld()
        {
            OnAIDisabled();
            Brain?.OnOwnerExitWorld();
        }

        public void ProcessInterrupts()
        {
            if (Brain == null) return;
            //  Some debug stuff
            // TODO Brain ProcessInterrupts
        }

        private bool HasNotExceededMaxThinksPerFrame(TimeSpan timeOffset)
        {
            if (Game == null || Brain == null) return false;

            if (timeOffset == TimeSpan.Zero && Brain.LastThinkQTime == Game.NumQuantumFixedTimeUpdates && Brain.ThinkCountPerFrame > 4)
            {
                Logger.Warn($"Tried to schedule too many thinks on the same frame. Frame: {Game.NumQuantumFixedTimeUpdates}, " +
                    $"Agent: {Owner}, Think count: {Brain?.ThinkCountPerFrame}");
                return false;
            }

            return true;
        }

        internal void AddPowersToPicker(Picker<ProceduralUsePowerContextPrototype> powerPicker, ProceduralUsePowerContextPrototype[] genericProceduralPowers)
        {
            throw new NotImplementedException();
        }

        internal void AddPowersToPicker(Picker<ProceduralUsePowerContextPrototype> powerPicker, ProceduralUsePowerContextPrototype primaryPower)
        {
            throw new NotImplementedException();
        }

        internal void ResetCurrentTargetState()
        {
            throw new NotImplementedException();
        }

        internal void Think()
        {
            throw new NotImplementedException();
        }

        internal void OnAIKilled()
        {
            throw new NotImplementedException();
        }
    }
}
