using System.Text;
using Google.ProtocolBuffers;
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

        public void Decode(CodedInputStream stream)
        {
            _id = stream.ReadRawVarint64();
            _position = new(stream);

            _areaList.Clear();
            ulong numAreas = stream.ReadRawVarint64();
            for (ulong i = 0; i < numAreas; i++)
                _areaList.Add(stream.ReadRawVarint64());
            _areaList.Sort();  // The client uses a sorted vector, does this actually need to be sorted?

            _cellList.Clear();
            ulong numCells = stream.ReadRawVarint64();
            for (ulong i = 0; i < numCells; i++)
                _cellList.Add(stream.ReadRawVarint64());
            _cellList.Sort();  // See above ^

            _type = (ObjectiveGraphType)stream.ReadRawVarint32();
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint64(_id);
            _position.Encode(stream);

            stream.WriteRawVarint64((ulong)_areaList.Count);
            foreach (ulong area in _areaList)
                stream.WriteRawVarint64(area);

            stream.WriteRawVarint64((ulong)_cellList.Count);
            foreach (ulong cell in _cellList)
                stream.WriteRawVarint64(cell);

            stream.WriteRawVarint32((uint)_type);
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
