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
        public ulong ConditionPrototypeId { get; set; }
        public ulong CreatorPowerPrototypeId { get; set; }
        public uint Index { get; set; }
        public ulong EngineAssetGuid { get; set; }
        public long StartTime { get; set; }
        public long PauseTime { get; set; }
        public long Duration { get; set; }  // 7200000 == 2 hours
        public int UpdateInterval { get; set; }
        public ReplicatedPropertyCollection PropertyCollection { get; set; }
        public uint CancelOnFlags { get; set; }

        public Condition(CodedInputStream stream)
        {
            Flags = stream.ReadRawVarint32().ToBoolArray(FlagCount);
            Id = stream.ReadRawVarint64();
            if (Flags[0] == false) CreatorId = stream.ReadRawVarint64();
            if (Flags[1] == false) UltimateCreatorId = stream.ReadRawVarint64();
            if (Flags[2] == false) ConditionPrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            if (Flags[3] == false) CreatorPowerPrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            if (Flags[4]) Index = stream.ReadRawVarint32();

            if (Flags[9]) EngineAssetGuid = stream.ReadRawVarint64();     // MarvelPlayer_BlackCat

            StartTime = stream.ReadRawInt64();
            if (Flags[6]) PauseTime = stream.ReadRawInt64();
            if (Flags[7]) Duration = stream.ReadRawInt64();
            if (Flags[10]) UpdateInterval = stream.ReadRawInt32();

            PropertyCollection = new(stream);

            if (Flags[11]) CancelOnFlags = stream.ReadRawVarint32();
        }

        public Condition() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint32(Flags.ToUInt32());
                cos.WriteRawVarint64(Id);
                if (Flags[0] == false) cos.WriteRawVarint64(CreatorId);
                if (Flags[1] == false) cos.WriteRawVarint64(UltimateCreatorId);
                if (Flags[2] == false) cos.WritePrototypeEnum(ConditionPrototypeId, PrototypeEnumType.All);
                if (Flags[3] == false) cos.WritePrototypeEnum(CreatorPowerPrototypeId, PrototypeEnumType.All);
                if (Flags[4]) cos.WriteRawVarint64(Index);
                if (Flags[9]) cos.WriteRawVarint64(EngineAssetGuid);
                cos.WriteRawInt64(StartTime);
                if (Flags[6]) cos.WriteRawInt64(PauseTime);
                if (Flags[7]) cos.WriteRawInt64(Duration);
                if (Flags[10]) cos.WriteRawInt32(UpdateInterval);
                PropertyCollection.Encode(cos);
                if (Flags[11]) cos.WriteRawVarint32(CancelOnFlags);

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

            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"CreatorId: {CreatorId}");
            sb.AppendLine($"UltimateCreatorId: {UltimateCreatorId}");
            sb.AppendLine($"ConditionPrototypeId: {GameDatabase.GetPrototypeName(ConditionPrototypeId)}");
            sb.AppendLine($"CreatorPowerPrototypeId: {GameDatabase.GetPrototypeName(CreatorPowerPrototypeId)}");
            sb.AppendLine($"Index: 0x{Index:X}");
            sb.AppendLine($"EngineAssetGuid: {EngineAssetGuid}");
            sb.AppendLine($"StartTime: {StartTime}");
            sb.AppendLine($"PauseTime: {PauseTime}");
            sb.AppendLine($"Duration: {Duration}");
            sb.AppendLine($"PropertyCollection: {PropertyCollection}");
            sb.AppendLine($"CancelOnFlags: 0x{CancelOnFlags:X}");

            return sb.ToString();
        }
    }
}
