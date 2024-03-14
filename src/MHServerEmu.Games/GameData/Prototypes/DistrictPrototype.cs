using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class DistrictPrototype : Prototype, IBinaryResource
    {
        public MarkerSetPrototype CellMarkerSet { get; private set; }
        public MarkerSetPrototype MarkerSet { get; private set; }                 // Size is always 0 in all of our files
        public PathCollectionPrototype PathCollection { get; private set; }

        public void Deserialize(BinaryReader reader)
        {
            CellMarkerSet = new(reader);
            MarkerSet = new(reader);
            PathCollection = new(reader);
        }
    }
}
