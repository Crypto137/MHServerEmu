
namespace MHServerEmu.Games.Behavior.StaticAI
{
    public interface IAIState
    {
        public bool Validate(IStateContext context);
        public void Start(IStateContext context);
        public StaticBehaviorReturnType Update(IStateContext context);
        public void End(AIController ownerController, StaticBehaviorReturnType state);
    }

    public class IStateContext
    {
        public AIController OwnerController;
        public IStateContext(AIController ownerController)
        {
            OwnerController = ownerController;
        }
    }

    public enum StaticBehaviorReturnType
    {
        None,
    }
}
