using System.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Inventories
{
    // TODO: InventoryIterator

    public class InventoryCollection : IEnumerable<Inventory>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<PrototypeId, Inventory> _inventoryDict = new();
        private Entity _owner;

        public void Initialize(Entity owner)
        {
            _owner = owner;
        }

        public bool CreateAndAddInventory(PrototypeId invProtoRef)
        {
            if (_owner == null) return Logger.WarnReturn(false, "CreateAndAddInventory(): _owner == null");
            if (invProtoRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "CreateAndAddInventory(): invProtoRef == PrototypeId.Invalid");
            
            if (GetInventoryByRef(invProtoRef) != null)
                return Logger.WarnReturn(false, $"Trying to add a duplicate Inventory [{GameDatabase.GetPrototypeName(invProtoRef)}] to an entity's InventoryCollection.\nEntity: [{_owner?.Id}]");

            Inventory inventory = new(_owner.Game);

            if (inventory.Initialize(invProtoRef, _owner.Id) == false)
                return Logger.WarnReturn(false, $"Failed to initialize inventory [{GameDatabase.GetPrototypeName(invProtoRef)}] for entity [{_owner?.Id}]");

            _inventoryDict.Add(invProtoRef, inventory);
            return true;
        }

        public Inventory GetInventoryByRef(PrototypeId invProtoRef)
        {
            if (_inventoryDict.TryGetValue(invProtoRef, out Inventory inventory) == false)
                return null;

            return inventory;
        }

        public bool GetInventoryForItem(Item item, InventoryCategory category, out Inventory inventory)
        {
            // TODO
            inventory = null;
            return false;
        }

        public void Clear()
        {
            _inventoryDict.Clear();
        }

        public IEnumerator<Inventory> GetEnumerator() => _inventoryDict.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
