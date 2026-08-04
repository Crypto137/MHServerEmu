using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Regions.ObjectiveGraphs
{
    public class ObjectiveGraphNode : IComparable<ObjectiveGraphNode>, ISerialize
    {
        private readonly Dictionary<ObjectiveGraphNode, float> _connections = new();

        private Game _game;
        private Region _region;

        private ulong _id;
        private Vector3 _position;
        private ObjectiveGraphType _type;

        // Note: the client uses SortedVectors here
        private List<ulong> _areas = new();
        private List<ulong> _cells = new();

        private float _shortestDistance = float.MaxValue;

        // replacement for pointer sort
        private static long _globalInstanceCount = 0;
        public long InstanceNumber { get; }

        public bool IsEntity { get => _id != 0; }

        public ObjectiveGraphNode(Game game, Region region, ulong id, Vector3 position, ObjectiveGraphType type)
        {
            InstanceNumber = _globalInstanceCount++;
            _game = game;
            _region = region;
            _id = id;
            _position = position;
            _type = type;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_id)}: {_id}");
            sb.AppendLine($"{nameof(_position)}: {_position}");

            for (int i = 0; i < _areas.Count; i++)
                sb.AppendLine($"{nameof(_areas)}[{i}]: {_areas[i]}");

            for (int i = 0; i < _cells.Count; i++)
                sb.AppendLine($"{nameof(_cells)}[{i}]: {_cells[i]}");

            sb.AppendLine($"{nameof(_type)}: {_type}");
            return sb.ToString();
        }

        public Dictionary<ObjectiveGraphNode, float>.Enumerator GetEnumerator()
        {
            return _connections.GetEnumerator();
        }

        public int CompareTo(ObjectiveGraphNode other)
        {
            return _shortestDistance.CompareTo(other._shortestDistance);
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _id);

            GetPosition(); // Update position
            success &= Serializer.Transfer(archive, ref _position);

            success &= Serializer.Transfer(archive, ref _areas);
            success &= Serializer.Transfer(archive, ref _cells);

            uint type = (uint)_type;
            success &= Serializer.Transfer(archive, ref type);
            _type = (ObjectiveGraphType)type;

            return success;
        }

        public Vector3 GetPosition()
        {
            if (IsEntity)
            {
                // TODO: update position from the world entity this node represents
            }

            return _position;
        }

        public void Connect(ObjectiveGraphNode node, float distance)
        {
            _connections[node] = distance;
        }

        public void Disconnect(ObjectiveGraphNode node)
        {
            _connections.Remove(node);
        }
    }
}
