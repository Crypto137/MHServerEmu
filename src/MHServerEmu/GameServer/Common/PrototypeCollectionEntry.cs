using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Common
{
    // Placeholder name until we figure out what this actually is
    public class PrototypeCollectionEntry
    {
        public ulong PrototypeId { get; set; }
        public uint Value { get; set; }

        public PrototypeCollectionEntry(CodedInputStream stream)
        {
            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);
            Value = stream.ReadRawVarint32();
        }

        public PrototypeCollectionEntry(ulong prototypeId, uint value)
        {
            PrototypeId = prototypeId;
            Value = value;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WritePrototypeId(PrototypeId, PrototypeEnumType.All);
                stream.WriteRawVarint32(Value);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            sb.AppendLine($"Value: 0x{Value:X}");
            return sb.ToString();
        }
    }
}
