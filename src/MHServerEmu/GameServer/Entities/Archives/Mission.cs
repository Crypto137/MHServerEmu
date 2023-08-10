using System.Reflection;
using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class Mission
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public ulong PrototypeId { get; set; }
        public ulong State { get; set; }
        public ulong GameTime { get; set; }
        public ulong PrototypeGuid { get; set; }
        public ulong Random { get; set; }   // zigzag or float?
        public Objective[] Objectives { get; set; }
        public ulong Participant { get; set; }
        public ulong ParticipantOwnerEntityId { get; set; }
        public bool BoolField { get; set; }

        public Mission(CodedInputStream stream, BoolBuffer boolBuffer)
        {
            PrototypeId = stream.ReadRawVarint64();
            State = stream.ReadRawVarint64();
            GameTime = stream.ReadRawVarint64();
            PrototypeGuid = stream.ReadRawVarint64();
            Random = stream.ReadRawVarint64();
            Objectives = new Objective[stream.ReadRawVarint64()];
            for (int i = 0; i < Objectives.Length; i++)
                Objectives[i] = new(stream);
            Participant = stream.ReadRawVarint64();
            ParticipantOwnerEntityId = stream.ReadRawVarint64();

            if (boolBuffer.IsEmpty) boolBuffer.SetBits(stream.ReadRawByte());
            BoolField = boolBuffer.ReadBool();

            Console.WriteLine(ParticipantOwnerEntityId);
        }

        public Mission()
        {

        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                //stream.WriteRawVarint64(ReplicationId);
                //stream.WriteRawString(Text);

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
