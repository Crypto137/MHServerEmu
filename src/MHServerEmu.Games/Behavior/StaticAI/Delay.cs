using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Delay : IAIState
    {
        public static Delay Instance { get; } = new();
        private Delay() { }

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
