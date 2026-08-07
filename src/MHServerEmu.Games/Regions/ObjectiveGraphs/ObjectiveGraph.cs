using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Serialization;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.Regions.ObjectiveGraphs
{
    public class ObjectiveGraph : ISerialize
    {
        private readonly Game _game;
        private readonly Region _region;

        private readonly List<ObjectiveGraphNode> _nodes = new();

        public ObjectiveGraph(Game game, Region region)
        {
            _game = game;
            _region = region;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            for (int i = 0; i < _nodes.Count; i++)
                sb.AppendLine($"{nameof(_nodes)}[{i}]: {_nodes[i]}");

            return sb.ToString();
        }

        public bool Serialize(Archive archive)
        {
            bool success = true;

            if (archive.IsPacking)
            {
                uint numNodes = (uint)_nodes.Count;
                success &= Serializer.Transfer(archive, ref numNodes);

                // Node connection information is stored in temporary structs
                using var connectionsHandle = ListPool<ObjectiveGraphConnection>.Get(out List<ObjectiveGraphConnection> connections);
                for (int i = 0; i < _nodes.Count; i++)
                {
                    ObjectiveGraphNode node = _nodes[i];
                    node.Serialize(archive);
                    uint index = (uint)i;
                    Serializer.Transfer(archive, ref index);
                    
                    foreach (var connection in node)
                        connections.Add(new(node, connection.Key, connection.Value));
                }

                uint numConnections = (uint)connections.Count;
                Serializer.Transfer(archive, ref numConnections);
                foreach (ObjectiveGraphConnection connection in connections)
                {
                    uint node0 = (uint)_nodes.IndexOf(connection.Node0);
                    uint node1 = (uint)_nodes.IndexOf(connection.Node1);
                    float distance = connection.Distance;
                    Serializer.Transfer(archive, ref node0);
                    Serializer.Transfer(archive, ref node1);
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
                    Verify.IsNotNull(node);
                }

                uint numConnections = 0;
                success &= Serializer.Transfer(archive, ref numConnections);

                for (uint i = 0; i < numConnections; i++)
                {
                    uint node0 = 0;
                    uint node1 = 0;
                    float distance = 0f;

                    success &= Serializer.Transfer(archive, ref node0);
                    success &= Serializer.Transfer(archive, ref node1);
                    success &= Serializer.Transfer(archive, ref distance);

                    if (!Verify.IsTrue(node0 < _nodes.Count && _nodes[(int)node0] != null))
                        continue;

                    if (!Verify.IsTrue(node1 < _nodes.Count && _nodes[(int)node1] != null))
                        continue;

                    _nodes[(int)node0].Connect(_nodes[(int)node1], distance);
                    _nodes[(int)node1].Connect(_nodes[(int)node0], distance);
                }

                _nodes.Sort();
            }

            return success;
        }

        private ObjectiveGraphNode PushNode(Archive archive)
        {
            ObjectiveGraphNode node = new(_game, _region, 0, Vector3.Zero, ObjectiveGraphType.Invalid);
            node.Serialize(archive);

            // This method is called only during deserialization, and existing nodes are cleared before deserialization.
            // So rather than resizing an array, we are going to use a list and use the encoded index just for validation.
            _nodes.Add(node);

            uint index = 0;
            Serializer.Transfer(archive, ref index);
            Verify.IsTrue(index == _nodes.Count - 1, $"Node index mismatch (expected {index}, actual {_nodes.Count - 1})");

            // TODO: find and insert into the correct cell node

            return node;
        }

        private void DestroyGraph()
        {
            if (!Verify.IsNotNull(_game)) return;
            _nodes.Clear();
        }
    }
}
