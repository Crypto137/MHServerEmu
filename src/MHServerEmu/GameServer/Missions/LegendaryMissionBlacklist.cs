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

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(Category);
                cos.WriteRawVarint64((ulong)Missions.Length);
                foreach (ulong mission in Missions) cos.WriteRawVarint64(mission);

                cos.Flush();
                return ms.ToArray();
            }
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
