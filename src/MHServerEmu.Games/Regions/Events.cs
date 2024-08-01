using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.Regions
{
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

    public struct EntitySetSimulatedGameEvent
    {
        public WorldEntity Entity;

        public EntitySetSimulatedGameEvent(WorldEntity entity)
        {
            Entity = entity;
        }
    }

    public struct EntitySetUnSimulatedGameEvent
    {
        public WorldEntity Entity;

        public EntitySetUnSimulatedGameEvent(WorldEntity entity)
        {
            Entity = entity;
        }
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

    public struct PlayerLeftRegionGameEvent
    {
        public Player Player;
        public PrototypeId RegionRef;

        public PlayerLeftRegionGameEvent(Player player, PrototypeId regionRef)
        {
            Player = player;
            RegionRef = regionRef;
        }
    }

    public struct PlayerCompletedMissionGameEvent
    {
        public Player Player;
        public PrototypeId MissionRef;
        public bool Participant;
        public bool Contributor;

        public PlayerCompletedMissionGameEvent(Player player, PrototypeId missionRef, bool participant, bool contributor)
        {
            Player = player;
            MissionRef = missionRef;
            Participant = participant;
            Contributor = contributor;
        }
    }

    public struct PlayerFailedMissionGameEvent
    {
        public Player Player;
        public PrototypeId MissionRef;
        public bool Participant;
        public bool Contributor;

        public PlayerFailedMissionGameEvent(Player player, PrototypeId missionRef, bool participant, bool contributor)
        {
            Player = player;
            MissionRef = missionRef;
            Participant = participant;
            Contributor = contributor;
        }
    }

    public struct EntityAggroedGameEvent
    {
        public WorldEntity AggroEntity;
    }

    public struct AreaCreatedGameEvent
    {
        public Area Area;

        public AreaCreatedGameEvent(Area area)
        {
            Area = area;
        }
    }

    public struct CellCreatedGameEvent
    {
        public Cell Cell;

        public CellCreatedGameEvent(Cell cell)
        {
            Cell = cell;
        }
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
