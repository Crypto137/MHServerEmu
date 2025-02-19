using MHServerEmu.Core.Collections;
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
        None                    = 0ul,
        Attack                  = 1ul << 0,
        Converse                = 1ul << 1,
        PickUp                  = 1ul << 2,
        Throw                   = 1ul << 3,
        Use                     = 1ul << 4,
        Equip                   = 1ul << 5,
        Destroy                 = 1ul << 6,
        Buy                     = 1ul << 7,
        Sell                    = 1ul << 8,
        Donate                  = 1ul << 9,
        DonatePetTech           = 1ul << 10,
        Teleport                = 1ul << 11,
        MakeLeader              = 1ul << 12,
        GroupChangeTypeToRaid   = 1ul << 13,
        GroupChangeTypeToParty  = 1ul << 14,
        PartyShareLegendaryQuest= 1ul << 15,
        Social                  = 1ul << 16,
        Resurrect               = 1ul << 17,
        Chat                    = 1ul << 18,
        PartyInvite             = 1ul << 19,
        Friend                  = 1ul << 20,
        Inspect                 = 1ul << 21,
        Trade                   = 1ul << 22,
        ViewPSNProfile          = 1ul << 23,
        GuildInvite             = 1ul << 24,
        GuildPromote            = 1ul << 25,
        Heal                    = 1ul << 26,
        Flag27                  = 1ul << 27,
        StoryWarp               = 1ul << 28,
        Follow                  = 1ul << 29,
        Duel                    = 1ul << 30,
        Neutral                 = 1ul << 31,
        PartyLeave              = 1ul << 32,
        PartyBoot               = 1ul << 33,
        Unfriend                = 1ul << 34,
        Ignore                  = 1ul << 35,
        Unignore                = 1ul << 36,
        Report                  = 1ul << 37,
        ReportAsSpam            = 1ul << 38,
        GuildDemote             = 1ul << 39,
        GuildKick               = 1ul << 40,
        GuildLeave              = 1ul << 41,
        Mute                    = 1ul << 42,
        MoveToGeneralInventory  = 1ul << 43,
        MoveToStash             = 1ul << 44,
        SlotCraftingIngredient  = 1ul << 45,
        MoveToTradeInventory    = 1ul << 46,
        MoveToTeamUp            = 1ul << 47,
        LinkItemInChat          = 1ul << 48,
        OpenMTXStore            = 1ul << 49,
        All                     = ulong.MaxValue
    }

    public enum InteractionResult
    {
        Success,
        Failure,
        OutOfRange,
        AttackFail,
        ExecutingPower,
    }

    [Flags]
    public enum InteractionFlags
    {
        None                        = 0,
        Flag0                       = 1 << 0,
        Default                     = 1 << 1,
        Flag2                       = 1 << 2,
        Flag3                       = 1 << 3,
        StopMove                    = 1 << 4,
        DeadInteractor              = 1 << 5,
        DormanInvisibleInteractee   = 1 << 6,
        EvaluateInteraction         = 1 << 7,
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
        private readonly SortedVector<PrototypeId> _encounterRefs = new();
        private readonly SortedVector<PrototypeId> _regionRefs = new();
        private readonly SortedVector<PrototypeId> _clusterRefs = new();
        private readonly SortedVector<PrototypeId> _missionRefs = new();
        private readonly List<EntityFilterPrototype> _entityFilters = new();

        public PrototypeId FilterContextMissionRef { get; set; }

        public EntityFilterWrapper()
        {
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

            if (_regionRefs.Count > 0)
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
                    if (RegionPrototype.Equivalent(regionRef.As<RegionPrototype>(), region.Prototype))
                    {
                        found = true;
                        break;
                    }

                if (found == false) return false;
            }

            if (_encounterRefs.Count > 0)
            {
                var encounterRef = entity.EncounterResourcePrototype;
                if (encounterRef == PrototypeId.Invalid || _encounterRefs.Contains(encounterRef) == false) return false;
            }

            if (_missionRefs.Count > 0)
            {
                PrototypeId missionRef = entity.MissionPrototype;
                if (missionRef == PrototypeId.Invalid || _missionRefs.Contains(missionRef) == false) return false;
            }

            if (_clusterRefs.Count > 0)
            {
                PrototypeId clusterRef = entity.ClusterPrototype;
                if (clusterRef == PrototypeId.Invalid || _clusterRefs.Contains(clusterRef) == false) return false;
            }

            if (_entityFilters.Count > 0)
            {
                foreach (var filter in _entityFilters)
                    if (filter.Evaluate(entity, new (FilterContextMissionRef)) == false) return false;
            }

            return true;
        }

    }
}
