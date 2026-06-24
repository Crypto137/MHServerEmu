using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Populations;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
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

        public EntityFilterContext(ulong powerOwnerId, ulong partyId)
        {
            MissionRef = PrototypeId.Invalid;
            PowerOwnerId = powerOwnerId;
            PartyId = partyId;
        }
    }

    public class EntityFilterPrototype : Prototype
    {
        //---

        public virtual void GetAreaDataRefs(HashSet<PrototypeId> refs) { }
        public virtual void GetEntityDataRefs(HashSet<PrototypeId> refs) { }
        public virtual void GetRegionDataRefs(HashSet<PrototypeId> refs) { }
        public virtual void GetKeywordDataRefs(HashSet<PrototypeId> refs) { }

        public virtual bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            return true;
        }
    }

    public class EntityFilterFilterListPrototype : EntityFilterPrototype
    {
        public EntityFilterPrototype[] Filters { get; protected set; }

        //---

        public override void GetAreaDataRefs(HashSet<PrototypeId> refs)
        {
            if (!Verify.IsTrue(Filters.HasValue())) return;

            foreach (EntityFilterPrototype prototype in Filters)
            {
                if (Verify.IsNotNull(prototype))
                    prototype.GetAreaDataRefs(refs);
            }
        }

        public override void GetEntityDataRefs(HashSet<PrototypeId> refs)
        {
            if (!Verify.IsTrue(Filters.HasValue())) return;

            foreach (EntityFilterPrototype prototype in Filters)
            {
                if (Verify.IsNotNull(prototype))
                    prototype.GetEntityDataRefs(refs);
            }
        }

        public override void GetRegionDataRefs(HashSet<PrototypeId> refs)
        {
            if (!Verify.IsTrue(Filters.HasValue())) return;

            foreach (EntityFilterPrototype prototype in Filters)
            {
                if (Verify.IsNotNull(prototype))
                    prototype.GetRegionDataRefs(refs);
            }
        }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            Verify.IsTrue(false,
                $"You used a FilterList node in your Entity Filter!  This is a base class and shouldn't be used; use EntityFilter.AND or EntityFilter.OR instead or your filter won't work!  (FilterList::Evaluate is being called for Mission {context.MissionRef.GetName()}.)");
            return false;
        }
    }

    public class EntityFilterAndPrototype : EntityFilterFilterListPrototype
    {
        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            if (!Verify.IsTrue(Filters.HasValue())) return true;

            foreach (EntityFilterPrototype prototype in Filters)
            {
                if (Verify.IsNotNull(prototype) && prototype.Evaluate(entity, context) == false)
                    return false;
            }

            return true;
        }
    }

    public class EntityFilterHasAlliancePrototype : EntityFilterPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public AlliancePrototype Alliance { get; protected set; }

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            return entity.Alliance == Alliance;
        }
    }

    public class EntityFilterScriptKeyPrototype : EntityFilterPrototype
    {
        public ScriptRoleKeyEnum ScriptKey { get; protected set; }

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            return entity.GetScriptRoleKey() == ScriptKey;
        }
    }

    public class EntityFilterHasKeywordPrototype : EntityFilterPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype Keyword { get; protected set; }

        //---

        public override void GetKeywordDataRefs(HashSet<PrototypeId> refs)
        {
            if (!Verify.IsNotNull(Keyword)) return;
            refs.Add(Keyword.DataRef);
        }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            return entity.HasKeyword(Keyword);
        }
    }

    public class EntityFilterHasNegStatusEffectPrototype : EntityFilterPrototype
    {
        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            ConditionCollection collection = entity.ConditionCollection;
            return collection != null && collection.HasANegativeStatusEffectCondition();
        }
    }

    public class EntityFilterHasPrototypePrototype : EntityFilterPrototype
    {
        public PrototypeId EntityPrototype { get; protected set; }
        public bool IncludeChildPrototypes { get; protected set; }

        //---

        public override void GetEntityDataRefs(HashSet<PrototypeId> refs)
        {
            if (!Verify.IsTrue(EntityPrototype != PrototypeId.Invalid)) return;

            if (GameDatabase.DataDirectory.PrototypeIsADefaultPrototype(EntityPrototype) == false)
                refs.Add(EntityPrototype);
        }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            if (IncludeChildPrototypes)
                return entity.IsAPrototype(EntityPrototype);
            else
                return entity.PrototypeDataRef == EntityPrototype;
        }
    }

    public class EntityFilterInAreaPrototype : EntityFilterPrototype
    {
        public PrototypeId InArea { get; protected set; }

        //---

        public override void GetAreaDataRefs(HashSet<PrototypeId> refs)
        {
            if (Verify.IsTrue(InArea != PrototypeId.Invalid))
                refs.Add(InArea);
        }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            PrototypeId areaRef = PrototypeId.Invalid;

            Area area = entity.Area;
            if (area != null)
            {
                areaRef = area.PrototypeDataRef;
            }
            else
            {
                ref RegionLocation ownerLocation = ref entity.GetOwnerLocation(out bool hasOwnerLocation);
                if (hasOwnerLocation)
                {
                    area = ownerLocation.Area;
                    if (area != null)
                        areaRef = area.PrototypeDataRef;
                }
                else
                {
                    areaRef = entity.ExitWorldRegionLocation.AreaRef;
                }
                
                if (areaRef == PrototypeId.Invalid)
                    areaRef = entity.Properties[PropertyEnum.ContextAreaRef];
            }

            return areaRef == InArea;
        }
    }

    public class EntityFilterInCellPrototype : EntityFilterPrototype
    {
        public AssetId[] InCells { get; protected set; }

        //---

        private readonly List<PrototypeId> _cellPrototypes = new();     // HashSet possibly faster here? Need to measure if it's worth it.

        public override void PostProcess()
        {
            base.PostProcess();

            if (InCells.HasValue())
            {
                foreach (AssetId cell in InCells)
                {
                    PrototypeId cellRef = GameDatabase.GetDataRefByAsset(cell);
                    if (Verify.IsTrue(cellRef != PrototypeId.Invalid) && _cellPrototypes.Contains(cellRef) == false)
                        _cellPrototypes.Add(cellRef);
                }
            }
        }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            Cell cell = entity.Cell;
            if (cell == null)
                return false;

            return _cellPrototypes.Contains(cell.PrototypeDataRef);
        }
    }

    public class EntityFilterInLocationWithKeywordPrototype : EntityFilterPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype Keyword { get; protected set; }

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            if (entity.IsInWorld)
            {
                return entity.RegionLocation.HasKeyword(Keyword);
            }
            else
            {
                ref RegionLocation ownerLocation = ref entity.GetOwnerLocation(out bool hasOwnerLocation);
                if (hasOwnerLocation)
                    return ownerLocation.HasKeyword(Keyword);
                else
                    return entity.ExitWorldRegionLocation.HasKeyword(Keyword);
            }
        }
    }

    public class EntityFilterInRegionPrototype : EntityFilterPrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public RegionPrototype InRegion { get; protected set; }

        //---

        public override void GetRegionDataRefs(HashSet<PrototypeId> refs)
        {
            if (Verify.IsNotNull(InRegion))
                refs.Add(InRegion.DataRef);
        }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            Region region = entity.Region;
            if (region == null)
            {
                ref RegionLocation ownerLocation = ref entity.GetOwnerLocation(out bool hasOwnerLocation);
                if (hasOwnerLocation)
                    region = ownerLocation.Region;
                else
                    region = entity.ExitWorldRegionLocation.GetRegion();
            }

            return region != null && RegionPrototype.Equivalent(InRegion, region.Prototype);
        }
    }

    public class EntityFilterMissionStatePrototype : EntityFilterPrototype
    {
        public PrototypeId Mission { get; protected set; }
        public MissionState State { get; protected set; }

        //---

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
        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            WorldEntityPrototype worldEntityProto = entity.WorldEntityPrototype;
            if (!Verify.IsNotNull(worldEntityProto)) return false;

            AlliancePrototype allianceProto = worldEntityProto.Alliance;
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

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            if (entity is Avatar avatar)
            {
                AvatarPrototype avatarProto = avatar.AvatarPrototype;
                if (Verify.IsNotNull(avatarProto) && avatarProto.IsMemberOfSuperteam(Superteam))
                    return true;
            }

            return false;
        }
    }

    public class EntityFilterIsMissionContributorPrototype : EntityFilterPrototype
    {
        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            Player player = null;
            if (entity is Avatar avatar)
                player = avatar.GetOwnerOfType<Player>();

            if (player != null)
            {
                Mission mission = MissionManager.FindMissionForPlayer(player, context.MissionRef);
                if (mission != null)
                    return mission.GetContribution(player) > 0f;
            }

            return false;
        }
    }

    public class EntityFilterIsMissionParticipantPrototype : EntityFilterPrototype
    {
        //---

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
        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            return context.PartyId != 0 && entity.PartyId == context.PartyId;
        }
    }

    public class EntityFilterIsPlayerAvatarPrototype : EntityFilterPrototype
    {
        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            return entity is Avatar;
        }
    }

    public class EntityFilterIsPowerOwnerPrototype : EntityFilterPrototype
    {
        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            return context.PowerOwnerId != 0 && entity.Id == context.PowerOwnerId;
        }
    }

    public class EntityFilterNotPrototype : EntityFilterPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(EntityFilter)) return true;
            return EntityFilter.Evaluate(entity, context) == false;
        }
    }

    public class EntityFilterOrPrototype : EntityFilterFilterListPrototype
    {
        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            if (!Verify.IsTrue(Filters.HasValue())) return false;

            foreach (EntityFilterPrototype prototype in Filters)
            {
                if (Verify.IsNotNull(prototype) && prototype.Evaluate(entity, context))
                    return true;
            }

            return false;
        }
    }

    public class EntityFilterSpawnedByEncounterPrototype : EntityFilterPrototype
    {
        public AssetId EncounterResource { get; protected set; }

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            if (entity.Properties.HasProperty(PropertyEnum.EncounterResource))
                return entity.Properties[PropertyEnum.EncounterResource] == GameDatabase.GetDataRefByAsset(EncounterResource);

            return false;
        }
    }

    public class EntityFilterSpawnedByMissionPrototype : EntityFilterPrototype
    {
        public PrototypeId MissionPrototype { get; protected set; }

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            PrototypeId missionRef = entity.MissionPrototype;

            if (MissionPrototype != PrototypeId.Invalid)
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

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            SpawnGroup spawnGroup = entity.SpawnGroup;
            if (spawnGroup != null && spawnGroup.SpawnerId != 0)
            {
                Spawner spawner = entity.Game.EntityManager.GetEntity<Spawner>(spawnGroup.SpawnerId);
                if (spawner != null && spawner.PrototypeDataRef == SpawnerPrototype)
                    return true;
            }

            return false;
        }
    }

    public class EntityFilterHasPrestigeLevelPrototype : EntityFilterPrototype
    {
        public PrototypeId PrestigeLevel { get; protected set; }

        //---

        private int _prestigeLevelIndex;

        public override void PostProcess()
        {
            _prestigeLevelIndex = int.MaxValue;

            if (PrestigeLevel != PrototypeId.Invalid)
            {
                AdvancementGlobalsPrototype advancementGlobalsProto = GameDatabase.AdvancementGlobalsPrototype;
                PrestigeLevelPrototype prestigeLevelProto = GameDatabase.GetPrototype<PrestigeLevelPrototype>(PrestigeLevel);

                if (Verify.IsNotNull(advancementGlobalsProto) && Verify.IsNotNull(prestigeLevelProto))
                    _prestigeLevelIndex = advancementGlobalsProto.GetPrestigeLevelIndex(prestigeLevelProto);
            }
        }

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            return entity is Avatar avatar && avatar.PrestigeLevel >= _prestigeLevelIndex;
        }
    }

    public class EntityFilterHasRankPrototype : EntityFilterPrototype
    {
        public PrototypeId RankPrototype { get; protected set; }

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;

            if (RankPrototype != PrototypeId.Invalid)
                return entity.Properties[PropertyEnum.Rank] == RankPrototype;
            
            return true;
        }
    }

    public class EntityFilterItemRarityPrototype : EntityFilterPrototype
    {
        public PrototypeId Rarity { get; protected set; }

        //---

        public override bool Evaluate(WorldEntity entity, EntityFilterContext context)
        {
            if (!Verify.IsNotNull(entity)) return false;
            if (!Verify.IsTrue(Rarity != PrototypeId.Invalid)) return false;

            if (entity is Item item)
                return item.Properties[PropertyEnum.ItemRarity] == Rarity;

            return false;
        }
    }
}
