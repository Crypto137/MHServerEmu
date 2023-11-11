using System.Text.Json.Serialization;
using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Resource;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class ResourceMarkerPrototype : MarkerPrototype
    {
        [JsonPropertyOrder(2)]
        public string Resource { get; }

        public ResourceMarkerPrototype(BinaryReader reader)
        {
            ProtoNameHash = ResourcePrototypeHash.ResourceMarkerPrototype;

            Resource = reader.ReadFixedString32();

            Position = reader.ReadVector3();
            Rotation = reader.ReadVector3();
        }
    }
}
