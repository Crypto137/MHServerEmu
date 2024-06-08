namespace MHServerEmu.Core.System.Time
{
    /// <summary>
    /// Represents game time that advances in fixed steps (quantums).
    /// </summary>
    public class FixedQuantumGameTime
    {
        private TimeSpan _quantumGameTime;
        private TimeSpan _quantumSize;

        /// <summary>
        /// Constructs a new <see cref="FixedQuantumGameTime"/> with the specified time step size. 
        /// </summary>
        public FixedQuantumGameTime(TimeSpan quantumSize)
        {
            _quantumGameTime = Clock.GameTime;
            SetQuantumSize(quantumSize);
        }

        /// <summary>
        /// Sets time step size for this <see cref="FixedQuantumGameTime"/>.
        /// </summary>
        public void SetQuantumSize(TimeSpan quantumSize)
        {
            _quantumSize = quantumSize;
            long numTimeQuantums = Clock.CalcNumTimeQuantums(_quantumGameTime, _quantumSize);
            _quantumGameTime = _quantumSize * numTimeQuantums;
        }

        /// <summary>
        /// Advances <see cref="FixedQuantumGameTime"/> to the most recent time step.
        /// </summary>
        public void UpdateToNow()
        {
            TimeSpan gameTime = Clock.GameTime;
            long numTimeQuantums = Clock.CalcNumTimeQuantums(gameTime - _quantumGameTime, _quantumSize);
            _quantumGameTime += _quantumSize * numTimeQuantums;
        }

        // Output to string as microseconds because in the client this class is microsecond-based.
        public override string ToString() => (_quantumGameTime.Ticks / 10).ToString();

        public static explicit operator TimeSpan(FixedQuantumGameTime fixedQuantumGameTime) => fixedQuantumGameTime._quantumGameTime;
    }
}
