using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;

namespace MHServerEmu.Games.Behavior
{
    public class AIThinkEvent : ScheduledEvent
    {
        public AIController OwnerController;

        public override void OnTriggered()
        {
            OwnerController?.Think();
        }
    }

    public class EntityDeadGameEvent
    {
        public WorldEntity Defender;
    }
}
