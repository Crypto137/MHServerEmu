using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Missions
{
    public class LegendaryMissionBlacklist
    {
        public ulong Category { get; set; }         // Prototype GUID
        public ulong[] Missions { get; set; }       // Prototype GUIDs

        public LegendaryMissionBlacklist(CodedInputStream stream)
        {
            Category = stream.ReadRawVarint64();

            Missions = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Missions.Length; i++)
                Missions[i] = stream.ReadRawVarint64();
        }

        public LegendaryMissionBlacklist(ulong category, ulong[] missions)
        {
            Category = category;
            Missions = missions;
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(Category);
            stream.WriteRawVarint64((ulong)Missions.Length);
            foreach (ulong mission in Missions) stream.WriteRawVarint64(mission);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Category: {GameDatabase.GetPrototypeName(GameDatabase.GetDataRefByPrototypeGuid(Category))}");
            for (int i = 0; i < Missions.Length; i++) sb.AppendLine($"Mission{i}: {GameDatabase.GetPrototypeName(GameDatabase.GetDataRefByPrototypeGuid(Missions[i]))}");
            return sb.ToString();
        }
    }
}
