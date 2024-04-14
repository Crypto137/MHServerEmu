using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities
{
    public class InventoryLocation
    {
        public ulong ContainerId { get; set; }          // invLocContainerEntityId
        public PrototypeId InventoryRef { get; set; }   // invLocInventoryPrototypeId
        public uint Slot { get; set; }

        public InventoryLocation(CodedInputStream stream)
        {
            ContainerId = stream.ReadRawVarint64();
            InventoryRef = stream.ReadPrototypeRef<InventoryPrototype>();
            Slot = stream.ReadRawVarint32();
        }

        public InventoryLocation(ulong containerId, PrototypeId inventoryRef, uint slot)
        {
            ContainerId = containerId;
            InventoryRef = inventoryRef;
            Slot = slot;
        }

        public InventoryLocation()
        {
            ContainerId = 0;
            InventoryRef = PrototypeId.Invalid;
            Slot = 0xFFFFFFFF; // -1
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ContainerId);
            stream.WritePrototypeRef<InventoryPrototype>(InventoryRef);
            stream.WriteRawVarint64(Slot);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(ContainerId)}: {ContainerId}");
            sb.AppendLine($"{nameof(InventoryRef)}: {GameDatabase.GetPrototypeName(InventoryRef)}");
            sb.AppendLine($"{nameof(Slot)}: {Slot}");
            return sb.ToString();
        }
    }
}
