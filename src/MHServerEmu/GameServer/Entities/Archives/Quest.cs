using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Entities.Archives
{
    public class Quest
    {
        public ulong PrototypeId { get; set; }
        public ulong[] Fields { get; set; }

        public Quest(ulong prototypeId, ulong[] fields)
        {
            PrototypeId = prototypeId;
            Fields = fields;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(PrototypeId);
                stream.WriteRawVarint64((ulong)Fields.Length);
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
                streamWriter.WriteLine($"PrototypeId: {PrototypeId}");
                for (int i = 0; i < Fields.Length; i++) streamWriter.WriteLine($"Field{i}: {Fields[i]}");
                */
                streamWriter.WriteLine($"PrototypeId: 0x{PrototypeId.ToString("X")}");
                for (int i = 0; i < Fields.Length; i++) streamWriter.WriteLine($"Field{i}: 0x{Fields[i].ToString("X")}");
                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
