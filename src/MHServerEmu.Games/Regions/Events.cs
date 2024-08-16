using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Populations;

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

    public struct ClusterEnemiesClearedGameEvent
    {
        public SpawnGroup SpawnGroup;
        public WorldEntity Killer;

        public ClusterEnemiesClearedGameEvent(SpawnGroup spawnGroup, WorldEntity killer)
        {
            SpawnGroup = spawnGroup;
            Killer = killer;
        }
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

    public struct PlayerDonatedItemGameEvent
    {
        public Player Player;
        public Item Item;
        public int Count;

        public PlayerDonatedItemGameEvent(Player player, Item item, int count)
        {
            Player = player;
            Item = item;
            Count = count;
        }
    }

    public struct PlayerCraftedItemGameEvent
    {
        public Player Player;
        public Item Item;
        public PrototypeId RecipeRef;
        public int Count;

        public PlayerCraftedItemGameEvent(Player player, Item item, PrototypeId recipeRef, int count)
        {
            Player = player;
            Item = item;
            RecipeRef = recipeRef;
            Count = count;
        }
    }

    public struct PlayerBoughtItemGameEvent
    {
        public Player Player;
        public Item Item;
        public int Count;

        public PlayerBoughtItemGameEvent(Player player, Item item, int count)
        {
            Player = player;
            Item = item;
            Count = count;
        }
    }

    public struct PlayerCollectedItemGameEvent
    {
        public Player Player;
        public Item Item;
        public int Count;

        public PlayerCollectedItemGameEvent(Player player, Item item, int count)
        {
            Player = player;
            Item = item;
            Count = count;
        }
    }

    public struct PlayerLostItemGameEvent
    {
        public Player Player;
        public Item Item;
        public int Count;

        public PlayerLostItemGameEvent(Player player, Item item, int count)
        {
            Player = player;
            Item = item;
            Count = count;
        }
    }

    public struct PlayerPreItemPickupGameEvent
    {
        public Player Player;
        public Item Item;

        public PlayerPreItemPickupGameEvent(Player player, Item item)
        {
            Player = player;
            Item = item;
        }
    }

    public struct PlayerEquippedItemGameEvent
    {
        public Player Player;
        public Item Item;

        public PlayerEquippedItemGameEvent(Player player, Item item)
        {
            Player = player;
            Item = item;
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

    public struct EmotePerformedGameEvent
    {
        public Player Player;
        public PrototypeId EmotePowerRef;

        public EmotePerformedGameEvent(Player player, PrototypeId emotePowerRef)
        {
            Player = player;
            EmotePowerRef = emotePowerRef;
        }
    }

    public struct LoadingScreenFinishedGameEvent
    {
        public Player Player;
        public PrototypeId RegionRef;

        public LoadingScreenFinishedGameEvent(Player player, PrototypeId regionRef)
        {
            Player = player;
            RegionRef = regionRef;
        }
    }

    public struct CinematicFinishedGameEvent
    {
        public Player Player;
        public PrototypeId MovieRef;

        public CinematicFinishedGameEvent(Player player, PrototypeId movieRef)
        {
            Player = player;
            MovieRef = movieRef;
        }
    }

    public struct AvatarUsedPowerGameEvent
    {
        public Player Player;
        public Avatar Avatar;
        public PrototypeId PowerRef;
        public ulong TargetEntityId;

        public AvatarUsedPowerGameEvent(Player player, Avatar avatar, PrototypeId powerRef, ulong targetEntityId)
        {
            Player = player;
            Avatar = avatar;
            PowerRef = powerRef;
            TargetEntityId = targetEntityId;
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

    public struct CurrencyCollectedGameEvent
    {
        public Player Player;
        public PrototypeId CurrencyType;
        public int Amount;

        public CurrencyCollectedGameEvent(Player player, PrototypeId currency, int amount)
        {
            Player = player;
            CurrencyType = currency;
            Amount = amount;
        }
    }

    public struct AvatarLeveledUpGameEvent
    {
        public Player Player;
        public PrototypeId AvatarRef;
        public int Level;

        public AvatarLeveledUpGameEvent(Player player, PrototypeId avatarRef, int level)
        {
            Player = player;
            AvatarRef = avatarRef;
            Level = level;
        }
    }

    public struct PlayerEnteredCellGameEvent
    {
        public Player Player;
        public PrototypeId CellRef;

        public PlayerEnteredCellGameEvent(Player player, PrototypeId cellRef)
        {
            Player = player;
            CellRef = cellRef;
        }
    }

    public struct PlayerLeftCellGameEvent
    {
        public Player Player;
        public PrototypeId CellRef;

        public PlayerLeftCellGameEvent(Player player, PrototypeId cellRef)
        {
            Player = player;
            CellRef = cellRef;
        }
    }

    public struct PlayerUnlockedAvatarGameEvent
    {
        public Player Player;
        public PrototypeId AvatarRef;

        public PlayerUnlockedAvatarGameEvent(Player player, PrototypeId avatarRef)
        {
            Player = player;
            AvatarRef = avatarRef;
        }
    }

    public struct PlayerSwitchedToAvatarGameEvent
    {
        public Player Player;
        public PrototypeId AvatarRef;

        public PlayerSwitchedToAvatarGameEvent(Player player, PrototypeId avatarRef)
        {
            Player = player;
            AvatarRef = avatarRef;
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

    public struct PlayerFactionChangedGameEvent
    {
        public Player Player;
        public PrototypeId FactionRef;

        public PlayerFactionChangedGameEvent(Player player, PrototypeId factionRef)
        {
            Player = player;
            FactionRef = factionRef;
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

    public struct EntityStatusEffectGameEvent
    {
        public WorldEntity Entity;
        public Player Player;
        public bool Status;
        public bool NegStatusEffect;

        public EntityStatusEffectGameEvent(WorldEntity entity, Player player, bool status, bool negStatusEffect)
        {
            Entity = entity;
            Player = player;
            Status = status;
            NegStatusEffect = negStatusEffect;
        }
    }

    public struct AdjustHealthGameEvent
    {
        public WorldEntity Entity;
        public WorldEntity Attacker;
        public Player Player;
        public long Damage;
        public bool Dodged;

        public AdjustHealthGameEvent(WorldEntity entity, WorldEntity killer, Player player, long damage, bool dodged)
        {
            Entity = entity;
            Attacker = killer;
            Player = player;
            Damage = damage;
            Dodged = dodged;
        }
    }

    public struct EntityAggroedGameEvent
    {
        public Player Player;
        public WorldEntity AggroEntity;

        public EntityAggroedGameEvent(Player player, WorldEntity aggroEntity)
        {
            Player = player;
            AggroEntity = aggroEntity;
        }
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
