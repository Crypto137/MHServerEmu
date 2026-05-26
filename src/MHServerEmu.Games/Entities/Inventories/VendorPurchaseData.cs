using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Entities.Inventories
{
    /// <summary>
    /// A wrapper around <see cref="GBitArray"/> for keeping track of items purchased from specific vendors.
    /// </summary>
    public class VendorPurchaseData : ISerialize
    {
        private const uint InitialCapacity = 128;
        private const uint MaxCapacity = 1024;

        // NOTE: We can't use the size of the bit array as is because it's a multiple of the word size (8 for ulong)
        private PrototypeId _inventoryProtoRef = PrototypeId.Invalid;
        private uint _numItems = 0;
        private GBitArray _itemBits = new();

        public PrototypeId InventoryProtoRef { get => _inventoryProtoRef; }

        public VendorPurchaseData(PrototypeId inventoryProtoRef)
        {
            _inventoryProtoRef = inventoryProtoRef;
            _itemBits.Resize((int)InitialCapacity);
        }

        public override string ToString()
        {
            return $"{_inventoryProtoRef.GetName()} ({_numItems} items)";
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;
            success &= Serializer.Transfer(archive, ref _inventoryProtoRef);
            success &= Serializer.Transfer(archive, ref _numItems);
            success &= Serializer.Transfer(archive, ref _itemBits);
            return success;
        }

        public bool Initialize(uint numItems)
        {
            if (_numItems == numItems)
                return true;

            if (!Verify.IsTrue(numItems <= MaxCapacity)) return false;

            _numItems = numItems;

            // Increase the capacity of the array to fit all items if needed
            int capacity = _itemBits.Size;
            if (capacity < _numItems)
            {
                while (capacity < _numItems)
                    capacity *= 2;
                _itemBits.Resize(capacity);
            }

            return true;
        }

        public void Clear()
        {
            _numItems = 0;
            _itemBits.Clear();
        }

        public bool HasItemBeenPurchased(uint slot)
        {
            if (!Verify.IsTrue(slot < _numItems)) return false;
            return _itemBits[(int)slot];
        }

        public bool RecordItemPurchase(uint slot)
        {
            if (!Verify.IsTrue(slot < _numItems)) return false;
            _itemBits.Set((int)slot);
            return true;
        }
    }
}
