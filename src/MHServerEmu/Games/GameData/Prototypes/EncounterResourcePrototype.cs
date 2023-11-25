using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class EncounterResourcePrototype
    {
        public PrototypeGuid PopulationMarkerGuid { get; }
        public string ClientMap { get; }
        public MarkerSetPrototype MarkerSet { get; }
        public NaviPatchSourcePrototype NaviPatchSource { get; }

        public EncounterResourcePrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                ResourceHeader header = new(reader);

                PopulationMarkerGuid = (PrototypeGuid)reader.ReadUInt64();
                ClientMap = reader.ReadFixedString32();
                MarkerSet = new(reader);
                NaviPatchSource = new(reader);
            }
        }
    }
}
