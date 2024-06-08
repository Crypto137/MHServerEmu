using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.Games.Events
{
    public class EventScheduler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // TODO: Fix multithreading issues with region generation and remove locks
        // TODO: Implement frame buckets
        private readonly HashSet<ScheduledEvent> _scheduledEvents = new();

        private bool _cancellingAllEvents = false;

        public TimeSpan CurrentTime { get; private set; }

        public EventScheduler(TimeSpan currentTime, TimeSpan quantumSize, int numBuckets = 256)
        {
            CurrentTime = currentTime;
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
            CancelEvent((T)eventPointer);
        }

        public void CancelAllEvents()
        {
            _cancellingAllEvents = true;

            lock (_scheduledEvents)
            {
                // TODO: Remove this when we have proper data structures to store scheduled events in
                Stack<ScheduledEvent> eventStack = new();

                foreach (ScheduledEvent @event in _scheduledEvents)
                    eventStack.Push(@event);

                while (eventStack.Count > 0)
                {
                    ScheduledEvent @event = eventStack.Pop();
                    CancelEvent(@event);
                }
            }

            _cancellingAllEvents = false;
        }

        public void CancelAllEvents(EventGroup eventGroup)
        {
            while (eventGroup.IsEmpty == false)
                CancelEvent(eventGroup.Front);
        }

        public void TriggerEvents(TimeSpan currentGameTime)
        {
            if (CurrentTime > currentGameTime) return;      // No time travel backwards in time here

            int numEvents = 0;

            lock (_scheduledEvents)
            {
                var frameEvents = _scheduledEvents.Where(@event => @event.FireTime <= CurrentTime).OrderBy(@event => @event.FireTime);

                foreach (ScheduledEvent @event in frameEvents)
                {
                    CurrentTime = @event.FireTime;
                    _scheduledEvents.Remove(@event);
                    @event.EventGroupNode?.Remove();
                    @event.InvalidatePointers();
                    @event.OnTriggered();
                    numEvents++;
                }
            }

            if (numEvents > 0) Logger.Trace($"Triggered {numEvents} event(s) ({_scheduledEvents.Count} more scheduled)");

            CurrentTime = currentGameTime;
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
            lock (_scheduledEvents) _scheduledEvents.Add(@event);
        }

        private void CancelEvent(ScheduledEvent @event)
        {
            lock (_scheduledEvents)
            {
                _scheduledEvents.Remove(@event);
                @event.EventGroupNode?.Remove();
                @event.InvalidatePointers();
                @event.OnCancelled();
            }
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
