using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Properties;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Dialog;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Interact : IAIState
    {
        public static readonly Logger Logger = LogManager.CreateLogger();
        public static Interact Instance { get; } = new();
        private Interact() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            ownerController.Blackboard.PropertyCollection.RemoveProperty(PropertyEnum.AIInteractStarted);
        }

        public void Start(in IStateContext context) { }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            StaticBehaviorReturnType failResult = StaticBehaviorReturnType.Failed;
            if (context == null) return failResult;
            if (context is not InteractContext interactContext) return failResult;
            AIController ownerController = interactContext.OwnerController;
            if (ownerController == null) return failResult;
            Agent agent = ownerController.Owner;
            if (agent == null) return failResult;
            BehaviorBlackboard blackboard = ownerController.Blackboard;

            if (blackboard.PropertyCollection[PropertyEnum.AIInteractStarted] == false)
            {
                WorldEntity interactTarget = ownerController.InteractEntity;
                if (interactTarget == null) return failResult;
                
                var interacteeDesc = new EntityDesc(agent.Game, interactTarget.Id, string.Empty);
                if (agent.StartInteractionWith(interacteeDesc, InteractionFlags.Default, false, InteractionMethod.All) != InteractionResult.Success)
                    return failResult;

                blackboard.PropertyCollection[PropertyEnum.AIInteractStarted] = true;
            }

            if (blackboard.PropertyCollection.HasProperty(PropertyEnum.AIThrowPower))
            {
                if (blackboard.PropertyCollection.HasProperty(PropertyEnum.AIThrownObjectPickedUp))
                {
                    WorldEntity target = ownerController.TargetEntity;
                    if (agent.CanRotate && target != null && target.IsInWorld)
                    {
                        Locomotor locomotor = agent.Locomotor;
                        if (locomotor == null)
                        {
                            Logger.Warn($"Agent [{agent}] does not have a locomotor and should not be calling this function");
                            return failResult;
                        }
                        locomotor.LookAt(target.RegionLocation.Position);
                    }
                }
                return StaticBehaviorReturnType.Running;
            }

            return StaticBehaviorReturnType.Completed;
        }

        public bool Validate(in IStateContext context)
        {
            if (context == null) return false;
            if (context is not InteractContext interactContext) return false;
            AIController ownerController = interactContext.OwnerController;
            if (ownerController == null) return false;
            Agent agent = ownerController.Owner;
            if (agent == null) return false;
            Locomotor locomotor = agent.Locomotor;
            if (locomotor == null 
                || locomotor.HasPath 
                || agent.IsExecutingPower 
                || ownerController.ActivePowerRef != PrototypeId.Invalid) return false;

            WorldEntity interactTarget = ownerController.InteractEntity;
            if (interactTarget == null) return false;

            float distanceTo = agent.GetDistanceTo(interactTarget, true);
            if (distanceTo > 10.0f) return false;

            return true;
        }
    }

    public struct InteractContext : IStateContext
    {
        public AIController OwnerController { get; set; }

        public InteractContext(AIController ownerController, InteractContextPrototype proto)
        {
            OwnerController = ownerController;
        }
    }
}
