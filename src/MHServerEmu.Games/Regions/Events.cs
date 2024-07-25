using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;

namespace MHServerEmu.Games.Regions
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

    public struct EntityCollisionEvent
    {
        public WorldEntity Who;
        public WorldEntity Whom;

        public EntityCollisionEvent(WorldEntity who, WorldEntity whom)
        {
            Who = who;
            Whom = whom;
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

    public struct EntityLeaveDormantGameEvent
    {
        public WorldEntity Entity;

        public EntityLeaveDormantGameEvent(WorldEntity entity)
        {
            Entity = entity;
        }
    }

    public struct EntityEnteredMissionHotspotGameEvent
    {
        public WorldEntity Target;
        public Hotspot Hotspot;

        public EntityEnteredMissionHotspotGameEvent(WorldEntity target, Hotspot hotspot)
        {
            Target = target;
            Hotspot = hotspot;
        }
    }

    public struct EntityLeftMissionHotspotGameEvent
    {
        public WorldEntity Target;
        public Hotspot Hotspot;

        public EntityLeftMissionHotspotGameEvent(WorldEntity target, Hotspot hotspot)
        {
            Target = target;
            Hotspot = hotspot;
        }
    }
}
