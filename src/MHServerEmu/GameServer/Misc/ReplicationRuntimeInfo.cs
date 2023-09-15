using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Misc
{
    public class ReplicationRuntimeInfo
    {
        public ulong Field0 { get; set; }
        public string Field1 { get; set; }
        public int Field2 { get; set; }

        public ReplicationRuntimeInfo(CodedInputStream stream)
        {
            Field0 = stream.ReadRawVarint64();
            Field1 = stream.ReadRawString();
            Field2 = stream.ReadRawInt32();
        }

        public ReplicationRuntimeInfo(ulong field0, string field1, int field2)
        {
            Field0 = field0;
            Field1 = field1;
            Field2 = field2;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(Field0);
                cos.WriteRawString(Field1);
                cos.WriteRawInt32(Field2);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Field0: 0x{Field0:x}");
            sb.AppendLine($"Field1: {Field1}");
            sb.AppendLine($"Field2: {Field2}");
            return sb.ToString();
        }
    }
}
