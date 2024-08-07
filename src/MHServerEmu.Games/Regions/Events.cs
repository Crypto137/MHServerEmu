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

    public struct EntityEnteredWorldGameEvent
    {
        public WorldEntity Entity;

        public EntityEnteredWorldGameEvent(WorldEntity entity)
        {
            Entity = entity;
        }
    }

    public struct EntityExitedWorldGameEvent
    {
        public WorldEntity Entity;

        public EntityExitedWorldGameEvent(WorldEntity entity)
        {
            Entity = entity;
        }
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

    public struct PlayerEnteredAreaGameEvent
    {
        public Player Player;
        public PrototypeId AreaRef;

        public PlayerEnteredAreaGameEvent(Player player, PrototypeId areaRef)
        {
            Player = player;
            AreaRef = areaRef;
        }
    }

    public struct PlayerLeftAreaGameEvent
    {
        public Player Player;
        public PrototypeId AreaRef;

        public PlayerLeftAreaGameEvent(Player player, PrototypeId areaRef)
        {
            Player = player;
            AreaRef = areaRef;
        }
    }

    public struct PlayerBeginTravelToAreaGameEvent
    {
        public Player Player;
        public PrototypeId AreaRef;

        public PlayerBeginTravelToAreaGameEvent(Player player, PrototypeId areaRef)
        {
            Player = player;
            AreaRef = areaRef;
        }
    }

    public struct ActiveChapterChangedGameEvent
    {
        public Player Player;
        public PrototypeId ChapterRef;

        public ActiveChapterChangedGameEvent(Player player, PrototypeId chapterRef)
        {
            Player = player;
            ChapterRef = chapterRef;
        }
    }

    public struct PartySizeChangedGameEvent
    {
        public Player Player;
        public int PartySize;

        public PartySizeChangedGameEvent(Player player, int partySize)
        {
            Player = player;
            PartySize = partySize;
        }
    }

    public struct AvatarEnteredRegionGameEvent
    {
        public Player Player;
        public PrototypeId RegionRef;

        public AvatarEnteredRegionGameEvent(Player player, PrototypeId regionRef)
        {
            Player = player;
            RegionRef = regionRef;
        }
    }

    public struct PlayerEnteredRegionGameEvent
    {
        public Player Player;
        public PrototypeId RegionRef;

        public PlayerEnteredRegionGameEvent(Player player, PrototypeId regionRef)
        {
            Player = player;
            RegionRef = regionRef;
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

    public struct OpenMissionCompleteGameEvent
    {
        public PrototypeId MissionRef;

        public OpenMissionCompleteGameEvent(PrototypeId missionRef)
        {
            MissionRef = missionRef;
        }
    }

    public struct OpenMissionFailedGameEvent
    {
        public PrototypeId MissionRef;

        public OpenMissionFailedGameEvent(PrototypeId missionRef)
        {
            MissionRef = missionRef;
        }
    }

    public struct MissionObjectiveUpdatedGameEvent
    {
        public Player Player;
        public PrototypeId MissionRef;
        public long ObjectiveId;

        public MissionObjectiveUpdatedGameEvent(Player player, PrototypeId missionRef, long objectiveId)
        {
            Player = player;
            MissionRef = missionRef;
            ObjectiveId = objectiveId;
        }
    }

    public struct PlayerCompletedMissionObjectiveGameEvent
    {
        public Player Player;
        public PrototypeId MissionRef;
        public long ObjectiveId;
        public bool Participant;
        public bool Contributor;

        public PlayerCompletedMissionObjectiveGameEvent(Player player, PrototypeId missionRef, long objectiveId, 
            bool participant, bool contributor)
        {
            Player = player;
            MissionRef = missionRef;
            ObjectiveId = objectiveId;
            Participant = participant;
            Contributor = contributor;
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

    public struct EntityEnteredAreaGameEvent
    {
        public WorldEntity Entity;
        public Area Area;

        public EntityEnteredAreaGameEvent(WorldEntity entity, Area area)
        {
            Entity = entity;
            Area = area;
        }
    }

    public struct EntityLeftAreaGameEvent
    {
        public WorldEntity Entity;
        public Area Area;

        public EntityLeftAreaGameEvent(WorldEntity entity, Area area)
        {
            Entity = entity;
            Area = area;
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
