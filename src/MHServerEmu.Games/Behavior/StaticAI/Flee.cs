using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Flee : IAIState
    {
        public static Flee Instance { get; } = new();
        private Flee() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            throw new NotImplementedException();
        }

        public void Start(IStateContext context)
        {
            throw new NotImplementedException();
        }

        public StaticBehaviorReturnType Update(IStateContext context)
        {
            throw new NotImplementedException();
        }

        public bool Validate(IStateContext context)
        {
            throw new NotImplementedException();
        }
    }

    public struct FleeContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public TimeSpan FleeTime;
        public float FleeTimeVariance;
        public float FleeHalfAngle;
        public float FleeDistanceMin;
        public bool FleeTowardAllies;
        public float FleeTowardAlliesPercentChance;

        public FleeContext(AIController ownerController, FleeContextPrototype proto)
        {
            OwnerController = ownerController;
            FleeTime = TimeSpan.FromSeconds(proto.FleeTime);
            FleeTimeVariance = proto.FleeTimeVariance;
            FleeHalfAngle = proto.FleeHalfAngle;
            FleeDistanceMin = proto.FleeDistanceMin;
            FleeTowardAllies = proto.FleeTowardAllies;
            FleeTowardAlliesPercentChance = proto.FleeTowardAlliesPercentChance;
        }
    }

}
