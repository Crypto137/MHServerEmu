using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Properties
{
    public class ReplicatedPropertyCollection : PropertyCollection, IArchiveMessageHandler
    {
        public ulong ReplicationId { get; set; }

        public ReplicatedPropertyCollection(CodedInputStream stream)
        {
            ReplicationId = stream.ReadRawVarint64();
            Decode(stream);
        }

        public ReplicatedPropertyCollection(ulong replicationId = 0)
        {
            ReplicationId = replicationId;
        }

        public override void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ReplicationId);
            base.Encode(stream);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(ReplicationId)}: {ReplicationId}");
            sb.Append(base.ToString());
            return sb.ToString();
        }
    }
}
