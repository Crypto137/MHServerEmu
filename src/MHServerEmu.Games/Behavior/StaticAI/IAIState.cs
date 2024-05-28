using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public interface IAIState
    {        
        public bool Validate(in IStateContext context);
        public void Start(in IStateContext context);
        public StaticBehaviorReturnType Update(in IStateContext context);
        public void End(AIController ownerController, StaticBehaviorReturnType state);
    }

    public interface IStateContext
    {
        public AIController OwnerController { get; set; }
        public static (IAIState, IStateContext) Create(AIController ownerController, Prototype proto) 
        {
            return proto switch
            {
                DelayContextPrototype delayProto => (Delay.Instance, new DelayContext(ownerController, delayProto)),
                DespawnContextPrototype despawnProto => (Despawn.Instance, new DespawnContext(ownerController, despawnProto)),
                FlankContextPrototype flankProto => (Flank.Instance, new FlankContext(ownerController, flankProto)),
                FleeContextPrototype fleeProto => (Flee.Instance, new FleeContext(ownerController, fleeProto)),
                FlockContextPrototype flockProto => (Flock.Instance, new FlockContext(ownerController, flockProto)),
                InteractContextPrototype interactProto => (Interact.Instance, new InteractContext(ownerController, interactProto)),
                MoveToContextPrototype moveToProto => (MoveTo.Instance, new MoveToContext(ownerController, moveToProto)),
                OrbitContextPrototype orbitProto => (Orbit.Instance, new OrbitContext(ownerController, orbitProto)),
                RotateContextPrototype rotateProto => (Rotate.Instance, new RotateContext(ownerController, rotateProto)),
                TeleportContextPrototype teleportProto => (Teleport.Instance, new TeleportContext(ownerController, teleportProto)),
                TriggerSpawnersContextPrototype triggerProto => (TriggerSpawners.Instance, new TriggerSpawnersContext(ownerController, triggerProto)),
                UseAffixPowerContextPrototype useAffixPowerProto => (UseAffixPower.Instance, new UseAffixPowerContext(ownerController, useAffixPowerProto)),
                UsePowerContextPrototype usePowerProto => (UsePower.Instance, new UsePowerContext(ownerController, usePowerProto)),
                WanderContextPrototype wanderProto => (Wander.Instance, new WanderContext(ownerController, wanderProto)),
                _ => throw new ArgumentException("Unknown prototype type"),
            };
        }
    }

    public enum StaticBehaviorReturnType
    {
        None = 0,
        Completed = 1,
        Failed = 2,
        Running = 3,
        Interrupted = 4,
    }
}
