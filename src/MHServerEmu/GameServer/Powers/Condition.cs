using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;
using MHServerEmu.GameServer.Properties;

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
        public ReplicatedPropertyCollection PropertyCollection { get; set; }
        public uint Field13 { get; set; }

        public Condition(CodedInputStream stream)
        {
            Flags = stream.ReadRawVarint32().ToBoolArray(FlagCount);
            Id = stream.ReadRawVarint64();
            if (Flags[0] == false) CreatorId = stream.ReadRawVarint64();
            if (Flags[1] == false) UltimateCreatorId = stream.ReadRawVarint64();
            if (Flags[2] == false) ConditionPrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);
            if (Flags[3] == false) CreatorPowerPrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);
            if (Flags[4]) Index = stream.ReadRawVarint32();

            if (Flags[9])
            {
                AssetId = stream.ReadRawVarint64();     // MarvelPlayer_BlackCat
                StartTime = stream.ReadRawInt32();
            }

            if (Flags[6]) PauseTime = stream.ReadRawInt32();
            if (Flags[7]) TimeRemaining = stream.ReadRawInt32();
            if (Flags[10]) UpdateInterval = stream.ReadRawInt32();

            PropertyCollection = new(stream);

            if (Flags[11]) Field13 = stream.ReadRawVarint32();
        }

        public Condition()
        {            
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32(Flags.ToUInt32());
                cos.WriteRawVarint64(Id);
                if (Flags[0] == false) cos.WriteRawVarint64(CreatorId);
                if (Flags[1] == false) cos.WriteRawVarint64(UltimateCreatorId);
                if (Flags[2] == false) cos.WritePrototypeId(ConditionPrototypeId, PrototypeEnumType.All);
                if (Flags[3] == false) cos.WritePrototypeId(CreatorPowerPrototypeId, PrototypeEnumType.All);
                if (Flags[4]) cos.WriteRawVarint64(Index);

                if (Flags[9])
                {
                    cos.WriteRawVarint64(AssetId);
                    cos.WriteRawInt32(StartTime);
                }

                if (Flags[6]) cos.WriteRawInt32(PauseTime);
                if (Flags[7]) cos.WriteRawInt32(TimeRemaining);
                if (Flags[10]) cos.WriteRawInt32(UpdateInterval);
                cos.WriteRawBytes(PropertyCollection.Encode());
                if (Flags[11]) cos.WriteRawVarint32(Field13);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.Append("Flags: ");
            for (int i = 0; i < Flags.Length; i++) if (Flags[i]) sb.Append($"{i} ");
            sb.AppendLine();

            sb.AppendLine($"Id: 0x{Id:X}");
            sb.AppendLine($"CreatorId: 0x{CreatorId:X}");
            sb.AppendLine($"UltimateCreatorId: 0x{UltimateCreatorId:X}");
            sb.AppendLine($"ConditionPrototypeId: {GameDatabase.GetPrototypePath(ConditionPrototypeId)}");
            sb.AppendLine($"CreatorPowerPrototypeId: {GameDatabase.GetPrototypePath(CreatorPowerPrototypeId)}");
            sb.AppendLine($"Index: 0x{Index:X}");
            sb.AppendLine($"AssetId: 0x{AssetId:X}");
            sb.AppendLine($"StartTime: 0x{StartTime:X}");
            sb.AppendLine($"PauseTime: 0x{PauseTime:X}");
            sb.AppendLine($"TimeRemaining: 0x{TimeRemaining:X}");
            sb.AppendLine($"PropertyCollection: {PropertyCollection}");
            sb.AppendLine($"Field13: 0x{Field13:X}");

            return sb.ToString();
        }
    }
}
