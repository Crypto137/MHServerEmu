using Gazillion;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Powers
{
    // Power data pipeline: PowerActivationSettings -> PowerApplication -> PowerPayload -> PowerResults

    public struct PowerActivationSettings
    {
        public ulong TargetEntityId = 0;
        public Vector3 TargetPosition = Vector3.Zero;
        public Vector3 UserPosition = Vector3.Zero;
        public Vector3 OriginalTargetPosition = Vector3.Zero;

        public float MovementSpeed = 0f;
        public TimeSpan MovementTime = TimeSpan.Zero;
        public uint PowerRandomSeed = 0;
        public ulong ItemSourceId = 0;
        public uint FXRandomSeed = 0;
        public PrototypeId TriggeringPowerRef = PrototypeId.Invalid;

        public PowerActivationSettingsFlags Flags = PowerActivationSettingsFlags.None;
        public TimeSpan VariableActivationTime = TimeSpan.Zero;
        public bool VariableActivationRelease = false;

        public readonly TimeSpan CreationTime = Game.Current != null ? Game.Current.CurrentTime : Clock.GameTime;

        public PowerResults PowerResults = null;
        public TimeSpan UnknownTimeSpan = TimeSpan.Zero;

        public PowerActivationSettings(ulong targetEntityId, Vector3 targetPosition, Vector3 userPosition)
        {
            TargetEntityId = targetEntityId;
            TargetPosition = targetPosition;
            UserPosition = userPosition;
        }

        public PowerActivationSettings(Vector3 userPosition)
        {
            UserPosition = userPosition;
        }

        public void ApplyProtobuf(NetMessageTryActivatePower tryActivatePower)
        {
            if (tryActivatePower.HasIdTargetEntity)
                TargetEntityId = tryActivatePower.IdTargetEntity;

            if (tryActivatePower.HasTargetPosition)
                TargetPosition = new(tryActivatePower.TargetPosition);

            if (tryActivatePower.HasMovementSpeed)
                MovementSpeed = tryActivatePower.MovementSpeed;

            if (tryActivatePower.HasMovementTimeMS)
                MovementTime = TimeSpan.FromMilliseconds(tryActivatePower.MovementTimeMS);

            if (tryActivatePower.HasPowerRandomSeed)
                PowerRandomSeed = tryActivatePower.PowerRandomSeed;

            if (tryActivatePower.HasItemSourceId)
                ItemSourceId = tryActivatePower.ItemSourceId;

            FXRandomSeed = tryActivatePower.FxRandomSeed;

            if (tryActivatePower.HasTriggeringPowerPrototypeId)
                TriggeringPowerRef = (PrototypeId)tryActivatePower.TriggeringPowerPrototypeId;
        }
    }
}
