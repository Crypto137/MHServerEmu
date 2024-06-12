using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    public class EventScheduler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

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

            // Process all frames that are within our time window
            for (long i = startFrame; i <= endFrame; i++)
            {
                _currentFrame = i;

                // TODO: Replace linq with buckets
                TimeSpan frameEndTime = (_currentFrame + 1) * _quantumSize;

                // Process events for this frame
                var frameEvents = _scheduledEvents.Where(@event => @event.FireTime <= frameEndTime).OrderBy(@event => @event.FireTime);
                while (frameEvents.Any())
                {
                    foreach (ScheduledEvent @event in frameEvents)
                    {
                        // It seems in the client time can roll back within the same frame, is this correct?
                        if (CurrentTime > @event.FireTime)
                            Logger.Debug($"TriggerEvents(): Time rollback (-{(CurrentTime - @event.FireTime).TotalMilliseconds} ms)");

                        CurrentTime = @event.FireTime;
                        _scheduledEvents.Remove(@event);
                        @event.EventGroupNode?.Remove();
                        @event.InvalidatePointers();
                        @event.OnTriggered();
                        numEvents++;
                    }

                    // See if any more events got scheduled for this frame
                    frameEvents = _scheduledEvents.Where(@event => @event.FireTime <= frameEndTime).OrderBy(@event => @event.FireTime);
                }
            }

            if (numEvents > 0) Logger.Trace($"Triggered {numEvents} event(s) in {endFrame - startFrame} frame(s) ({_scheduledEvents.Count} more scheduled)");

            CurrentTime = updateEndTime;
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
                Logger.Warn($"RescheduleEvent(): timeOffset < TimeSpan.Zero");
                timeOffset = TimeSpan.Zero;
            }

            @event.FireTime = CurrentTime + timeOffset;
        }
    }
}
