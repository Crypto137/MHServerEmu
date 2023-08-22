using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Properties;

namespace MHServerEmu.GameServer.Entities
{
    public class Entity
    {
        public ulong ReplicationPolicy { get; set; }
        public ulong ReplicationId { get; set; }
        public Property[] Properties { get; set; }
        public ulong[] UnknownFields { get; set; }

        public Entity(byte[] archiveData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);

            ReadEntityFields(stream);
            ReadUnknownFields(stream);
        }

        public Entity() { }

        public Entity(ulong replicationPolicy, ulong replicationId, Property[] properties, ulong[] unknownFields)
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
            using (MemoryStream stream = new())
            using (StreamWriter writer = new(stream))
            {
                WriteEntityString(writer);
                WriteUnknownFieldString(writer);

                writer.Flush();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        protected void ReadEntityFields(CodedInputStream stream)
        {
            ReplicationPolicy = stream.ReadRawVarint64();
            ReplicationId = stream.ReadRawVarint64();

            Properties = new Property[stream.ReadRawUInt32()];
            for (int i = 0; i < Properties.Length; i++)
            {
                ulong id = stream.ReadRawVarint64();
                ulong value = stream.ReadRawVarint64();
                Properties[i] = new(id, value);
            }
        }

        protected void ReadUnknownFields(CodedInputStream stream)
        {
            List<ulong> fieldList = new();
            while (!stream.IsAtEnd) fieldList.Add(stream.ReadRawVarint64());
            UnknownFields = fieldList.ToArray();
        }

        protected void WriteEntityFields(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ReplicationPolicy);
            stream.WriteRawVarint64(ReplicationId);
            stream.WriteRawBytes(BitConverter.GetBytes(Properties.Length));
            foreach (Property property in Properties) stream.WriteRawBytes(property.Encode());
        }

        protected void WriteUnknownFields(CodedOutputStream stream)
        {
            foreach (ulong field in UnknownFields) stream.WriteRawVarint64(field);
        }

        protected void WriteEntityString(StreamWriter writer)
        {
            writer.WriteLine($"ReplicationPolicy: 0x{ReplicationPolicy.ToString("X")}");
            writer.WriteLine($"ReplicationId: 0x{ReplicationId.ToString("X")}");

            for (int i = 0; i < Properties.Length; i++)
                writer.WriteLine($"Property{i}: {Properties[i]}");
        }

        protected void WriteUnknownFieldString(StreamWriter writer)
        {
            for (int i = 0; i < UnknownFields.Length; i++)
                writer.WriteLine($"UnknownField{i}: 0x{UnknownFields[i].ToString("X")}");
        }
    }
}
