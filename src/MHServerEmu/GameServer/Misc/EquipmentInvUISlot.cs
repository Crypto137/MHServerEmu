using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Misc
{
    public class EquipmentInvUISlot
    {
        public ulong Index { get; set; }
        public ulong PrototypeEnum { get; set; }

        public EquipmentInvUISlot(CodedInputStream stream)
        {
            Index = stream.ReadRawVarint64();
            PrototypeEnum = stream.ReadRawVarint64();
        }

        public EquipmentInvUISlot(ulong index, ulong prototypeEnum)
        {
            Index = index;
            PrototypeEnum = prototypeEnum;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Index);
                stream.WriteRawVarint64(PrototypeEnum);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Index: 0x{Index.ToString("X")}");
                streamWriter.WriteLine($"PrototypeEnum: 0x{PrototypeEnum.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

    }
}
