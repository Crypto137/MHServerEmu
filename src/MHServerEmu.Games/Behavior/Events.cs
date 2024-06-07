using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;

namespace MHServerEmu.Games.Behavior
{
    public class AIThinkEvent : ScheduledEvent
    {
        public AIController OwnerController;

        public override bool OnTriggered()
        {
            OwnerController?.Think();
            return true;
        }
    }

    public struct EntityDeadGameEvent
    {
        public WorldEntity Defender;
    }

    public struct AIBroadcastBlackboardGameEvent
    {
        public WorldEntity Broadcaster;
        public BehaviorBlackboard Blackboard;

        public AIBroadcastBlackboardGameEvent(WorldEntity broadcaster, BehaviorBlackboard blackboard)
        {
            Broadcaster = broadcaster;
            Blackboard = blackboard;
        }
    }

    public struct PlayerInteractGameEvent
    {
        public Player Player;
        public WorldEntity InteractableObject;

        public PlayerInteractGameEvent(Player player, WorldEntity interactableObject)
        {
            Player = player;
            InteractableObject = interactableObject;
        }
    }

    public struct EntityAggroedGameEvent
    {
        public WorldEntity AggroEntity;
    }
}
