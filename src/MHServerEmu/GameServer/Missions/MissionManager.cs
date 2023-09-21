using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.GameServer.Missions
{
    public class MissionManager
    {
        public ulong PrototypeId { get; set; }
        public Mission[] Missions { get; set; }
        public LegendaryMissionBlacklists[] LegendaryMissionBlacklists { get; set; }

        public MissionManager(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);

            Missions = new Mission[stream.ReadRawVarint64()];
            for (int i = 0; i < Missions.Length; i++)
                Missions[i] = new(stream, boolDecoder);
            LegendaryMissionBlacklists = new LegendaryMissionBlacklists[stream.ReadRawInt32()];
            for (int i = 0; i < LegendaryMissionBlacklists.Length; i++)
                LegendaryMissionBlacklists[i] = new(stream);
        }

        public MissionManager(ulong prototypeId, Mission[] missions, LegendaryMissionBlacklists[] legendaryMissionBlacklists)
        {
            PrototypeId = prototypeId;
            Missions = missions;
            LegendaryMissionBlacklists = legendaryMissionBlacklists;
        }

        public void EncodeBool(BoolEncoder boolEncoder)
        {
            foreach (Mission mission in Missions)
                boolEncoder.WriteBool(mission.Suspended);
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WritePrototypeId(PrototypeId, PrototypeEnumType.All);

                cos.WriteRawVarint64((ulong)Missions.Length);
                foreach (Mission mission in Missions)
                    cos.WriteRawBytes(mission.Encode(boolEncoder));

                cos.WriteRawInt32(LegendaryMissionBlacklists.Length);
                foreach (LegendaryMissionBlacklists quest in LegendaryMissionBlacklists)
                    cos.WriteRawBytes(quest.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            for (int i = 0; i < Missions.Length; i++) 
                sb.AppendLine($"Mission{i}: {Missions[i]}");
            for (int i = 0; i < LegendaryMissionBlacklists.Length; i++) 
                sb.AppendLine($"LegendaryMissionBlacklists{i}: {LegendaryMissionBlacklists[i]}");

            return sb.ToString();
        }
    }
}
