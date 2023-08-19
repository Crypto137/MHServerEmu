using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Encoding;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Missions
{
    public class Mission
    {
        public ulong PrototypeId { get; set; }
        public ulong State { get; set; }
        public ulong GameTime { get; set; }
        public ulong PrototypeGuid { get; set; }
        public int Random { get; set; }
        public Objective[] Objectives { get; set; }
        public ulong Participant { get; set; }
        public ulong ParticipantOwnerEntityId { get; set; }
        public bool BoolField { get; set; }

        public Mission(CodedInputStream stream, BoolDecoder boolDecoder)
        {
            PrototypeId = stream.ReadRawVarint64();
            State = stream.ReadRawVarint64();
            GameTime = stream.ReadRawVarint64();
            PrototypeGuid = stream.ReadRawVarint64();
            Random = stream.ReadRawInt32();
            Objectives = new Objective[stream.ReadRawVarint64()];
            for (int i = 0; i < Objectives.Length; i++)
                Objectives[i] = new(stream);
            Participant = stream.ReadRawVarint64();
            ParticipantOwnerEntityId = stream.ReadRawVarint64();

            if (boolDecoder.IsEmpty) boolDecoder.SetBits(stream.ReadRawByte());
            BoolField = boolDecoder.ReadBool();
        }

        public Mission(ulong prototypeId, ulong state, ulong gameTime, ulong prototypeGuid,
            Objective[] objectives, ulong participant, ulong participantOwnerEntityId, bool boolField)
        {
            PrototypeId = prototypeId;
            State = state;
            GameTime = gameTime;
            PrototypeGuid = prototypeGuid;
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

                stream.WriteRawVarint64(PrototypeId);
                stream.WriteRawVarint64(State);
                stream.WriteRawVarint64(GameTime);
                stream.WriteRawVarint64(PrototypeGuid);
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
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"PrototypeId: 0x{PrototypeId.ToString("X")}");
                streamWriter.WriteLine($"State: 0x{State.ToString("X")}");
                streamWriter.WriteLine($"GameTime: 0x{GameTime.ToString("X")}");
                streamWriter.WriteLine($"PrototypeGuid: 0x{PrototypeGuid.ToString("X")}");
                streamWriter.WriteLine($"Random: 0x{Random.ToString("X")}");
                for (int i = 0; i < Objectives.Length; i++) streamWriter.WriteLine($"Objective{i}: {Objectives[i]}");
                streamWriter.WriteLine($"Participant: 0x{Participant.ToString("X")}");
                streamWriter.WriteLine($"ParticipantOwnerEntityId: 0x{ParticipantOwnerEntityId.ToString("X")}");
                streamWriter.WriteLine($"BoolField: {BoolField}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
