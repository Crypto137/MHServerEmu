using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Dialog
{
    #region Enums
    [Flags]
    public enum InteractionOptimizationFlags
    {
        None = 0,
        Hint = 1 << 0,
        Flag1 = 1 << 1,
        Appearance = 1 << 2,
        Visibility = 1 << 3,
        ActionEntityTarget = 1 << 4,
        ConditionHotspot = 1 << 5,
        ConnectionTargetEnable = 1 << 6
    }

    [Flags]
    public enum InteractionMethod : ulong
    {
        None = 0L,
        Attack = 1L << 0,
        Converse = 1L << 1,
        PickUp = 1L << 2,
        Throw = 1L << 3,
        Use = 1L << 4,
        Equip = 1L << 5,
        Destroy = 1L << 6,
        Buy = 1L << 7,
        Sell = 1L << 8,
        Donate = 1L << 9,
        DonatePetTech = 1L << 10,
        Teleport = 1L << 11,
        MakeLeader = 1L << 12,
        GroupChangeTypeToRaid = 1L << 13,
        GroupChangeTypeToParty = 1L << 14,
        PartyShareLegendaryQuest = 1L << 15,
        Social = 1L << 16,
        Resurrect = 1L << 17,
        Chat = 1L << 18,
        PartyInvite = 1L << 19,
        Friend = 1L << 20,
        Inspect = 1L << 21,
        Trade = 1L << 22,
        ViewPSNProfile = 1L << 23,
        GuildInvite = 1L << 24,
        GuildPromote = 1L << 25,
        Heal = 1L << 26,
        Flag27 = 1L << 27,
        Flag28 = 1L << 28,
        Follow = 1L << 29,
        Duel = 1L << 30,
        Flag31 = 1L << 31,
        PartyLeave = 1L << 32,
        PartyBoot = 1L << 33,
        Unfriend = 1L << 34,
        Ignore = 1L << 35,
        Unignore = 1L << 36,
        Report = 1L << 37,
        ReportAsSpam = 1L << 38,
        GuildDemote = 1L << 39,
        GuildKick = 1L << 40,
        GuildLeave = 1L << 41,
        Mute = 1L << 42,
        MoveToGeneralInventory = 1L << 43,
        MoveToStash = 1L << 44,
        SlotCraftingIngredient = 1L << 45,
        MoveToTradeInventory = 1L << 46,
        MoveToTeamUp = 1L << 47,
        LinkItemInChat = 1L << 48,
    }

    [Flags]
    public enum InteractionFlags
    {
        None = 0,
    }

    [Flags]
    public enum MissionOptionTypeFlags
    {
        None = 0,
        ActivateCondition = 1 << 0,
        Skip = 1 << 1,
        SkipComplete = 1 << 2,
    }

    [Flags]
    public enum MissionStateFlags
    {
        Invalid = 1 << 0,
        Inactive = 1 << 1,
        Available = 1 << 2,
        Active = 1 << 3,
        Completed = 1 << 4,
        Failed = 1 << 5,
        OnStart = 1 << 6
    }

    [Flags]
    public enum MissionObjectiveStateFlags
    {
        Invalid = 1 << 0,
        Available = 1 << 1,
        Active = 1 << 2,
        Completed = 1 << 3,
        Failed = 1 << 4,
        Skipped = 1 << 5,
    }

    [Flags]
    public enum EntityTrackingFlag
    {
        None = 0,
        Appearance = 1 << 0,
        Hotspot = 1 << 1,
        HUD = 1 << 2,
        KismetSequenceTracking = 1 << 3,
        MissionAction = 1 << 4,
        MissionCondition = 1 << 5,
        MissionDialog = 1 << 6,
        SpawnedByMission = 1 << 7,
        TransitionRegion = 1 << 8,
    }
    #endregion

    public class EntityFilterWrapper
    {
        private SortedSet<PrototypeId> _encounterRefs;
        private SortedSet<PrototypeId> _regionRefs;
        private SortedSet<PrototypeId> _clusterRefs;
        private SortedSet<PrototypeId> _missionRefs;
        private List<EntityFilterPrototype> _entityFilters;
        public PrototypeId FilterContextMissionRef { get; set; }

        public EntityFilterWrapper()
        {
            _entityFilters = new();
            _regionRefs = new();
            _encounterRefs = new();
            _missionRefs = new();
            _clusterRefs = new();
        }

        public void AddEncounterResource(AssetId encounterResource)
        {
            if (encounterResource != AssetId.Invalid)
            {
                PrototypeId encounterRef = GameDatabase.GetDataRefByAsset(encounterResource);
                if (encounterRef != PrototypeId.Invalid) _encounterRefs.Add(encounterRef);
            }
        }

        public void AddEntityFilter(EntityFilterPrototype entityFilter)
        {
            if (entityFilter != null) _entityFilters.Add(entityFilter);
        }

        public void AddRegionPtrs(PrototypeId[] activeInRegions)
        {
            if (activeInRegions.HasValue()) 
                foreach(var region in activeInRegions) _regionRefs.Add(region);
        }

        public void AddSpawnedByClusterRefs(PrototypeId[] specificClusters)
        {
            if (specificClusters.HasValue())
                foreach (var cluster in specificClusters) _clusterRefs.Add(cluster);
        }

        public void AddSpawnedByMissionRef(PrototypeId contextMissionRef)
        {
            if (contextMissionRef != PrototypeId.Invalid) _missionRefs.Add(contextMissionRef);
        }

        public void AddSpawnedByMissionRefs(PrototypeId[] spawnedByMission)
        {
            if(spawnedByMission.HasValue())
                foreach (var mission in spawnedByMission) _missionRefs.Add(mission);
        }

        public bool EvaluateEntity(WorldEntity entity)
        {
            if (entity == null) return false;

            if (_regionRefs.Any())
            {
                Region region = entity.Region;
                if (region == null)
                {
                    RegionLocation ownerLocation = entity.GetOwnerLocation();
                    if (ownerLocation != null)
                        region = ownerLocation.Region;
                    else
                        region = entity.ExitWorldRegionLocation.GetRegion();
                }

                if (region == null) return false;

                bool found = false;
                foreach (var regionRef in _regionRefs)
                    if (RegionPrototype.Equivalent(regionRef.As<RegionPrototype>(), region.RegionPrototype))
                    {
                        found = true;
                        break;
                    }

                if (found == false) return false;
            }

            if (_encounterRefs.Any())
            {
                var ecounterRef = entity.EncounterResourcePrototype;
                if (ecounterRef == PrototypeId.Invalid || _encounterRefs.Contains(ecounterRef) == false) return false;
            }

            if (_missionRefs.Any())
            {
                PrototypeId missionRef = entity.MissionPrototype;
                if (missionRef == PrototypeId.Invalid || _missionRefs.Contains(missionRef) == false) return false;
            }

            if (_clusterRefs.Any())
            {
                PrototypeId clusterRef = entity.ClusterPrototype;
                if (clusterRef == PrototypeId.Invalid || _clusterRefs.Contains(clusterRef) == false) return false;
            }

            if (_entityFilters.Any())
            {
                foreach (var filter in _entityFilters)
                    if (filter.Evaluate(entity, new (FilterContextMissionRef)) == false) return false;
            }

            return true;
        }

    }
}
