namespace MHServerEmu.Common
{
    /// <summary>
    /// Provides Gazillion time functionality.
    /// </summary>
    public static class Clock
    {
        private const long GameTimeEpochTimestamp = 1348306278045983;    // Sep 22 2012 09:31:18 GMT+0000, calculated from packet dumps

        private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime GameTimeEpoch = UnixEpoch.AddTicks(GameTimeEpochTimestamp * 10L);

        /// <summary>
        /// Returns the current calendar time (epoch at Jan 01 1970 00:00:00 GMT+0000) as a <see cref="TimeSpan"/>.
        /// </summary>
        public static TimeSpan GetDateTime()
        {
            return DateTime.UtcNow - UnixEpoch;
        }

        /// <summary>
        /// Returns the current game time (epoch at Sep 22 2012 09:31:18 GMT+0000) as a <see cref="TimeSpan"/>.
        /// </summary>
        public static TimeSpan GetGameTime()
        {
            return DateTime.UtcNow - GameTimeEpoch;
        }

        /// <summary>
        /// Returns <see cref="DateTime"/> corresponding to the provided millisecond game time timestamp.
        /// </summary>
        public static DateTime GameTimeMillisecondsToDateTime(long timestamp)
        {
            return GameTimeEpoch.AddMilliseconds(timestamp);
        }

        /// <summary>
        /// Returns <see cref="DateTime"/> corresponding to the provided microsecond game time timestamp.
        /// </summary>
        public static DateTime GameTimeMicrosecondsToDateTime(long timestamp)
        {
            return GameTimeEpoch.AddTicks(timestamp * 10);
        }
    }
}
