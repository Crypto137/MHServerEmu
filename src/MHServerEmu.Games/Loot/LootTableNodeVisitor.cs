using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// An abstract class for implementing the visitor pattern used in loot tables (separate from IItemResolver).
    /// </summary>
    public abstract class LootTableNodeVisitor
    {
        public abstract void Visit(LootNodePrototype lootNodeProto);
    }
}
