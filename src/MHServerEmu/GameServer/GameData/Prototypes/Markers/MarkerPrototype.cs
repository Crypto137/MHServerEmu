using MHServerEmu.GameServer.Common;

namespace MHServerEmu.GameServer.GameData.Prototypes.Markers
{
    public enum MarkerPrototypeHash : uint
    {
        CellConnector = 2901607432,
        DotCorner = 468664301,
        Entity = 3862899546,
        RoadConnection = 576407411
    }

    /// <summary>
    /// This is a parent class for all other MarkerPrototypes.
    /// </summary>
    public class MarkerPrototype
    {
        public MarkerPrototypeHash ProtoNameHash { get; protected set; }
        public Vector3 Position { get; protected set; }
        public Vector3 Rotation { get; protected set; }
    }
}
