using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

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
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64(ReplicationId);
                cos.WriteRawString(Text);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"ReplicationId: {ReplicationId}");
            sb.AppendLine($"Text: {Text}");
            return sb.ToString();
        }
    }
}
