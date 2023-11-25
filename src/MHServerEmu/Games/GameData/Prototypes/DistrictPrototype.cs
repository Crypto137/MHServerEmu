using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class DistrictPrototype
    {
        public MarkerSetPrototype CellMarkerSet { get; }
        public MarkerSetPrototype MarkerSet { get; }                 // Size is always 0 in all of our files
        public PathCollectionPrototype PathCollection { get; }

        public DistrictPrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                ResourceHeader header = new(reader);

                CellMarkerSet = new(reader);
                MarkerSet = new(reader);
                PathCollection = new(reader);
            }
        }
    }
}
