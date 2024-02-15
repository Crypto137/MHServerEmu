using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Properties
{
    public class ReplicatedPropertyCollection : PropertyCollection, IArchiveMessageHandler
    {
        public ulong ReplicationId { get; set; }

        public ReplicatedPropertyCollection(CodedInputStream stream)
        {
            ReplicationId = stream.ReadRawVarint64();

            uint propertyCount = stream.ReadRawUInt32();
            for (int i = 0; i < propertyCount; i++)
                _propertyList.Add(new(stream));
        }

        public ReplicatedPropertyCollection(ulong replicationId, List<Property> propertyList = null) : base(propertyList)
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
