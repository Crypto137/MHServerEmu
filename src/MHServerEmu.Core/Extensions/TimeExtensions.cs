using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.Extensions
{
    public static class TimeExtensions
    {
        public static long CalcNumTimeQuantums(this TimeSpan timeSpan, TimeSpan quantumSize)
        {
            return Clock.CalcNumTimeQuantums(timeSpan, quantumSize);
        }

        /// <summary>
        /// Gets the value of the current <see cref="TimeSpan"/> structure expressed in whole microseconds.
        /// </summary>
        public static long TotalMicrosecondsInt(this TimeSpan timeSpan)
        {
            return timeSpan.Ticks / TimeSpan.TicksPerMicrosecond;
        }
    }
}
