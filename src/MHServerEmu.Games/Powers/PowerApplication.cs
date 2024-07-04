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

        public uint PowerRandomSeed { get; set; }
        public uint FXRandomSeed { get; set; }
        public ulong ItemSourceId { get; set; }

        public bool SkipRangeCheck { get; set; }
        public int BeamSweepVar { get; set; } = -1;
        public TimeSpan UnknownTimeSpan { get; set; } = TimeSpan.Zero;
    }
}
