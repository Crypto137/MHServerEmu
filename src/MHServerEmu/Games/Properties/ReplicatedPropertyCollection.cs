using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Properties
{
    public class ReplicatedPropertyCollection : ArchiveMessageHandler
    {
        // C# doesn't support multiple inheritance, so we'll have to have an instance of PropertyCollection to wrap around
        private readonly PropertyCollection _propertyCollection;

        public List<Property> List { get => _propertyCollection.PropertyList; }

        public ReplicatedPropertyCollection(CodedInputStream stream) : base(stream)
        {
            _propertyCollection = new(stream);
        }

        public ReplicatedPropertyCollection(ulong replicationId, List<Property> propertyList = null) : base(replicationId)
        {
            _propertyCollection = new(propertyList);
        }

        public override void Encode(CodedOutputStream stream)
        {
            base.Encode(stream);
            _propertyCollection.Encode(stream);
        }

        public Property GetPropertyByEnum(PropertyEnum propertyEnum)
        {
            return _propertyCollection.GetPropertyByEnum(propertyEnum);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationId: {ReplicationId}");
            sb.AppendLine(_propertyCollection.ToString());
            return sb.ToString();
        }
    }
}
