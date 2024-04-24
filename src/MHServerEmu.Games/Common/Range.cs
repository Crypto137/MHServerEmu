
namespace MHServerEmu.Games.Common
{
    public class Range<T> where T : IComparable<T>
    {
        public T Min { get; private set; }
        public T Max { get; private set; }

        public Range(T min, T max)
        {
            Min = min;
            Max = max;
        }

        public bool Intersects(Range<T> other)
        {
            return (Min.CompareTo(other.Max) <= 0) && (Max.CompareTo(other.Min) >= 0);
        }
    }
}
