using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class Condition
    {
        public ulong Flags { get; set; }
        public ulong Field1 { get; set; }
        public ulong Field2 { get; set; }
        public ulong Field3 { get; set; }
        public ulong Prototype4 { get; set; }
        public ulong Prototype8 { get; set; }
        public ulong UnknownSize { get; set; }
        public ulong AssetId { get; set; }
        public ulong Time1 { get; set; }    // zigzag
        public ulong Time2 { get; set; }
        public ulong Time3 { get; set; }
        public ulong UnknownInt { get; set; }   // zigzag?
        public ulong PropertyCollectionReplicationId { get; set; }
        public Property[] Properties { get; set; }
        public uint Field13 { get; set; }

        public Condition(CodedInputStream stream)
        {
            Flags = stream.ReadRawVarint64();
            Field1 = stream.ReadRawVarint64();
            if ((Flags & 0x1) == 0) Field2 = stream.ReadRawVarint64();
            if ((Flags & 0x2) == 0) Field3 = stream.ReadRawVarint64();
            if ((Flags & 0x4) == 0) Prototype4 = stream.ReadRawVarint64();
            if ((Flags & 0x8) == 0) Prototype8 = stream.ReadRawVarint64();
            if ((Flags & 0x10) > 0) UnknownSize = stream.ReadRawVarint64();

            if ((Flags & 0x200) > 0)
            {
                AssetId = stream.ReadRawVarint64(); // MarvelPlayer_BlackCat
                Time1 = stream.ReadRawVarint64();   // zigzag
            }

            if ((Flags & 0x40) > 0) Time2 = stream.ReadRawVarint64();
            if ((Flags & 0x80) > 0) Time3 = stream.ReadRawVarint64();
            if ((Flags & 0x400) > 0) UnknownInt = stream.ReadRawVarint64();

            Console.WriteLine((Flags & 0x400).ToString());
            
            PropertyCollectionReplicationId = stream.ReadRawVarint64();
            Properties = new Property[stream.ReadRawUInt32()];
            for (int i = 0; i < Properties.Length; i++)
                Properties[i] = new(stream);

            if ((Flags & 0x800) > 0) Field13 = stream.ReadRawVarint32();
        }

        public Condition(ulong prototypeId, uint value)
        {
            
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                //stream.WriteRawVarint64(PrototypeId);
                //stream.WriteRawVarint64(Value);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Flags: 0x{Flags.ToString("X")}");
                streamWriter.WriteLine($"Field1: 0x{Field1.ToString("X")}");
                streamWriter.WriteLine($"Field2: 0x{Field2.ToString("X")}");
                streamWriter.WriteLine($"Field3: 0x{Field3.ToString("X")}");
                streamWriter.WriteLine($"Prototype4: 0x{Prototype4.ToString("X")}");
                streamWriter.WriteLine($"Prototype8: 0x{Prototype8.ToString("X")}");
                streamWriter.WriteLine($"UnknownSize: 0x{UnknownSize.ToString("X")}");
                streamWriter.WriteLine($"AssetId: 0x{AssetId.ToString("X")}");
                streamWriter.WriteLine($"Time1: 0x{Time1.ToString("X")}");
                streamWriter.WriteLine($"Time2: 0x{Time2.ToString("X")}");
                streamWriter.WriteLine($"Time3: 0x{Time3.ToString("X")}");
                streamWriter.WriteLine($"PropertyCollectionReplicationId: 0x{PropertyCollectionReplicationId.ToString("X")}");
                for (int i = 0; i < Properties.Length; i++) streamWriter.WriteLine($"Property{i}: {Properties[i]}");
                streamWriter.WriteLine($"Field13: 0x{Field13.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
