using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class DistrictPrototype
    {
        public ResourceHeader Header { get; }
        public ResourceMarkerPrototype[] CellMarkerSet { get; }
        public MarkerPrototype[] MarkerSet { get; }                 // Size is always 0 in all of our files
        public PathCollectionPrototype PathCollection { get; }

        public DistrictPrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = new(reader);

                CellMarkerSet = new ResourceMarkerPrototype[reader.ReadUInt32()];
                for (int i = 0; i < CellMarkerSet.Length; i++)
                    CellMarkerSet[i] = (ResourceMarkerPrototype)ReadMarkerPrototype(reader);

                MarkerSet = new MarkerPrototype[reader.ReadUInt32()];
                for (int i = 0; i < MarkerSet.Length; i++)
                    MarkerSet[i] = ReadMarkerPrototype(reader);

                PathCollection = new(reader);
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
}
