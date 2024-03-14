using MHServerEmu.Core.Extensions;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    public class ResourceMarkerPrototype : MarkerPrototype
    {
        public string Resource { get; }

        public ResourceMarkerPrototype(BinaryReader reader)
        {
            Resource = reader.ReadFixedString32();

            ReadMarker(reader);
        }
    }
}
