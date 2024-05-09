using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Teleport : IAIState
    {
        public static Teleport Instance { get; } = new();
        private Teleport() { }

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

    public struct TeleportContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public TeleportType TeleportType;

        public TeleportContext(AIController ownerController, TeleportContextPrototype proto)
        {
            OwnerController = ownerController;
            TeleportType = proto.TeleportType;
        }
    }

}
