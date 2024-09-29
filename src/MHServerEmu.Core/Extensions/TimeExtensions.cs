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
        /// Calculates the average value of a collection of <see cref="TimeSpan"/>.
        /// </summary>
        public static TimeSpan ToAverage(this IEnumerable<TimeSpan> timeSpans)
        {
            TimeSpan total = TimeSpan.Zero;
            int count = 0;

            foreach (TimeSpan timeSpan in timeSpans)
            {
                total += timeSpan;
                count++;
            }

            if (count == 0)
                return TimeSpan.Zero;

            return total / count;
        }

        /// <summary>
        /// Calculates the median value of a collection of <see cref="TimeSpan"/>.
        /// </summary>
        public static TimeSpan ToMedian(this IEnumerable<TimeSpan> timeSpans)
        {
            List<TimeSpan> list = new(timeSpans);
            int count = list.Count;

            if (count == 0)
                return TimeSpan.Zero;

            list.Sort();
            return list[count / 2];
        }
    }
}
