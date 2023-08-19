using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Powers
{
    public class Condition
    {
        public ulong Flags { get; set; }
        public ulong Id { get; set; }
        public ulong CreatorId { get; set; }
        public ulong UltimateCreatorId { get; set; }
        public ulong ConditionPrototypeRef { get; set; }
        public ulong CreatorPowerPrototypeRef { get; set; }
        public ulong UnknownSize { get; set; }
        public ulong AssetId { get; set; }
        public ulong StartTime { get; set; }    // zigzag
        public ulong PauseTime { get; set; }
        public ulong Field10 { get; set; }      // time3
        public ulong UpdateInterval { get; set; }   // zigzag int
        public ulong PropertyCollectionReplicationId { get; set; }
        public Property[] Properties { get; set; }
        public uint Field13 { get; set; }

        public Condition(CodedInputStream stream)
        {
            Flags = stream.ReadRawVarint64();
            Id = stream.ReadRawVarint64();
            if ((Flags & 0x1) == 0) CreatorId = stream.ReadRawVarint64();
            if ((Flags & 0x2) == 0) UltimateCreatorId = stream.ReadRawVarint64();
            if ((Flags & 0x4) == 0) ConditionPrototypeRef = stream.ReadRawVarint64();
            if ((Flags & 0x8) == 0) CreatorPowerPrototypeRef = stream.ReadRawVarint64();
            if ((Flags & 0x10) > 0) UnknownSize = stream.ReadRawVarint64();

            if ((Flags & 0x200) > 0)
            {
                AssetId = stream.ReadRawVarint64();     // MarvelPlayer_BlackCat
                StartTime = stream.ReadRawVarint64();   // zigzag
            }

            if ((Flags & 0x40) > 0) PauseTime = stream.ReadRawVarint64();
            if ((Flags & 0x80) > 0) Field10 = stream.ReadRawVarint64();
            if ((Flags & 0x400) > 0) UpdateInterval = stream.ReadRawVarint64();
            
            PropertyCollectionReplicationId = stream.ReadRawVarint64();
            Properties = new Property[stream.ReadRawUInt32()];
            for (int i = 0; i < Properties.Length; i++)
                Properties[i] = new(stream);

            if ((Flags & 0x800) > 0) Field13 = stream.ReadRawVarint32();
        }

        public Condition()
        {            
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Flags);
                stream.WriteRawVarint64(Id);
                if ((Flags & 0x1) == 0) stream.WriteRawVarint64(CreatorId);
                if ((Flags & 0x2) == 0) stream.WriteRawVarint64(UltimateCreatorId);
                if ((Flags & 0x4) == 0) stream.WriteRawVarint64(ConditionPrototypeRef);
                if ((Flags & 0x8) == 0) stream.WriteRawVarint64(CreatorPowerPrototypeRef);
                if ((Flags & 0x10) > 0) stream.WriteRawVarint64(UnknownSize);

                if ((Flags & 0x200) > 0)
                {
                    stream.WriteRawVarint64(AssetId);
                    stream.WriteRawVarint64(StartTime);
                }

                if ((Flags & 0x40) > 0) stream.WriteRawVarint64(PauseTime);
                if ((Flags & 0x80) > 0) stream.WriteRawVarint64(Field10);
                if ((Flags & 0x400) > 0) stream.WriteRawVarint64(UpdateInterval);

                stream.WriteRawVarint64(PropertyCollectionReplicationId);
                stream.WriteRawUInt32((uint)Properties.Length);
                foreach (Property property in Properties) stream.WriteRawBytes(property.Encode());

                if ((Flags & 0x800) > 0) stream.WriteRawVarint32(Field13);

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
                streamWriter.WriteLine($"Id: 0x{Id.ToString("X")}");
                streamWriter.WriteLine($"CreatorId: 0x{CreatorId.ToString("X")}");
                streamWriter.WriteLine($"UltimateCreatorId: 0x{UltimateCreatorId.ToString("X")}");
                streamWriter.WriteLine($"ConditionPrototypeRef: 0x{ConditionPrototypeRef.ToString("X")}");
                streamWriter.WriteLine($"CreatorPowerPrototypeRef: 0x{CreatorPowerPrototypeRef.ToString("X")}");
                streamWriter.WriteLine($"UnknownSize: 0x{UnknownSize.ToString("X")}");
                streamWriter.WriteLine($"AssetId: 0x{AssetId.ToString("X")}");
                streamWriter.WriteLine($"StartTime: 0x{StartTime.ToString("X")}");
                streamWriter.WriteLine($"PauseTime: 0x{PauseTime.ToString("X")}");
                streamWriter.WriteLine($"Field10: 0x{Field10.ToString("X")}");
                streamWriter.WriteLine($"PropertyCollectionReplicationId: 0x{PropertyCollectionReplicationId.ToString("X")}");
                for (int i = 0; i < Properties.Length; i++) streamWriter.WriteLine($"Property{i}: {Properties[i]}");
                streamWriter.WriteLine($"Field13: 0x{Field13.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
