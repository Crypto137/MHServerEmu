using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.Entities
{
    public class Entity
    {
        public uint ReplicationPolicy { get; set; }
        public ulong ReplicationId { get; set; }
        public Property[] Properties { get; set; }
        public ulong[] UnknownFields { get; set; } = Array.Empty<ulong>();

        public Entity(byte[] archiveData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);

            ReadEntityFields(stream);
            ReadUnknownFields(stream);
        }

        public Entity() { }

        public Entity(uint replicationPolicy, ulong replicationId, Property[] properties, ulong[] unknownFields)
        {
            ReplicationPolicy = replicationPolicy;
            ReplicationId = replicationId;
            Properties = properties;
            UnknownFields = unknownFields;
        }

        public virtual byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                WriteEntityFields(stream);
                WriteUnknownFields(stream);

                stream.Flush();
                return memoryStream.ToArray();
            }
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
            ReplicationId = stream.ReadRawVarint64();

            Properties = new Property[stream.ReadRawUInt32()];
            for (int i = 0; i < Properties.Length; i++)
                Properties[i] = new(stream);
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
            stream.WriteRawVarint64(ReplicationId);
            stream.WriteRawBytes(BitConverter.GetBytes(Properties.Length));
            foreach (Property property in Properties) stream.WriteRawBytes(property.Encode());
        }

        protected void WriteUnknownFields(CodedOutputStream stream)
        {
            foreach (ulong field in UnknownFields) stream.WriteRawVarint64(field);
        }

        protected void WriteEntityString(StringBuilder sb)
        {
            sb.AppendLine($"ReplicationPolicy: 0x{ReplicationPolicy:X}");
            sb.AppendLine($"ReplicationId: 0x{ReplicationId:X}");

            for (int i = 0; i < Properties.Length; i++)
                sb.AppendLine($"Property{i}: {Properties[i]}");
        }

        protected void WriteUnknownFieldString(StringBuilder sb)
        {
            for (int i = 0; i < UnknownFields.Length; i++)
                sb.AppendLine($"UnknownField{i}: 0x{UnknownFields[i]:X}");
        }
    }
}
