using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot.Visitors
{
    /// <summary>
    /// An interface for various visitor pattern implementations used in loot tables.
    /// </summary>
    public interface ILootTableNodeVisitor
    {
        public void Visit(LootNodePrototype lootNodeProto);
    }
}
