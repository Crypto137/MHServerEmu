using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Delay : IAIState
    {
        public static Delay Instance { get; } = new();
        private Delay() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state) { }

        public void Start(in IStateContext context)
        {
            if (context is not DelayContext delayContext) return;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return;
            BehaviorBlackboard blackboard = ownerController.Blackboard;
            Agent agent = ownerController.Owner;
            if (agent == null) return;
            Game game = agent.Game;
            if (game == null) return;

            long delayMS = game.Random.Next(delayContext.MinDelayMS, delayContext.MaxDelayMS);
            blackboard.PropertyCollection[PropertyEnum.AIDelayCompletionTime] = (long)game.GetCurrentTime().TotalMilliseconds + delayMS;
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var returnType = StaticBehaviorReturnType.Failed;
            if (context is not DelayContext) return returnType;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return returnType;
            Agent agent = ownerController.Owner;
            if (agent == null) return returnType;
            Game game = agent.Game;
            if (game == null) return returnType;

            BehaviorBlackboard blackboard = ownerController.Blackboard;
            WorldEntity target = ownerController.TargetEntity;
            if (target != null && target.IsInWorld && agent.CanRotate)
                agent.OrientToward(target.RegionLocation.Position);

            if ((long)game.GetCurrentTime().TotalMilliseconds < blackboard.PropertyCollection[PropertyEnum.AIDelayCompletionTime])
                return StaticBehaviorReturnType.Running;
            else
                return StaticBehaviorReturnType.Completed;
        }

        public bool Validate(in IStateContext context)
        {
            return true;
        }
    }

    public struct DelayContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public int MinDelayMS;
        public int MaxDelayMS;

        public DelayContext(AIController ownerController, DelayContextPrototype proto)
        {
            OwnerController = ownerController;
            MinDelayMS = proto.MinDelayMS;
            MaxDelayMS = proto.MaxDelayMS;
        }
    }
}
