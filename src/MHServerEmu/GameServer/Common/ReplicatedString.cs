using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common;

namespace MHServerEmu.GameServer.Common
{
    public class ReplicatedString
    {
        public ulong ReplicationId { get; set; }
        public string Text { get; set; }

        public ReplicatedString(CodedInputStream stream)
        {
            ReplicationId = stream.ReadRawVarint64();
            Text = stream.ReadRawString();
        }

        public ReplicatedString(ulong repId, string text)
        {
            ReplicationId = repId;
            Text = text;
        }

        public byte[] Encode()
        {
            using (MemoryStream memoryStream = new())
            {
                CodedOutputStream stream = CodedOutputStream.CreateInstance(memoryStream);

                stream.WriteRawVarint64(ReplicationId);
                stream.WriteRawString(Text);

                stream.Flush();
                return memoryStream.ToArray();
            }
        }

        public override string ToString()
        {
            using (MemoryStream memoryStream = new())
            using (StreamWriter streamWriter = new(memoryStream))
            {
                streamWriter.WriteLine($"ReplicationId: 0x{ReplicationId.ToString("X")}");
                streamWriter.WriteLine($"Text: {Text}");

                streamWriter.Flush();
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}
