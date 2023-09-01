using System.Text.Json.Serialization;
using MHServerEmu.Common.Extensions;
using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.GameData.Gpak;

namespace MHServerEmu.GameServer.GameData.Prototypes.Markers
{
    public class DotCornerMarkerPrototype : MarkerPrototype
    {
        [JsonPropertyOrder(2)]
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
