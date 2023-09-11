using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Missions
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
            using (MemoryStream ms = new ())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(Index1);
                cos.WriteRawVarint64(Index2);
                cos.WriteRawVarint64(State);
                cos.WriteRawVarint64(Time);
                cos.WriteRawVarint64(Field4);
                cos.WriteRawVarint64(Field5);
                cos.WriteRawVarint64(Field6);
                cos.WriteRawVarint64(Field7);
                cos.WriteRawVarint64(Field8);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Index1: 0x{Index1:X}");
            sb.AppendLine($"Index2: 0x{Index2:X}");
            sb.AppendLine($"State: 0x{State:X}");
            sb.AppendLine($"Time: 0x{Time:X}");
            sb.AppendLine($"Field4: 0x{Field4:X}");
            sb.AppendLine($"Field5: 0x{Field5:X}");
            sb.AppendLine($"Field6: 0x{Field6:X}");
            sb.AppendLine($"Field7: 0x{Field7:X}");
            sb.AppendLine($"Field8: 0x{Field8:X}");
            return sb.ToString();
        }
    }
}
