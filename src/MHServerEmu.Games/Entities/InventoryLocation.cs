using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities
{
    public class InventoryLocation
    {
        public ulong ContainerId { get; set; }
        public PrototypeId Inventory { get; set; }
        public uint Slot { get; set; }

        public InventoryLocation(CodedInputStream stream)
        {
            ContainerId = stream.ReadRawVarint64();
            Inventory = stream.ReadPrototypeRef<InventoryPrototype>();
            Slot = stream.ReadRawVarint32();
        }

        public InventoryLocation(ulong containerEntityId, PrototypeId inventoryPrototypeId, uint slot)
        {
            ContainerId = containerEntityId;
            Inventory = inventoryPrototypeId;
            Slot = slot;
        }

        public InventoryLocation()
        {
            ContainerId = 0;
            Inventory = PrototypeId.Invalid;
            Slot = 0xFFFFFFFF; // -1
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ContainerId);
            stream.WritePrototypeRef<InventoryPrototype>(Inventory);
            stream.WriteRawVarint64(Slot);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ContainerEntityId: {ContainerId}");
            sb.AppendLine($"InventoryPrototypeId: {GameDatabase.GetPrototypeName(Inventory)}");
            sb.AppendLine($"Slot: {Slot}");
            return sb.ToString();
        }
    }
}
