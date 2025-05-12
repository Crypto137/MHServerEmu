using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Dialog;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Missions.Conditions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum VOCategory
    {
        Default = 0,
        Mission1 = 1,
        Mission2 = 2,
        Mission3 = 3,
        MissionBestow = 4,
        MissionInProgress = 5,
        MissionCompleted = 6,
        MissionAccepted = 7,
        InsufficientFunds = 8,
        VendorError = 9,
        Invalid = -1,
    }

    [AssetEnum((int)None)]
    public enum OnInteractAction
    {
        None = 0,
        Despawn = 1,
        Disable = 2,
    }

    [AssetEnum((int)Success)]
    public enum MetaGameCompleteType
    {
        Success = 0,
        Failure = 1,
        DidNotParticipate = 2,
    }

    [AssetEnum((int)Invalid)]
    public enum MissionShowObjsSettings // Missions/Types/ShowObjsSettings.type
    {
        Invalid = -1,
        FromTargetMission = 0,
        FromThisMission = 1,
        SuppressAllObjs = 2,
    }

    #endregion

    public class MissionItemRequiredEntryPrototype : Prototype
    {
        public PrototypeId ItemPrototype { get; protected set; }
        public long Num { get; protected set; }
        public bool Remove { get; protected set; }
    }

    public class MissionConditionPrototype : Prototype
    {
        public StoryNotificationPrototype StoryNotification { get; protected set; }
        public bool NoTrackingOptimization { get; protected set; }
        public long MissionConditionGuid { get; protected set; }

        [DoNotCopy]
        public int Index { get; set; }

        public virtual MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner) { return null; }
        public virtual void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef) { }
        public virtual void GetPrototypeContextRefs(HashSet<PrototypeId> refs) { }
        public virtual void SetInterestLocations(HashSet<PrototypeId> regions, HashSet<PrototypeId> areas, HashSet<PrototypeId> cells) { }
    }

    public class MissionPlayerConditionPrototype : MissionConditionPrototype
    {
        public bool PartyMembersGetCredit { get; protected set; }
        public double OpenMissionContributionValue { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner) { return null; }
    }

    public class MissionConditionListPrototype : MissionConditionPrototype
    {
        public MissionConditionPrototype[] Conditions { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner) { return null; }

        public MissionConditionPrototypeIterator IteratePrototypes(Type conditionType = null)
        {            
            return new MissionConditionPrototypeIterator(this, conditionType);
        }

    }

    public class MissionConditionAndPrototype : MissionConditionListPrototype
    {
        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionAnd(mission, owner, this);
        }
    }

    public class MissionConditionActiveChapterPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId Chapter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionActiveChapter(mission, owner, this);
        }
    }

    public class MissionConditionPowerPointsRemainingPrototype : MissionPlayerConditionPrototype
    {
        public int MinPoints { get; protected set; }
        public int MaxPoints { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionPowerPointsRemaining(mission, owner, this);
        }
    }

    public class MissionConditionAreaBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId AreaPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionAreaBeginTravelTo(mission, owner, this);
        }
    }

    public class MissionConditionAreaContainsPrototype : MissionConditionPrototype
    {
        public PrototypeId Area { get; protected set; }
        public int CountMax { get; protected set; }
        public int CountMin { get; protected set; }
        public EntityFilterPrototype TargetFilter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionAreaContains(mission, owner, this);
        }
    }

    public class MissionConditionAreaEnterPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId RegionPrototype { get; protected set; }
        public PrototypeId AreaPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionAreaEnter(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (RegionPrototype != PrototypeId.Invalid) refs.Add(RegionPrototype);
        }

        public override void SetInterestLocations(HashSet<PrototypeId> regions, HashSet<PrototypeId> areas, HashSet<PrototypeId> cells)
        {
            if (RegionPrototype != PrototypeId.Invalid) regions.Add(RegionPrototype);
            if (AreaPrototype != PrototypeId.Invalid) areas.Add(AreaPrototype);
        }
    }

    public class MissionConditionAreaLeavePrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId AreaPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionAreaLeave(mission, owner, this);
        }
    }

    public class MissionConditionAvatarIsActivePrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId AvatarPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionAvatarIsActive(mission, owner, this);
        }

    }

    public class MissionConditionAvatarIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId AvatarPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionAvatarIsUnlocked(mission, owner, this);
        }
    }

    public class MissionConditionAvatarLevelUpPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId AvatarPrototype { get; protected set; }
        public long Level { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionAvatarLevelUp(mission, owner, this);
        }
    }

    public class MissionConditionAvatarUsedPowerPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId AreaPrototype { get; protected set; }
        public PrototypeId AvatarPrototype { get; protected set; }
        public PrototypeId PowerPrototype { get; protected set; }
        public PrototypeId RegionPrototype { get; protected set; }
        public PrototypeId WithinHotspot { get; protected set; }
        public int Count { get; protected set; }
        public EntityFilterPrototype TargetFilter { get; protected set; }
        public int WithinSeconds { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionAvatarUsedPower(mission, owner, this);
        }
    }

    public class MissionConditionCellEnterPrototype : MissionPlayerConditionPrototype
    {
        public AssetId[] Cells { get; protected set; }
        public PrototypeId[] Regions { get; protected set; }

        //---

        private readonly HashSet<PrototypeId> _cells = new();

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionCellEnter(mission, owner, this);
        }

        public override void PostProcess()
        {
            base.PostProcess();
            if (Cells.HasValue())
                foreach(var cell in Cells)
                {
                    var cellRef = GameDatabase.GetDataRefByAsset(cell);
                    if (cellRef != PrototypeId.Invalid) _cells.Add(cellRef);
                }
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (Regions.HasValue())
                foreach (var region in Regions) refs.Add(region);
        }

        public override void SetInterestLocations(HashSet<PrototypeId> regions, HashSet<PrototypeId> areas, HashSet<PrototypeId> cells)
        {
            if (Regions.HasValue())
                foreach (var region in Regions) regions.Add(region);

            foreach (PrototypeId cellProtoRef in _cells)
                cells.Add(cellProtoRef);
        }

        public bool Contains(PrototypeId cellRef)
        {
            return _cells.Contains(cellRef);
        }
    }

    public class MissionConditionCellLeavePrototype : MissionPlayerConditionPrototype
    {
        public AssetId[] Cells { get; protected set; }

        //---

        private readonly HashSet<PrototypeId> _cells = new();

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionCellLeave(mission, owner, this);
        }

        public override void PostProcess()
        {
            base.PostProcess();
            if (Cells.HasValue())
                foreach (var cell in Cells)
                {
                    var cellRef = GameDatabase.GetDataRefByAsset(cell);
                    if (cellRef != PrototypeId.Invalid) _cells.Add(cellRef);
                }
        }

        public bool Contains(PrototypeId cellRef)
        {
            return _cells.Contains(cellRef);
        }
    }

    public class MissionConditionCohortPrototype : MissionConditionPrototype
    {
        public PrototypeId CohortPrototype { get; protected set; }
        public PrototypeId ExperimentPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionCohort(mission, owner, this);
        }
    }

    public class MissionConditionCountPrototype : MissionConditionListPrototype
    {
        public long Count { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionCount(mission, owner, this);
        }
    }

    public class MissionConditionCurrencyCollectedPrototype : MissionPlayerConditionPrototype
    {
        public long AmountRequired { get; protected set; }
        public PrototypeId CurrencyType { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionCurrencyCollected(mission, owner, this);
        }
    }

    public class MissionConditionEmotePerformedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EmoteAvatarFilter { get; protected set; }
        public PrototypeId EmotePower { get; protected set; }
        public bool ObserverAvatarsOnly { get; protected set; }
        public EntityFilterPrototype ObserverFilter { get; protected set; }
        public int ObserverRadius { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionEmotePerformed(mission, owner, this);
        }
    }

    public class MissionConditionEntityAggroPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionEntityAggro(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper wrapper, PrototypeId contextMissionRef)
        {
            wrapper.FilterContextMissionRef = contextMissionRef;
            wrapper.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionEntityDamagedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public AssetId EncounterResource { get; protected set; }
        public bool LimitToDamageFromPlayerOMOnly { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionEntityDamaged(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }

            if (EncounterResource != AssetId.Invalid)
            {
                PrototypeId encounterRef = GameDatabase.GetDataRefByAsset(EncounterResource);
                var encounterProto = GameDatabase.GetPrototype<EncounterResourcePrototype>(encounterRef);
                encounterProto?.MarkerSet.GetContainedEntities(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
            entityFilter.AddEncounterResource(EncounterResource);
        }

    }

    public class MissionConditionEntityDeathPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public double OpenMissionContribValueDamage { get; protected set; }
        public double OpenMissionContribValueTanking { get; protected set; }
        public AssetId EncounterResource { get; protected set; }
        public int DelayDeathMS { get; protected set; }
        public bool EncounterResourceValidate { get; protected set; }
        public int WithinSeconds { get; protected set; }
        public bool MustBeTaggedByPlayer { get; protected set; }
        public HUDEntitySettingsPrototype EntityHUDSettingOverride { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionEntityDeath(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }

            if (EncounterResource != AssetId.Invalid)
            {
                PrototypeId encounterRef = GameDatabase.GetDataRefByAsset(EncounterResource);
                var encounterProto = GameDatabase.GetPrototype<EncounterResourcePrototype>(encounterRef);
                encounterProto?.MarkerSet.GetContainedEntities(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
            entityFilter.AddEncounterResource(EncounterResource);
        }

        public override void SetInterestLocations(HashSet<PrototypeId> regions, HashSet<PrototypeId> areas, HashSet<PrototypeId> cells)
        {
            if (EntityFilter != null)
            {
                HashSet<PrototypeId> refs = HashSetPool<PrototypeId>.Instance.Get();
                EntityFilter.GetRegionDataRefs(refs);
                foreach (var region in refs)
                    regions.Add(region);

                refs.Clear();
                EntityFilter.GetAreaDataRefs(refs);
                foreach (var area in refs)
                    areas.Add(area);

                HashSetPool<PrototypeId>.Instance.Return(refs);
            }
        }

    }

    public class MissionConditionEntityInteractPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId Cinematic { get; protected set; }
        public long Count { get; protected set; }
        public LocaleStringId DialogText { get; protected set; }
        public WeightedTextEntryPrototype[] DialogTextList { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public LootTablePrototype[] GiveItems { get; protected set; }
        public bool IsTurnInNPC { get; protected set; }
        public OnInteractAction OnInteract { get; protected set; }
        public MissionItemRequiredEntryPrototype[] RequiredItems { get; protected set; }
        public PrototypeId WithinHotspot { get; protected set; }
        public PrototypeId OnInteractBehavior { get; protected set; }
        public MissionActionPrototype[] OnInteractEntityActions { get; protected set; }
        public IncrementalActionEntryPrototype[] OnIncrementalActions { get; protected set; }
        public LocaleStringId DialogTextWhenInventoryFull { get; protected set; }
        public bool DropLootOnGround { get; protected set; }
        public int WithinSeconds { get; protected set; }
        public DialogStyle DialogStyle { get; protected set; }
        public HUDEntitySettingsPrototype EntityHUDSettingOverride { get; protected set; }
        public bool ShowRewards { get; protected set; }
        public VOCategory VoiceoverCategory { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionEntityInteract(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionFactionPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId Faction { get; protected set; }
        public bool EventOnly { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionFaction(mission, owner, this);
        }
    }

    public class MissionConditionGlobalEventCompletePrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId Event { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionGlobalEventComplete(mission, owner, this);
        }
    }

    public class MissionConditionMemberOfEventTeamPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId Team { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionMemberOfEventTeam(mission, owner, this);
        }
    }

    public class MissionConditionMetaGameCompletePrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId MetaGame { get; protected set; }
        public MetaGameCompleteType CompleteType { get; protected set; }
        public int Count { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionMetaGameComplete(mission, owner, this);
        }
    }

    public class MissionConditionMetaStateCompletePrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId MetaState { get; protected set; }
        public int MinDifficulty { get; protected set; }
        public PrototypeId RequiredAffix { get; protected set; }
        public PrototypeId RequiredRarity { get; protected set; }
        public int WaveNum { get; protected set; }
        public int Count { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionMetaStateComplete(mission, owner, this);
        }
    }

    public class MissionConditionMetaStateDeathLimitHitPrototype : MissionConditionPrototype
    {
        public PrototypeId MetaState { get; protected set; }
        public int Count { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionMetaStateDeathLimitHit(mission, owner, this);
        }
    }

    public class MissionConditionPublicEventIsActivePrototype : MissionConditionPrototype
    {
        public PrototypeId Event { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionPublicEventIsActive(mission, owner, this);
        }
    }

    public class MissionConditionLogicFalsePrototype : MissionConditionPrototype
    {
        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionLogicFalse(mission, owner, this);
        }
    }

    public class MissionConditionLogicTruePrototype : MissionConditionPrototype
    {
        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionLogicTrue(mission, owner, this);
        }
    }

    public class MissionConditionMissionCompletePrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId MissionPrototype { get; protected set; }
        public long Count { get; protected set; }
        public DistributionType CreditTo { get; protected set; }
        public PrototypeId MissionKeyword { get; protected set; }
        public PrototypeId[] WithinRegions { get; protected set; }        // VectorPrototypeRefPtr RegionPrototype
        public bool EvaluateOnRegionEnter { get; protected set; }
        public bool EvaluateOnReset { get; protected set; }
        public PrototypeId[] WithinAreas { get; protected set; }          // VectorPrototypeRefPtr AreaPrototype
        public MissionShowObjsSettings ShowObjs { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionMissionComplete(mission, owner, this);
        }
    }

    public class MissionConditionMissionFailedPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId MissionPrototype { get; protected set; }
        public long Count { get; protected set; }
        public DistributionType CreditTo { get; protected set; }
        public PrototypeId MissionKeyword { get; protected set; }
        public PrototypeId[] WithinRegions { get; protected set; }    // VectorPrototypeRefPtr RegionPrototype
        public bool EvaluateOnRegionEnter { get; protected set; }
        public bool EvaluateOnReset { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionMissionFailed(mission, owner, this);
        }
    }

    public class MissionConditionObjectiveCompletePrototype : MissionPlayerConditionPrototype
    {
        public long ObjectiveID { get; protected set; }
        public PrototypeId MissionPrototype { get; protected set; }
        public bool EvaluateOnReset { get; protected set; }
        public long Count { get; protected set; }
        public DistributionType CreditTo { get; protected set; }
        public bool EvaluateOnRegionEnter { get; protected set; }
        public bool ShowCountFromTargetObjective { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionObjectiveComplete(mission, owner, this);
        }
    }

    public class MissionConditionOrbPickUpPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionOrbPickUp(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionOrPrototype : MissionConditionListPrototype
    {
        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionOr(mission, owner, this);
        }
    }

    public class MissionConditionSpawnerDefeatedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public int Count { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionSpawnerDefeated(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionHealthRangePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public double HealthMinPct { get; protected set; }
        public double HealthMaxPct { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionHealthRange(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionHotspotContainsPrototype : MissionConditionPrototype
    {
        public int CountMax { get; protected set; }
        public int CountMin { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public EntityFilterPrototype TargetFilter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionHotspotContains(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionHotspotEnterPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionHotspotEnter(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionHotspotLeavePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionHotspotLeave(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionItemCollectPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public bool MustBeEquippableByAvatar { get; protected set; }
        public bool DestroyOnPickup { get; protected set; }
        public bool CountItemsOnMissionStart { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionItemCollect(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionItemEquipPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId ItemPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionItemEquip(mission, owner, this);
        }
    }

    public class MissionConditionInventoryCapacityPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId InventoryPrototype { get; protected set; }
        public int SlotsRemaining { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionInventoryCapacity(mission, owner, this);
        }
    }

    public class MissionConditionKismetSeqFinishedPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId KismetSeqPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionKismetSeqFinished(mission, owner, this);
        }
    }

    public class MissionConditionPowerEquipPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId PowerPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionPowerEquip(mission, owner, this);
        }
    }

    public class MissionConditionPartySizePrototype : MissionPlayerConditionPrototype
    {
        public int MaxSize { get; protected set; }
        public int MinSize { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionPartySize(mission, owner, this);
        }
    }

    public class MissionConditionRegionBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId RegionPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionRegionBeginTravelTo(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (RegionPrototype != PrototypeId.Invalid) refs.Add(RegionPrototype);
        }

        public override void SetInterestLocations(HashSet<PrototypeId> regions, HashSet<PrototypeId> areas, HashSet<PrototypeId> cells)
        {
            if (RegionPrototype != PrototypeId.Invalid) regions.Add(RegionPrototype);
        }
    }

    public class MissionConditionRegionContainsPrototype : MissionConditionPrototype
    {
        public int CountMax { get; protected set; }
        public int CountMin { get; protected set; }
        public PrototypeId Region { get; protected set; }
        public EntityFilterPrototype TargetFilter { get; protected set; }
        public bool RegionIncludeChildren { get; protected set; }
        public PrototypeId[] RegionsExclude { get; protected set; }   // VectorPrototypeRefPtr RegionPrototype

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionRegionContains(mission, owner, this);
        }
    }

    public class MissionConditionRegionEnterPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId RegionPrototype { get; protected set; }
        public bool WaitForCinematicFinished { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }  // VectorPrototypeRefPtr RegionKeywordPrototype
        public bool RegionIncludeChildren { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionRegionEnter(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (RegionPrototype != PrototypeId.Invalid) refs.Add(RegionPrototype);
        }

        public override void SetInterestLocations(HashSet<PrototypeId> regions, HashSet<PrototypeId> areas, HashSet<PrototypeId> cells)
        {
            if (RegionPrototype != PrototypeId.Invalid) regions.Add(RegionPrototype);
        }
    }

    public class MissionConditionRegionHasMatchPrototype : MissionPlayerConditionPrototype
    {
        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionRegionHasMatch(mission, owner, this);
        }
    }

    public class MissionConditionRegionLeavePrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId RegionPrototype { get; protected set; }
        public bool RegionIncludeChildren { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionRegionLeave(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (RegionPrototype != PrototypeId.Invalid) refs.Add(RegionPrototype);
        }
    }

    public class MissionConditionRemoteNotificationPrototype : MissionPlayerConditionPrototype
    {
        public LocaleStringId DialogText { get; protected set; }
        public PrototypeId WorldEntityPrototype { get; protected set; }
        public GameNotificationType NotificationType { get; protected set; }
        public AssetId VOTrigger { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionRemoteNotification(mission, owner, this);
        }
    }

    public class MissionConditionTeamUpIsActivePrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId TeamUpPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionTeamUpIsActive(mission, owner, this);
        }
    }

    public class MissionConditionTeamUpIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public PrototypeId TeamUpPrototype { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionTeamUpIsUnlocked(mission, owner, this);
        }
    }

    public class MissionConditionThrowablePickUpPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionThrowablePickUp(mission, owner, this);
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.FilterContextMissionRef = contextMissionRef;
            entityFilter.AddEntityFilter(EntityFilter);
        }
    }

    public class MissionConditionItemBuyPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionItemBuy(mission, owner, this);
        }
    }

    public class MissionConditionItemDonatePrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionItemDonate(mission, owner, this);
        }
    }

    public class MissionConditionItemCraftPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public PrototypeId UsingRecipe { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionItemCraft(mission, owner, this);
        }
    }

    public class MissionConditionClusterEnemiesClearedPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public bool OnlyCountMissionClusters { get; protected set; }
        public PrototypeId[] SpawnedByMission { get; protected set; }
        public PrototypeId[] SpecificClusters { get; protected set; }
        public PrototypeId[] WithinRegions { get; protected set; }    // VectorPrototypeRefPtr RegionPrototype
        public PrototypeId[] WithinAreas { get; protected set; }      // VectorPrototypeRefPtr AreaPrototype
        public bool PlayerKillerRequired { get; protected set; }

        public override MissionCondition AllocateCondition(Mission mission, IMissionConditionOwner owner)
        {
            return new MissionConditionClusterEnemiesCleared(mission, owner, this);
        }

        public override void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (SpecificClusters.HasValue())
                foreach (var clusterRef in SpecificClusters)
                {
                    var objectProto = GameDatabase.GetPrototype<PopulationObjectPrototype>(clusterRef);
                    objectProto?.GetContainedEntities(refs, true);
                }

            if (WithinRegions.HasValue())
                foreach (var region in WithinRegions)
                    refs.Add(region);
        }

        public override void BuildEntityFilter(EntityFilterWrapper entityFilter, PrototypeId contextMissionRef)
        {
            entityFilter.AddSpawnedByMissionRefs(SpawnedByMission);
            if (OnlyCountMissionClusters && contextMissionRef != PrototypeId.Invalid)
                entityFilter.AddSpawnedByMissionRef(contextMissionRef);

            entityFilter.AddSpawnedByClusterRefs(SpecificClusters);
            entityFilter.AddRegionPtrs(WithinRegions);

            entityFilter.FilterContextMissionRef = contextMissionRef;
        }

    }
}
