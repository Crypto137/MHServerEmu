using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoders;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.GameData;

namespace MHServerEmu.GameServer.Missions
{
    public class Mission
    {
        public ulong PrototypeId1 { get; set; }
        public ulong State { get; set; }
        public ulong GameTime { get; set; }
        public ulong PrototypeId2 { get; set; }
        public int Random { get; set; }
        public Objective[] Objectives { get; set; }
        public ulong Participant { get; set; }
        public ulong ParticipantOwnerEntityId { get; set; }
        public bool BoolField { get; set; }

        public Mission(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeId1 = stream.ReadRawVarint64();
            State = stream.ReadRawVarint64();
            GameTime = stream.ReadRawVarint64();
            PrototypeId2 = stream.ReadPrototypeId(PrototypeEnumType.All);
            Random = stream.ReadRawInt32();
            Objectives = new Objective[stream.ReadRawVarint64()];
            for (int i = 0; i < Objectives.Length; i++)
                Objectives[i] = new(stream);
            Participant = stream.ReadRawVarint64();
            ParticipantOwnerEntityId = stream.ReadRawVarint64();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            BoolField = boolDecoder.ReadBool();
        }

        public Mission(ulong prototypeId1, ulong state, ulong gameTime, ulong prototypeId2,
            Objective[] objectives, ulong participant, ulong participantOwnerEntityId, bool boolField)
        {
            PrototypeId1 = prototypeId1;
            State = state;
            GameTime = gameTime;
            PrototypeId2 = prototypeId2;
            Objectives = objectives;
            Participant = participant;
            ParticipantOwnerEntityId = participantOwnerEntityId;
            BoolField = boolField;
        }

        public byte[] Encode(BoolEncoder boolEncoder)
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(PrototypeId1);
                stream.WriteRawVarint64(State);
                stream.WriteRawVarint64(GameTime);
                stream.WritePrototypeId(PrototypeId2, PrototypeEnumType.All);
                stream.WriteRawInt32(Random);
                stream.WriteRawVarint64((ulong)Objectives.Length);
                foreach (Objective objective in Objectives)
                    stream.WriteRawBytes(objective.Encode());
                stream.WriteRawVarint64(Participant);
                stream.WriteRawVarint64(ParticipantOwnerEntityId);

                byte bitBuffer = boolEncoder.GetBitBuffer();             //BoolField
                if (bitBuffer != 0)
                    stream.WriteRawByte(bitBuffer);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"PrototypeId1: 0x{PrototypeId1:X}");
            sb.AppendLine($"State: 0x{State:X}");
            sb.AppendLine($"GameTime: 0x{GameTime:X}");
            sb.AppendLine($"PrototypeId2: {GameDatabase.GetPrototypePath(PrototypeId2)}");
            sb.AppendLine($"Random: 0x{Random:X}");
            for (int i = 0; i < Objectives.Length; i++) sb.AppendLine($"Objective{i}: {Objectives[i]}");
            sb.AppendLine($"Participant: 0x{Participant:X}");
            sb.AppendLine($"ParticipantOwnerEntityId: 0x{ParticipantOwnerEntityId:X}");
            sb.AppendLine($"BoolField: {BoolField}");
            return sb.ToString();
        }
    }
}
