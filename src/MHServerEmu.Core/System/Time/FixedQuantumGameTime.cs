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
            _quantumSize = quantumSize;
            long numTimeQuantums = Clock.CalcNumTimeQuantums(Clock.GameTime, _quantumSize);
            _quantumGameTime = _quantumSize * numTimeQuantums;
        }

        /// <summary>
        /// Sets time step size for this <see cref="FixedQuantumGameTime"/>.
        /// </summary>
        public void SetQuantumSize(TimeSpan quantumSize)
        {
            _quantumSize = quantumSize;
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

        /// <summary>
        /// Advances <see cref="FixedQuantumGameTime"/> one time step forward if it is behind. Returns <see langword="true"/> if time advanced.
        /// </summary>
        public bool UpdateNow()
        {
            TimeSpan gameTime = Clock.GameTime;

            long numSteps = Clock.CalcNumTimeQuantums(gameTime - _quantumGameTime, _quantumSize);
            if (numSteps == 0) return false;

            _quantumGameTime += _quantumSize;
            return true;
        }

        public int CompareTo(TimeSpan other)
        {
            return _quantumGameTime.CompareTo(other);
        }

        public override string ToString() => ((long)_quantumGameTime.TotalMilliseconds).ToString();
        public static explicit operator TimeSpan(FixedQuantumGameTime fixedQuantumGameTime) => fixedQuantumGameTime._quantumGameTime;
    }
}
