using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class EncounterResourcePrototype : Prototype, IBinaryResource
    {
        public PrototypeGuid PopulationMarkerGuid { get; private set; }
        public string ClientMap { get; private set; }
        public MarkerSetPrototype MarkerSet { get; private set; }
        public NaviPatchSourcePrototype NaviPatchSource { get; private set; }

        public void Deserialize(BinaryReader reader)
        {
            PopulationMarkerGuid = (PrototypeGuid)reader.ReadUInt64();
            ClientMap = reader.ReadFixedString32();

            MarkerSet = new(reader);
            NaviPatchSource = new(reader);
        }
    }
}
