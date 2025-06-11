using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot.Visitors
{
    /// <summary>
    /// Recursively visits all nodes in the provided <see cref="LootTablePrototype"/> array
    /// and checks if any of them contains a node of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Implicitly converts to <see cref="bool"/> representing the result of the search.
    /// </remarks>
    public struct LootTableContainsNodeOfType<T> : ILootTableNodeVisitor where T: LootNodePrototype
    {
        private bool _found = false;

        public LootTableContainsNodeOfType(LootTablePrototype[] tables)
        {
            if (tables.IsNullOrEmpty())
                return;

            foreach (LootTablePrototype lootTableProto in tables)
                lootTableProto.Visit(ref this);
        }

        public void Visit(LootNodePrototype lootNodeProto)
        {
            if (_found)
                return;

            _found = lootNodeProto is T;
        }

        public static implicit operator bool(LootTableContainsNodeOfType<T> visitor)
        {
            return visitor._found;
        }
    }
}
