using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Invalid)]
    public enum ScoreTableValueType
    {
        Invalid = 0,
        Int = 1,
        Float = 2,
    }

    [AssetEnum((int)Invalid)]
    public enum ScoreTableValueEvent
    {
        Invalid = 0,
        DamageTaken = 1,
        DamageDealt = 2,
        Deaths = 3,
        PlayerAssists = 4,
        PlayerDamageDealt = 5,
        PlayerKills = 6,
    }

    #endregion

    public struct EntityFilterContext
    {
        public PrototypeId MissionRef;
        public ulong PowerOwnerId;
        public ulong PartyId;

        public EntityFilterContext(PrototypeId missionRef) 
        {
            MissionRef = missionRef;
            PowerOwnerId = 0;
            PartyId = 0;
        }
    }

    public class EntityFilterPrototype : Prototype
    {
        public virtual void GetAreaDataRefs(HashSet<PrototypeId> refs) { }
        public virtual void GetEntityDataRefs(HashSet<PrototypeId> refs) { }
        public virtual void GetRegionDataRefs(HashSet<PrototypeId> refs) { }
        public virtual void GetKeywordDataRefs(HashSet<PrototypeId> refs) { }
        public virtual bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            return true;
        }
    }

    public class EntityFilterFilterListPrototype : EntityFilterPrototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        public EntityFilterPrototype[] Filters { get; protected set; }

        public override void GetAreaDataRefs(HashSet<PrototypeId> refs)
        {
            if (Filters.IsNullOrEmpty()) return;
            foreach (var prototype in Filters)
                prototype?.GetAreaDataRefs(refs);
        }
        public override void GetEntityDataRefs(HashSet<PrototypeId> refs)
        {
            if (Filters.IsNullOrEmpty()) return;
            foreach (var prototype in Filters)
                prototype?.GetEntityDataRefs(refs);
        }
        public override void GetRegionDataRefs(HashSet<PrototypeId> refs)
        {
            if (Filters.IsNullOrEmpty()) return;
            foreach (var prototype in Filters)
                prototype?.GetRegionDataRefs(refs);
        }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            Logger.Error("You used a FilterList node in your Entity Filter!\n" +
                "This is a base class and shouldn't be used; \n" +
                "use EntityFilter.AND or EntityFilter.OR instead or your filter won't work! \n" +
                $"(FilterList::Evaluate is being called for Mission {GameDatabase.GetFormattedPrototypeName(context.MissionRef)}.)");
            return false;
        }
    }

    public class EntityFilterAndPrototype : EntityFilterFilterListPrototype
    {
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            if (Filters == null) return true;
            foreach (var prototype in Filters)
                if (prototype == null || prototype.Evaluate(entity, context) == false)
                    return false;
            return true;
        }
    }

    public class EntityFilterHasAlliancePrototype : EntityFilterPrototype
    {
        public PrototypeId Alliance { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            return entity.Alliance.DataRef == Alliance;
        }
    }

    public class EntityFilterScriptKeyPrototype : EntityFilterPrototype
    {
        public ScriptRoleKeyEnum ScriptKey { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            return entity.GetScriptRoleKey() == ScriptKey;
        }
    }

    public class EntityFilterHasKeywordPrototype : EntityFilterPrototype
    {
        public PrototypeId Keyword { get; protected set; }

        public override void GetKeywordDataRefs(HashSet<PrototypeId> refs)
        {
            if (Keyword != PrototypeId.Invalid) refs.Add(Keyword);
        }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            return entity.HasKeyword(Keyword);
        }
    }

    public class EntityFilterHasNegStatusEffectPrototype : EntityFilterPrototype
    {
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            var collection = entity.ConditionCollection;
            return collection != null && collection.HasANegativeStatusEffectCondition();
        }
    }

    public class EntityFilterHasPrototypePrototype : EntityFilterPrototype
    {
        public PrototypeId EntityPrototype { get; protected set; }
        public bool IncludeChildPrototypes { get; protected set; }

        public override void GetEntityDataRefs(HashSet<PrototypeId> refs)
        {
            if (EntityPrototype != PrototypeId.Invalid && GameDatabase.DataDirectory.PrototypeIsADefaultPrototype(EntityPrototype) == false)
                refs.Add(EntityPrototype);
        }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            if (IncludeChildPrototypes)
                return entity.IsAPrototype(EntityPrototype);
            else
                return entity.PrototypeDataRef == EntityPrototype;
        }
    }

    public class EntityFilterInAreaPrototype : EntityFilterPrototype
    {
        public PrototypeId InArea { get; protected set; }

        public override void GetAreaDataRefs(HashSet<PrototypeId> refs)
        {
            if (InArea != PrototypeId.Invalid) refs.Add(InArea);
        }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;

            PrototypeId areaRef = PrototypeId.Invalid;

            Area area = entity.Area;
            if (area != null)
            {
                areaRef = area.PrototypeDataRef;
            }
            else
            {
                RegionLocation ownerLocation = entity.GetOwnerLocation();
                if (ownerLocation != null)
                {
                    area = ownerLocation.Area;
                    if (area != null)
                        areaRef = area.PrototypeDataRef;
                }
                else
                    areaRef = entity.ExitWorldRegionLocation.AreaRef;
                
                if (areaRef == PrototypeId.Invalid)
                    areaRef = entity.Properties[PropertyEnum.ContextAreaRef];
            }

            return areaRef == InArea;
        }

    }

    public class EntityFilterInCellPrototype : EntityFilterPrototype
    {
        public AssetId[] InCells { get; protected set; }

        private readonly List<PrototypeId> CellPrototypes = new();

        public override void PostProcess()
        {
            base.PostProcess();
            if (InCells.HasValue())
                foreach(var cell in InCells)
                {
                    PrototypeId cellRef = GameDatabase.GetDataRefByAsset(cell);
                    if (cellRef != PrototypeId.Invalid) CellPrototypes.Add(cellRef);
                }    
        }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;

            Cell cell = entity.Cell;
            if (cell == null) return false;

            return CellPrototypes.Contains(cell.PrototypeId);
        }
    }

    public class EntityFilterInLocationWithKeywordPrototype : EntityFilterPrototype
    {
        public PrototypeId Keyword { get; protected set; }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;

            var keywordProto = Keyword.As<KeywordPrototype>();

            if (entity.IsInWorld)
                return entity.RegionLocation.HasKeyword(keywordProto);
            else
            {
                RegionLocation ownerLocation = entity.GetOwnerLocation();
                if (ownerLocation != null)
                    return ownerLocation.HasKeyword(keywordProto);
                else
                    return entity.ExitWorldRegionLocation.HasKeyword(keywordProto);
            }
        }
    }

    public class EntityFilterInRegionPrototype : EntityFilterPrototype
    {
        public PrototypeId InRegion { get; protected set; }

        public override void GetRegionDataRefs(HashSet<PrototypeId> refs)
        {
            if (InRegion != PrototypeId.Invalid) refs.Add(InRegion);
        }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;

            Region region = entity.Region;
            if (region == null)
            {
                RegionLocation ownerLocation = entity.GetOwnerLocation();
                if (ownerLocation != null)
                    region = ownerLocation.Region;
                else
                    region = entity.ExitWorldRegionLocation.GetRegion();
            }
            return region != null && RegionPrototype.Equivalent(InRegion.As<RegionPrototype>(), region.RegionPrototype);
        }
    }

    public class EntityFilterMissionStatePrototype : EntityFilterPrototype
    {
        public PrototypeId Mission { get; protected set; }
        public MissionState State { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            Player player = null;
            if (entity is Avatar avatar)
                player = avatar.GetOwnerOfType<Player>();

            if (player != null)
            {
                Mission mission = MissionManager.FindMissionForPlayer(player, Mission);
                return mission.State == State;
            }

            return false;
        }
    }

    public class EntityFilterIsHostileToPlayersPrototype : EntityFilterPrototype
    {
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;

            var worldEntityProto = entity.WorldEntityPrototype;
            if (worldEntityProto == null) return false;

            var allianceProto = worldEntityProto.Alliance.As<AlliancePrototype>();
            if (allianceProto == null)
            {
                PrototypeId allianceOverrideRef = entity.Properties[PropertyEnum.AllianceOverride];
                if (allianceOverrideRef != PrototypeId.Invalid)
                    allianceProto = allianceOverrideRef.As<AlliancePrototype>();
                else
                    return false;
            }

            return AlliancePrototype.IsHostileToPlayerAlliance(allianceProto);
        }
    }

    public class EntityFilterIsMemberOfSuperteamPrototype : EntityFilterPrototype
    {
        public PrototypeId Superteam { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            if (entity is Avatar avatar)
            {
                AvatarPrototype avatarProto = avatar.AvatarPrototype;
                if (avatarProto != null && avatarProto.IsMemberOfSuperteam(Superteam))
                    return true;
            }
            return false;
        }
    }

    public class EntityFilterIsMissionContributorPrototype : EntityFilterPrototype
    {
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            Player player = null;
            if (entity is Avatar avatar)
                player = avatar.GetOwnerOfType<Player>();

            if (player != null)
            {
                Mission mission = MissionManager.FindMissionForPlayer(player, context.MissionRef);
                // TODO write Contributor
            }

            return false;
        }
    }

    public class EntityFilterIsMissionParticipantPrototype : EntityFilterPrototype
    {
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            Player player = null;
            if (entity is Avatar avatar)
                player = avatar.GetOwnerOfType<Player>();

            if (player != null)
            {
                Mission mission = MissionManager.FindMissionForPlayer(player, context.MissionRef);
                if (mission != null) 
                    return mission.HasParticipant(player);
            }

            return false;
        }
    }

    public class EntityFilterIsPartyMemberPrototype : EntityFilterPrototype
    {
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            return context.PartyId != 0 && entity.PartyId == context.PartyId;
        }
    }

    public class EntityFilterIsPlayerAvatarPrototype : EntityFilterPrototype
    {
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            return entity is Avatar;
        }
    }

    public class EntityFilterIsPowerOwnerPrototype : EntityFilterPrototype
    {        
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            return context.PowerOwnerId != 0 && entity.Id == context.PowerOwnerId;
        }
    }

    public class EntityFilterNotPrototype : EntityFilterPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (EntityFilter == null) return true;
            return EntityFilter.Evaluate(entity, context) == false;
        }
    }

    public class EntityFilterOrPrototype : EntityFilterFilterListPrototype
    {
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null || Filters.IsNullOrEmpty()) return false;
            foreach (var filter in Filters)
                if (filter != null && filter.Evaluate(entity, context)) return true;
            return false;
        }
    }

    public class EntityFilterSpawnedByEncounterPrototype : EntityFilterPrototype
    {
        public AssetId EncounterResource { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            if (entity.Properties.HasProperty(PropertyEnum.EncounterResource))
                return entity.Properties[PropertyEnum.EncounterResource] == GameDatabase.GetDataRefByAsset(EncounterResource);
            return false;
        }
    }

    public class EntityFilterSpawnedByMissionPrototype : EntityFilterPrototype
    {
        public PrototypeId MissionPrototype { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            var missionRef = entity.MissionPrototype;

            if (missionRef != PrototypeId.Invalid)
                return missionRef == MissionPrototype;
            else if (context.MissionRef != PrototypeId.Invalid)
                return missionRef == context.MissionRef;
            else
                return missionRef != PrototypeId.Invalid;
        }
    }

    public class EntityFilterSpawnedBySpawnerPrototype : EntityFilterPrototype
    {
        public PrototypeId SpawnerPrototype { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            SpawnGroup spawnGroup = entity.SpawnGroup;
            if (spawnGroup != null && spawnGroup.SpawnerId != 0)
            {
                var manager = entity.Game?.EntityManager;
                if (manager == null) return false;
                var spawner = manager.GetEntity<Spawner>(spawnGroup.SpawnerId);
                if (spawner != null && spawner.PrototypeDataRef == SpawnerPrototype)
                    return true;
            }
            return false;
        }
    }

    public class EntityFilterHasPrestigeLevelPrototype : EntityFilterPrototype
    {
        public PrototypeId PrestigeLevel { get; protected set; }

        private int _prestigeLevelIndex;
        public override void PostProcess()
        {
            _prestigeLevelIndex = int.MaxValue;
            if (PrestigeLevel != PrototypeId.Invalid)
            {
                var advancementGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;
                var prestigeLevelProto = GameDatabase.GetPrototype<PrestigeLevelPrototype>(PrestigeLevel);
                if (advancementGlobalsProto != null && prestigeLevelProto != null)
                    _prestigeLevelIndex = advancementGlobalsProto.GetPrestigeLevelIndex(prestigeLevelProto);
            }
        }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            if (entity is Avatar avatar)
                return avatar.PrestigeLevel == _prestigeLevelIndex;
            return false;
        }
    }

    public class EntityFilterHasRankPrototype : EntityFilterPrototype
    {
        public PrototypeId RankPrototype { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null) return false;
            if (RankPrototype != PrototypeId.Invalid)
                return entity.Properties[PropertyEnum.Rank] == RankPrototype;
            return true;
        }
    }

    public class EntityFilterItemRarityPrototype : EntityFilterPrototype
    {
        public PrototypeId Rarity { get; protected set; }
        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (entity == null || Rarity == PrototypeId.Invalid) return false;
            if (entity is Item item)
                return item.Properties[PropertyEnum.ItemRarity] == Rarity;
            return false;
        }
    }

    public class ScoreTableSchemaEntryPrototype : Prototype
    {
        public ScoreTableValueType Type { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public EvalPrototype EvalOnPlayerAdd { get; protected set; }
        public EvalPrototype EvalAuto { get; protected set; }
        public EntityFilterPrototype OnEntityDeathFilter { get; protected set; }
        public ScoreTableValueEvent Event { get; protected set; }


    }

    public class ScoreTableSchemaPrototype : Prototype
    {
        public ScoreTableSchemaEntryPrototype[] Schema { get; protected set; }
    }
}
