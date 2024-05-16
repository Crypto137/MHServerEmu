using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Inventories
{
    // NOTE/TODO: This would not be client-accurate, but we can potentially refactor this as a readonly struct for optimization.
    public class InventoryLocation : IEquatable<InventoryLocation>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly InventoryLocation Invalid = new();

        public ulong ContainerId { get; private set; } = 0;     // Entity id
        public InventoryPrototype InventoryPrototype { get; private set; } = null;
        public uint Slot { get; private set; } = Inventory.InvalidSlot;

        public PrototypeId InventoryRef { get => InventoryPrototype != null ? InventoryPrototype.DataRef : PrototypeId.Invalid; }
        public InventoryCategory InventoryCategory { get => InventoryPrototype != null ? InventoryPrototype.Category : InventoryCategory.None; }
        public InventoryConvenienceLabel InventoryConvenienceLabel { get => InventoryPrototype != null ? InventoryPrototype.ConvenienceLabel : InventoryConvenienceLabel.None; }

        public bool IsValid { get => ContainerId != 0 && InventoryRef != PrototypeId.Invalid && Slot != Inventory.InvalidSlot; }

        public InventoryLocation() { }

        public InventoryLocation(ulong containerId, PrototypeId inventoryRef, uint slot)
        {
            ContainerId = containerId;
            InventoryPrototype = inventoryRef.As<InventoryPrototype>();
            Slot = slot;
        }

        public InventoryLocation(InventoryLocation other)
        {
            ContainerId = other.ContainerId;
            InventoryPrototype = other.InventoryPrototype;
            Slot = other.Slot;
        }

        public static bool SerializeTo(Archive archive, InventoryLocation invLoc)
        {
            if (archive.IsPacking == false) return Logger.WarnReturn(false, "SerializeTo(): archive.IsPacking == false");

            bool success = true;
            
            ulong containerId = invLoc.ContainerId;
            PrototypeId inventoryRef = invLoc.InventoryRef;
            uint slot = invLoc.Slot;

            success &= Serializer.Transfer(archive, ref containerId);
            success &= Serializer.TransferPrototypeEnum<InventoryPrototype>(archive, ref inventoryRef);
            success &= Serializer.Transfer(archive, ref slot);

            return success;
        }

        public static bool SerializeFrom(Archive archive, InventoryLocation invLoc)
        {
            if (archive.IsUnpacking == false) return Logger.WarnReturn(false, "SerializeFrom(): archive.IsUnpacking == false");

            bool success = true;

            ulong containerId = invLoc.ContainerId;
            PrototypeId inventoryRef = invLoc.InventoryRef;
            uint slot = invLoc.Slot;

            success &= Serializer.Transfer(archive, ref containerId);
            success &= Serializer.TransferPrototypeEnum<InventoryPrototype>(archive, ref inventoryRef);
            success &= Serializer.Transfer(archive, ref slot);

            invLoc.Set(containerId, inventoryRef, slot);

            return success;
        }

        public Inventory GetInventory()
        {
            // NYI
            Logger.Warn("GetInventory(): Not yet implemented!");
            return null;
        }

        public void Set(ulong containerId, PrototypeId inventoryRef, uint slot)
        {
            ContainerId = containerId;
            InventoryPrototype = inventoryRef.As<InventoryPrototype>();
            Slot = slot;
        }

        public void Set(InventoryLocation other)
        {
            ContainerId = other.ContainerId;
            InventoryPrototype = other.InventoryPrototype;
            Slot = other.Slot;
        }

        public void Clear() => Set(0, PrototypeId.Invalid, Inventory.InvalidSlot);

        public override string ToString()
        {
            return $"{nameof(ContainerId)}={ContainerId}, {nameof(InventoryRef)}={GameDatabase.GetPrototypeName(InventoryRef)}, {nameof(Slot)}={Slot}";
        }

        public override bool Equals(object obj)
        {
            if (obj is not InventoryLocation other) return false;
            return Equals(other);
        }

        public bool Equals(InventoryLocation other)
        {
            return ContainerId.Equals(other.ContainerId)
                && InventoryPrototype.Equals(other.InventoryPrototype)
                && Slot.Equals(other.Slot);
        }
    }
}
