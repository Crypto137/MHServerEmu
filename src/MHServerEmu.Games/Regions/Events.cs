using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;

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

    public struct EntityInventoryChangedEvent
    {
        public Entity Entity;

        public EntityInventoryChangedEvent(Entity entity)
        {
            Entity = entity;
        }
    }

    public struct EntityDeadGameEvent
    {
        public WorldEntity Defender;
        public WorldEntity Attacker;
        public Player Killer;

        public EntityDeadGameEvent(WorldEntity defender, WorldEntity attacker, Player killer)
        {
            Defender = defender;
            Attacker = attacker;
            Killer = killer;
        }
    }

    public struct ClusterEnemiesClearedGameEvent
    {
        public SpawnGroup SpawnGroup;
        public ulong KillerId;

        public ClusterEnemiesClearedGameEvent(SpawnGroup spawnGroup, ulong killerId)
        {
            SpawnGroup = spawnGroup;
            KillerId = killerId;
        }
    }

    public struct EntityResurrectEvent
    {
        public WorldEntity Entity;

        public EntityResurrectEvent(WorldEntity entity)
        {
            Entity = entity;
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

    public struct PlayerUnlockedTeamUpGameEvent
    {
        public Player Player;
        public PrototypeId TeamUpRef;

        public PlayerUnlockedTeamUpGameEvent(Player player, PrototypeId teamUpRef)
        {
            Player = player;
            TeamUpRef = teamUpRef;
        }
    }

    public struct PlayerDeathLimitHitGameEvent
    {
        public Player Player;
        public PrototypeId MetaStateRef;

        public PlayerDeathLimitHitGameEvent(Player player, PrototypeId metaStateRef)
        {
            Player = player;
            MetaStateRef = metaStateRef;
        }
    }

    public struct NotificationInteractGameEvent
    {
        public Player Player;
        public PrototypeId MissionRef;

        public NotificationInteractGameEvent(Player player, PrototypeId missionRef)
        {
            Player = player;
            MissionRef = missionRef;
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

    public struct SpawnerDefeatedGameEvent
    {
        public Player Player;
        public Spawner Spawner;

        public SpawnerDefeatedGameEvent(Player player, Spawner spawner)
        {
            Player = player;
            Spawner = spawner;
        }
    }

    public struct ThrowablePickedUpGameEvent
    {
        public Player Player;
        public WorldEntity Throwable;

        public ThrowablePickedUpGameEvent(Player player, WorldEntity throwable)
        {
            Player = player;
            Throwable = throwable;
        }
    }

    public struct PlayerInteractGameEvent
    {
        public Player Player;
        public WorldEntity InteractableObject;
        public PrototypeId MissionRef;

        public PlayerInteractGameEvent(Player player, WorldEntity interactableObject, PrototypeId missionRef)
        {
            Player = player;
            InteractableObject = interactableObject;
            MissionRef = missionRef;
        }
    }

    public struct OrbPickUpEvent
    {
        public Player Player;
        public WorldEntity Orb;

        public OrbPickUpEvent(Player player, WorldEntity orb)
        {
            Player = player;
            Orb = orb;
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

    public struct PlayerBeginTravelToRegionGameEvent
    {
        public Player Player;
        public PrototypeId RegionRef;

        public PlayerBeginTravelToRegionGameEvent(Player player, PrototypeId regionRef)
        {
            Player = player;
            RegionRef = regionRef;
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

    public struct PlayerMetaGameCompleteGameEvent
    {
        public Player Player;
        public PrototypeId MetaGameRef;
        public MetaGameCompleteType CompleteType;

        public PlayerMetaGameCompleteGameEvent(Player player, PrototypeId metaGameRef, MetaGameCompleteType completeType)
        {
            Player = player;
            MetaGameRef = metaGameRef;
            CompleteType = completeType;
        }
    }

    public struct PlayerEventTeamChangedGameEvent
    {
        public Player Player;
        public PrototypeId EventTeamRef;

        public PlayerEventTeamChangedGameEvent(Player player, PrototypeId eventTeamRef)
        {
            Player = player;
            EventTeamRef = eventTeamRef;
        }
    }

    public struct KismetSeqFinishedGameEvent
    {
        public Player Player;
        public PrototypeId KismetSeqRef;

        public KismetSeqFinishedGameEvent(Player player, PrototypeId kismetSeqRef)
        {
            Player = player;
            KismetSeqRef = kismetSeqRef;
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
    public struct PlayerRequestMissionRewardsGameEvent
    {
        public Player Player;
        public PrototypeId MissionRef;
        public uint ConditionIndex;
        public ulong EntityId;

        public PlayerRequestMissionRewardsGameEvent(Player player, PrototypeId missionRef, uint conditionIndex, ulong entityId)
        {
            Player = player;
            MissionRef = missionRef;
            ConditionIndex = conditionIndex;
            EntityId = entityId;
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

    public struct PlayerDeathRecordedEvent
    {
        public Player Player;

        public PlayerDeathRecordedEvent(Player player)
        {
            Player = player;
        }
    }

    public struct PlayerRegionChangeGameEvent
    {
        public Player Player;

        public PlayerRegionChangeGameEvent(Player player)
        {
            Player = player;
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
        public PropertyEnum StatusProp;
        public bool Status;
        public bool NegStatusEffect;

        public EntityStatusEffectGameEvent(WorldEntity entity, Player player, PropertyEnum statusProp, bool status, bool negStatusEffect)
        {
            Entity = entity;
            Player = player;
            StatusProp = statusProp;
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
