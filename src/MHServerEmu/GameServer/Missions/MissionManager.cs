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
        public Quest[] Quests { get; set; }

        public MissionManager(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);

            Missions = new Mission[stream.ReadRawVarint64()];
            for (int i = 0; i < Missions.Length; i++)
                Missions[i] = new(stream, boolDecoder);
            Quests = new Quest[stream.ReadRawInt32()];
            for (int i = 0; i < Quests.Length; i++)
                Quests[i] = new(stream);
        }

        public MissionManager(ulong prototypeId, Mission[] missions, Quest[] quests)
        {
            PrototypeId = prototypeId;
            Missions = missions;
            Quests = quests;
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

                cos.WriteRawInt32(Quests.Length);
                foreach (Quest quest in Quests)
                    cos.WriteRawBytes(quest.Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            for (int i = 0; i < Missions.Length; i++) sb.AppendLine($"Mission{i}: {Missions[i]}");
            for (int i = 0; i < Quests.Length; i++) sb.AppendLine($"Quest{i}: {Quests[i]}");

            return sb.ToString();
        }
    }
}
