using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Inventories
{
    public class InventoryCollection
    {
        private readonly Dictionary<PrototypeId, Inventory> _inventoryDict = new();
        private Entity _owner;

        public int Count { get => _inventoryDict.Count; }

        public void Initialize(Entity owner)
        {
            _owner = owner;
        }

        public bool CreateAndAddInventory(PrototypeId invProtoRef)
        {
            if (!Verify.IsNotNull(_owner)) return false;
            if (!Verify.IsTrue(invProtoRef != PrototypeId.Invalid)) return false;

            if (!Verify.IsTrue(GetInventoryByRef(invProtoRef) == null, $"Trying to add a duplicate Inventory [{invProtoRef.GetName()}] to an entity's InventoryCollection.\nEntity: [{_owner}]"))
                return false;

            Inventory inventory = new(_owner.Game);

            if (!Verify.IsTrue(inventory.Initialize(invProtoRef, _owner.Id), $"Failed to initialize inventory [{invProtoRef.GetName()}] for entity [{_owner}]"))
                return false;

            _inventoryDict.Add(invProtoRef, inventory);
            return true;
        }

        public Inventory GetInventoryByRef(PrototypeId invProtoRef)
        {
            if (_inventoryDict.TryGetValue(invProtoRef, out Inventory inventory) == false)
                return null;

            return inventory;
        }

        public bool GetInventoryForItem(Item item, InventoryCategory category, out Inventory outInventory)
        {
            outInventory = null;

            if (!Verify.IsNotNull(_owner)) return false;

            // NOTE: The client uses the sort flag here, but it's bad for performance, so I will leave it out for now
            foreach (Inventory inventory in new InventoryIterator(_owner/*, InventoryIterationFlags.SortByPrototypeRef*/))
            {
                if (inventory.Category != category)
                    continue;

                if (inventory.IsSlotAvailableForEntity(item, true) == false)
                    continue;

                if (item.CanChangeInventoryLocation(inventory) != InventoryResult.Success)
                    continue;

                outInventory = inventory;
                return true;
            }

            return true;
        }

        public void Clear()
        {
            _inventoryDict.Clear();
        }

        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        public struct Enumerator : IEnumerator<Inventory>
        {
            // A simple wrapper around Dictionary<PrototypeId, Inventory>.ValueCollection.Enumerator
            // to avoid exposing the internal implementation (and be less wordy in general).

            private readonly InventoryCollection _inventoryCollection;
            private Dictionary<PrototypeId, Inventory>.ValueCollection.Enumerator _enumerator;

            public Inventory Current { get => _enumerator.Current; }
            object IEnumerator.Current { get => Current; }

            public Enumerator(InventoryCollection inventoryCollection)
            {
                _inventoryCollection = inventoryCollection;
                _enumerator = _inventoryCollection._inventoryDict.Values.GetEnumerator();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Dispose();
                _enumerator = _inventoryCollection._inventoryDict.Values.GetEnumerator();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}
