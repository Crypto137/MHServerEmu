using System.Diagnostics;

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

        // DateTime.UtcNow is not precise enough for our needs, so we use it only as a reference point for our stopwatch.
        private static readonly DateTime _utcBase;
        private static readonly Stopwatch _utcStopwatch;

        static Clock()
        {
            _utcBase = System.DateTime.UtcNow;
            _utcStopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Returns a <see cref="System.DateTime"/> representing the current precise date and time, expressed as the Coordinated Universal Time (UTC).
        /// </summary>
        public static DateTime UtcNowPrecise { get => _utcBase.Add(_utcStopwatch.Elapsed); }

        /// <summary>
        /// Returns a <see cref="TimeSpan"/> representing the current calendar time (epoch Jan 01 1970 00:00:00 GMT+0000).
        /// </summary>
        public static TimeSpan DateTime { get => UtcNowPrecise - UnixEpoch; }

        /// <summary>
        /// Returns a <see cref="TimeSpan"/> representing the current game time (epoch Sep 22 2012 09:31:18 GMT+0000).
        /// </summary>
        public static TimeSpan GameTime { get => UtcNowPrecise - GameTimeEpoch; }

        /// <summary>
        /// Returns a <see cref="System.DateTime"/> corresponding to the provided millisecond calendar time timestamp.
        /// </summary>
        public static DateTime DateTimeMillisecondsToDateTime(long timestamp)
        {
            return UnixEpoch.AddMilliseconds(timestamp);
        }

        /// <summary>
        /// Returns a <see cref="System.DateTime"/> corresponding to the provided microsecond celandar time timestamp.
        /// </summary>
        public static DateTime DateTimeMicrosecondsToDateTime(long timestamp)
        {
            return UnixEpoch.AddTicks(timestamp * 10);
        }

        /// <summary>
        /// Returns a <see cref="System.DateTime"/> corresponding to the provided millisecond game time timestamp.
        /// </summary>
        public static DateTime GameTimeMillisecondsToDateTime(long timestamp)
        {
            return GameTimeEpoch.AddMilliseconds(timestamp);
        }

        /// <summary>
        /// Returns a <see cref="System.DateTime"/> corresponding to the provided microsecond game time timestamp.
        /// </summary>
        public static DateTime GameTimeMicrosecondsToDateTime(long timestamp)
        {
            return GameTimeEpoch.AddTicks(timestamp * 10);
        }
    }
}
