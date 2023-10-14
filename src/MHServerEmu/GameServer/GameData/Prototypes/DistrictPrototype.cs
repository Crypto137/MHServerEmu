using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.GameData.Prototypes.Markers;

namespace MHServerEmu.GameServer.GameData.Prototypes
{
    public class DistrictPrototype
    {
        public uint Header { get; }
        public uint Version { get; }
        public uint ClassId { get; }
        public ResourceMarkerPrototype[] CellMarkerSet { get; }
        public MarkerPrototype[] MarkerSet { get; }                 // size is always 0 in all of our files
        public PathNodeSetPrototype[] PathCollection { get; }       // PathCollectionPrototype

        public DistrictPrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = reader.ReadUInt32();
                Version = reader.ReadUInt32();
                ClassId = reader.ReadUInt32();

                CellMarkerSet = new ResourceMarkerPrototype[reader.ReadUInt32()];
                for (int i = 0; i < CellMarkerSet.Length; i++)
                    CellMarkerSet[i] = (ResourceMarkerPrototype)ReadMarkerPrototype(reader);

                MarkerSet = new MarkerPrototype[reader.ReadUInt32()];
                for (int i = 0; i < MarkerSet.Length; i++)
                    MarkerSet[i] = ReadMarkerPrototype(reader);

                PathCollection = new PathNodeSetPrototype[reader.ReadUInt32()];
                for (int i = 0; i < PathCollection.Length; i++)
                    PathCollection[i] = new(reader);
            }
        }

        private MarkerPrototype ReadMarkerPrototype(BinaryReader reader)
        {
            MarkerPrototype markerPrototype;
            ResourcePrototypeHash hash = (ResourcePrototypeHash)reader.ReadUInt32();

            if (hash == ResourcePrototypeHash.ResourceMarkerPrototype)
                markerPrototype = new ResourceMarkerPrototype(reader);
            else
                throw new($"Unknown ResourcePrototypeHash {(uint)hash}");   // Throw an exception if there's a hash for a type we didn't expect

            return markerPrototype;
        }
    }

    public class PathNodeSetPrototype
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

    public class PathNodePrototype
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
