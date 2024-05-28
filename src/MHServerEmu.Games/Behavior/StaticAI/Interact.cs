using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Interact : IAIState
    {
        public static Interact Instance { get; } = new();
        private Interact() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            throw new NotImplementedException();
        }

        public void Start(in IStateContext context)
        {
            throw new NotImplementedException();
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            throw new NotImplementedException();
        }

        public bool Validate(in IStateContext context)
        {
            throw new NotImplementedException();
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
