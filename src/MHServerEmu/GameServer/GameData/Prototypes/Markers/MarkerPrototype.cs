using MHServerEmu.GameServer.Common;
using MHServerEmu.GameServer.GameData.Gpak;

namespace MHServerEmu.GameServer.GameData.Prototypes.Markers
{
    /// <summary>
    /// This is a parent class for all other MarkerPrototypes.
    /// </summary>
    public class MarkerPrototype
    {
        public ResourcePrototypeHash ProtoNameHash { get; protected set; }    // DJB hash of the class name
        public Vector3 Position { get; protected set; }
        public Vector3 Rotation { get; protected set; }
    }
}
