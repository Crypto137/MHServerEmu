using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum EntityGameEventEnum
    {
        Invalid = 0,
        AdjustHealth = 1,
        EntityDead = 2,
        EntityEnteredWorld = 3,
        EntityExitedWorld = 4,
        PlayerInteract = 5,
    }

    #endregion

    public class GameEventPrototype : Prototype
    {
    }

    public class EntityGameEventPrototype : GameEventPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public EntityGameEventEnum Event { get; protected set; }
        public bool UniqueEntities { get; protected set; }
    }

    public class EntityGameEventEvalPrototype : Prototype
    {
        public EntityGameEventPrototype Event { get; protected set; }
        public EvalPrototype Eval { get; protected set; }
    }
}
