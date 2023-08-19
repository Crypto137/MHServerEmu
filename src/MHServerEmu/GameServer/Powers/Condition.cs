using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Powers
{
    public class Condition
    {
        public uint FieldFlags { get; set; }
        public ulong Id { get; set; }
        public ulong CreatorId { get; set; }
        public ulong UltimateCreatorId { get; set; }
        public ulong ConditionPrototypeRef { get; set; }    // enum
        public ulong CreatorPowerPrototypeRef { get; set; } // enum
        public uint Index { get; set; }
        public ulong AssetId { get; set; }
        public int StartTime { get; set; }
        public int PauseTime { get; set; }
        public int TimeRemaining { get; set; }  // 7200000 == 2 hours
        public int UpdateInterval { get; set; }
        public uint PropertyCollectionReplicationId { get; set; }
        public Property[] Properties { get; set; }
        public uint Field13 { get; set; }

        public Condition(CodedInputStream stream)
        {
            FieldFlags = stream.ReadRawVarint32();
            Id = stream.ReadRawVarint64();
            if ((FieldFlags & 0x1) == 0) CreatorId = stream.ReadRawVarint64();
            if ((FieldFlags & 0x2) == 0) UltimateCreatorId = stream.ReadRawVarint64();
            if ((FieldFlags & 0x4) == 0) ConditionPrototypeRef = stream.ReadRawVarint64();
            if ((FieldFlags & 0x8) == 0) CreatorPowerPrototypeRef = stream.ReadRawVarint64();
            if ((FieldFlags & 0x10) > 0) Index = stream.ReadRawVarint32();

            if ((FieldFlags & 0x200) > 0)
            {
                AssetId = stream.ReadRawVarint64();     // MarvelPlayer_BlackCat
                StartTime = stream.ReadRawInt32();
            }

            if ((FieldFlags & 0x40) > 0) PauseTime = stream.ReadRawInt32();
            if ((FieldFlags & 0x80) > 0) TimeRemaining = stream.ReadRawInt32();
            if ((FieldFlags & 0x400) > 0) UpdateInterval = stream.ReadRawInt32();
            
            PropertyCollectionReplicationId = stream.ReadRawVarint32();
            Properties = new Property[stream.ReadRawUInt32()];
            for (int i = 0; i < Properties.Length; i++)
                Properties[i] = new(stream);

            if ((FieldFlags & 0x800) > 0) Field13 = stream.ReadRawVarint32();
        }

        public Condition()
        {            
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(FieldFlags);
                stream.WriteRawVarint64(Id);
                if ((FieldFlags & 0x1) == 0) stream.WriteRawVarint64(CreatorId);
                if ((FieldFlags & 0x2) == 0) stream.WriteRawVarint64(UltimateCreatorId);
                if ((FieldFlags & 0x4) == 0) stream.WriteRawVarint64(ConditionPrototypeRef);
                if ((FieldFlags & 0x8) == 0) stream.WriteRawVarint64(CreatorPowerPrototypeRef);
                if ((FieldFlags & 0x10) > 0) stream.WriteRawVarint64(Index);

                if ((FieldFlags & 0x200) > 0)
                {
                    stream.WriteRawVarint64(AssetId);
                    stream.WriteRawInt32(StartTime);
                }

                if ((FieldFlags & 0x40) > 0) stream.WriteRawInt32(PauseTime);
                if ((FieldFlags & 0x80) > 0) stream.WriteRawInt32(TimeRemaining);
                if ((FieldFlags & 0x400) > 0) stream.WriteRawInt32(UpdateInterval);

                stream.WriteRawVarint32(PropertyCollectionReplicationId);
                stream.WriteRawUInt32((uint)Properties.Length);
                foreach (Property property in Properties) stream.WriteRawBytes(property.Encode());

                if ((FieldFlags & 0x800) > 0) stream.WriteRawVarint32(Field13);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"FieldFlags: 0x{FieldFlags.ToString("X")}");
                streamWriter.WriteLine($"Id: 0x{Id.ToString("X")}");
                streamWriter.WriteLine($"CreatorId: 0x{CreatorId.ToString("X")}");
                streamWriter.WriteLine($"UltimateCreatorId: 0x{UltimateCreatorId.ToString("X")}");
                streamWriter.WriteLine($"ConditionPrototypeRef: 0x{ConditionPrototypeRef.ToString("X")}");
                streamWriter.WriteLine($"CreatorPowerPrototypeRef: 0x{CreatorPowerPrototypeRef.ToString("X")}");
                streamWriter.WriteLine($"Index: 0x{Index.ToString("X")}");
                streamWriter.WriteLine($"AssetId: 0x{AssetId.ToString("X")}");
                streamWriter.WriteLine($"StartTime: 0x{StartTime.ToString("X")}");
                streamWriter.WriteLine($"PauseTime: 0x{PauseTime.ToString("X")}");
                streamWriter.WriteLine($"TimeRemaining: 0x{TimeRemaining.ToString("X")}");
                streamWriter.WriteLine($"PropertyCollectionReplicationId: 0x{PropertyCollectionReplicationId.ToString("X")}");
                for (int i = 0; i < Properties.Length; i++) streamWriter.WriteLine($"Property{i}: {Properties[i]}");
                streamWriter.WriteLine($"Field13: 0x{Field13.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
