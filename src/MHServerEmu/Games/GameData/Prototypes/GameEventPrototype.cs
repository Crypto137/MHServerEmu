namespace MHServerEmu.Games.GameData.Prototypes
{
    public class GameEventPrototype : Prototype
    {
        public GameEventPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(GameEventPrototype), proto); }
    }

    public class EntityGameEventPrototype : GameEventPrototype
    {
        public EntityFilterPrototype EntityFilter;
        public EntityGameEventEnum Event;
        public bool UniqueEntities;
        public EntityGameEventPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityGameEventPrototype), proto); }
    }
    public enum EntityGameEventEnum
    {
        Invalid = 0,
        AdjustHealth = 1,
        EntityDead = 2,
        EntityEnteredWorld = 3,
        EntityExitedWorld = 4,
        PlayerInteract = 5,
    }
    public class EntityGameEventEvalPrototype : Prototype
    {
        public EntityGameEventPrototype Event;
        public EvalPrototype Eval;
        public EntityGameEventEvalPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityGameEventEvalPrototype), proto); }
    }

}
