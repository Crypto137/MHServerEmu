using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities.Inventories
{
    /// <summary>
    /// Represents the location of an <see cref="Entity"/> in an <see cref="Inventory"/>.
    /// </summary>
    public class InventoryLocation : IEquatable<InventoryLocation>
    {
        // NOTE/TODO: This would not be client-accurate, but maybe we can potentially refactor this as a readonly struct for optimization.

        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly InventoryLocation Invalid = new();

        public ulong ContainerId { get; private set; } = Entity.InvalidId;     // Entity id
        public InventoryPrototype InventoryPrototype { get; private set; } = null;
        public uint Slot { get; private set; } = Inventory.InvalidSlot;

        public PrototypeId InventoryRef { get => InventoryPrototype != null ? InventoryPrototype.DataRef : PrototypeId.Invalid; }
        public InventoryCategory InventoryCategory { get => InventoryPrototype != null ? InventoryPrototype.Category : InventoryCategory.None; }
        public InventoryConvenienceLabel InventoryConvenienceLabel { get => InventoryPrototype != null ? InventoryPrototype.ConvenienceLabel : InventoryConvenienceLabel.None; }

        public bool IsValid { get => ContainerId != Entity.InvalidId && InventoryRef != PrototypeId.Invalid && Slot != Inventory.InvalidSlot; }

        public bool IsArtifactInventory { get => InventoryPrototype?.IsArtifactInventory == true; }

        /// <summary>
        /// Constructs a default <see cref="InventoryLocation"/>.
        /// </summary>
        public InventoryLocation() { }

        /// <summary>
        /// Constructs an <see cref="InventoryLocation"/> with the specified parameters. 
        /// </summary>
        public InventoryLocation(ulong containerId, PrototypeId inventoryRef, uint slot = Inventory.InvalidSlot)
        {
            ContainerId = containerId;
            InventoryPrototype = inventoryRef.As<InventoryPrototype>();
            Slot = slot;
        }

        /// <summary>
        /// Constructs a copy of the provided <see cref="InventoryLocation"/>.
        /// </summary>
        public InventoryLocation(InventoryLocation other)
        {
            Set(other);
        }

        /// <summary>
        /// Serializes the provided <see cref="InventoryLocation"/> to an <see cref="Archive"/>.
        /// </summary>
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

        /// <summary>
        /// Deserializes an <see cref="InventoryLocation"/> from the provided <see cref="Archive"/>.
        /// </summary>
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

        /// <summary>
        /// Returns the <see cref="Inventory"/> this <see cref="InventoryLocation"/> refers to.
        /// Returns <see langword="null"/> if this location is invalid (i.e. entity is not in an inventory).
        /// </summary>
        public Inventory GetInventory()
        {
            if (IsValid == false) return null;

            Entity container = Game.Current.EntityManager.GetEntity<Entity>(ContainerId);
            if (container == null) return null;
            
            return container.GetInventoryByRef(InventoryRef);
        }

        /// <summary>
        /// Updates this <see cref="InventoryLocation"/> with the provided parameters.
        /// </summary>
        public void Set(ulong containerId, PrototypeId inventoryRef, uint slot)
        {
            ContainerId = containerId;
            InventoryPrototype = inventoryRef.As<InventoryPrototype>();
            Slot = slot;
        }

        /// <summary>
        /// Copies all parameters from the provided <see cref="InventoryLocation"/>.
        /// </summary>
        public void Set(InventoryLocation other)
        {
            ContainerId = other.ContainerId;
            InventoryPrototype = other.InventoryPrototype;
            Slot = other.Slot;
        }

        /// <summary>
        /// Resets this <see cref="InventoryLocation"/> to default parameters.
        /// </summary>
        public void Clear()
        {
            Set(Entity.InvalidId, PrototypeId.Invalid, Inventory.InvalidSlot);
        }

        public override string ToString()
        {
            return $"{nameof(ContainerId)}={ContainerId}, {nameof(InventoryRef)}={GameDatabase.GetPrototypeName(InventoryRef)}, {nameof(Slot)}={Slot}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ContainerId, InventoryPrototype, Slot);
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
