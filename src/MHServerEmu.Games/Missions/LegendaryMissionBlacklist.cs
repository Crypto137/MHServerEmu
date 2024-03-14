using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Missions
{
    public class LegendaryMissionBlacklist
    {
        public PrototypeGuid[] Missions { get; set; }

        public LegendaryMissionBlacklist(CodedInputStream stream)
        {           
            Missions = new PrototypeGuid[stream.ReadRawVarint64()];
            for (int i = 0; i < Missions.Length; i++)
                Missions[i] = (PrototypeGuid)stream.ReadRawVarint64();
        }

        public LegendaryMissionBlacklist(PrototypeGuid[] missions)
        {
            Missions = missions;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64((ulong)Missions.Length);
            foreach (ulong mission in Missions) stream.WriteRawVarint64(mission);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < Missions.Length; i++) sb.AppendLine($"Mission{i}: {GameDatabase.GetPrototypeNameByGuid(Missions[i])}");
            return sb.ToString();
        }
    }
}
