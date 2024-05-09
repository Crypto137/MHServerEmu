using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public interface IAIState
    {        
        public bool Validate(IStateContext context);
        public void Start(IStateContext context);
        public StaticBehaviorReturnType Update(IStateContext context);
        public void End(AIController ownerController, StaticBehaviorReturnType state);
    }

    public interface ISingleton<T>
    {
        public static T Instance { get; }
    }

    public interface IStateContext
    {
        public AIController OwnerController { get; set; }
        public static IStateContext Create(AIController ownerController, Prototype proto) 
        {
            return proto switch
            {
                DelayContextPrototype delayProto => new DelayContext(ownerController, delayProto),
                DespawnContextPrototype despawnProto => new DespawnContext(ownerController, despawnProto),
                FlankContextPrototype flankProto => new FlankContext(ownerController, flankProto),
                FleeContextPrototype fleeProto => new FleeContext(ownerController, fleeProto),
                FlockContextPrototype flockProto => new FlockContext(ownerController, flockProto),
                InteractContextPrototype interactProto => new InteractContext(ownerController, interactProto),
                MoveToContextPrototype moveToProto => new MoveToContext(ownerController, moveToProto),
                OrbitContextPrototype orbitProto => new OrbitContext(ownerController, orbitProto),
                RotateContextPrototype rotateProto => new RotateContext(ownerController, rotateProto),
                TeleportContextPrototype teleportProto => new TeleportContext(ownerController, teleportProto),
                TriggerSpawnersContextPrototype triggerProto => new TriggerSpawnersContext(ownerController, triggerProto),
                UseAffixPowerContextPrototype useAffixPowerProto => new UseAffixPowerContext(ownerController, useAffixPowerProto),
                UsePowerContextPrototype usePowerProto => new UsePowerContext(ownerController, usePowerProto),
                WanderContextPrototype wanderProto => new WanderContext(ownerController, wanderProto),
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
