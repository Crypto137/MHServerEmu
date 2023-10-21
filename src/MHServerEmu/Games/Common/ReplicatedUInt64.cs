using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.Games.Common
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

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ReplicationId);
            stream.WriteRawVarint64(Value);
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

