using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.GameData.Prototypes.Markers;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class EncounterPrototype
    {
        public ResourceHeader Header { get; }
        public ulong PopulationMarkerGuid { get; }
        public string ClientMap { get; }
        public MarkerPrototype[] MarkerSet { get; }
        public NaviPatchSourcePrototype NaviPatchSource { get; }

        public EncounterPrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = new(reader);
                PopulationMarkerGuid = reader.ReadUInt64();
                ClientMap = reader.ReadFixedString32();

                MarkerSet = new MarkerPrototype[reader.ReadUInt32()];
                for (int i = 0; i < MarkerSet.Length; i++)
                    MarkerSet[i] = ReadMarkerPrototype(reader);

                NaviPatchSource = new(reader);
            }
        }

        private MarkerPrototype ReadMarkerPrototype(BinaryReader reader)
        {
            MarkerPrototype markerPrototype;
            ResourcePrototypeHash hash = (ResourcePrototypeHash)reader.ReadUInt32();

            switch (hash)
            {
                case ResourcePrototypeHash.CellConnectorMarkerPrototype:
                    markerPrototype = new CellConnectorMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.DotCornerMarkerPrototype:
                    markerPrototype = new DotCornerMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.EntityMarkerPrototype:
                    markerPrototype = new EntityMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.RoadConnectionMarkerPrototype:
                    markerPrototype = new RoadConnectionMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.ResourceMarkerPrototype:
                    markerPrototype = new ResourceMarkerPrototype(reader);
                    break;
                case ResourcePrototypeHash.UnrealPropMarkerPrototype:
                    markerPrototype = new UnrealPropMarkerPrototype(reader);
                    break;
                default:
                    throw new($"Unknown ResourcePrototypeHash {(uint)hash}");   // Throw an exception if there's a hash for a type we didn't expect
            }

            return markerPrototype;
        }
    }
}
