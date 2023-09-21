using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Missions
{
    public class LegendaryMissionBlacklists
    {
        public ulong Category { get; set; }
        public ulong[] Blacklist { get; set; }

        public LegendaryMissionBlacklists(CodedInputStream stream)
        {
            Category = stream.ReadRawVarint64();

            Blacklist = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Blacklist.Length; i++)
            {
                Blacklist[i] = stream.ReadRawVarint64();
            }
        }

        public LegendaryMissionBlacklists(ulong prototypeGuid, ulong[] blacklist)
        {
            Category = prototypeGuid;
            Blacklist = blacklist;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(Category);
                cos.WriteRawVarint64((ulong)Blacklist.Length);
                foreach (ulong blacklist in Blacklist) cos.WriteRawVarint64(blacklist);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Category: {GameDatabase.GetPrototypePath(GameDatabase.GetPrototypeId(Category))}");
            for (int i = 0; i < Blacklist.Length; i++) sb.AppendLine($"LegendaryMission{i}: {GameDatabase.GetPrototypePath(GameDatabase.GetPrototypeId(Blacklist[i]))}");
            return sb.ToString();
        }
    }
}
