using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.GameServer.Missions
{
    public class ObjectiveGraph
    {
        public ObjectiveGraphNode[] Nodes { get; set; }
        public ObjectiveGraphConnection[] Connections { get; set; }

        public ObjectiveGraph(CodedInputStream stream)
        {
            Nodes = new ObjectiveGraphNode[stream.ReadRawVarint64()];
            for (int i = 0; i < Nodes.Length; i++)
                Nodes[i] = new(stream);

            Connections = new ObjectiveGraphConnection[stream.ReadRawVarint64()];
            for (int i = 0; i < Connections.Length; i++)
                Connections[i] = new(stream);
        }

        public ObjectiveGraph() { }

        public byte[] Encode()
        {
            using (MemoryStream ms = new())
            {
                CodedOutputStream cos = CodedOutputStream.CreateInstance(ms);

                cos.WriteRawVarint64((ulong)Nodes.Length);
                for (int i = 0; i < Nodes.Length; i++)
                    cos.WriteRawBytes(Nodes[i].Encode());

                cos.WriteRawVarint64((ulong)Connections.Length);
                for (int i = 0; i < Connections.Length; i++)
                    cos.WriteRawBytes(Connections[i].Encode());

                cos.Flush();
                return ms.ToArray();
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < Nodes.Length; i++) sb.AppendLine($"Node{i}: {Nodes[i]}");
            for (int i = 0; i < Connections.Length; i++) sb.AppendLine($"Connection{i}: {Connections[i]}");
            return sb.ToString();
        }
    }
}
