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
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(ContainerEntityId);
                stream.WritePrototypeId(InventoryPrototypeId, PrototypeEnumType.Inventory);
                stream.WriteRawVarint64(Slot);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"ContainerEntityId: 0x{ContainerEntityId.ToString("X")}");
                streamWriter.WriteLine($"InventoryPrototypeId: {GameDatabase.GetPrototypePath(InventoryPrototypeId)}");
                streamWriter.WriteLine($"Slot: 0x{Slot.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
