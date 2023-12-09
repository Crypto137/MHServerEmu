using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Missions
{
    public class MissionManager
    {
        public PrototypeId PrototypeId { get; set; }
        public Mission[] Missions { get; set; }
        public LegendaryMissionBlacklist[] LegendaryMissionBlacklists { get; set; }

        public MissionManager(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeId = stream.ReadPrototypeEnum<Prototype>();

            Missions = new Mission[stream.ReadRawVarint64()];
            for (int i = 0; i < Missions.Length; i++)
                Missions[i] = new(stream, boolDecoder);

            LegendaryMissionBlacklists = new LegendaryMissionBlacklist[stream.ReadRawInt32()];
            for (int i = 0; i < LegendaryMissionBlacklists.Length; i++)
                LegendaryMissionBlacklists[i] = new(stream);
        }

        public MissionManager(PrototypeId prototypeId, Mission[] missions, LegendaryMissionBlacklist[] legendaryMissionBlacklists)
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

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WritePrototypeEnum<Prototype>(PrototypeId);

            stream.WriteRawVarint64((ulong)Missions.Length);
            foreach (Mission mission in Missions) mission.Encode(stream, boolEncoder);

            stream.WriteRawInt32(LegendaryMissionBlacklists.Length);
            foreach (LegendaryMissionBlacklist blacklist in LegendaryMissionBlacklists) blacklist.Encode(stream);
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
