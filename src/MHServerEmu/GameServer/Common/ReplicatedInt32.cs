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

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ReplicationId);
            stream.WriteRawInt32(Value);
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


