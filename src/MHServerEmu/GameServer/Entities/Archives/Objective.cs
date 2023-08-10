using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class Objective
    {
        public ulong Index1 { get; set; }
        public ulong Index2 { get; set; }   // Gazillion::Serializer::Transfer_MissionObjective
        public ulong State { get; set; }
        public ulong Time { get; set; }
        public ulong Field4 { get; set; }   // uint
        public ulong Field5 { get; set; }
        public ulong Field6 { get; set; }
        public ulong Field7 { get; set; }
        public ulong Field8 { get; set; }

        public Objective(CodedInputStream stream)
        {
            Index1 = stream.ReadRawVarint64();
            Index2 = stream.ReadRawVarint64();
            State = stream.ReadRawVarint64();
            Time = stream.ReadRawVarint64();
            Field4 = stream.ReadRawVarint64();
            Field5 = stream.ReadRawVarint64();
            Field6 = stream.ReadRawVarint64();
            Field7 = stream.ReadRawVarint64();
            Field8 = stream.ReadRawVarint64();
        }

        public Objective(ulong index1, ulong index2, ulong state, ulong time, ulong field4,
            ulong field5, ulong field6, ulong field7, ulong field8)
        {
            Index1 = index1;
            Index2 = index2;
            State = state;
            Time = time;
            Field4 = field4;
            Field5 = field5;
            Field6 = field6;
            Field7 = field7;
            Field8 = field8;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new ())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Index1);
                stream.WriteRawVarint64(Index2);
                stream.WriteRawVarint64(State);
                stream.WriteRawVarint64(Time);
                stream.WriteRawVarint64(Field4);
                stream.WriteRawVarint64(Field5);
                stream.WriteRawVarint64(Field6);
                stream.WriteRawVarint64(Field7);
                stream.WriteRawVarint64(Field8);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Index1: 0x{Index1.ToString("X")}");
                streamWriter.WriteLine($"Index2: 0x{Index2.ToString("X")}");
                streamWriter.WriteLine($"State: 0x{State.ToString("X")}");
                streamWriter.WriteLine($"Time: 0x{Time.ToString("X")}");
                streamWriter.WriteLine($"Field4: 0x{Field4.ToString("X")}");
                streamWriter.WriteLine($"Field5: 0x{Field5.ToString("X")}");
                streamWriter.WriteLine($"Field6: 0x{Field6.ToString("X")}");
                streamWriter.WriteLine($"Field7: 0x{Field7.ToString("X")}");
                streamWriter.WriteLine($"Field8: 0x{Field8.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
