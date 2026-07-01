using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class PathCollectionPrototype : Prototype, IBinaryResource
    {
        public PathNodeSetPrototype[] PathNodeSets { get; protected set; }

        //---

        public void Deserialize(BinaryReader reader)
        {
            PathNodeSets = BinaryResourceSerializer.ReadPrototypeContainer<PathNodeSetPrototype>(reader);
        }
    }

    public class PathNodeSetPrototype : Prototype, IBinaryResource
    {
        public short Group { get; protected set; }
        public PathNodePrototype[] PathNodes { get; protected set; }
        public short NumNodes { get; protected set; }

        //---

        public void Deserialize(BinaryReader reader)
        {
            Group = reader.ReadInt16();
            PathNodes = BinaryResourceSerializer.ReadPrototypeContainer<PathNodePrototype>(reader);
            NumNodes = reader.ReadInt16();
        }
    }

    public class PathNodePrototype : Prototype, IBinaryResource
    {
        public Vector3 Position { get; protected set; }

        //---

        public void Deserialize(BinaryReader reader)
        {
            Position = reader.Read<Vector3>();
        }
    }
}
