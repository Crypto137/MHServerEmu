using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.Entities
{
    public class Entity
    {
        public EntityBaseData BaseData { get; set; }
        public ulong RegionId { get; set; } = 0;

        public uint ReplicationPolicy { get; set; }
        public ReplicatedPropertyCollection PropertyCollection { get; set; }

        public Entity(EntityBaseData baseData, ByteString archiveData)
        {
            BaseData = baseData;
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData.ToByteArray());
            Decode(stream);
        }

        // Base data is required for all entities, so there's no parameterless constructor
        public Entity(EntityBaseData baseData) { BaseData = baseData; }

        public Entity(EntityBaseData baseData, uint replicationPolicy, ReplicatedPropertyCollection propertyCollection)
        {
            BaseData = baseData;
            ReplicationPolicy = replicationPolicy;
            PropertyCollection = propertyCollection;
        }

        protected virtual void Decode(CodedInputStream stream)
        {
            ReplicationPolicy = stream.ReadRawVarint32();
            PropertyCollection = new(stream);
        }

        public virtual void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32(ReplicationPolicy);
            stream.WriteRawBytes(PropertyCollection.Encode());
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
            sb.AppendLine($"ReplicationPolicy: 0x{ReplicationPolicy:X}");
            sb.AppendLine($"PropertyCollection: {PropertyCollection}");
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            BuildString(sb);
            return sb.ToString();
        }
    }
}
