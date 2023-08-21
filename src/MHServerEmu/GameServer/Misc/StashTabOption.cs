using Google.ProtocolBuffers;
using System.Text;
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
            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Property);
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

                stream.WritePrototypeId(PrototypeId, PrototypeEnumType.Property);
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
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
                streamWriter.WriteLine($"Name: {Name}");
                streamWriter.WriteLine($"AssetRef: 0x{AssetRef.ToString("X")}");
                streamWriter.WriteLine($"Field2: 0x{Field2}");
                streamWriter.WriteLine($"Field3: 0x{Field3}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
