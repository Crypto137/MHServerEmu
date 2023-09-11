using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Social
{
    public class Friend
    {
        public string Field0 { get; set; }  // name
        public ulong Field1 { get; set; }   // id
        public ulong Field2 { get; set; }   // PrototypeDataRef
        public ulong Field3 { get; set; }   // PrototypeDataRef
        public ulong Field4 { get; set; }   // uchar
        public int Field5 { get; set; }
        public string Field6 { get; set; }  // name again
        public string Field7 { get; set; }  // ??
        public ulong Field8 { get; set; }   // u64
        public ulong Field9 { get; set; }   // u64
        public int Field10 { get; set; }
        public int Field11 { get; set; }

        public Friend(CodedInputStream stream)
        {
            Field0 = stream.ReadRawString();
            Field1 = stream.ReadRawVarint64();
            Field2 = stream.ReadRawVarint64();
            Field3 = stream.ReadRawVarint64();
            Field4 = stream.ReadRawVarint64();
            Field5 = stream.ReadRawInt32();
            Field6 = stream.ReadRawString();
            Field7 = stream.ReadRawString();
            Field8 = stream.ReadRawVarint64();
            Field9 = stream.ReadRawVarint64();
            Field10 = stream.ReadRawInt32();
            Field11 = stream.ReadRawInt32();
        }

        public Friend(string field0, ulong field1, ulong field2, ulong field3, ulong field4, int field5,
            string field6, string field7, ulong field8, ulong field9, int field10, int field11)
        {
            Field0 = field0;
            Field1 = field1;
            Field2 = field2;
            Field3 = field3;
            Field4 = field4;
            Field5 = field5;
            Field6 = field6;
            Field7 = field7;
            Field8 = field8;
            Field9 = field9;
            Field10 = field10;
            Field11 = field11;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawString(Field0);
                cos.WriteRawVarint64(Field1);
                cos.WriteRawVarint64(Field2);
                cos.WriteRawVarint64(Field3);
                cos.WriteRawVarint64(Field4);
                cos.WriteRawInt32(Field5);
                cos.WriteRawString(Field6);
                cos.WriteRawString(Field7);
                cos.WriteRawVarint64(Field8);
                cos.WriteRawVarint64(Field9);
                cos.WriteRawInt32(Field10);
                cos.WriteRawInt32(Field11);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Field0: {Field0}");
            sb.AppendLine($"Field1: 0x{Field1:X}");
            sb.AppendLine($"Field2: 0x{Field2:X}");
            sb.AppendLine($"Field3: 0x{Field3:X}");
            sb.AppendLine($"Field4: 0x{Field4:X}");
            sb.AppendLine($"Field5: 0x{Field5:X}");
            sb.AppendLine($"Field6: {Field6}");
            sb.AppendLine($"Field7: {Field7}");
            sb.AppendLine($"Field8: 0x{Field8:X}");
            sb.AppendLine($"Field9: 0x{Field9:X}");
            sb.AppendLine($"Field10: 0x{Field10:X}");
            sb.AppendLine($"Field11: 0x{Field11:X}");
            return sb.ToString();
        }
    }
}
