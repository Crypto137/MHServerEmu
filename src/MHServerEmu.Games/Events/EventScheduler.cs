using System.Diagnostics;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Metrics;

namespace MHServerEmu.Games.Events
{
    public class EventScheduler
    {
        private const int MaxEventsPerUpdate = 250000;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan EventTriggerTimeLogThreshold = TimeSpan.FromMilliseconds(5);

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        // TODO: Implement frame buckets
        private readonly HashSet<ScheduledEvent> _scheduledEvents = new();

        private TimeSpan _quantumSize;
        private long _currentFrame;
        private bool _cancellingAllEvents = false;

        public TimeSpan CurrentTime { get; private set; }

        public EventScheduler(TimeSpan currentTime, TimeSpan quantumSize, int numWindowBuckets = 256)
        {
            CurrentTime = currentTime;
            _quantumSize = quantumSize;

            _currentFrame = currentTime.CalcNumTimeQuantums(quantumSize);
        }

        public bool ScheduleEvent<T>(EventPointer<T> eventPointer, TimeSpan timeOffset, EventGroup eventGroup = null) where T: ScheduledEvent, new()
        {
            if (eventPointer.IsValid) return Logger.WarnReturn(false, $"ScheduleEvent<{typeof(T).Name}>(): eventPointer.IsValid");

            if (_cancellingAllEvents) return false;

            T @event = ConstructAndScheduleEvent<T>(timeOffset);
            eventGroup?.Add(@event);
            eventPointer.Set(@event);

            return true;
        }

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

        public void CancelEvent<T>(EventPointer<T> eventPointer) where T: ScheduledEvent
        {
            if (eventPointer.IsValid)
                CancelEvent((T)eventPointer);
        }

        public void CancelAllEvents()
        {
            _cancellingAllEvents = true;

            // TODO: Remove this when we have proper data structures to store scheduled events in
            Stack<ScheduledEvent> eventStack = new();

            foreach (ScheduledEvent @event in _scheduledEvents)
                eventStack.Push(@event);

            while (eventStack.Count > 0)
            {
                ScheduledEvent @event = eventStack.Pop();
                CancelEvent(@event);
            }

            _cancellingAllEvents = false;
        }

        public void CancelAllEvents(EventGroup eventGroup)
        {
            while (eventGroup.IsEmpty == false)
                CancelEvent(eventGroup.Front);
        }

        public void TriggerEvents(TimeSpan updateEndTime)
        {
            if (CurrentTime > updateEndTime) return;      // No time travel outside of frame

            int numEvents = 0;

            long startFrame = CurrentTime.CalcNumTimeQuantums(_quantumSize);
            long endFrame = updateEndTime.CalcNumTimeQuantums(_quantumSize);

            List<ScheduledEvent> frameEvents = ListPool<ScheduledEvent>.Instance.Get();

            // Process all frames that are within our time window
            for (long i = startFrame; i <= endFrame; i++)
            {
                _currentFrame = i;

                // TODO: Implement the real bucketing system
                TimeSpan frameEndTime = (_currentFrame + 1) * _quantumSize;

                // Process events for this frame
                TEMP_FillFrameBucket(frameEvents, frameEndTime);
                while (frameEvents.Count > 0)
                {
                    foreach (ScheduledEvent @event in frameEvents)
                    {
                        if (@event.IsValid == false) continue;          // skip cancelled events
                        if (@event.FireTime > frameEndTime) continue;   // skip rescheduled events

                        // It seems in the client time can roll back within the same frame, is this correct?
                        /*
                        if (CurrentTime > @event.FireTime)
                        {
                            TimeSpan rollback = CurrentTime - @event.FireTime;
                            if (rollback > _quantumSize)
                                Logger.Warn($"TriggerEvents(): Time rollback larger than quantum size (-{rollback.TotalMilliseconds} ms)");
                        }
                        */

                        CurrentTime = @event.FireTime;
                        _scheduledEvents.Remove(@event);
                        @event.EventGroupNode?.Remove();
                        @event.InvalidatePointers();

                        TimeSpan referenceTime = _stopwatch.Elapsed;
                        @event.OnTriggered();
                        TimeSpan triggerTime = _stopwatch.Elapsed - referenceTime;

                        if (triggerTime >= _quantumSize)
                            Logger.Warn($"{@event.GetType().Name} took {(_stopwatch.Elapsed - referenceTime).TotalMilliseconds} ms");

                        if (++numEvents > MaxEventsPerUpdate)
                            throw new Exception($"Infinite loop detected in EventScheduler.");
                    }

                    // See if any more events got scheduled for this frame
                    TEMP_FillFrameBucket(frameEvents, frameEndTime);
                }

                CurrentTime = frameEndTime;
            }

            // Record metrics
            ulong gameId = Game.Current != null ? Game.Current.Id : 0;
            MetricsManager.Instance.RecordGamePerformanceMetric(gameId, GamePerformanceMetricEnum.ScheduledEventsPerUpdate, numEvents);
            MetricsManager.Instance.RecordGamePerformanceMetric(gameId, GamePerformanceMetricEnum.EventSchedulerFramesPerUpdate, 1 + endFrame - startFrame);
            MetricsManager.Instance.RecordGamePerformanceMetric(gameId, GamePerformanceMetricEnum.RemainingScheduledEvents, _scheduledEvents.Count);

            //if (numEvents > 0)
            //    Logger.Trace($"Triggered {numEvents} event(s) in {1 + endFrame - startFrame} frame(s) ({_scheduledEvents.Count} more scheduled)");

            ListPool<ScheduledEvent>.Instance.Return(frameEvents);
        }

        public Dictionary<string, int> GetScheduledEventCounts()
        {
            Dictionary<string, int> countDict = new();

            foreach (var value in _scheduledEvents)
            {
                string eventName = value.GetType().Name;
                countDict.TryGetValue(eventName, out int count);
                count++;
                countDict[eventName] = count;
            }

            return countDict;
        }

        private T ConstructAndScheduleEvent<T>(TimeSpan timeOffset) where T : ScheduledEvent, new()
        {
            T @event = new();

            if (timeOffset < TimeSpan.Zero)
            {
                Logger.Warn($"ConstructEvent<{typeof(T).Name}>(): timeOffset < TimeSpan.Zero");
                timeOffset = TimeSpan.Zero;
            }

            @event.FireTime = CurrentTime + timeOffset;
            ScheduleEvent(@event);

            return @event;
        }

        private void ScheduleEvent(ScheduledEvent @event)
        {
            // Just add it to the event collection for now
            // TODO: Frame buckets
            _scheduledEvents.Add(@event);
        }

        private void CancelEvent(ScheduledEvent @event)
        {
            _scheduledEvents.Remove(@event);
            @event.EventGroupNode?.Remove();
            @event.InvalidatePointers();
            @event.OnCancelled();
        }

        private void RescheduleEvent(ScheduledEvent @event, TimeSpan timeOffset)
        {
            // TODO: Do the actual rescheduling
            if (timeOffset < TimeSpan.Zero)
            {
                Logger.Warn($"RescheduleEvent(): timeOffset < TimeSpan.Zero for {@event.GetType().Name}");
                timeOffset = TimeSpan.Zero;
            }

            @event.FireTime = CurrentTime + timeOffset;
        }

        private void TEMP_FillFrameBucket(List<ScheduledEvent> frameEvents, TimeSpan frameEndTime)
        {
            // A horribly inefficient temp helper method until we get proper bucketing working.
            // NOTE: Events need to be fired in the order they are scheduled for cases like powers that apply and end at the same time.
            // Where() + OrderBy() LINQ combo does not return reliable results, most likely because of deferred execution.
            frameEvents.Clear();
            foreach (ScheduledEvent @event in _scheduledEvents)
            {
                if (@event.FireTime <= frameEndTime)
                    frameEvents.Add(@event);
            }

            frameEvents.Sort(static (a, b) => a.SortOrder.CompareTo(b.SortOrder));
        }
    }
}
