using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class Friend
    {
        public string Field0 { get; set; }  // name
        public ulong Field1 { get; set; }   // id
        public ulong Field2 { get; set; }   // PrototypeDataRef
        public ulong Field3 { get; set; }   // PrototypeDataRef
        public ulong Field4 { get; set; }   // uchar
        public ulong Field5 { get; set; }   // int
        public string Field6 { get; set; }  // name again
        public string Field7 { get; set; }  // ??
        public ulong Field8 { get; set; }   // u64
        public ulong Field9 { get; set; }   // u64
        public ulong Field10 { get; set; }  // int (>> 1)
        public ulong Field11 { get; set; }  // int

        public Friend(CodedInputStream stream)
        {
            Field0 = stream.ReadRawString();
            Field1 = stream.ReadRawVarint64();
            Field2 = stream.ReadRawVarint64();
            Field3 = stream.ReadRawVarint64();
            Field4 = stream.ReadRawVarint64();
            Field5 = stream.ReadRawVarint64();
            Field6 = stream.ReadRawString();
            Field7 = stream.ReadRawString();
            Field8 = stream.ReadRawVarint64();
            Field9 = stream.ReadRawVarint64();
            Field10 = stream.ReadRawVarint64();
            Field11 = stream.ReadRawVarint64();
        }

        public Friend(string field0, ulong field1, ulong field2, ulong field3, ulong field4, ulong field5,
            string field6, string field7, ulong field8, ulong field9, ulong field10, ulong field11)
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
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawString(Field0);
                stream.WriteRawVarint64(Field1);
                stream.WriteRawVarint64(Field2);
                stream.WriteRawVarint64(Field3);
                stream.WriteRawVarint64(Field4);
                stream.WriteRawVarint64(Field5);
                stream.WriteRawString(Field6);
                stream.WriteRawString(Field7);
                stream.WriteRawVarint64(Field8);
                stream.WriteRawVarint64(Field9);
                stream.WriteRawVarint64(Field10);
                stream.WriteRawVarint64(Field11);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"Field0: {Field0}");
                streamWriter.WriteLine($"Field1: 0x{Field1.ToString("X")}");
                streamWriter.WriteLine($"Field2: 0x{Field2.ToString("X")}");
                streamWriter.WriteLine($"Field3: 0x{Field3.ToString("X")}");
                streamWriter.WriteLine($"Field4: 0x{Field4.ToString("X")}");
                streamWriter.WriteLine($"Field5: 0x{Field5.ToString("X")}");
                streamWriter.WriteLine($"Field6: {Field6}");
                streamWriter.WriteLine($"Field7: {Field7}");
                streamWriter.WriteLine($"Field8: 0x{Field8.ToString("X")}");
                streamWriter.WriteLine($"Field9: 0x{Field9.ToString("X")}");
                streamWriter.WriteLine($"Field10: 0x{Field10.ToString("X")}");
                streamWriter.WriteLine($"Field11: 0x{Field11.ToString("X")}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
