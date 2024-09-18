using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    using static MHServerEmu.Games.Missions.MissionManager;

    #region Enums

    [AssetEnum]
    public enum DailyMissionBannerImageType     // UI/Types/DailyMissionBannerType.type
    {
        Asgard = 0,
        CastleAndMoon = 1,
        Cave = 2,
        CityNight = 3,
        CityNight_v2 = 4,
        MetalFloor = 5,
        Nightclub = 6,
        Norway = 7,
        Odin = 8,
        RedMoon = 9,
        Space = 10,
        TowerBaseSunset = 11,
    }

    [AssetEnum((int)Invalid)]
    public enum MissionTimeExpiredResult        // Missions/Types/OnTimeExpired.type
    {
        Invalid = 0,
        Complete = 1,
        Fail = 2,
    }

    [AssetEnum((int)Never)]
    public enum MissionShowInTracker            // Missions/Types/ShowInTracker.type
    {
        Never = 0,
        IfObjectivesVisible = 1,
        Always = 2,
    }

    [AssetEnum((int)Invalid)]
    public enum MissionShowInLog                // Missions/Types/ShowInMissionLog.type
    {
        Invalid = -1,
        Never = 0,
        OnlyWhenActive = 1,
        Always = 2,
    }

    [AssetEnum]
    public enum DailyMissionType
    {
        Patrol = 0,
        Survival = 1,
        Terminal = 2,
    }

    [AssetEnum((int)Invalid)]
    public enum DailyMissionResetFrequency
    {
        Invalid = -1,
        Daily = 0,
        Weekly = 1,
    }

    [AssetEnum((int)Invalid)]
    public enum AdvancedMissionFrequencyType
    {
        Invalid = 0,
        Repeatable = 1,
        Daily = 2,
        Weekly = 3,
    }

    #endregion

    public class MissionGlobalsPrototype : Prototype
    {
        public int MissionLevelLowerBoundsOffset { get; protected set; }
        public int MissionLevelUpperBoundsOffset { get; protected set; }
        public CurveId OpenMissionContributionReward { get; protected set; }
        public PrototypeId InitialChapter { get; protected set; }
        public BannerMessagePrototype InventoryFullMessage { get; protected set; }
        public PrototypeId InitialStoryWarp { get; protected set; }
        public PrototypeId MigrationStoryEndMission { get; protected set; }
        public int LegendaryMissionLevelUnlock { get; protected set; }
        public PrototypeId LegendaryChapter { get; protected set; }
        public PrototypeId LegendaryMissionPlaceholder { get; protected set; }
        public EvalPrototype LegendaryRerollCost { get; protected set; }
        public LocaleStringId LegendaryMissionLogTooltip { get; protected set; }
        public PrototypeId LoreChapter { get; protected set; }
        public DailyMissionBannerImageType DailyMissionBannerFriday { get; protected set; }
        public DailyMissionBannerImageType DailyMissionBannerMonday { get; protected set; }
        public DailyMissionBannerImageType DailyMissionBannerSaturday { get; protected set; }
        public DailyMissionBannerImageType DailyMissionBannerSunday { get; protected set; }
        public DailyMissionBannerImageType DailyMissionBannerThursday { get; protected set; }
        public DailyMissionBannerImageType DailyMissionBannerTuesday { get; protected set; }
        public DailyMissionBannerImageType DailyMissionBannerWednesday { get; protected set; }
        public PrototypeId EventMissionsChapter { get; protected set; }
        public PrototypeId AccountMissionsChapter { get; protected set; }
    }

    public class MissionTypePrototype : Prototype
    {
        public LocaleStringId Name { get; protected set; }
        public int Priority { get; protected set; }
        public AssetId EdgeIcon { get; protected set; }
        public PrototypeId MapIcon { get; protected set; }
    }

    public class MissionItemDropEntryPrototype : Prototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public PrototypeId LootTablePrototype { get; protected set; }
    }

    public class MissionPopulationEntryPrototype : Prototype
    {
        public long Count { get; protected set; }
        public PopulationObjectPrototype Population { get; protected set; }
        public PrototypeId[] RestrictToAreas { get; protected set; }
        public PrototypeId[] RestrictToRegions { get; protected set; }            // VectorPrototypeRefPtr RegionPrototype
        public PrototypeId[] RestrictToRegionsExclude { get; protected set; }     // VectorPrototypeRefPtr RegionPrototype
        public bool RestrictToRegionsIncludeChildren { get; protected set; }
        public AssetId[] RestrictToCells { get; protected set; }
        public PrototypeId RestrictToDifficultyMin { get; protected set; }
        public PrototypeId RestrictToDifficultyMax { get; protected set; }
    }

    public class MissionDialogTextPrototype : Prototype
    {
        public LocaleStringId Text { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public DialogStyle DialogStyle { get; protected set; }

        public void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (EntityFilter != null)
            {
                EntityFilter.GetEntityDataRefs(refs);
                EntityFilter.GetKeywordDataRefs(refs);
                EntityFilter.GetRegionDataRefs(refs);
            }
        }
    }

    public class MissionObjectiveHintPrototype : Prototype
    {
        public EntityFilterPrototype PlayerStateFilter { get; protected set; }
        public EntityFilterPrototype TargetEntity { get; protected set; }
        public PrototypeId TargetArea { get; protected set; }
        public PrototypeId TargetRegion { get; protected set; }

        public void GetPrototypeContextRefs(HashSet<PrototypeId> refs)
        {
            if (TargetEntity != null)
            {
                TargetEntity.GetEntityDataRefs(refs);
                TargetEntity.GetAreaDataRefs(refs);
                TargetEntity.GetRegionDataRefs(refs);
            }

            if (TargetRegion != PrototypeId.Invalid)
            {
                refs.Add(TargetRegion);
            }

            if (TargetArea != PrototypeId.Invalid)
            {
                refs.Add(TargetArea);
            }
        }
    }

    public class MissionObjectivePrototype : Prototype
    {
        public MissionDialogTextPrototype[] DialogText { get; protected set; }
        public MissionConditionListPrototype FailureConditions { get; protected set; }
        public MissionItemDropEntryPrototype[] ItemDrops { get; protected set; }
        public bool ItemDropsCleanupRemaining { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public MissionActionPrototype[] OnFailActions { get; protected set; }
        public MissionActionPrototype[] OnStartActions { get; protected set; }
        public MissionActionPrototype[] OnSuccessActions { get; protected set; }
        public MissionConditionListPrototype ActivateConditions { get; protected set; }
        public MissionConditionListPrototype SuccessConditions { get; protected set; }
        public MissionTimeExpiredResult TimeExpiredResult { get; protected set; }
        public long TimeLimitSeconds { get; protected set; }
        public InteractionSpecPrototype[] InteractionsWhenActive { get; protected set; }
        public InteractionSpecPrototype[] InteractionsWhenComplete { get; protected set; }
        public LocaleStringId TextWhenCompleted { get; protected set; }
        public LocaleStringId TextWhenUpdated { get; protected set; }
        public bool ShowInMissionLog { get; protected set; }
        public bool Required { get; protected set; }
        public bool ShowNotificationIcon { get; protected set; }
        public bool Checkpoint { get; protected set; }
        public bool ShowInMissionTracker { get; protected set; }
        public LocaleStringId MissionLogAppendWhenActive { get; protected set; }
        public bool PlayerHUDShowObjsOnMap { get; protected set; }
        public bool PlayerHUDShowObjsOnMapNoPing { get; protected set; }
        public bool PlayerHUDShowObjsOnScreenEdge { get; protected set; }
        public bool PlayerHUDShowObjsOnEntityFloor { get; protected set; }
        public int PlayerHUDObjectiveArrowDistOvrde { get; protected set; }
        public MissionObjectiveHintPrototype[] ObjectiveHints { get; protected set; }
        public bool ShowCountInUI { get; protected set; }
        public bool ShowTimerInUI { get; protected set; }
        public float Order { get; protected set; }
        public PrototypeId MetaGameWidget { get; protected set; }
        public PrototypeId MetaGameWidgetFail { get; protected set; }
        public bool FailureFailsMission { get; protected set; }
        public bool ShowFailCountInUI { get; protected set; }
        public LocaleStringId TextWhenFailed { get; protected set; }
        public LocaleStringId TextWhenFailUpdated { get; protected set; }
        public EvalPrototype TimeLimitSecondsEval { get; protected set; }
        public LootTablePrototype[] Rewards { get; protected set; }
        public int CounterType { get; protected set; }
        public LocaleStringId MetaGameDetails { get; protected set; }
        public int MetaGameDetailsDelayMS { get; protected set; }
        public AssetId MetaGameDetailsNPCIconPath { get; protected set; }
        public PrototypeId LogoffEntryDisplayIfNotComplete { get; protected set; }
        public LocaleStringId MissionLogObjectiveHint { get; protected set; }
        public PrototypeId MusicState { get; protected set; }
        public MissionDialogTextPrototype[] DialogTextWhenCompleted { get; protected set; }
        public MissionDialogTextPrototype[] DialogTextWhenFailed { get; protected set; }
        public InteractionSpecPrototype[] InteractionsWhenFailed { get; protected set; }
        public bool PlayerHUDShowObjsOnEntityAbove { get; protected set; }
        public MissionActionPrototype[] OnAvailableActions { get; protected set; }

        public void EnumerateConditions(ref int index)
        {
            MissionPrototype.EnumerateConditionList(ref index, ActivateConditions);
            MissionPrototype.EnumerateConditionList(ref index, FailureConditions);
            MissionPrototype.EnumerateConditionList(ref index, SuccessConditions);
        }

        public bool GetConditionsOfType(Type conditionType, List<MissionConditionPrototype> conditions)
        {
            MissionPrototype.GetConditionsOfTypeFromConditionList(conditionType, conditions, ActivateConditions);
            MissionPrototype.GetConditionsOfTypeFromConditionList(conditionType, conditions, FailureConditions);
            MissionPrototype.GetConditionsOfTypeFromConditionList(conditionType, conditions, SuccessConditions);

            return conditions.Count > 0;
        }
    }

    public class MissionNamedObjectivePrototype : MissionObjectivePrototype
    {
        public long ObjectiveID { get; protected set; }
        public bool SendMetricEvents { get; protected set; }
    }

    public class OpenMissionRewardEntryPrototype : Prototype
    {
        public PrototypeId ChestEntity { get; protected set; }
        public double ContributionPercentage { get; protected set; }
        public PrototypeId[] Rewards { get; protected set; }
    }

    public class MissionPrototype : Prototype
    {
        public MissionConditionListPrototype ActivateConditions { get; protected set; }
        public PrototypeId Chapter { get; protected set; }
        public MissionDialogTextPrototype[] DialogText { get; protected set; }
        public MissionConditionListPrototype FailureConditions { get; protected set; }
        public long Level { get; protected set; }
        public LocaleStringId MissionLogDescription { get; protected set; }
        public LocaleStringId Name { get; protected set; }
        public MissionObjectivePrototype[] Objectives { get; protected set; }
        public MissionActionPrototype[] OnFailActions { get; protected set; }
        public MissionActionPrototype[] OnStartActions { get; protected set; }
        public MissionActionPrototype[] OnSuccessActions { get; protected set; }
        public MissionPopulationEntryPrototype[] PopulationSpawns { get; protected set; }
        public MissionConditionListPrototype PrereqConditions { get; protected set; }
        public bool Repeatable { get; protected set; }
        public LootTablePrototype[] Rewards { get; protected set; }
        public MissionTimeExpiredResult TimeExpiredResult { get; protected set; }
        public long TimeLimitSeconds { get; protected set; }
        public InteractionSpecPrototype[] InteractionsWhenActive { get; protected set; }
        public InteractionSpecPrototype[] InteractionsWhenComplete { get; protected set; }
        public LocaleStringId TextWhenActivated { get; protected set; }
        public LocaleStringId TextWhenCompleted { get; protected set; }
        public LocaleStringId TextWhenFailed { get; protected set; }
        public bool ShowInteractIndicators { get; protected set; }
        public bool ShowBannerMessages { get; protected set; }
        public bool ShowInMissionLogDEPRECATED { get; protected set; }
        public bool ShowNotificationIcon { get; protected set; }
        public int SortOrder { get; protected set; }
        public MissionConditionListPrototype ActivateNowConditions { get; protected set; }
        public MissionShowInTracker ShowInMissionTracker { get; protected set; }
        public PrototypeId ResetsWithRegion { get; protected set; }
        public LocaleStringId MissionLogDescriptionComplete { get; protected set; }
        public bool PlayerHUDShowObjs { get; protected set; }
        public bool PlayerHUDShowObjsOnMap { get; protected set; }
        public bool PlayerHUDShowObjsOnMapNoPing { get; protected set; }
        public bool PlayerHUDShowObjsOnScreenEdge { get; protected set; }
        public bool PlayerHUDShowObjsOnEntityFloor { get; protected set; }
        public bool PlayerHUDShowObjsNoActivateCond { get; protected set; }
        public DesignWorkflowState DesignState { get; protected set; }
        public long ResetTimeSeconds { get; protected set; }
        public bool ShowInMissionTrackerFilterByChap { get; protected set; }
        public bool ShowMapPingOnPortals { get; protected set; }
        public bool PopulationRequired { get; protected set; }
        public bool SaveStatePerAvatar { get; protected set; }
        public MissionTrackerFilterType ShowInMissionTrackerFilterType { get; protected set; }
        public int Version { get; protected set; }
        public bool DEPRewardLevelBasedOnAvatarLevel { get; protected set; }
        public LocaleStringId MissionLogHint { get; protected set; }
        public PrototypeId LootCooldownChannel { get; protected set; }
        public LocaleStringId MetaGameDetails { get; protected set; }
        public int MetaGameDetailsDelayMS { get; protected set; }
        public bool ShowTimerInUI { get; protected set; }
        public AssetId MetaGameDetailsNPCIconPath { get; protected set; }
        public bool DropLootOnGround { get; protected set; }
        public PrototypeId[] Keywords { get; protected set; }
        public PrototypeId[] RegionRestrictionKeywords { get; protected set; }
        public bool ForceTrackerPageOnStart { get; protected set; }
        public PrototypeId MusicState { get; protected set; }
        public MissionDialogTextPrototype[] DialogTextWhenCompleted { get; protected set; }
        public MissionDialogTextPrototype[] DialogTextWhenFailed { get; protected set; }
        public InteractionSpecPrototype[] InteractionsWhenFailed { get; protected set; }
        public bool PlayerHUDShowObjsOnEntityAbove { get; protected set; }
        public PrototypeId MissionType { get; protected set; }
        public MissionShowInLog ShowInMissionLog { get; protected set; }
        public bool SuspendIfNoMatchingKeyword { get; protected set; }
        public MissionActionPrototype[] OnAvailableActions { get; protected set; }
        public MissionConditionListPrototype CompleteNowConditions { get; protected set; }
        public LootTablePrototype[] CompleteNowRewards { get; protected set; }
        public DesignWorkflowState DesignStatePS4 { get; protected set; }
        public DesignWorkflowState DesignStateXboxOne { get; protected set; }

        [DoNotCopy]
        public PrototypeId FirstMarker { get; private set; }
        [DoNotCopy]
        public bool HasClientInterest { get; private set; } = true;
        [DoNotCopy]
        public bool HasItemDrops { get; private set; }
        [DoNotCopy]
        public bool HasMissionLogRewards { get; private set; }
        [DoNotCopy]
        public List<MissionConditionPrototype> HotspotConditionList { get; private set; }

        [DoNotCopy]
        public int MissionPrototypeEnumValue { get; private set; }

        private readonly SortedSet<PrototypeId> PopulationRegions = new();
        private readonly SortedSet<PrototypeId> PopulationAreas = new();
        private KeywordsMask _keywordsMask;
        private KeywordsMask _regionRestrictionKeywordsMask;

        public override bool ApprovedForUse()
        {
            // TODO: console support            
            if (DisabledMissions.Contains((MissionPrototypeId)DataRef)) return false;
            if (EventMissions.Contains((MissionPrototypeId)DataRef)) return true;            
            return GameDatabase.DesignStateOk(DesignState);
        }

        public bool IsLiveTuningEnabled()
        {
            return LiveTuningManager.GetLiveMissionTuningVar(this, MissionTuningVar.eMTV_Enabled) != 0f;
        }

        public bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && KeywordPrototype.TestKeywordBit(_keywordsMask, keywordProto);
        }

        public bool HasRegionRestrictionKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && KeywordPrototype.TestKeywordBit(_regionRestrictionKeywordsMask, keywordProto);
        }

        public override void PostProcess()
        {
            base.PostProcess();

            _keywordsMask = KeywordPrototype.GetBitMaskForKeywordList(Keywords);
            _regionRestrictionKeywordsMask = KeywordPrototype.GetBitMaskForKeywordList(RegionRestrictionKeywords);
            
            EnumerateConditions();

            HotspotConditionList = new List<MissionConditionPrototype>();
            GetConditionsOfType(typeof(MissionConditionHotspotEnterPrototype), HotspotConditionList);
            GetConditionsOfType(typeof(MissionConditionHotspotLeavePrototype), HotspotConditionList);

            if (HotspotConditionList.Count == 0)
                HotspotConditionList = null;
            /*
            if (GameDatabase.DataDirectory.PrototypeIsAbstract(DataRef) == false)
                FirstMarker = FindFirstMarker();
            else
                FirstMarker = PrototypeId.Invalid;

            PopulateMissionActionReferencedPowers();
            HasClientInterest = GetHasClientInterest();
            HasItemDrops = GetHasItemDrops();
            HasMissionLogRewards = GetHasMissionLogRewards();*/
            
            PopulatePopulationForZoneLookups(PopulationRegions, PopulationAreas);

            MissionPrototypeEnumValue = GetEnumValueFromBlueprint(LiveTuningData.GetMissionBlueprintDataRef());
        }

        private bool GetHasMissionLogRewards()
        {
            throw new NotImplementedException();
        }

        private bool GetHasItemDrops()
        {
            throw new NotImplementedException();
        }

        private bool GetHasClientInterest()
        {
            throw new NotImplementedException();
        }

        private void PopulateMissionActionReferencedPowers()
        {
            throw new NotImplementedException();
        }

        private PrototypeId FindFirstMarker()
        {
            throw new NotImplementedException();
        }

        private bool GetConditionsOfType(Type conditionType, List<MissionConditionPrototype> conditions)
        {
            GetConditionsOfTypeFromConditionList(conditionType, conditions, ActivateConditions);
            GetConditionsOfTypeFromConditionList(conditionType, conditions, ActivateNowConditions);
            GetConditionsOfTypeFromConditionList(conditionType, conditions, FailureConditions);
            GetConditionsOfTypeFromConditionList(conditionType, conditions, PrereqConditions);

            if (Objectives.HasValue())
                foreach (var missionObjectiveProto in Objectives)
                    missionObjectiveProto?.GetConditionsOfType(conditionType, conditions);

            return conditions.Count > 0;
        }

        public static void GetConditionsOfTypeFromConditionList(Type conditionType, List<MissionConditionPrototype> conditions, MissionConditionListPrototype conditionList)
        {
            if (conditionList != null)
                foreach (var prototype in conditionList.IteratePrototypes(conditionType))
                    conditions.Add(prototype);
        }

        public void EnumerateConditions()
        {
            int index = 0;
            EnumerateConditionList(ref index, ActivateConditions);
            EnumerateConditionList(ref index, ActivateNowConditions);
            EnumerateConditionList(ref index, FailureConditions);
            EnumerateConditionList(ref index, PrereqConditions);

            if (Objectives.HasValue())
                foreach (var missionObjectivePrototype in Objectives)
                    missionObjectivePrototype?.EnumerateConditions(ref index);
        }

        public static void EnumerateConditionList(ref int index, MissionConditionListPrototype conditionList)
        {
            if (conditionList != null)
                foreach (var prototype in conditionList.IteratePrototypes())
                    if (prototype != null) prototype.Index = index++;
        }

        public bool HasPopulationInRegion(Region region)
        {
            if (PopulationSpawns.HasValue())
            {
                PrototypeId regionRef = region.PrototypeDataRef;

                if (PopulationRegions.Any())
                    return PopulationRegions.Contains(regionRef);

                if (PopulationAreas.Any())
                    foreach (var areaRef in PopulationAreas)
                        if (region.GetArea(areaRef) != null) return true;
            }
            return false;
        }

        public bool PopulatePopulationForZoneLookups(SortedSet<PrototypeId> regions, SortedSet<PrototypeId> areas)
        {
            if (PopulationSpawns.HasValue())
            {
                foreach (var entryProto in PopulationSpawns)
                {
                    if (entryProto == null) continue;                    
                    if (entryProto.RestrictToRegions.HasValue())
                    {
                        foreach (var restrictRef in entryProto.RestrictToRegions)
                        {
                            if (restrictRef == PrototypeId.Invalid) continue;                            
                            regions.Add(restrictRef);

                            if (entryProto.RestrictToRegionsIncludeChildren) 
                            {
                                foreach (var regionRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<RegionPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
                                {                                        
                                    if (GameDatabase.DataDirectory.PrototypeIsAPrototype(regionRef, restrictRef))  
                                        regions.Add(regionRef);
                                }
                            }
                            
                        }
                    }

                    if (entryProto.RestrictToAreas.HasValue())
                    {
                        foreach (var areaRef in entryProto.RestrictToAreas)
                            areas.Add(areaRef);
                    }                    
                }

                if (regions.Count > 0)
                {
                    List<PrototypeId> regionList = new (regions);
                    foreach (PrototypeId regionRef in regionList)
                    {
                        RegionPrototype regionProto = GameDatabase.GetPrototype<RegionPrototype>(regionRef);
                        if (regionProto != null && regionProto.AltRegions.HasValue())
                        {
                            foreach (var altRegionRef in regionProto.AltRegions)
                                regions.Add(altRegionRef);
                        }
                    }
                }
            }

            return regions.Count > 0;
        }
    }

    public class OpenMissionPrototype : MissionPrototype
    {
        public bool ParticipationBasedOnAreaCell { get; protected set; }
        public OpenMissionRewardEntryPrototype[] RewardsByContribution { get; protected set; }
        public StoryNotificationPrototype StoryNotification { get; protected set; }
        public PrototypeId[] ActiveInRegions { get; protected set; }          // VectorPrototypeRefPtr RegionPrototype
        public bool ActiveInRegionsIncludeChildren { get; protected set; }
        public PrototypeId[] ActiveInRegionsExclude { get; protected set; }   // VectorPrototypeRefPtr RegionPrototype
        public PrototypeId[] ActiveInAreas { get; protected set; }
        public AssetId[] ActiveInCells { get; protected set; }
        public bool ResetWhenUnsimulated { get; protected set; }
        public double MinimumContributionForCredit { get; protected set; }
        public bool RespawnInPlace { get; protected set; }
        public double ParticipantTimeoutInSeconds { get; protected set; }
        public bool RespawnOnRestart { get; protected set; }
        public int IdleTimeoutSeconds { get; protected set; }
        public double ParticipationContributionValue { get; protected set; }
        public long AchievementTimeLimitSeconds { get; protected set; }
        public bool ShowToastMessages { get; protected set; }
    }

    public class LegendaryMissionCategoryPrototype : Prototype
    {
        public LocaleStringId Name { get; protected set; }
        public int Weight { get; protected set; }
        public int BlacklistLength { get; protected set; }
    }

    public class LegendaryMissionPrototype : MissionPrototype
    {
        public EvalPrototype EvalCanStart { get; protected set; }
        public PrototypeId Category { get; protected set; }
    }

    public class DailyMissionPrototype : MissionPrototype
    {
        public Weekday Day { get; protected set; }
        public DailyMissionType Type { get; protected set; }
        public AssetId Image { get; protected set; }
        public DailyMissionResetFrequency ResetFrequency { get; protected set; }
    }

    public class AdvancedMissionCategoryPrototype : LegendaryMissionCategoryPrototype
    {
        public Weekday WeeklyResetDay { get; protected set; }
        public AdvancedMissionFrequencyType MissionType { get; protected set; }
        public PrototypeId CategoryLabel { get; protected set; }
    }

    public class AdvancedMissionPrototype : MissionPrototype
    {
        public PrototypeId CategoryType { get; protected set; }
        public PrototypeId ReputationExperienceType { get; protected set; }
    }
}
