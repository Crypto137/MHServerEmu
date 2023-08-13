using Google.ProtocolBuffers;
using System.Text;
using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Misc
{
    public class StashTabOption
    {
        public ulong PrototypeEnum { get; set; }
        public string Name { get; set; }
        public ulong AssetRef { get; set; }
        public int Field2 { get; set; }
        public int Field3 { get; set; }

        public StashTabOption(CodedInputStream stream)
        {
            PrototypeEnum = stream.ReadRawVarint64();
            Name = stream.ReadRawString();
            AssetRef = stream.ReadRawVarint64();
            Field2 = stream.ReadRawInt32();
            Field3 = stream.ReadRawInt32();            
        }

        public StashTabOption(ulong prototypeEnum, string name, ulong assetRef, int field2, int field3)
        {
            PrototypeEnum = prototypeEnum;
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

                stream.WriteRawVarint64(PrototypeEnum);
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
                streamWriter.WriteLine($"PrototypeEnum: 0x{PrototypeEnum.ToString("X")}");
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
