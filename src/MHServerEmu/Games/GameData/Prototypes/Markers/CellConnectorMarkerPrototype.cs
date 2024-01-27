using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class CellConnectorMarkerPrototype : MarkerPrototype
    {
        public Vector3 Extents { get; }

        public CellConnectorMarkerPrototype(BinaryReader reader)
        {
            Extents = reader.ReadVector3();

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }
    }
}
