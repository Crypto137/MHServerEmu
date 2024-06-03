using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class Despawn : IAIState
    {
        public static Despawn Instance { get; } = new();
        private Despawn() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state) { }

        public void Start(in IStateContext context) { }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var returnType = StaticBehaviorReturnType.Failed;
            if (context is not DespawnContext despawnContext) return returnType;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return returnType;
            Agent ownerAgent = ownerController.Owner;
            if (ownerAgent == null) return returnType;

            if (despawnContext.DespawnTarget)
            {
                WorldEntity target = ownerController.TargetEntity;
                if (target != null)
                {
                    if (despawnContext.UseKillInsteadOfDestroy)
                        target.Kill(null);
                    else
                        target.Destroy();

                    ownerController.SetTargetEntity(null);
                }
            }

            if (despawnContext.DespawnOwner)
            {
                if (despawnContext.UseKillInsteadOfDestroy)
                    ownerAgent.Kill(null);
                else
                    ownerAgent.Destroy();
            }

            return StaticBehaviorReturnType.Completed;
        }

        public bool Validate(in IStateContext context)
        {
            return true;
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
