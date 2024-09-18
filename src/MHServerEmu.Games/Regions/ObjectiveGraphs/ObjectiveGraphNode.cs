using System.Text;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Regions.ObjectiveGraphs
{
    public class ObjectiveGraphNode : IComparable<ObjectiveGraphNode>, ISerialize
    {
        private readonly Dictionary<ObjectiveGraphNode, float> _connectionDict = new();

        private Game _game;
        private Region _region;

        private ulong _id;
        private Vector3 _position;
        private ObjectiveGraphType _type;

        // Note: the client uses SortedVectors here
        private List<ulong> _areaList = new();
        private List<ulong> _cellList = new();

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

        public bool Serialize(Archive archive)
        {
            bool success = true;

            success &= Serializer.Transfer(archive, ref _id);

            GetPosition(); // Update position
            success &= Serializer.Transfer(archive, ref _position);

            success &= Serializer.Transfer(archive, ref _areaList);
            success &= Serializer.Transfer(archive, ref _cellList);

            uint type = (uint)_type;
            success &= Serializer.Transfer(archive, ref type);
            _type = (ObjectiveGraphType)type;

            return success;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"{nameof(_id)}: {_id}");
            sb.AppendLine($"{nameof(_position)}: {_position}");

            for (int i = 0; i < _areaList.Count; i++)
                sb.AppendLine($"{nameof(_areaList)}[{i}]: {_areaList[i]}");

            for (int i = 0; i < _cellList.Count; i++)
                sb.AppendLine($"{nameof(_cellList)}[{i}]: {_cellList[i]}");

            sb.AppendLine($"{nameof(_type)}: {_type}");
            return sb.ToString();
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
