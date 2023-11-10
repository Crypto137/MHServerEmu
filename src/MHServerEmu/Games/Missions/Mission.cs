using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Missions
{
    public class Mission
    {
        public PrototypeGuid PrototypeGuid { get; set; }
        public ulong State { get; set; }
        public ulong TimeExpireCurrentState { get; set; }
        public PrototypeId PrototypeId { get; set; }
        public int Random { get; set; }
        public Objective[] Objectives { get; set; }
        public ulong[] Participants { get; set; }
        public bool Suspended { get; set; }

        public Mission(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeGuid = (PrototypeGuid)stream.ReadRawVarint64();
            State = stream.ReadRawVarint64();
            TimeExpireCurrentState = stream.ReadRawVarint64();
            PrototypeId = stream.ReadPrototypeEnum(PrototypeEnumType.All);
            Random = stream.ReadRawInt32();

            Objectives = new Objective[stream.ReadRawVarint64()];
            for (int i = 0; i < Objectives.Length; i++)
                Objectives[i] = new(stream);

            Participants = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Participants.Length; i++)
                Participants[i] = stream.ReadRawVarint64();

            Suspended = boolDecoder.ReadBool(stream);
        }

        public Mission(PrototypeGuid prototypeGuid, ulong state, ulong timeExpireCurrentState, PrototypeId prototypeId,
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

        public void Encode(CodedOutputStream stream, BoolEncoder boolEncoder)
        {
            stream.WriteRawVarint64((ulong)PrototypeGuid);
            stream.WriteRawVarint64(State);
            stream.WriteRawVarint64(TimeExpireCurrentState);
            stream.WritePrototypeEnum(PrototypeId, PrototypeEnumType.All);
            stream.WriteRawInt32(Random);

            stream.WriteRawVarint64((ulong)Objectives.Length);
            foreach (Objective objective in Objectives) objective.Encode(stream);

            stream.WriteRawVarint64((ulong)Participants.Length);
            foreach (ulong Participant in Participants) stream.WriteRawVarint64(Participant);

            boolEncoder.WriteBuffer(stream);   // Suspended
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeGuid: {GameDatabase.GetPrototypeName(GameDatabase.GetDataRefByPrototypeGuid(PrototypeGuid))}");
            sb.AppendLine($"State: 0x{State:X}");
            sb.AppendLine($"TimeExpireCurrentState: 0x{TimeExpireCurrentState:X}");
            sb.AppendLine($"PrototypeId: {GameDatabase.GetPrototypeName(PrototypeId)}");
            sb.AppendLine($"Random: 0x{Random:X}");
            for (int i = 0; i < Objectives.Length; i++) sb.AppendLine($"Objective{i}: {Objectives[i]}");
            for (int i = 0; i < Participants.Length; i++) sb.AppendLine($"Participant{i}: {Participants[i]}");
            sb.AppendLine($"Suspended: {Suspended}");
            return sb.ToString();
        }
    }
}
