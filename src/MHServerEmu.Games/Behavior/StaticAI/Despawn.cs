
namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Despawn : IAIState
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

    public class DespawnContext : IStateContext
    {
        public DespawnContext(AIController ownerController) : base(ownerController)
        {
         
        }
    }
}
