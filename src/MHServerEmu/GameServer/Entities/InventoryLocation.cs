using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Entities
{
    public class InventoryLocation
    {
        public ulong ContainerEntityId { get; set; }
        public ulong InventoryPrototypeId { get; set; }
        public uint Slot { get; set; }

        public InventoryLocation(CodedInputStream stream)
        {
            ContainerEntityId = stream.ReadRawVarint64();
            InventoryPrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.Inventory);
            Slot = stream.ReadRawVarint32();
        }

        public InventoryLocation(ulong containerEntityId, ulong inventoryPrototypeId, uint slot)
        {
            ContainerEntityId = containerEntityId;
            InventoryPrototypeId = inventoryPrototypeId;
            Slot = slot;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(ContainerEntityId);
                cos.WritePrototypeEnum(InventoryPrototypeId, PrototypeEnumType.Inventory);
                cos.WriteRawVarint64(Slot);

                cos.Flush();
                return ms.ToArray();
            }
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
