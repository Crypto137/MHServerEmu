using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Common
{
    public class ReplicatedUInt64
    {
        public ulong ReplicationId { get; set; }
        public ulong Value { get; set; }

        public ReplicatedUInt64(CodedInputStream stream)
        {
            ReplicationId = stream.ReadRawVarint64();
            Value = stream.ReadRawVarint64();
        }

        public ReplicatedUInt64(ulong repId, ulong value)
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
                cos.WriteRawVarint64(Value);

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

