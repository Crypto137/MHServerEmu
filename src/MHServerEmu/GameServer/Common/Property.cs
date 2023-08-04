using System.Text;
using Gazillion;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Common
{
    public class Property
    {
        public ulong Id { get; set; }
        public ulong Value { get; set; }

        public Property(ulong id, ulong value)
        {
            Id = id;
            Value = value;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(Id);
                stream.WriteRawVarint64(Value);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public NetStructProperty ToNetStruct() => NetStructProperty.CreateBuilder().SetId(Id).SetValue(Value).Build();

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                /* dec output
                streamWriter.WriteLine($"Id: {Id}");
                streamWriter.WriteLine($"Value: {Value}");
                */
                streamWriter.WriteLine($"Id: 0x{Id.ToString("X")}");
                streamWriter.WriteLine($"Value: 0x{Value.ToString("X")}");
                streamWriter.Flush();

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
