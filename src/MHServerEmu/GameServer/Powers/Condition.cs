using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Powers
{
    public class Condition
    {
        private const int FlagCount = 16;

        public bool[] Flags { get; set; }   // mystery flags: 5, 8
        public ulong Id { get; set; }
        public ulong CreatorId { get; set; }
        public ulong UltimateCreatorId { get; set; }
        public ulong ConditionPrototypeId { get; set; }    // enum
        public ulong CreatorPowerPrototypeId { get; set; } // enum
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
            Flags = stream.ReadRawVarint32().ToBoolArray(FlagCount);
            Id = stream.ReadRawVarint64();
            if (Flags[0] == false) CreatorId = stream.ReadRawVarint64();
            if (Flags[1] == false) UltimateCreatorId = stream.ReadRawVarint64();
            if (Flags[2] == false) ConditionPrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Property);
            if (Flags[3] == false) CreatorPowerPrototypeId = stream.ReadPrototypeId(PrototypeEnumType.Property);
            if (Flags[4]) Index = stream.ReadRawVarint32();

            if (Flags[9])
            {
                AssetId = stream.ReadRawVarint64();     // MarvelPlayer_BlackCat
                StartTime = stream.ReadRawInt32();
            }

            if (Flags[6]) PauseTime = stream.ReadRawInt32();
            if (Flags[7]) TimeRemaining = stream.ReadRawInt32();
            if (Flags[10]) UpdateInterval = stream.ReadRawInt32();
            
            PropertyCollectionReplicationId = stream.ReadRawVarint32();
            Properties = new Property[stream.ReadRawUInt32()];
            for (int i = 0; i < Properties.Length; i++)
                Properties[i] = new(stream);

            if (Flags[11]) Field13 = stream.ReadRawVarint32();
        }

        public Condition()
        {            
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint32(Flags.ToUInt32());
                stream.WriteRawVarint64(Id);
                if (Flags[0] == false) stream.WriteRawVarint64(CreatorId);
                if (Flags[1] == false) stream.WriteRawVarint64(UltimateCreatorId);
                if (Flags[2] == false) stream.WritePrototypeId(ConditionPrototypeId, PrototypeEnumType.Property);
                if (Flags[3] == false) stream.WritePrototypeId(CreatorPowerPrototypeId, PrototypeEnumType.Property);
                if (Flags[4]) stream.WriteRawVarint64(Index);

                if (Flags[9])
                {
                    stream.WriteRawVarint64(AssetId);
                    stream.WriteRawInt32(StartTime);
                }

                if (Flags[6]) stream.WriteRawInt32(PauseTime);
                if (Flags[7]) stream.WriteRawInt32(TimeRemaining);
                if (Flags[10]) stream.WriteRawInt32(UpdateInterval);

                stream.WriteRawVarint32(PropertyCollectionReplicationId);
                stream.WriteRawUInt32((uint)Properties.Length);
                foreach (Property property in Properties) stream.WriteRawBytes(property.Encode());

                if (Flags[11]) stream.WriteRawVarint32(Field13);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                for (int i = 0; i < Flags.Length; i++) streamWriter.WriteLine($"Flag{i}: {Flags[i]}");
                streamWriter.WriteLine($"Id: 0x{Id.ToString("X")}");
                streamWriter.WriteLine($"CreatorId: 0x{CreatorId.ToString("X")}");
                streamWriter.WriteLine($"UltimateCreatorId: 0x{UltimateCreatorId.ToString("X")}");
                streamWriter.WriteLine($"ConditionPrototypeId: {GameDatabase.GetPrototypePath(ConditionPrototypeId)}");
                streamWriter.WriteLine($"CreatorPowerPrototypeId: {GameDatabase.GetPrototypePath(CreatorPowerPrototypeId)}");
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
