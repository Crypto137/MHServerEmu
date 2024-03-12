using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class DotCornerMarkerPrototype : MarkerPrototype
    {
        public Vector3 Extents { get; }

        public DotCornerMarkerPrototype(BinaryReader reader)
        {
            Extents = reader.ReadVector3();

            ReadMarker(reader);
        }
    }
}
