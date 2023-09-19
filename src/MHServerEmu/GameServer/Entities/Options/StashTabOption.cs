using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Entities.Options
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
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WritePrototypeId(PrototypeId, PrototypeEnumType.All);
                cos.WriteRawString(Name);
                cos.WriteRawVarint64(AssetRef);
                cos.WriteRawInt32(Field2);
                cos.WriteRawInt32(Field3);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            sb.AppendLine($"Name: {Name}");
            sb.AppendLine($"AssetRef: {AssetRef}");
            sb.AppendLine($"Field2: 0x{Field2}");
            sb.AppendLine($"Field3: 0x{Field3}");
            return sb.ToString();
        }
    }
}
