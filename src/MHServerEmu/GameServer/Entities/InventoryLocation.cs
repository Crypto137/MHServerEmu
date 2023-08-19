using System.Text;
using Google.ProtocolBuffers;

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
            InventoryPrototypeId = stream.ReadRawVarint64();
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
                stream.WriteRawVarint64(InventoryPrototypeId);
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
                streamWriter.WriteLine($"InventoryPrototypeId: 0x{InventoryPrototypeId.ToString("X")}");
                streamWriter.WriteLine($"Slot: 0x{Slot.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
