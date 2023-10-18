using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Common
{
    public class ReplicatedInt32
    {
        public ulong ReplicationId { get; set; }
        public int Value { get; set; }

        public ReplicatedInt32(CodedInputStream stream)
        {
            ReplicationId = stream.ReadRawVarint64();
            Value = stream.ReadRawInt32();
        }

        public ReplicatedInt32(ulong repId, int value)
        {
            ReplicationId = repId;
            Value = value;
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(ReplicationId);
                cos.WriteRawInt32(Value);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationId: {ReplicationId}");
            sb.AppendLine($"Value: {Value}");
            return sb.ToString();
        }
    }
}


