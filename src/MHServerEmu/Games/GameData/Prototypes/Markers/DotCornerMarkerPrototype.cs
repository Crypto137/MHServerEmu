using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class DotCornerMarkerPrototype : MarkerPrototype
    {
        public Vector3 Extents { get; }

        public DotCornerMarkerPrototype(BinaryReader reader)
        {
            ProtoNameHash = ResourcePrototypeHash.DotCornerMarkerPrototype;

            Extents = reader.ReadVector3();

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }
    }
}
