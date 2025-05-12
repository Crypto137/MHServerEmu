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
            private readonly Inventory _inventory;
            private readonly EntityManager _manager;
            private Inventory.Enumerator _inventoryEnumerator;

            public WorldEntity Current { get; private set; } = default;
            object IEnumerator.Current { get => Current; }

            public Enumerator(WorldEntity owner)
            {
                _inventory = owner.GetInventory(InventoryConvenienceLabel.Summoned);
                _manager = owner.Game?.EntityManager;

                if (_inventory != null)
                    _inventoryEnumerator = _inventory.GetEnumerator();
            }

            public bool MoveNext()
            {
                if (_inventory == null || _manager == null)
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

            public void Dispose()
            {
                _inventoryEnumerator.Dispose();
            }

            public void Reset()
            {
                _inventoryEnumerator.Reset();
            }
        }
    }
}
