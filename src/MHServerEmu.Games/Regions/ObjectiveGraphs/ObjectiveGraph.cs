using System.Text;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Regions.ObjectiveGraphs
{
    public class ObjectiveGraph : ISerialize
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Game _game;
        private readonly Region _region;

        private readonly List<ObjectiveGraphNode> _nodeList = new();

        public ObjectiveGraph(Game game, Region region)
        {
            _game = game;
            _region = region;
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                uint numNodes = (uint)_nodeList.Count;
                success &= Serializer.Transfer(archive, ref numNodes);

                // Node connection information is stored in temporary structs
                List<ObjectiveGraphConnection> connectionList = new();
                for (int i = 0; i < _nodeList.Count; i++)
                {
                    ObjectiveGraphNode node = _nodeList[i];
                    node.Serialize(archive);
                    uint index = (uint)i;
                    Serializer.Transfer(archive, ref index);
                    
                    foreach (var connection in node.IterateConnections())
                        connectionList.Add(new(node, connection.Key, connection.Value));
                }

                uint numConnections = (uint)connectionList.Count;
                Serializer.Transfer(archive, ref numConnections);
                foreach (ObjectiveGraphConnection connection in connectionList)
                {
                    uint nodeIndex0 = (uint)_nodeList.IndexOf(connection.Node0);
                    uint nodeIndex1 = (uint)_nodeList.IndexOf(connection.Node1);
                    float distance = connection.Distance;
                    Serializer.Transfer(archive, ref nodeIndex0);
                    Serializer.Transfer(archive, ref nodeIndex1);
                    Serializer.Transfer(archive, ref distance);
                }
            }
            else
            {
                DestroyGraph();

                uint numNodes = 0;
                success &= Serializer.Transfer(archive, ref numNodes);
                for (uint i = 0; i < numNodes; i++)
                {
                    ObjectiveGraphNode node = PushNode(archive);
                    if (node == null) Logger.Warn($"Serialize(): node == null");
                }

                uint numConnections = 0;
                success &= Serializer.Transfer(archive, ref numConnections);

                for (uint i = 0; i < numConnections; i++)
                {
                    uint nodeIndex0 = 0;
                    uint nodeIndex1 = 0;
                    float distance = 0f;

                    success &= Serializer.Transfer(archive, ref nodeIndex0);
                    success &= Serializer.Transfer(archive, ref nodeIndex1);
                    success &= Serializer.Transfer(archive, ref distance);

                    if (nodeIndex0 >= _nodeList.Count || _nodeList[(int)nodeIndex0] == null)
                    {
                        Logger.Warn($"Serialize(): Unpacked invalid index {nodeIndex0} for node0");
                        continue;
                    }

                    if (nodeIndex1 >= _nodeList.Count || _nodeList[(int)nodeIndex1] == null)
                    {
                        Logger.Warn($"Serialize(): Unpacked invalid index {nodeIndex1} for node1");
                        continue;
                    }

                    _nodeList[(int)nodeIndex0].Connect(_nodeList[(int)nodeIndex1], distance);
                    _nodeList[(int)nodeIndex1].Connect(_nodeList[(int)nodeIndex0], distance);
                }

                _nodeList.Sort();
            }

            return success;
        }

        public void Decode(CodedInputStream stream)
        {
            DestroyGraph();

            uint numNodes = stream.ReadRawVarint32();
            for (uint i = 0; i < numNodes; i++)
            {
                ObjectiveGraphNode node = OLD_PushNode(stream);
                if (node == null) Logger.Warn($"Decode(): node == null");
            }

            uint numConnections = stream.ReadRawVarint32();
            for (uint i = 0; i < numConnections; i++)
            {
                int nodeIndex0 = (int)stream.ReadRawVarint32();
                int nodeIndex1 = (int)stream.ReadRawVarint32();
                float distance = stream.ReadRawFloat();

                if (nodeIndex0 >= _nodeList.Count || _nodeList[nodeIndex0] == null)
                {
                    Logger.Warn($"Decode(): Invalid index {nodeIndex0} for node0");
                    continue;
                }

                if (nodeIndex1 >= _nodeList.Count || _nodeList[nodeIndex1] == null)
                {
                    Logger.Warn($"Decode(): Invalid index {nodeIndex1} for node1");
                    continue;
                }

                _nodeList[nodeIndex0].Connect(_nodeList[nodeIndex1], distance);
                _nodeList[nodeIndex1].Connect(_nodeList[nodeIndex0], distance);
            }

            _nodeList.Sort();
        }

        public void Encode(CodedOutputStream stream)
        {
            stream.WriteRawVarint32((uint)_nodeList.Count);

            // Node connection information is stored in temporary structs
            List<ObjectiveGraphConnection> connectionList = new();
            for (int i = 0; i < _nodeList.Count; i++)
            {
                ObjectiveGraphNode node = _nodeList[i];
                node.Encode(stream);
                stream.WriteRawVarint32((uint)i);

                foreach (var connection in node.IterateConnections())
                    connectionList.Add(new(node, connection.Key, connection.Value));
            }

            stream.WriteRawVarint32((uint)connectionList.Count);
            foreach (ObjectiveGraphConnection connection in connectionList)
            {
                uint nodeIndex0 = (uint)_nodeList.IndexOf(connection.Node0);
                uint nodeIndex1 = (uint)_nodeList.IndexOf(connection.Node1);
                stream.WriteRawVarint32(nodeIndex0);
                stream.WriteRawVarint32(nodeIndex1);
                stream.WriteRawFloat(connection.Distance);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            for (int i = 0; i < _nodeList.Count; i++)
                sb.AppendLine($"{nameof(_nodeList)}[{i}]: {_nodeList[i]}");
            
            return sb.ToString();
        }


        private ObjectiveGraphNode PushNode(Archive archive)
        {
            ObjectiveGraphNode node = new(_game, _region, 0, Vector3.Zero, ObjectiveGraphType.Invalid);
            node.Serialize(archive);

            // This method is called only during deserialization, and existing nodes are cleared before deserialization.
            // So rather than resizing an array, we are going to use a list and use the encoded index just for verification.
            _nodeList.Add(node);

            uint index = 0;
            Serializer.Transfer(archive, ref index);
            if (_nodeList.Count - 1 != (int)index)
                Logger.Warn($"PushNode(): Node index mismatch (expected {index}, actual {_nodeList.Count - 1})");

            // TODO: find and insert into the correct cell node

            return node;
        }

        private ObjectiveGraphNode OLD_PushNode(CodedInputStream stream)    // REMOVEME
        {
            ObjectiveGraphNode node = new(_game, _region, 0, Vector3.Zero, ObjectiveGraphType.Invalid);
            node.Decode(stream);

            // This method is called only during deserialization, and existing nodes are cleared before deserialization.
            // So rather than resizing an array, we are going to use a list and use the encoded index just for verification.
            _nodeList.Add(node);

            int index = (int)stream.ReadRawVarint32();
            if (_nodeList.Count - 1 != index)
                Logger.Warn($"PushNode(): Node index mismatch");

            // TODO: find and insert into the correct cell node

            return node;
        }

        private void DestroyGraph()
        {
            // if (_game == null) return;
            _nodeList.Clear();
        }
    }
}
