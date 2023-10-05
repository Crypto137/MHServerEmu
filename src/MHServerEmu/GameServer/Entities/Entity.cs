using System.Text;
using Google.ProtocolBuffers;
using Gazillion;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.Entities
{
    public class Entity
    {
        public EntityBaseData BaseData { get; set; }

        public uint ReplicationPolicy { get; set; }
        public ReplicatedPropertyCollection PropertyCollection { get; set; }
        public ulong[] UnknownFields { get; set; } = Array.Empty<ulong>();

        public Entity(EntityBaseData baseData, byte[] archiveData)
        {
            BaseData = baseData;
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);

            ReadEntityFields(stream);
            ReadUnknownFields(stream);
        }

        public Entity() { }

        public Entity(EntityBaseData baseData, uint replicationPolicy, ReplicatedPropertyCollection propertyCollection, ulong[] unknownFields)
        {
            BaseData = baseData;
            ReplicationPolicy = replicationPolicy;
            PropertyCollection = propertyCollection;
            UnknownFields = unknownFields;
        }

        public virtual byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                WriteEntityFields(cos);
                WriteUnknownFields(cos);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public NetMessageEntityCreate ToNetMessageEntityCreate()
        {
            return NetMessageEntityCreate.CreateBuilder()
                .SetBaseData(ByteString.CopyFrom(BaseData.Encode()))
                .SetArchiveData(ByteString.CopyFrom(Encode()))
                .Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            WriteEntityString(sb);
            WriteUnknownFieldString(sb);
            return sb.ToString();
        }

        protected void ReadEntityFields(CodedInputStream stream)
        {
            ReplicationPolicy = stream.ReadRawVarint32();
            PropertyCollection = new(stream);
        }

        protected void ReadUnknownFields(CodedInputStream stream)
        {
            List<ulong> fieldList = new();
            while (!stream.IsAtEnd) fieldList.Add(stream.ReadRawVarint64());
            UnknownFields = fieldList.ToArray();
        }

        protected void WriteEntityFields(CodedOutputStream stream)
        {
            stream.WriteRawVarint32(ReplicationPolicy);
            stream.WriteRawBytes(PropertyCollection.Encode());
        }

        protected void WriteUnknownFields(CodedOutputStream stream)
        {
            foreach (ulong field in UnknownFields) stream.WriteRawVarint64(field);
        }

        protected void WriteEntityString(StringBuilder sb)
        {
            sb.AppendLine($"ReplicationPolicy: 0x{ReplicationPolicy:X}");
            sb.AppendLine($"PropertyCollection: {PropertyCollection}");
        }

        protected void WriteUnknownFieldString(StringBuilder sb)
        {
            for (int i = 0; i < UnknownFields.Length; i++)
                sb.AppendLine($"UnknownField{i}: 0x{UnknownFields[i]:X}");
        }
    }
}
