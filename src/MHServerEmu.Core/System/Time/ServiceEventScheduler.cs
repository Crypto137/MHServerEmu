using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;

namespace MHServerEmu.Core.System.Time
{
    /// <summary>
    /// A timed event scheduler implementation intended to be used in game services where performance and precise timing is not as important.
    /// </summary>
    public class ServiceEventScheduler<THandle, TEventData>
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<THandle, ServiceEvent> _events = new();

        public ServiceEventScheduler()
        {
        }

        public void TriggerEvents()
        {
            if (_events.Count == 0)
                return;

            TimeSpan now = Clock.UnixTime;

            List<ServiceEvent> events = ListPool<ServiceEvent>.Instance.Get();
            events.AddRange(_events.Values);

            foreach (ServiceEvent serviceEvent in events)
            {
                if (now < serviceEvent.FireTime)
                    continue;

                // Important: remove before triggering, because triggering can schedule another event with the same handle.
                _events.Remove(serviceEvent.Handle);
                serviceEvent.Trigger();
            }

            ListPool<ServiceEvent>.Instance.Return(events);
        }

        public bool ScheduleEvent(THandle handle, TimeSpan delay, Action<TEventData> callback, TEventData eventData = default)
        {
            if (callback == null) return Logger.WarnReturn(false, "ScheduleEvent(): callback == null");

            CancelEvent(handle);

            TimeSpan fireTime = Clock.UnixTime + delay;
            ServiceEvent @event = new(handle, fireTime, callback, eventData);
            _events.Add(handle, @event);

            return true;
        }

        public bool CancelEvent(THandle handle)
        {
            return _events.Remove(handle);
        }

        private readonly struct ServiceEvent(THandle handle, TimeSpan fireTime, Action<TEventData> callback, TEventData eventData)
        {
            public readonly THandle Handle = handle;
            public readonly TimeSpan FireTime = fireTime;
            public readonly Action<TEventData> Callback = callback;
            public readonly TEventData EventData = eventData;

            public void Trigger()
            {
                Callback(EventData);
            }
        }
    }
}
