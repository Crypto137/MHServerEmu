using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class RoadConnectionMarkerPrototype : MarkerPrototype
    {
        public Vector3 Extents { get; }

        public RoadConnectionMarkerPrototype(BinaryReader reader)
        {
            Extents = reader.ReadVector3();

            ReadMarker(reader);
        }
    }
}
