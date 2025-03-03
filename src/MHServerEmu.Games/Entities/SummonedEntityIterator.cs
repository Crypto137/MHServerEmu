using MHServerEmu.Games.Entities.Inventories;
using System.Collections;

namespace MHServerEmu.Games.Entities
{
    public readonly struct SummonedEntityIterator
    {
        private readonly WorldEntity _owner;

        public SummonedEntityIterator(WorldEntity owner)
        {
            _owner = owner;
        }

        public Enumerator GetEnumerator() => new (_owner);

        public struct Enumerator : IEnumerator<WorldEntity>
        {
            private readonly EntityManager _manager;
            private IEnumerator<Inventory.IterationEntry> _inventoryEnumerator;

            public Enumerator(WorldEntity owner)
            {
                _manager = owner.Game?.EntityManager;
                var inventory = owner.GetInventory(InventoryConvenienceLabel.Summoned);
                _inventoryEnumerator = inventory?.GetEnumerator();
            }

            public WorldEntity Current { get; private set; } = default;

            object IEnumerator.Current { get => Current; }

            public bool MoveNext()
            {
                if (_manager == null || _inventoryEnumerator == null)
                    return false;

                while (_inventoryEnumerator.MoveNext())
                {
                    var entry = _inventoryEnumerator.Current;
                    var entity = _manager.GetEntity<WorldEntity>(entry.Id);
                    if (entity == null) continue;
                    Current = entity;
                    return true;
                }

                Current = null;
                return false;
            }

            public void Reset()
            {
                _inventoryEnumerator?.Reset();
                Current = default;
            }

            public void Dispose()
            {
                _inventoryEnumerator?.Dispose();
            }
        }
    }
}
