using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot.Visitors
{
    // This isn't really needed for anything but debugging crafting recipes

    public struct LootTableContainsMutationOfType<T> : ILootTableNodeVisitor where T : LootMutationPrototype
    {
        private bool _found = false;

        public LootTableContainsMutationOfType(LootTablePrototype table)
        {
            if (table == null)
                return;

            table.Visit(ref this);
        }

        public void Visit(LootNodePrototype lootNodeProto)
        {
            if (_found)
                return;

            LootMutationPrototype[] mutations = null;

            switch (lootNodeProto)
            {
                case LootDropItemPrototype itemProto:
                    mutations = itemProto.Mutations;
                    break;

                case LootDropClonePrototype cloneProto:
                    mutations = cloneProto.Mutations;
                    break;
            }

            if (mutations.HasValue() == false)
                return;

            foreach (LootMutationPrototype mutationProto in mutations)
            {
                if (mutationProto is T)
                {
                    _found = true;
                    break;
                }
            }
        }

        public static implicit operator bool(LootTableContainsMutationOfType<T> visitor)
        {
            return visitor._found;
        }
    }
}
