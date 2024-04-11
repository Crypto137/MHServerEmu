using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Regions.ObjectiveGraphs
{
    public class ObjectiveGraphNode : IComparable<ObjectiveGraphNode>
    {
        private readonly Dictionary<ObjectiveGraphNode, float> _connectionDict = new();

        private float _shortestDistance = float.MaxValue;

        // replacement for pointer sort
        private static long _globalInstanceCount = 0;
  
        public long InstanceNumber { get; }

        public ulong Id { get; set; }
        public Vector3 Position { get; set; }
        public ulong[] Areas { get; set; }
        public ulong[] Cells { get; set; }
        public int Type { get; set; }

        public ObjectiveGraphNode()
        {
            InstanceNumber = _globalInstanceCount++;
        }

        public void Decode(CodedInputStream stream)
        {
            Id = stream.ReadRawVarint64();
            Position = new(stream);

            Areas = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Areas.Length; i++)
                Areas[i] = stream.ReadRawVarint64();

            Cells = new ulong[stream.ReadRawVarint64()];
            for (int i = 0; i < Cells.Length; i++)
                Cells[i] = stream.ReadRawVarint64();

            Type = stream.ReadRawInt32();
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(Id);
            Position.Encode(stream);

            stream.WriteRawVarint64((ulong)Areas.Length);
            for (int i = 0; i < Areas.Length; i++)
                stream.WriteRawVarint64(Areas[i]);

            stream.WriteRawVarint64((ulong)Cells.Length);
            for (int i = 0; i < Cells.Length; i++)
                stream.WriteRawVarint64(Cells[i]);

            stream.WriteRawInt32(Type);
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"Id: {Id}");
            sb.AppendLine($"Position: {Position}");
            for (int i = 0; i < Areas.Length; i++) sb.AppendLine($"Area{i}: {Areas[i]}");
            for (int i = 0; i < Cells.Length; i++) sb.AppendLine($"Cell{i}: {Cells[i]}");
            sb.AppendLine($"Type: {Type}");
            return sb.ToString();
        }

        public void Connect(ObjectiveGraphNode node, float distance)
        {
            _connectionDict[node] = distance;
        }

        public void Disconnect(ObjectiveGraphNode node)
        {
            _connectionDict.Remove(node);
        }

        public IEnumerable<KeyValuePair<ObjectiveGraphNode, float>> IterateConnections()
        {
            foreach (var kvp in _connectionDict)
                yield return kvp;
        }

        public int CompareTo(ObjectiveGraphNode other)
        {
            return _shortestDistance.CompareTo(other._shortestDistance);
        }
    }
}
