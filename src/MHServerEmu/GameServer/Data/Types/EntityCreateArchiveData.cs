using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Data.Types
{
    public class EntityCreateArchiveData
    {
        public ulong Header { get; }
        public ulong RepId { get; }
        public uint Size { get; }

        public ulong[] Fields { get; }

        public EntityCreateArchiveData(byte[] data)
        {
            CodedInputStream stream = CodedInputStream.CreateInstance(data);

            Header = stream.ReadRawVarint64();
            RepId = stream.ReadRawVarint64();
            Size = BitConverter.ToUInt32(stream.ReadRawBytes(4));

            List<ulong> fieldList = new();
            while (!stream.IsAtEnd)
            {
                fieldList.Add(stream.ReadRawVarint64());
            }

            Fields = fieldList.ToArray();
        }

        public EntityCreateArchiveData(ulong header, ulong repId, uint size, ulong[] fields)
        {
            Header = header;
            RepId = repId;
            Size = size;
            Fields = fields;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Header);
                stream.WriteRawVarint64(RepId);
                stream.WriteRawBytes(BitConverter.GetBytes(Size));
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
                streamWriter.WriteLine($"RepId: {RepId}");
                streamWriter.WriteLine($"Size: {Size}");
                for (int i = 0; i < Fields.Length; i++) streamWriter.WriteLine($"Field{i}: {Fields[i]}");
                */
                streamWriter.WriteLine($"Header: 0x{Header.ToString("X")}");
                streamWriter.WriteLine($"RepId: 0x{RepId.ToString("X")}");
                streamWriter.WriteLine($"Size: 0x{Size.ToString("X")}");
                for (int i = 0; i < Fields.Length; i++) streamWriter.WriteLine($"Field{i}: 0x{Fields[i].ToString("X")}");

                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
