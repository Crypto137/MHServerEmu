using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Misc
{
    public class EquipmentInvUISlot
    {
        public ulong Index { get; set; }
        public ulong PrototypeId { get; set; }

        public EquipmentInvUISlot(CodedInputStream stream)
        {
            Index = stream.ReadRawVarint64();
            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);
        }

        public EquipmentInvUISlot(ulong index, ulong prototypeId)
        {
            Index = index;
            PrototypeId = prototypeId;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Index);
                stream.WritePrototypeId(PrototypeId, PrototypeEnumType.All);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Index: 0x{Index:X}");
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            return sb.ToString();
        }

    }
}
