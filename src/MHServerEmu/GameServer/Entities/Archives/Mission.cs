using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;

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
        public byte BoolByte { get; set; }


        public Mission(CodedInputStream stream)
        {
            PrototypeId = stream.ReadRawVarint64();
            State = stream.ReadRawVarint64();
            GameTime = stream.ReadRawVarint64();
            PrototypeGuid = stream.ReadRawVarint64();
            Random = stream.ReadRawVarint64();
            Objectives = new Objective[stream.ReadRawVarint64()];
            for (int i = 0; i < Objectives.Length; i++)
                for (int j = 0; j < 9; j++) stream.ReadRawVarint64();   // skip objectives
            Participant = stream.ReadRawVarint64();
            ParticipantOwnerEntityId = stream.ReadRawVarint64();



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
                //streamWriter.WriteLine($"ReplicationId: 0x{ReplicationId.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
