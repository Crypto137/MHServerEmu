using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    // Base abstract class for PowerPayload and PowerResults
    public abstract class PowerEffectsPacket
    {
        public ulong PowerOwnerId { get; protected set; }
        public ulong UltimateOwnerId { get; protected set; }
        public ulong TargetId { get; protected set; }
        public Vector3 PowerOwnerPosition { get; protected set; }
        public PowerPrototype PowerPrototype { get; protected set; }

        public PropertyCollection Properties { get; } = new();

        // long - TimeSpan?
        // BitArray - keywords?
    }
}
