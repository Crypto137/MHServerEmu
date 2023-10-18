using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Properties
{
    public class ReplicatedPropertyCollection
    {
        public ulong ReplicationId { get; set; }
        public List<Property> List { get; set; } = new();

        public ReplicatedPropertyCollection(CodedInputStream stream)
        {
            ReplicationId = stream.ReadRawVarint64();

            uint propertyCount = stream.ReadRawUInt32();
            for (int i = 0; i < propertyCount; i++)
                List.Add(new(stream));
        }

        public ReplicatedPropertyCollection(ulong replicationId, List<Property> propertyList = null)
        {
            ReplicationId = replicationId;
            if (propertyList != null) List.AddRange(propertyList);
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ReplicationId);
            stream.WriteRawUInt32((uint)List.Count);
            foreach (Property property in List) property.Encode(stream);
        }

        public Property GetPropertyByEnum(PropertyEnum id)
        {
            return List.Find(property => property.Enum == id);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationId: {ReplicationId}");
            for (int i = 0; i < List.Count; i++) sb.AppendLine($"Property{i}: {List[i]}");
            return sb.ToString();
        }
    }
}
