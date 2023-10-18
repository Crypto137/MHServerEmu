using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Missions
{
    public class MissionManager
    {
        public ulong PrototypeId { get; set; }
        public Mission[] Missions { get; set; }
        public LegendaryMissionBlacklist[] LegendaryMissionBlacklists { get; set; }

        public MissionManager(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.All);

            Missions = new Mission[stream.ReadRawVarint64()];
            for (int i = 0; i < Missions.Length; i++)
                Missions[i] = new(stream, boolDecoder);

            LegendaryMissionBlacklists = new LegendaryMissionBlacklist[stream.ReadRawInt32()];
            for (int i = 0; i < LegendaryMissionBlacklists.Length; i++)
                LegendaryMissionBlacklists[i] = new(stream);
        }

        public MissionManager(ulong prototypeId, Mission[] missions, LegendaryMissionBlacklist[] legendaryMissionBlacklists)
        {
            PrototypeId = prototypeId;
            Missions = missions;
            LegendaryMissionBlacklists = legendaryMissionBlacklists;
        }

        public void EncodeBools(BoolEncoder boolEncoder)
        {
            foreach (Mission mission in Missions)
                boolEncoder.EncodeBool(mission.Suspended);
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WritePrototypeEnum(PrototypeId, PrototypeEnumType.All);

                cos.WriteRawVarint64((ulong)Missions.Length);
                foreach (Mission mission in Missions)
                    cos.WriteRawBytes(mission.Encode(boolEncoder));

                cos.WriteRawInt32(LegendaryMissionBlacklists.Length);
                foreach (LegendaryMissionBlacklist blacklist in LegendaryMissionBlacklists)
                    cos.WriteRawBytes(blacklist.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypeName(PrototypeId)}");
            for (int i = 0; i < Missions.Length; i++) sb.AppendLine($"Mission{i}: {Missions[i]}");
            for (int i = 0; i < LegendaryMissionBlacklists.Length; i++) sb.AppendLine($"LegendaryMissionBlacklist{i}: {LegendaryMissionBlacklists[i]}");
            return sb.ToString();
        }
    }
}
