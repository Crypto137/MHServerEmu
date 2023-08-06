using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class Entity
    {
        public ulong ReplicationPolicy { get; }
        public ulong ReplicationId { get; }
        public Property[] Properties { get; }

        public ulong[] UnknownFields { get; }

        public Entity(byte[] archiveData)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(archiveData);

            ReplicationPolicy = stream.ReadRawVarint64();
            ReplicationId = stream.ReadRawVarint64();

            Properties = new Property[BitConverter.ToUInt32(stream.ReadRawBytes(4))];
            for (int i = 0; i < Properties.Length; i++)
            {
                ulong id = stream.ReadRawVarint64();
                ulong value = stream.ReadRawVarint64();
                Properties[i] = new(id, value);
            }

            List<ulong> fieldList = new();
            while (!stream.IsAtEnd)
            {
                fieldList.Add(stream.ReadRawVarint64());
            }

            UnknownFields = fieldList.ToArray();
        }

        public Entity(ulong replicationPolicy, ulong replicationId, Property[] properties, ulong[] unknownFields)
        {
            ReplicationPolicy = replicationPolicy;
            ReplicationId = replicationId;
            Properties = properties;
            UnknownFields = unknownFields;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(ReplicationPolicy);
                stream.WriteRawVarint64(ReplicationId);
                stream.WriteRawBytes(BitConverter.GetBytes(Properties.Length));
                foreach (Property property in Properties) stream.WriteRawBytes(property.Encode());
                foreach (ulong field in UnknownFields) stream.WriteRawVarint64(field);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"ReplicationPolicy: 0x{ReplicationPolicy.ToString("X")}");
                streamWriter.WriteLine($"ReplicationId: 0x{ReplicationId.ToString("X")}");
                for (int i = 0; i < Properties.Length; i++) streamWriter.WriteLine($"Property{i}: {Properties[i]}");
                for (int i = 0; i < UnknownFields.Length; i++) streamWriter.WriteLine($"UnknownField{i}: 0x{UnknownFields[i].ToString("X")}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
