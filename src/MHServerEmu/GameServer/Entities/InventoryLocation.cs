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
            InventoryPrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Inventory);
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
                cos.WritePrototypeId(InventoryPrototypeId, PrototypeEnumType.Inventory);
                cos.WriteRawVarint64(Slot);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ContainerEntityId: 0x{ContainerEntityId:X}");
            sb.AppendLine($"InventoryPrototypeId: {GameDatabase.GetPrototypePath(InventoryPrototypeId)}");
            sb.AppendLine($"Slot: 0x{Slot:X}");
            return sb.ToString();
        }
    }
}
