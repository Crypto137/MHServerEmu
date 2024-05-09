using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Orbit : IAIState, ISingleton<Orbit>
    {
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

    public struct OrbitContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public float ThetaInDegrees;

        public OrbitContext(AIController ownerController, OrbitContextPrototype proto)
        {
            OwnerController = ownerController;
            ThetaInDegrees = proto.ThetaInDegrees;
        }
    }

}
