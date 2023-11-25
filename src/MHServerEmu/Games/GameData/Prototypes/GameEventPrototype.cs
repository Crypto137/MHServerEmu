namespace MHServerEmu.Games.GameData.Prototypes
{
    public enum EntityGameEventEnum
    {
        Invalid = 0,
        AdjustHealth = 1,
        EntityDead = 2,
        EntityEnteredWorld = 3,
        EntityExitedWorld = 4,
        PlayerInteract = 5,
    }

    public class GameEventPrototype : Prototype
    {
    }

    public class EntityGameEventPrototype : GameEventPrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
        public EntityGameEventEnum Event { get; set; }
        public bool UniqueEntities { get; set; }
    }

    public class EntityGameEventEvalPrototype : Prototype
    {
        public EntityGameEventPrototype Event { get; set; }
        public EvalPrototype Eval { get; set; }
    }
}
