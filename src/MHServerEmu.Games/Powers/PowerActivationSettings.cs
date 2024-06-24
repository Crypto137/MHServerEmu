using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Powers
{
    public struct PowerActivationSettings
    {
        public ulong TargetEntityId;
        public Vector3 UserPosition;
        public Vector3 TargetPosition;

        public PowerActivationSettings(ulong targetEntityId, Vector3 userPosition, Vector3 targetPosition)
        {
            TargetEntityId = targetEntityId;
            UserPosition = userPosition;
            TargetPosition = targetPosition;
        }
    }
}
