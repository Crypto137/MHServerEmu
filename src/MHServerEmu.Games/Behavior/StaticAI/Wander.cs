using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Wander : IAIState
    {
        public static Wander Instance { get; } = new();
        private Wander() { }

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

    public struct WanderContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public WanderBasePointType FromPoint;
        public MovementSpeedOverride MovementSpeed;
        public float RangeMax;
        public float RangeMin;

        public WanderContext(AIController ownerController, WanderContextPrototype proto)
        {
            OwnerController = ownerController;
            FromPoint = proto.FromPoint;
            RangeMax = proto.RangeMax;
            RangeMin = proto.RangeMin;
            MovementSpeed = proto.MovementSpeed;
        }
    }
}
