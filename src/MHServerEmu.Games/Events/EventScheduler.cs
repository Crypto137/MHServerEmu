using System.Diagnostics;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Metrics;

namespace MHServerEmu.Games.Events
{
    /// <summary>
    /// Manages <see cref="ScheduledEvent"/> instances.
    /// </summary>
    public class EventScheduler
    {
        private const int MaxEventsPerUpdate = 250000;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly ScheduledEventPool _eventPool = new();

        private readonly TimeSpan _quantumSize;

        // Windows are collections of buckets for upcoming frames
        private readonly long _numWindowBuckets;
        private readonly WindowBucket[] _windowBuckets;

        // Each millisecond in the frame has its own bucket implemented via an intrusive linked list
        private readonly long _numFrameBuckets;
        private readonly LinkedList<ScheduledEvent>[] _frameBuckets;

        private long _currentFrame;
        private bool _cancellingAllEvents = false;

        /// <summary>
        /// Time that advances as <see cref="ScheduledEvent"/> instances are fired.
        /// </summary>
        public TimeSpan CurrentTime { get; private set; }

        /// <summary>
        /// Constructs a new <see cref="EventScheduler"/> instance. 
        /// </summary>
        public EventScheduler(TimeSpan currentTime, TimeSpan quantumSize, int numWindowBuckets = 256)
        {
            CurrentTime = currentTime;
            _quantumSize = quantumSize;

            _numWindowBuckets = numWindowBuckets;
            _windowBuckets = new WindowBucket[_numWindowBuckets];
            for (int i = 0; i < _numWindowBuckets; i++)
                _windowBuckets[i] = new(_eventPool.GetList());

            _numFrameBuckets = (long)_quantumSize.TotalMilliseconds;
            _frameBuckets = new LinkedList<ScheduledEvent>[_numFrameBuckets];
            for (long i = 0; i < _numFrameBuckets; i++)
                _frameBuckets[i] = new();

            _currentFrame = currentTime.CalcNumTimeQuantums(_quantumSize);
        }

        /// <summary>
        /// Creates an instance of <typeparamref name="T"/> and links it to the provided <see cref="EventPointer{T}"/>.
        /// </summary>
        public bool ScheduleEvent<T>(EventPointer<T> eventPointer, TimeSpan timeOffset, EventGroup eventGroup = null) where T: ScheduledEvent, new()
        {
            if (eventPointer.IsValid) return Logger.WarnReturn(false, $"ScheduleEvent<{typeof(T).Name}>(): eventPointer.IsValid");

            if (_cancellingAllEvents)
                return false;

            T @event = ConstructAndScheduleEvent<T>(timeOffset);
            eventGroup?.Add(@event);
            eventPointer.Set(@event);

            return true;
        }

        /// <summary>
        /// Reschedules the <typeparamref name="T"/> instance linked to the provided <see cref="EventPointer{T}"/> to <see cref="CurrentTime"/> + timeOffset.
        /// </summary>
        public bool RescheduleEvent<T>(EventPointer<T> eventPointer, TimeSpan timeOffset) where T: ScheduledEvent
        {
            if (eventPointer.IsValid == false)
                return Logger.WarnReturn(false, $"RescheduleEvent<{typeof(T).Name}>: eventPointer.IsValid == false");

            if (_cancellingAllEvents)
                CancelEvent((T)eventPointer);
            else
                RescheduleEvent((T)eventPointer, timeOffset);

            return true;
        }

        /// <summary>
        /// Cancels the <typeparamref name="T"/> instance linked to the provided <see cref="EventPointer{T}"/>.
        /// </summary>
        public void CancelEvent<T>(EventPointer<T> eventPointer) where T: ScheduledEvent
        {
            if (eventPointer.IsValid)
                CancelEvent((T)eventPointer);
        }

        /// <summary>
        /// Cancels all <see cref="ScheduledEvent"/> instances managed by this <see cref="EventScheduler"/>.
        /// </summary>
        public void CancelAllEvents()
        {
            _cancellingAllEvents = true;

            // Cancel events scheduled for the current frame
            for (long i = 0; i < _numFrameBuckets; i++)
            {
                LinkedList<ScheduledEvent> eventList = _frameBuckets[i];
                while (eventList.PopFront(out ScheduledEvent @event))
                    CancelEvent(@event);
            }

            // Cancel window bucket events
            for (long i = 0; i < _numWindowBuckets; i++)
            {
                WindowBucket windowBucket = _windowBuckets[i];

                while (windowBucket.NextList.PopFront(out ScheduledEvent @event))
                    CancelEvent(@event);

                foreach (LinkedList<ScheduledEvent> futureList in windowBucket.FutureListDict.Values)
                {
                    while (futureList.PopFront(out ScheduledEvent @event))
                        CancelEvent(@event);

                    _eventPool.ReturnList(futureList);
                }

                windowBucket.FutureListDict.Clear();
            }

            _cancellingAllEvents = false;
        }

        /// <summary>
        /// Cancels all <see cref="ScheduledEvent"/> instances belonging to the provided <see cref="EventGroup"/>.
        /// </summary>
        public void CancelAllEvents(EventGroup eventGroup)
        {
            while (eventGroup.IsEmpty == false)
                CancelEvent(eventGroup.Front);
        }

        /// <summary>
        /// Cancels all <see cref="ScheduledEvent"/> instances belonging to the provided <see cref="EventGroup"/> that pass the provided filter.
        /// </summary>
        public void CancelEventsFiltered<T>(EventGroup eventGroup, in T filter) where T: struct, IScheduledEventFilter
        {
            List<ScheduledEvent> filteredList = ListPool<ScheduledEvent>.Instance.Get();

            foreach (ScheduledEvent @event in eventGroup)
            {
                if (filter.Filter(@event))
                    filteredList.Add(@event);
            }

            foreach (ScheduledEvent @event in filteredList)
                CancelEvent(@event);

            ListPool<ScheduledEvent>.Instance.Return(filteredList);
        }

        /// <summary>
        /// Triggers <see cref="ScheduledEvent"/> instances starting at <see cref="CurrentTime"/> and ending at the provided timestamp.
        /// </summary>
        public void TriggerEvents(TimeSpan updateEndTime)
        {
            // No time travel allowed
            if (CurrentTime > updateEndTime)
                return;

            int numEvents = 0;

            long startFrame = CurrentTime.CalcNumTimeQuantums(_quantumSize);
            long endFrame = updateEndTime.CalcNumTimeQuantums(_quantumSize);

            // Process all frames within our time window
            for (long currentFrame = startFrame; currentFrame <= endFrame; currentFrame++)
            {
                _currentFrame = currentFrame;

                // Process events for each millisecond of the frame covered by our time window
                long frameBucketStart = currentFrame == startFrame ? ((long)CurrentTime.TotalMilliseconds % _numFrameBuckets) : 0;
                long frameBucketEnd = currentFrame == endFrame ? ((long)updateEndTime.TotalMilliseconds % _numFrameBuckets) + 1 : _numFrameBuckets;

                for (long i = frameBucketStart; i < frameBucketEnd; i++)
                {
                    LinkedList<ScheduledEvent> eventList = _frameBuckets[i];
                    while (eventList.PopFront(out ScheduledEvent @event))
                    {
                        CurrentTime = @event.FireTime;

                        @event.EventGroupNode.Remove();
                        @event.InvalidatePointers();

                        TimeSpan referenceTime = _stopwatch.Elapsed;
                        @event.OnTriggered();
                        TimeSpan triggerTime = _stopwatch.Elapsed - referenceTime;

                        if (triggerTime >= _quantumSize)
                            Logger.Warn($"{@event.GetType().Name} took {(_stopwatch.Elapsed - referenceTime).TotalMilliseconds} ms");

                        if (++numEvents > MaxEventsPerUpdate)
                            throw new Exception($"Infinite loop detected in EventScheduler.");

                        _eventPool.Return(@event);
                    }
                }

                // Prepare the next frame
                if (currentFrame != endFrame)
                {
                    WindowBucket windowBucket = _windowBuckets[(currentFrame + 1) % _numWindowBuckets];

                    // Bucket sort events for the next frame
                    while (windowBucket.NextList.PopFront(out ScheduledEvent @event))
                    {
                        long frameBucketIndex = (long)@event.FireTime.TotalMilliseconds % _numFrameBuckets;
                        _frameBuckets[frameBucketIndex].AddLast(@event.ProcessListNode);
                    }

                    // Prepare events for the next time we reach this window bucket
                    if (windowBucket.FutureListDict.Remove(currentFrame + 1 + _numWindowBuckets, out LinkedList<ScheduledEvent> futureList))
                    {
                        (windowBucket.NextList, futureList) = (futureList, windowBucket.NextList);
                        _eventPool.ReturnList(futureList);
                    }

                    // Advance time to the beginning of the next frame
                    CurrentTime = (currentFrame + 1) * _quantumSize;
                }
            }

            // Advance to the end
            CurrentTime = updateEndTime;

            // Record metrics
            ulong gameId = Game.Current != null ? Game.Current.Id : 0;
            MetricsManager.Instance.RecordGamePerformanceMetric(gameId, GamePerformanceMetricEnum.ScheduledEventsPerUpdate, numEvents);
        }

        /// <summary>
        /// Returns a <see cref="string"/> representing the current state of the underlying <see cref="ScheduledEventPool"/>.
        /// </summary>
        public string GetPoolReportString()
        {
            return _eventPool.GetReportString();
        }

        /// <summary>
        /// Allocates and schedules an instance of <typeparamref name="T"/>.
        /// </summary>
        private T ConstructAndScheduleEvent<T>(TimeSpan timeOffset) where T : ScheduledEvent, new()
        {
            T @event = _eventPool.Get<T>();

            if (timeOffset < TimeSpan.Zero)
            {
                Logger.Warn($"ConstructEvent<{typeof(T).Name}>(): timeOffset < TimeSpan.Zero");
                timeOffset = TimeSpan.Zero;
            }

            @event.FireTime = CurrentTime + timeOffset;
            ScheduleEvent(@event);

            return @event;
        }

        /// <summary>
        /// Schedules the provided <see cref="ScheduledEvent"/> instance by bucket sorting it.
        /// </summary>
        private void ScheduleEvent(ScheduledEvent @event)
        {
            long fireTimeFrame = @event.FireTime.CalcNumTimeQuantums(_quantumSize);

            if (fireTimeFrame == _currentFrame)
            {
                // If this event falls into the current frame, put it straight into the bucket for the appropriate ms
                long frameBucketIndex = (long)@event.FireTime.TotalMilliseconds % _numFrameBuckets;
                _frameBuckets[frameBucketIndex].AddLast(@event.ProcessListNode);
            }
            else
            {
                WindowBucket windowBucket = _windowBuckets[fireTimeFrame % _numWindowBuckets];

                if ((fireTimeFrame - _currentFrame) <= _numWindowBuckets)
                {
                    // This event will be happening soon (the next time this window is reached)
                    windowBucket.NextList.AddLast(@event.ProcessListNode);
                }
                else
                {
                    // This event will not be happening within the current window range, put it away for now
                    if (windowBucket.FutureListDict.TryGetValue(fireTimeFrame, out LinkedList<ScheduledEvent> futureList) == false)
                    {
                        futureList = _eventPool.GetList();
                        windowBucket.FutureListDict.Add(fireTimeFrame, futureList);
                    }

                    futureList.AddLast(@event.ProcessListNode);
                }
            }
        }

        /// <summary>
        /// Cancels the provided <see cref="ScheduledEvent"/> instance and invalidates the linked <see cref="EventPointer{T}"/>.
        /// </summary>
        private void CancelEvent(ScheduledEvent @event)
        {
            @event.ProcessListNode.Remove();
            @event.EventGroupNode.Remove();
            @event.InvalidatePointers();
            @event.OnCancelled();

            _eventPool.Return(@event);
        }

        /// <summary>
        /// Reschedules the provided <see cref="ScheduledEvent"/> instance by bucket sorting it again using the provided new time offset.
        /// </summary>
        private bool RescheduleEvent(ScheduledEvent @event, TimeSpan timeOffset)
        {
            if (@event.ProcessListNode.List == null)
                return Logger.WarnReturn(false, $"RescheduleEvent(): Attempting to reschedule a {@event.GetType().Name} that is not currently scheduled");

            if (timeOffset < TimeSpan.Zero)
            {
                Logger.Warn($"RescheduleEvent(): timeOffset < TimeSpan.Zero for {@event.GetType().Name}");
                timeOffset = TimeSpan.Zero;
            }

            // Calculate frames for times before and after rescheduling and update fire time on the event
            TimeSpan fireTimeBefore = @event.FireTime;
            TimeSpan fireTimeAfter = CurrentTime + timeOffset;

            long fireTimeFrameBefore = fireTimeBefore.CalcNumTimeQuantums(_quantumSize);
            long fireTimeFrameAfter = fireTimeAfter.CalcNumTimeQuantums(_quantumSize);

            @event.FireTime = fireTimeAfter;

            // Resort this event into the appropriate bucket
            if (fireTimeFrameAfter == fireTimeFrameBefore)
            {
                // If we are rescheduling an event within the current frame, we need to redo the bucket sort for it.
                // In other cases it will be bucket sorted in OnTriggered() when the scheduler reaches the frame.
                if (fireTimeFrameAfter == _currentFrame)
                {
                    @event.ProcessListNode.Remove();
                    long frameBucketIndex = (long)@event.FireTime.TotalMilliseconds % _numFrameBuckets;
                    _frameBuckets[frameBucketIndex].AddLast(@event.ProcessListNode);
                }
            }
            else
            {
                @event.ProcessListNode.Remove();

                if (fireTimeFrameAfter == _currentFrame)
                {
                    // This event is being rescheduled from a window bucket into the current frame
                    long frameBucketIndex = (long)@event.FireTime.TotalMilliseconds % _numFrameBuckets;
                    _frameBuckets[frameBucketIndex].AddLast(@event.ProcessListNode);
                }
                else
                {
                    // This event is being rescheduled into a window bucket
                    WindowBucket windowBucket = _windowBuckets[fireTimeFrameAfter % _numWindowBuckets];

                    if ((fireTimeFrameAfter - _currentFrame) < _numWindowBuckets)
                    {
                        // This event will be happening soon (the next time this window is reached)
                        windowBucket.NextList.AddLast(@event.ProcessListNode);
                    }
                    else
                    {
                        // This event will not be happening within the current window range, put it away for now
                        if (windowBucket.FutureListDict.TryGetValue(fireTimeFrameAfter, out LinkedList<ScheduledEvent> futureList) == false)
                        {
                            futureList = _eventPool.GetList();
                            windowBucket.FutureListDict.Add(fireTimeFrameAfter, futureList);
                        }

                        futureList.AddLast(@event.ProcessListNode);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Container for <see cref="LinkedList{T}"/> instances holding bucket sorted events for upcoming frames.
        /// </summary>
        private class WindowBucket
        {
            public LinkedList<ScheduledEvent> NextList;
            public readonly Dictionary<long, LinkedList<ScheduledEvent>> FutureListDict = new();

            public WindowBucket(LinkedList<ScheduledEvent> nextList)
            {
                NextList = nextList;
            }
        }
    }
}
