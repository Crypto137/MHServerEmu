using MHServerEmu.Games.Events;

namespace MHServerEmu.Games.Behavior;
public class AIThinkEvent : ScheduledEvent
{
    public AIController OwnerController;

    public override void OnTriggered()
    {
        OwnerController?.Think();
    }
}
