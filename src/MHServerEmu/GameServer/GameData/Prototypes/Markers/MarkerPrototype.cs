using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.GameData.Prototypes.Markers
{
    /// <summary>
    /// Precalculated hashes for known marker prototypes.
    /// </summary>
    public enum MarkerPrototypeHash : uint
    {
        CellConnectorMarkerPrototype = 2901607432,
        DotCornerMarkerPrototype = 468664301,
        EntityMarkerPrototype = 3862899546,
        RoadConnectionMarkerPrototype = 576407411
    }

    /// <summary>
    /// This is a parent class for all other MarkerPrototypes.
    /// </summary>
    public class MarkerPrototype
    {
        public MarkerPrototypeHash ProtoNameHash { get; protected set; }    // DJB hash of the class name
        public Vector3 Position { get; protected set; }
        public Vector3 Rotation { get; protected set; }
    }
}
