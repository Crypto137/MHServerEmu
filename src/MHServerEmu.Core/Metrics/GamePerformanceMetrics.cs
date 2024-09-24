using MHServerEmu.Core.Collections;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.Metrics
{
    public class GamePerformanceMetrics
    {
        private readonly CircularBuffer<TimeSpan> _fixedUpdateTimeBuffer = new(1024);    // At 20 FPS this gives us about 51.2 seconds of data

        public ulong GameId { get; }

        public TimeSpan MinFixedUpdateTime { get; private set; } = TimeSpan.MaxValue;
        public TimeSpan MaxFixedUpdateTime { get; private set; } = TimeSpan.MinValue;

        public GamePerformanceMetrics(ulong gameId)
        {
            GameId = gameId;
        }

        public TimeSpan CalculateAverageFixedUpdateTime()
        {
            lock (_fixedUpdateTimeBuffer)
            {
                if (_fixedUpdateTimeBuffer.Count == 0) return TimeSpan.Zero;

                TimeSpan average = TimeSpan.Zero;

                foreach (TimeSpan timeSpan in _fixedUpdateTimeBuffer)
                    average += timeSpan;

                average /= _fixedUpdateTimeBuffer.Count;
                return average;
            }
        }

        public TimeSpan CalculateMedianFixedUpdateTime()
        {
            lock (_fixedUpdateTimeBuffer)
            {
                if (_fixedUpdateTimeBuffer.Count == 0) return TimeSpan.Zero;

                List<TimeSpan> list = new(_fixedUpdateTimeBuffer.Count);

                foreach (TimeSpan timeSpan in _fixedUpdateTimeBuffer)
                    list.Add(timeSpan);

                list.Sort();
                return list[list.Count / 2];
            }
        }

        public void RecordFixedUpdateTime(TimeSpan processTime)
        {
            lock (_fixedUpdateTimeBuffer)
            {
                _fixedUpdateTimeBuffer.Add(processTime);

                MinFixedUpdateTime = Clock.Min(MinFixedUpdateTime, processTime);
                MaxFixedUpdateTime = Clock.Max(MaxFixedUpdateTime, processTime);
            }
        }

        public Report GetReport()
        {
            return new(this);
        }

        public readonly struct Report
        {
            public float Min { get; }
            public float Max { get; }
            public float Average { get; }
            public float Median { get; }

            public Report(GamePerformanceMetrics metrics)
            {
                Min = (float)metrics.MinFixedUpdateTime.TotalMilliseconds;
                Max = (float)metrics.MaxFixedUpdateTime.TotalMilliseconds;
                Average = (float)metrics.CalculateAverageFixedUpdateTime().TotalMilliseconds;
                Median = (float)metrics.CalculateMedianFixedUpdateTime().TotalMilliseconds;
            }

            public override string ToString()
            {
                return $"min={Min} ms, max={Max} ms, avg={Average} ms, mdn={Median} ms";
            }
        }
    }
}
