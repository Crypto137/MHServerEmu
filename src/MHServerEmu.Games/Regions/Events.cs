using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Regions
{
    public readonly struct EntityCollisionEvent(WorldEntity who, WorldEntity whom) : IGameEventData
    {
        public readonly WorldEntity Who = who;
        public readonly WorldEntity Whom = whom;
    }

    public readonly struct EntityInventoryChangedEvent(Entity entity) : IGameEventData
    {
        public readonly Entity Entity = entity;
    }

    public readonly struct EntityDeadGameEvent(WorldEntity defender, WorldEntity attacker, Player killer) : IGameEventData
    {
        public readonly WorldEntity Defender = defender;
        public readonly WorldEntity Attacker = attacker;
        public readonly Player Killer = killer;
    }

    public readonly struct ClusterEnemiesClearedGameEvent(SpawnGroup spawnGroup, ulong killerId) : IGameEventData
    {
        public readonly SpawnGroup SpawnGroup = spawnGroup;
        public readonly ulong KillerId = killerId;
    }

    public readonly struct EntityResurrectEvent(WorldEntity entity) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
    }

    public readonly struct EntityEnteredWorldGameEvent(WorldEntity entity) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
    }

    public readonly struct EntityExitedWorldGameEvent(WorldEntity entity) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
    }

    public readonly struct EntitySetSimulatedGameEvent(WorldEntity entity) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
    }

    public readonly struct EntitySetUnSimulatedGameEvent(WorldEntity entity) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
    }

    public readonly struct AIBroadcastBlackboardGameEvent(WorldEntity broadcaster, BehaviorBlackboard blackboard) : IGameEventData
    {
        public readonly WorldEntity Broadcaster = broadcaster;
        public readonly BehaviorBlackboard Blackboard = blackboard;
    }

    public readonly struct PlayerUnlockedTeamUpGameEvent(Player player, PrototypeId teamUpRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId TeamUpRef = teamUpRef;
    }

    public readonly struct PlayerDeathLimitHitGameEvent(Player player, PrototypeId metaStateRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId MetaStateRef = metaStateRef;
    }

    public readonly struct NotificationInteractGameEvent(Player player, PrototypeId missionRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId MissionRef = missionRef;
    }

    public readonly struct PlayerDonatedItemGameEvent(Player player, Item item, int count) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly Item Item = item;
        public readonly int Count = count;
    }

    public readonly struct PlayerCraftedItemGameEvent(Player player, Item item, PrototypeId recipeRef, int count) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly Item Item = item;
        public readonly PrototypeId RecipeRef = recipeRef;
        public readonly int Count = count;
    }

    public readonly struct PlayerBoughtItemGameEvent(Player player, Item item, int count) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly Item Item = item;
        public readonly int Count = count;
    }

    public readonly struct PlayerCollectedItemGameEvent(Player player, Item item, int count) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly Item Item = item;
        public readonly int Count = count;
    }

    public readonly struct PlayerLostItemGameEvent(Player player, Item item, int count) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly Item Item = item;
        public readonly int Count = count;
    }

    public readonly struct PlayerPreItemPickupGameEvent(Player player, Item item) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly Item Item = item;
    }

    public readonly struct PlayerEquippedItemGameEvent(Player player, Item item) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly Item Item = item;
    }

    public readonly struct SpawnerDefeatedGameEvent(Player player, Spawner spawner) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly Spawner Spawner = spawner;
    }

    public readonly struct ThrowablePickedUpGameEvent(Player player, WorldEntity throwable) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly WorldEntity Throwable = throwable;
    }

    public readonly struct PlayerInteractGameEvent(Player player, WorldEntity interactableObject, PrototypeId missionRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly WorldEntity InteractableObject = interactableObject;
        public readonly PrototypeId MissionRef = missionRef;
    }

    public readonly struct OrbPickUpEvent(Player player, WorldEntity orb) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly WorldEntity Orb = orb;
    }

    public readonly struct PlayerEnteredAreaGameEvent(Player player, PrototypeId areaRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId AreaRef = areaRef;
    }

    public readonly struct PlayerLeftAreaGameEvent(Player player, PrototypeId areaRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId AreaRef = areaRef;
    }

    public readonly struct PlayerBeginTravelToAreaGameEvent(Player player, PrototypeId areaRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId AreaRef = areaRef;
    }

    public readonly struct EmotePerformedGameEvent(Player player, PrototypeId emotePowerRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId EmotePowerRef = emotePowerRef;
    }

    public readonly struct PlayerBeginTravelToRegionGameEvent(Player player, PrototypeId regionRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId RegionRef = regionRef;
    }

    public readonly struct LoadingScreenFinishedGameEvent(Player player, PrototypeId regionRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId RegionRef = regionRef;
    }

    public readonly struct PlayerMetaGameCompleteGameEvent(Player player, PrototypeId metaGameRef, MetaGameCompleteType completeType) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId MetaGameRef = metaGameRef;
        public readonly MetaGameCompleteType CompleteType = completeType;
    }

    public readonly struct PlayerEventTeamChangedGameEvent(Player player, PrototypeId eventTeamRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId EventTeamRef = eventTeamRef;
    }

    public readonly struct KismetSeqFinishedGameEvent(Player player, PrototypeId kismetSeqRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId KismetSeqRef = kismetSeqRef;
    }

    public readonly struct CinematicFinishedGameEvent(Player player, PrototypeId movieRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId MovieRef = movieRef;
    }
    public readonly struct PlayerRequestMissionRewardsGameEvent(Player player, PrototypeId missionRef, uint conditionIndex, ulong entityId) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId MissionRef = missionRef;
        public readonly uint ConditionIndex = conditionIndex;
        public readonly ulong EntityId = entityId;
    }

    public readonly struct AvatarUsedPowerGameEvent(Player player, Avatar avatar, PrototypeId powerRef, ulong targetEntityId) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly Avatar Avatar = avatar;
        public readonly PrototypeId PowerRef = powerRef;
        public readonly ulong TargetEntityId = targetEntityId;
    }

    public readonly struct ActiveChapterChangedGameEvent(Player player, PrototypeId chapterRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId ChapterRef = chapterRef;
    }

    public readonly struct PartySizeChangedGameEvent(Player player, int partySize) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly int PartySize = partySize;
    }

    public readonly struct PlayerLeavePartyGameEvent(Player player) : IGameEventData
    {
        public readonly Player Player = player;
    }

    public readonly struct CurrencyCollectedGameEvent(Player player, PrototypeId currency, int amount) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId CurrencyType = currency;
        public readonly int Amount = amount;
    }

    public readonly struct AvatarLeveledUpGameEvent(Player player, PrototypeId avatarRef, int level) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId AvatarRef = avatarRef;
        public readonly int Level = level;
    }

    public readonly struct PlayerEnteredCellGameEvent(Player player, PrototypeId cellRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId CellRef = cellRef;
    }

    public readonly struct PlayerLeftCellGameEvent(Player player, PrototypeId cellRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId CellRef = cellRef;
    }

    public readonly struct PlayerUnlockedAvatarGameEvent(Player player, PrototypeId avatarRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId AvatarRef = avatarRef;
    }

    public readonly struct PlayerSwitchedToAvatarGameEvent(Player player, PrototypeId avatarRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId AvatarRef = avatarRef;
    }

    public readonly struct AvatarEnteredRegionGameEvent(Player player, PrototypeId regionRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId RegionRef = regionRef;
    }

    public readonly struct PlayerEnteredRegionGameEvent(Player player, PrototypeId regionRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId RegionRef = regionRef;
    }

    public readonly struct PlayerLeftRegionGameEvent(Player player, PrototypeId regionRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId RegionRef = regionRef;
    }

    public readonly struct PlayerDeathRecordedEvent(Player player) : IGameEventData
    {
        public readonly Player Player = player;
    }

    public readonly struct PlayerRegionChangeGameEvent(Player player) : IGameEventData
    {
        public readonly Player Player = player;
    }

    public readonly struct PlayerFactionChangedGameEvent(Player player, PrototypeId factionRef) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId FactionRef = factionRef;
    }

    public readonly struct OpenMissionCompleteGameEvent(PrototypeId missionRef) : IGameEventData
    {
        public readonly PrototypeId MissionRef = missionRef;
    }

    public readonly struct OpenMissionFailedGameEvent(PrototypeId missionRef) : IGameEventData
    {
        public readonly PrototypeId MissionRef = missionRef;
    }

    public readonly struct MissionObjectiveUpdatedGameEvent(Player player, PrototypeId missionRef, long objectiveId) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId MissionRef = missionRef;
        public readonly long ObjectiveId = objectiveId;
    }

    public readonly struct PlayerCompletedMissionObjectiveGameEvent(Player player, PrototypeId missionRef, long objectiveId,
        bool participant, bool contributor) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId MissionRef = missionRef;
        public readonly long ObjectiveId = objectiveId;
        public readonly bool Participant = participant;
        public readonly bool Contributor = contributor;
    }

    public readonly struct PlayerCompletedMissionGameEvent(Player player, PrototypeId missionRef, bool participant, bool contributor) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId MissionRef = missionRef;
        public readonly bool Participant = participant;
        public readonly bool Contributor = contributor;
    }

    public readonly struct PlayerFailedMissionGameEvent(Player player, PrototypeId missionRef, bool participant, bool contributor) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly PrototypeId MissionRef = missionRef;
        public readonly bool Participant = participant;
        public readonly bool Contributor = contributor;
    }

    public readonly struct EntityStatusEffectGameEvent(WorldEntity entity, Player player, PropertyEnum statusProp, bool status, bool negStatusEffect) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
        public readonly Player Player = player;
        public readonly PropertyEnum StatusProp = statusProp;
        public readonly bool Status = status;
        public readonly bool NegStatusEffect = negStatusEffect;
    }

    public readonly struct AdjustHealthGameEvent(WorldEntity entity, WorldEntity killer, Player player, long damage, bool dodged) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
        public readonly WorldEntity Attacker = killer;
        public readonly Player Player = player;
        public readonly long Damage = damage;
        public readonly bool Dodged = dodged;
    }

    public readonly struct EntityAggroedGameEvent(Player player, WorldEntity aggroEntity) : IGameEventData
    {
        public readonly Player Player = player;
        public readonly WorldEntity AggroEntity = aggroEntity;
    }

    public readonly struct AreaCreatedGameEvent(Area area) : IGameEventData
    {
        public readonly Area Area = area;
    }

    public readonly struct CellCreatedGameEvent(Cell cell) : IGameEventData
    {
        public readonly Cell Cell = cell;
    }

    public readonly struct EntityEnteredAreaGameEvent(WorldEntity entity, Area area) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
        public readonly Area Area = area;
    }

    public readonly struct EntityLeftAreaGameEvent(WorldEntity entity, Area area) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
        public readonly Area Area = area;
    }

    public readonly struct EntityLeaveDormantGameEvent(WorldEntity entity) : IGameEventData
    {
        public readonly WorldEntity Entity = entity;
    }

    public readonly struct EntityEnteredMissionHotspotGameEvent(WorldEntity target, Hotspot hotspot) : IGameEventData
    {
        public readonly WorldEntity Target = target;
        public readonly Hotspot Hotspot = hotspot;
    }

    public readonly struct EntityLeftMissionHotspotGameEvent(WorldEntity target, Hotspot hotspot) : IGameEventData
    {
        public readonly WorldEntity Target = target;
        public readonly Hotspot Hotspot = hotspot;
    }
}
