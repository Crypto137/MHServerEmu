using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Common.Extensions;

namespace MHServerEmu.GameServer.Missions
{
    public class ObjectiveGraphConnection
    {
        public int Node0 { get; set; }
        public int Node1 { get; set; }
        public float Distance { get; set; }

        public ObjectiveGraphConnection(CodedInputStream stream)
        {
            Node0 = stream.ReadRawInt32();
            Node1 = stream.ReadRawInt32();
            Distance = stream.ReadRawFloat();
        }

        public ObjectiveGraphConnection() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawInt32(Node0);
                cos.WriteRawInt32(Node1);
                cos.WriteRawFloat(Distance);

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Node0: {Node0}");
            sb.AppendLine($"Node1: {Node1}");
            sb.AppendLine($"Distance: {Distance}");
            return sb.ToString();
        }
    }
}
