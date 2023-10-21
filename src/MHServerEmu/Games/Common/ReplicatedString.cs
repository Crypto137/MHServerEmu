using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.Games.Common
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

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(ReplicationId);
            stream.WriteRawString(Text);
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
