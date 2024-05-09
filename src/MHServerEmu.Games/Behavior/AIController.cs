
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior
{
    public class AIController
    {
        private ProceduralAI.ProceduralAI _brain { get; set; }

        public BehaviorSensorySystem Senses { get; set; }
        
        public WorldEntity Owner { get; set; }
        
        public Game Game { get; internal set; }
        
        public bool IsEnabled { get; private set; }
        
        public ReplicatedPropertyCollection Properties { get; internal set; } // This seems to be returned by the Blackboard property? (need to check in IDA)
        
        public WorldEntity TargetEntity => Senses.GetCurrentTarget(); // Should we null check senses here?

        public WorldEntity InteractEntity => GetInteractEntityHelper();

        public WorldEntity AssistedEntity => GetAssistedEntityHelper();
        
        public bool IsOwnerValid()
        {
            if (Owner != null)
            {
                if(!Owner.IsInWorld ||
                    Owner.IsSimulated || 
                    Owner.TestStatus(EntityStatus.PendingDestroy) || 
                    Owner.TestStatus(EntityStatus.Destroyed))
                {
                    return false;
                }
            }
            
            return true;
        }

        public bool GetDesiredIsWalkingState(MovementSpeedOverride movementSpeedOverride)
        {
            var locomotor = Owner?.Locomotor;
            
            if (locomotor != null && locomotor.SupportsWalking)
            {
                if (movementSpeedOverride == MovementSpeedOverride.Walk)
                    return true;
                
                return Senses.GetCurrentTarget() == null;
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

        public float GetAggroRangeAlly()
        {
            return Properties.HasProperty(PropertyEnum.AIAggroRangeOverrideAlly) ? 
                Properties[PropertyEnum.AIAggroRangeOverrideAlly].ToFloat() : 
                Properties[PropertyEnum.AIAggroRangeAlly].ToFloat();
        }

        public float GetAggroRangeHostile()
        {
            return Properties.HasProperty(PropertyEnum.AIAggroRangeOverrideHostile)
                ? Properties[PropertyEnum.AIAggroRangeOverrideHostile].ToFloat()
                : Properties[PropertyEnum.AIAggroRangeHostile].ToFloat();
        }

        public void OnAIActivated()
        {
            throw new NotImplementedException();
        }

        public void OnAIEnteredWorld()
        {
            throw new NotImplementedException();
        }

        public void OnAIAllianceChange()
        {
            throw new NotImplementedException();
        }

        public void OnAIEnabled()
        {
            throw new NotImplementedException();
        }
        
        public void ProcessInterrupts()
        {
            //if(_brain == null)
            //  Some debug stuff

            return;
        }

        public void SetIsEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (enabled)
            { 
                OnAIEnabled();
            }

            OnAIDisabled();
        }
        
        private void OnAIDisabled()
        {
            throw new NotImplementedException();
        }
        
        private bool HasNotExceededMaxThinksPerFrame(TimeSpan elapsed)
        {
            if (Game == null)
            {
                return false;
            }

            if (_brain == null)
            {
                return false;
            }

            var notExceeded = true;

            if (elapsed == TimeSpan.Zero)
            {   
                // ToDo: Fill me in we are looking up timesteps in the game object 
            }

            return notExceeded;
        }
    }
}
