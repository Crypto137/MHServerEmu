using System.Text.Json.Serialization;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Resource;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class CellConnectorMarkerPrototype : MarkerPrototype
    {
        [JsonPropertyOrder(2)]
        public Vector3 Extents { get; }

        public CellConnectorMarkerPrototype(BinaryReader reader)
        {
            ProtoNameHash = ResourcePrototypeHash.CellConnectorMarkerPrototype;

            Extents = reader.ReadVector3();

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }
    }
}
