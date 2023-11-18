using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Prototypes.Markers;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class DistrictPrototype : Prototype
    {
        public uint Header { get; }
        public uint Version { get; }
        public uint ClassId { get; }
        public MarkerSetPrototype CellMarkerSet { get; }
        public MarkerSetPrototype MarkerSet { get; }                 // size is always 0 in all of our files
        public PathCollectionPrototype PathCollection { get; }      

        public DistrictPrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                Version = reader.ReadUInt32();
                ClassId = reader.ReadUInt32();

                CellMarkerSet = new(reader);
                MarkerSet = new(reader);
                PathCollection = new(reader);
            }
        }

    }
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
        public ResourcePrototypeHash ProtoNameHash { get; }
        public ushort Group { get; }
        public PathNodePrototype[] PathNodes { get; }
        public ushort NumNodes { get; }

        public PathNodeSetPrototype(BinaryReader reader)
        {
            ProtoNameHash = (ResourcePrototypeHash)reader.ReadUInt32();
            Group = reader.ReadUInt16();

            PathNodes = new PathNodePrototype[reader.ReadUInt32()];
            for (int i = 0; i < PathNodes.Length; i++)
                PathNodes[i] = new(reader);

            NumNodes = reader.ReadUInt16();
        }
    }

    public class PathNodePrototype : Prototype
    {
        public ResourcePrototypeHash ProtoNameHash { get; }
        public Vector3 Position { get; }

        public PathNodePrototype(BinaryReader reader)
        {
            ProtoNameHash = (ResourcePrototypeHash)reader.ReadUInt32();
            Position = reader.ReadVector3();
        }
    }
}
