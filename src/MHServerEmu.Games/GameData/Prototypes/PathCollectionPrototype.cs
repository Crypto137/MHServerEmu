using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PathCollectionPrototype : Prototype
    {
        public PathNodeSetPrototype[] PathNodeSets { get; }

        public PathCollectionPrototype(BinaryReader reader)
        {
            PathNodeSets = new PathNodeSetPrototype[reader.ReadUInt32()];
            for (int i = 0; i < PathNodeSets.Length; i++)
                PathNodeSets[i] = new(reader);
        }
    }

    public class PathNodeSetPrototype : Prototype
    {
        public ushort Group { get; }
        public PathNodePrototype[] PathNodes { get; }
        public ushort NumNodes { get; }

        public PathNodeSetPrototype(BinaryReader reader)
        {
            var protoNameHash = (ResourcePrototypeHash)reader.ReadUInt32();
            Group = reader.ReadUInt16();

            PathNodes = new PathNodePrototype[reader.ReadUInt32()];
            for (int i = 0; i < PathNodes.Length; i++)
                PathNodes[i] = new(reader);

            NumNodes = reader.ReadUInt16();
        }
    }

    public class PathNodePrototype : Prototype
    {
        public Vector3 Position { get; }

        public PathNodePrototype(BinaryReader reader)
        {
            var protoNameHash = (ResourcePrototypeHash)reader.ReadUInt32();
            Position = reader.ReadVector3();
        }
    }
}
