using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Missions
{
    public class Mission
    {
        public ulong PrototypeGuid { get; set; }
        public ulong State { get; set; }
        public ulong TimeExpireCurrentState { get; set; }
        public ulong PrototypeId { get; set; }
        public int Random { get; set; }
        public Objective[] Objectives { get; set; }
        public ulong[] Participants { get; set; }
        public bool Suspended { get; set; }

        public Mission(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeGuid = stream.ReadRawVarint64();
            State = stream.ReadRawVarint64();
            TimeExpireCurrentState = stream.ReadRawVarint64();
            PrototypeId = stream.ReadPrototypeId(PrototypeEnumType.All);
            Random = stream.ReadRawInt32();

            Objectives = new Objective[stream.ReadRawVarint64()];
            for (int i = 0; i < Objectives.Length; i++)
                Objectives[i] = new(stream);

            Participants = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Participants.Length; i++)
                Participants[i] = stream.ReadRawVarint64();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            Suspended = boolDecoder.ReadBool();
        }

        public Mission(ulong prototypeGuid, ulong state, ulong timeExpireCurrentState, ulong prototypeId,
            Objective[] objectives, ulong[] participants, bool suspended)
        {
            PrototypeGuid = prototypeGuid;
            State = state;
            TimeExpireCurrentState = timeExpireCurrentState;
            PrototypeId = prototypeId;
            Objectives = objectives;
            Participants = participants;
            Suspended = suspended;
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(PrototypeGuid);
                cos.WriteRawVarint64(State);
                cos.WriteRawVarint64(TimeExpireCurrentState);
                cos.WritePrototypeId(PrototypeId, PrototypeEnumType.All);
                cos.WriteRawInt32(Random);

                cos.WriteRawVarint64((ulong)Objectives.Length);
                foreach (Objective objective in Objectives)
                    cos.WriteRawBytes(objective.Encode());

                cos.WriteRawVarint64((ulong)Participants.Length);
                foreach (ulong Participant in Participants)
                    cos.WriteRawVarint64(Participant);

                boolEncoder.WriteBuffer(cos);   // Suspended   

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeGuid: {GameDatabase.GetPrototypePath(GameDatabase.GetPrototypeId(PrototypeGuid))}");
            sb.AppendLine($"State: 0x{State:X}");
            sb.AppendLine($"TimeExpireCurrentState: 0x{TimeExpireCurrentState:X}");
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypePath(PrototypeId)}");
            sb.AppendLine($"Random: 0x{Random:X}");
            for (int i = 0; i < Objectives.Length; i++) sb.AppendLine($"Objective{i}: {Objectives[i]}");
            for (int i = 0; i < Participants.Length; i++) sb.AppendLine($"Participant{i}: {Participants[i]}");
            sb.AppendLine($"Suspended: {Suspended}");
            return sb.ToString();
        }
    }
}
