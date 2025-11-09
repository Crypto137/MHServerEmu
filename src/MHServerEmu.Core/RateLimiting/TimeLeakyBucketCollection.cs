using System.Diagnostics;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.RateLimiting
{
    public class TimeLeakyBucketCollection<TKey>
    {
        // See here for reference: https://www.codeofhonor.com/blog/using-transaction-rate-limiting-to-improve-service-reliability

        private readonly Dictionary<TKey, TimeSpan> _dict = new();

        private readonly TimeSpan _cost;
        private readonly TimeSpan _maxCost;

        public TimeLeakyBucketCollection(TimeSpan cost, int burst)
        {
            _cost = cost;
            _maxCost = cost * burst;

            Debug.Assert(_cost < _maxCost);
        }

        public bool AddTime(TKey key)
        {
            _dict.TryGetValue(key, out TimeSpan time);

            TimeSpan currentTime = Clock.ElapsedTime;
            if (currentTime - time > TimeSpan.Zero)
                time = currentTime;

            TimeSpan newTime = time + _cost;
            if (newTime - currentTime >= _maxCost)
            {
                _dict[key] = time;
                return false;
            }

            _dict[key] = newTime;
            return true;
        }

        public void Reset()
        {
            _dict.Clear();
        }
    }
}
