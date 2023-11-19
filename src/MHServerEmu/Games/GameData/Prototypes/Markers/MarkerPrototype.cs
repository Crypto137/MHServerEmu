using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    /// <summary>
    /// This is the parent class for all other MarkerPrototypes.
    /// </summary>
    public class MarkerPrototype
    {
        public ResourcePrototypeHash ProtoNameHash { get; protected set; }    // DJB hash of the class name
        public Vector3 Position { get; protected set; }
        public Vector3 Rotation { get; protected set; }
    }

    public class MarkerSetPrototype
    {
        public MarkerPrototype[] Markers { get; }

        public MarkerSetPrototype(BinaryReader reader)
        {
            Markers = new MarkerPrototype[reader.ReadInt32()];
            for (int i = 0; i < Markers.Length; i++)
                Markers[i] = ReadMarkerPrototype(reader);
        }

        private MarkerPrototype ReadMarkerPrototype(BinaryReader reader)
        {
            ResourcePrototypeHash hash = (ResourcePrototypeHash)reader.ReadUInt32();

            switch (hash)
            {
                case ResourcePrototypeHash.CellConnectorMarkerPrototype:
                    return new CellConnectorMarkerPrototype(reader);
                case ResourcePrototypeHash.DotCornerMarkerPrototype:
                    return new DotCornerMarkerPrototype(reader);
                case ResourcePrototypeHash.EntityMarkerPrototype:
                    return new EntityMarkerPrototype(reader);
                case ResourcePrototypeHash.RoadConnectionMarkerPrototype:
                    return new RoadConnectionMarkerPrototype(reader);
                case ResourcePrototypeHash.ResourceMarkerPrototype:
                    return new ResourceMarkerPrototype(reader);
                case ResourcePrototypeHash.UnrealPropMarkerPrototype:
                    return new UnrealPropMarkerPrototype(reader);
                default:
                    throw new($"Unknown ResourcePrototypeHash {(uint)hash}");   // Throw an exception if there's a hash for a type we didn't expect
            }
        }
    }
}
