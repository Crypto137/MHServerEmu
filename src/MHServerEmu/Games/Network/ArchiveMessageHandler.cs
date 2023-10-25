using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.Games.Network
{
    public class ArchiveMessageHandler
    {
        public const ulong InvalidReplicationId = 0;

        public ulong ReplicationId { get; set; }

        public ArchiveMessageHandler(CodedInputStream stream)
        {
            ReplicationId = stream.ReadRawVarint64();
        }

        public ArchiveMessageHandler(ulong replicationId)
        {
            ReplicationId = replicationId;
        }

        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ReplicationId);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            BuildString(sb);
            return sb.ToString();
        }

        protected virtual void BuildString(StringBuilder sb)
        {
            sb.AppendLine($"ReplicationId: {ReplicationId}");
        }
    }
}
