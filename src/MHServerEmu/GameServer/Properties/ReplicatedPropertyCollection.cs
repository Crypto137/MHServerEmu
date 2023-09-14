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

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                cos.WriteRawVarint64(ReplicationId);
                cos.WriteRawUInt32((uint)List.Count);
                foreach (Property property in List) cos.WriteRawBytes(property.Encode());

                cos.Flush();
                return ms.ToArray();
            }
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
