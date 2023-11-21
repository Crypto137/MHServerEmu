using System.Text.Json.Serialization;
using MHServerEmu.Games.Common;

namespace MHServerEmu.Games.GameData.Prototypes.Markers
{
    /// <summary>
    /// This is a parent class for all other MarkerPrototypes.
    /// </summary>
    public class MarkerPrototype : Prototype
    {
        [JsonPropertyOrder(1), JsonConverter(typeof(JsonStringEnumConverter))]
        public ResourcePrototypeHash ProtoNameHash { get; protected set; }    // DJB hash of the class name
        [JsonPropertyOrder(15)]
        public Vector3 Position { get; protected set; }
        [JsonPropertyOrder(16)]
        public Vector3 Rotation { get; protected set; }
    }

    public class MarkerFilterPrototype : Prototype
    {
        public MarkerFilterPrototype() { }
    }

    public class MarkerSetPrototype : Prototype
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
