using System.Diagnostics;

namespace MHServerEmu.Core.System.Time
{
    /// <summary>
    /// Provides Gazillion time functionality.
    /// </summary>
    public static class Clock
    {
        // System.DateTime in C# represents time from Jan 01 0001 to Dec 31 9999.
        // The game uses two kinds of time: DateTime (calendar time) and GameTime.
        // DateTime is Unix time (the number of microseconds elapsed since Jan 01 1970).
        // GameTime is the number of microseconds elapsed since Sep 22 2012.
        // We represent DateTime and GameTime as TimeSpan values.

        private const long GameTimeEpochTimestamp = 1348306278045983;    // Sep 22 2012 09:31:18 GMT+0000, calculated from packet dumps

        private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime GameTimeEpoch = UnixEpoch.AddTicks(GameTimeEpochTimestamp * 10L);

        // DateTime.UtcNow is not precise enough for our needs, so we use it only as a reference point for our stopwatch.
        private static readonly DateTime _utcBase;
        private static readonly Stopwatch _utcStopwatch;

        static Clock()
        {
            _utcBase = DateTime.UtcNow;
            _utcStopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Returns a <see cref="DateTime"/> representing the current precise date and time, expressed as the Coordinated Universal Time (UTC).
        /// </summary>
        public static DateTime UtcNowPrecise { get => _utcBase.Add(_utcStopwatch.Elapsed); }

        /// <summary>
        /// Returns a <see cref="TimeSpan"/> representing the current calendar Unix time (epoch Jan 01 1970 00:00:00 GMT+0000).
        /// </summary>
        public static TimeSpan UnixTime { get => UtcNowPrecise - UnixEpoch; }

        /// <summary>
        /// Returns a <see cref="TimeSpan"/> representing the current game time (epoch Sep 22 2012 09:31:18 GMT+0000).
        /// </summary>
        public static TimeSpan GameTime { get => UtcNowPrecise - GameTimeEpoch; }

        /// <summary>
        /// Returns a <see cref="TimeSpan"/> representing the amount of time since <see cref="Clock"/> was initialized.
        /// </summary>
        public static TimeSpan ElapsedTime { get => _utcStopwatch.Elapsed; }

        /// <summary>
        /// Returns the number of steps in a <see cref="TimeSpan"/> given the provided step size.
        /// </summary>
        public static long CalcNumTimeQuantums(TimeSpan time, TimeSpan quantumSize)
        {
            return time.Ticks / quantumSize.Ticks;
        }

        #region Conversion

        /// <summary>
        /// Returns a <see cref="DateTime"/> corresponding to the provided millisecond Unix time timestamp.
        /// </summary>
        public static DateTime UnixTimeMillisecondsToDateTime(long timestamp)
        {
            return UnixEpoch.AddMilliseconds(timestamp);
        }

        /// <summary>
        /// Returns a <see cref="DateTime"/> corresponding to the provided microsecond Unix time timestamp.
        /// </summary>
        public static DateTime UnixTimeMicrosecondsToDateTime(long timestamp)
        {
            return UnixEpoch.AddTicks(timestamp * 10);
        }

        /// <summary>
        /// Returns a <see cref="DateTime"/> corresponding to the provided millisecond game time timestamp.
        /// </summary>
        public static DateTime GameTimeMillisecondsToDateTime(long timestamp)
        {
            return GameTimeEpoch.AddMilliseconds(timestamp);
        }

        /// <summary>
        /// Returns a <see cref="DateTime"/> corresponding to the provided microsecond game time timestamp.
        /// </summary>
        public static DateTime GameTimeMicrosecondsToDateTime(long timestamp)
        {
            return GameTimeEpoch.AddTicks(timestamp * 10);
        }

        /// <summary>
        /// Converts the provided <see cref="DateTime"/> value to a <see cref="TimeSpan"/> representing Unix time.
        /// </summary>
        public static TimeSpan DateTimeToUnixTime(DateTime dateTime)
        {
            return dateTime - UnixEpoch;
        }

        /// <summary>
        /// Converts the provided <see cref="DateTime"/> value to a <see cref="TimeSpan"/> representing game time.
        /// </summary>
        public static TimeSpan DateTimeToGameTime(DateTime dateTime)
        {
            return dateTime - GameTimeEpoch;
        }

        /// <summary>
        /// Converts the provided <see cref="TimeSpan"/> value representing Unix time to <see cref="DateTime"/>.
        /// </summary>
        public static DateTime UnixTimeToDateTime(TimeSpan timeSpan)
        {
            return UnixEpoch.Add(timeSpan);
        }

        /// <summary>
        /// Converts the provided <see cref="TimeSpan"/> value representing game time to <see cref="DateTime"/>.
        /// </summary>
        public static DateTime GameTimeToDateTime(TimeSpan timeSpan)
        {
            return GameTimeEpoch.Add(timeSpan);
        }

        #endregion

        public static TimeSpan Max(TimeSpan time1, TimeSpan time2)
        {
            return (time1 > time2) ? time1 : time2;
        }
        public static TimeSpan Min(TimeSpan time1, TimeSpan time2)
        {
            return (time1 < time2) ? time1 : time2;
        }
    }
}
