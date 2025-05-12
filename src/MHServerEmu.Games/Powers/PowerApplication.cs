using MHServerEmu.Core.VectorMath;

namespace MHServerEmu.Games.Powers
{
    // Power data pipeline: PowerActivationSettings -> PowerApplication -> PowerPayload -> PowerResults
    // NOTE: This is a class and not a struct because a reference to it is passed to scheduled events
    public class PowerApplication
    {
        public ulong UserEntityId { get; set; }
        public Vector3 UserPosition { get; set; }
        public ulong TargetEntityId { get; set; }
        public Vector3 TargetPosition { get; set; }

        public float MovementSpeed { get; set; }
        public TimeSpan MovementTime { get; set; }
        public TimeSpan VariableActivationTime { get; set; }

        public int PowerRandomSeed { get; set; }
        public int FXRandomSeed { get; set; }
        public ulong ItemSourceId { get; set; }

        public bool SkipRangeCheck { get; set; }
        public int BeamSweepTick { get; set; } = -1;
        public TimeSpan UnknownTimeSpan { get; set; } = TimeSpan.Zero;

        public bool IsFree { get; set; }

        public PowerResults PowerResults { get; set; }

        public PowerApplication() { }

        public PowerApplication(PowerApplication other)
        {
            UserEntityId = other.UserEntityId;
            UserPosition = other.UserPosition;
            TargetEntityId = other.TargetEntityId;
            TargetPosition = other.TargetPosition;
            
            MovementSpeed = other.MovementSpeed;
            MovementTime = other.MovementTime;
            VariableActivationTime = other.VariableActivationTime;

            PowerRandomSeed = other.PowerRandomSeed;
            FXRandomSeed = other.FXRandomSeed;          // FXRandomSeed should probably be randomized for each application instead
            ItemSourceId = other.ItemSourceId;

            SkipRangeCheck = other.SkipRangeCheck;
            BeamSweepTick = other.BeamSweepTick;
            UnknownTimeSpan = other.UnknownTimeSpan;
        }
    }
}
