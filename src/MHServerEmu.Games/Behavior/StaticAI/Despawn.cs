using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Despawn : IAIState, ISingleton<Despawn>
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

    public struct DespawnContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public bool DespawnOwner;
        public bool DespawnTarget;
        public bool UseKillInsteadOfDestroy;

        public DespawnContext(AIController ownerController, DespawnContextPrototype proto)
        {
            OwnerController = ownerController;
            DespawnOwner = proto.DespawnOwner;
            DespawnTarget = proto.DespawnTarget;
            UseKillInsteadOfDestroy = proto.UseKillInsteadOfDestroy;
        }
    }

}
