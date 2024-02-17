using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Network;

namespace MHServerEmu.Games.Entities
{
    public class Entity
    {
        public EntityBaseData BaseData { get; set; }
        public ulong RegionId { get; set; } = 0;

        public AoiNetworkPolicyValues ReplicationPolicy { get; set; }
        public ReplicatedPropertyCollection Properties { get; set; }

        public Entity(EntityBaseData baseData, ByteString archiveData)
        {
            BaseData = baseData;
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData.ToByteArray());
            Decode(stream);
        }

        // Base data is required for all entities, so there's no parameterless constructor
        public Entity(EntityBaseData baseData) { BaseData = baseData; }

        public Entity(EntityBaseData baseData, AoiNetworkPolicyValues replicationPolicy, ReplicatedPropertyCollection propertyCollection)
        {
            BaseData = baseData;
            ReplicationPolicy = replicationPolicy;
            Properties = propertyCollection;
        }

        protected virtual void Decode(CodedInputStream stream)
        {
            ReplicationPolicy = (AoiNetworkPolicyValues)stream.ReadRawVarint32();
            Properties = new(stream);
        }

        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32((uint)ReplicationPolicy);
            Properties.Encode(stream);
        }

        public ByteString Serialize()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);
                Encode(cos);
                cos.Flush();
                return ByteString.CopyFrom(ms.ToArray());
            }
        }

        public NetMessageEntityCreate ToNetMessageEntityCreate()
        {
            return NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(BaseData.Serialize())
                .SetArchiveData(Serialize())
                .Build();
        }

        protected virtual void BuildString(StringBuilder sb)
        {
            sb.AppendLine($"ReplicationPolicy: {ReplicationPolicy}");
            sb.AppendLine($"Properties: {Properties}");
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            BuildString(sb);
            return sb.ToString();
        }
    }
}
