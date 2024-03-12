using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Entities
{
    public class InventoryLocation
    {
        public ulong ContainerEntityId { get; set; }
        public PrototypeId InventoryPrototypeId { get; set; }
        public uint Slot { get; set; }

        public InventoryLocation(CodedInputStream stream)
        {
            ContainerEntityId = stream.ReadRawVarint64();
            InventoryPrototypeId = stream.ReadPrototypeRef<InventoryPrototype>();
            Slot = stream.ReadRawVarint32();
        }

        public InventoryLocation(ulong containerEntityId, PrototypeId inventoryPrototypeId, uint slot)
        {
            ContainerEntityId = containerEntityId;
            InventoryPrototypeId = inventoryPrototypeId;
            Slot = slot;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ContainerEntityId);
            stream.WritePrototypeRef<InventoryPrototype>(InventoryPrototypeId);
            stream.WriteRawVarint64(Slot);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ContainerEntityId: {ContainerEntityId}");
            sb.AppendLine($"InventoryPrototypeId: {GameDatabase.GetPrototypeName(InventoryPrototypeId)}");
            sb.AppendLine($"Slot: {Slot}");
            return sb.ToString();
        }
    }
}
