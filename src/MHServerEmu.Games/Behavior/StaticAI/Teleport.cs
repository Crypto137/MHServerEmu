
namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Teleport : IAIState
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

    public class TeleportContext : IStateContext
    {
        public TeleportContext(AIController ownerController) : base(ownerController)
        {
        }
    }
}
