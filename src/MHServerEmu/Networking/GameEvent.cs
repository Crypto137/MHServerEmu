using Gazillion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHServerEmu.Networking
{
    public enum EventEnum
    {
        StartThrowing,
        EndThrowing
    }
    public class GameEvent
    {
        private DateTime creationTime;
        private TimeSpan lifetime;
        public FrontendClient Client { get; }
        public EventEnum Event { get; }
        public bool IsRunning { get; set; }
        public ulong Data { get; set; }

        public bool IsExpired()
        {
            return DateTime.Now.Subtract(creationTime) >= lifetime;
        }

        public GameEvent(FrontendClient client, EventEnum gameEvent, long lifetimeMs, ulong data)
        {
            Client = client;
            Event = gameEvent;
            creationTime = DateTime.Now;
            lifetime = TimeSpan.FromMilliseconds(lifetimeMs);
            Data = data;
            IsRunning = true;
        }
    }
}
