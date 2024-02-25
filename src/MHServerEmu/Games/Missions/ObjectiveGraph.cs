using System.Text;
using Google.ProtocolBuffers;

namespace MHServerEmu.Games.Missions
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

        public ObjectiveGraph(Game game, Regions.Region region) { }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64((ulong)Nodes.Length);
            for (int i = 0; i < Nodes.Length; i++) Nodes[i].Encode(stream);

            stream.WriteRawVarint64((ulong)Connections.Length);
            for (int i = 0; i < Connections.Length; i++) Connections[i].Encode(stream);
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
