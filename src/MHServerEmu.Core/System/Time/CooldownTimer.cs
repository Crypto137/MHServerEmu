namespace MHServerEmu.Core.System.Time
{
    /// <summary>
    /// Checks if enough time has passed to perform an action.
    /// </summary>
    public struct CooldownTimer
    {
        private readonly TimeSpan _cooldown;
        private TimeSpan _lastTime;

        /// <summary>
        /// Constructs a new <see cref="CooldownTimer"/> for the specified <see cref="TimeSpan"/>.
        /// </summary>
        public CooldownTimer(TimeSpan cooldown)
        {
            _cooldown = cooldown;
            _lastTime = GetTime();
        }

        /// <summary>
        /// Returns <see langword="true"/> if enough time has passed since the last successful call of this function.
        /// </summary>
        public bool Check()
        {
            TimeSpan now = GetTime();

            if ((now - _lastTime) < _cooldown)
                return false;

            _lastTime = now;
            return true;
        }

        /// <summary>
        /// Returns a <see cref="TimeSpan"/> representing current time.
        /// </summary>
        private static TimeSpan GetTime()
        {
            return Clock.UnixTime;
        }
    }
}
