using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class EntityEnterGameWorldArchiveData
    {
        public ulong Header { get; }
        public ulong EntityId { get; set; }
        public ulong[] Fields { get; }

        public EntityEnterGameWorldArchiveData(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            Header = stream.ReadRawVarint64();
            EntityId = stream.ReadRawVarint64();

            List<ulong> fieldList = new();
            while (!stream.IsAtEnd)
            {
                fieldList.Add(stream.ReadRawVarint64());
            }

            Fields = fieldList.ToArray();
        }

        public EntityEnterGameWorldArchiveData(ulong header, ulong entityId, ulong[] fields)
        {
            Header = header;
            EntityId = entityId;
            Fields = fields;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Header);
                stream.WriteRawVarint64(EntityId);
                foreach (ulong field in Fields) stream.WriteRawVarint64(field);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                /* dec output
                streamWriter.WriteLine($"Header: {Header}");
                streamWriter.WriteLine($"EntityId: {EntityId}");
                for (int i = 0; i < Fields.Length; i++) streamWriter.WriteLine($"Field{i}: {Fields[i]}");
                */
                streamWriter.WriteLine($"Header: 0x{Header.ToString("X")}");
                streamWriter.WriteLine($"EntityId: 0x{EntityId.ToString("X")}");
                for (int i = 0; i < Fields.Length; i++) streamWriter.WriteLine($"Field{i}: 0x{Fields[i].ToString("X")}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
