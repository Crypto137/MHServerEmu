using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.Extensions
{
    public static class TimeExtensions
    {
        public static long CalcNumTimeQuantums(this TimeSpan timeSpan, TimeSpan quantumSize)
        {
            return Clock.CalcNumTimeQuantums(timeSpan, quantumSize);
        }
    }
}
