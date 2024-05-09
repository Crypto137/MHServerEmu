using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Rotate : IAIState
    {
        public static Rotate Instance { get; } = new();
        private Rotate() { }

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

    public struct RotateContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public bool Clockwise;
        public bool RotateTowardsTarget;
        public int Degrees;
        public float SpeedOverride;

        public RotateContext(AIController ownerController, RotateContextPrototype proto)
        {
            OwnerController = ownerController;
            Clockwise = proto.Clockwise;
            RotateTowardsTarget = proto.RotateTowardsTarget;
            Degrees = proto.Degrees;
            SpeedOverride = proto.SpeedOverride;
        }
    }

}
