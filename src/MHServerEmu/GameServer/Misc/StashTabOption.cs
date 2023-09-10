using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Misc
{
    public class StashTabOption
    {
        public ulong PrototypeId { get; set; }
        public string Name { get; set; }
        public ulong AssetRef { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }

        public StashTabOption(CodedInputStream stream)
        {
            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);
            Name = stream.ReadRawString();
            AssetRef = stream.ReadRawVarint64();
            Field2 = stream.ReadRawInt32();
            Field3 = stream.ReadRawInt32();            
        }

        public StashTabOption(ulong prototypeId, string name, ulong assetRef, int field2, int field3)
        {
            PrototypeId = prototypeId;
            Name = name;
            AssetRef = assetRef;
            Field2 = field2;
            Field3 = field3;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WritePrototypeId(PrototypeId, PrototypeEnumType.All);
                stream.WriteRawString(Name);
                stream.WriteRawVarint64(AssetRef);
                stream.WriteRawInt32(Field2);
                stream.WriteRawInt32(Field3);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"AssetRef: 0x{AssetRef:X}");
            sb.AppendLine($"Field2: 0x{Field2}");
            sb.AppendLine($"Field3: 0x{Field3}");
            return sb.ToString();
        }
    }
}
