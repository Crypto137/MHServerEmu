using MHServerEmu.Games.GameData.Calligraphy;

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
    }

    [AssetEnum]
    public enum OnInteractAction
    {
        Despawn = 1,
        Disable = 2,
    }

    [AssetEnum]
    public enum MetaGameCompleteType
    {
        Success = 0,
        Failure = 1,
        DidNotParticipate = 2,
    }

    [AssetEnum]
    public enum MissionShowObjsSettings // Missions/Types/ShowObjsSettings.type
    {
        FromTargetMission = 0,
        FromThisMission = 1,
        SuppressAllObjs = 2,
    }

    #endregion

    public class MissionItemRequiredEntryPrototype : Prototype
    {
        public ulong ItemPrototype { get; set; }
        public long Num { get; set; }
        public bool Remove { get; set; }
    }

    public class MissionConditionPrototype : Prototype
    {
        public StoryNotificationPrototype StoryNotification { get; set; }
        public bool NoTrackingOptimization { get; set; }
        public long MissionConditionGuid { get; set; }
    }

    public class MissionPlayerConditionPrototype : MissionConditionPrototype
    {
        public bool PartyMembersGetCredit { get; set; }
        public double OpenMissionContributionValue { get; set; }
    }

    public class MissionConditionListPrototype : MissionConditionPrototype
    {
        public MissionConditionPrototype[] Conditions { get; set; }
    }

    public class MissionConditionAndPrototype : MissionConditionListPrototype
    {
    }

    public class MissionConditionActiveChapterPrototype : MissionPlayerConditionPrototype
    {
        public ulong Chapter { get; set; }
    }

    public class MissionConditionPowerPointsRemainingPrototype : MissionPlayerConditionPrototype
    {
        public int MinPoints { get; set; }
        public int MaxPoints { get; set; }
    }

    public class MissionConditionAreaBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype { get; set; }
    }

    public class MissionConditionAreaContainsPrototype : MissionConditionPrototype
    {
        public ulong Area { get; set; }
        public int CountMax { get; set; }
        public int CountMin { get; set; }
        public EntityFilterPrototype TargetFilter { get; set; }
    }

    public class MissionConditionAreaEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; set; }
        public ulong AreaPrototype { get; set; }
    }

    public class MissionConditionAreaLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype { get; set; }
    }

    public class MissionConditionAvatarIsActivePrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype { get; set; }
    }

    public class MissionConditionAvatarIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype { get; set; }
    }

    public class MissionConditionAvatarLevelUpPrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype { get; set; }
        public long Level { get; set; }
    }

    public class MissionConditionAvatarUsedPowerPrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype { get; set; }
        public ulong AvatarPrototype { get; set; }
        public ulong PowerPrototype { get; set; }
        public ulong RegionPrototype { get; set; }
        public ulong WithinHotspot { get; set; }
        public int Count { get; set; }
        public EntityFilterPrototype TargetFilter { get; set; }
        public int WithinSeconds { get; set; }
    }

    public class MissionConditionCellEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong[] Cells { get; set; }
        public ulong[] Regions { get; set; }
    }

    public class MissionConditionCellLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong[] Cells { get; set; }
    }

    public class MissionConditionCohortPrototype : MissionConditionPrototype
    {
        public ulong CohortPrototype { get; set; }
        public ulong ExperimentPrototype { get; set; }
    }

    public class MissionConditionCountPrototype : MissionConditionListPrototype
    {
        public long Count { get; set; }
    }

    public class MissionConditionCurrencyCollectedPrototype : MissionPlayerConditionPrototype
    {
        public long AmountRequired { get; set; }
        public ulong CurrencyType { get; set; }
    }

    public class MissionConditionEmotePerformedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EmoteAvatarFilter { get; set; }
        public ulong EmotePower { get; set; }
        public bool ObserverAvatarsOnly { get; set; }
        public EntityFilterPrototype ObserverFilter { get; set; }
        public int ObserverRadius { get; set; }
    }

    public class MissionConditionEntityAggroPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class MissionConditionEntityDamagedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
        public ulong EncounterResource { get; set; }
        public bool LimitToDamageFromPlayerOMOnly { get; set; }
    }

    public class MissionConditionEntityDeathPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
        public double OpenMissionContribValueDamage { get; set; }
        public double OpenMissionContribValueTanking { get; set; }
        public ulong EncounterResource { get; set; }
        public int DelayDeathMS { get; set; }
        public bool EncounterResourceValidate { get; set; }
        public int WithinSeconds { get; set; }
        public bool MustBeTaggedByPlayer { get; set; }
        public HUDEntitySettingsPrototype EntityHUDSettingOverride { get; set; }
    }

    public class MissionConditionEntityInteractPrototype : MissionPlayerConditionPrototype
    {
        public ulong Cinematic { get; set; }
        public long Count { get; set; }
        public ulong DialogText { get; set; }
        public WeightedTextEntryPrototype[] DialogTextList { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
        public LootTablePrototype[] GiveItems { get; set; }
        public bool IsTurnInNPC { get; set; }
        public OnInteractAction OnInteract { get; set; }
        public MissionItemRequiredEntryPrototype[] RequiredItems { get; set; }
        public ulong WithinHotspot { get; set; }
        public ulong OnInteractBehavior { get; set; }
        public MissionActionPrototype[] OnInteractEntityActions { get; set; }
        public IncrementalActionEntryPrototype[] OnIncrementalActions { get; set; }
        public ulong DialogTextWhenInventoryFull { get; set; }
        public bool DropLootOnGround { get; set; }
        public int WithinSeconds { get; set; }
        public DialogStyle DialogStyle { get; set; }
        public HUDEntitySettingsPrototype EntityHUDSettingOverride { get; set; }
        public bool ShowRewards { get; set; }
        public VOCategory VoiceoverCategory { get; set; }
    }

    public class MissionConditionFactionPrototype : MissionPlayerConditionPrototype
    {
        public ulong Faction { get; set; }
        public bool EventOnly { get; set; }
    }

    public class MissionConditionGlobalEventCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong Event { get; set; }
    }

    public class MissionConditionMemberOfEventTeamPrototype : MissionPlayerConditionPrototype
    {
        public ulong Team { get; set; }
    }

    public class MissionConditionMetaGameCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MetaGame { get; set; }
        public MetaGameCompleteType CompleteType { get; set; }
        public int Count { get; set; }
    }

    public class MissionConditionMetaStateCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MetaState { get; set; }
        public int MinDifficulty { get; set; }
        public ulong RequiredAffix { get; set; }
        public ulong RequiredRarity { get; set; }
        public int WaveNum { get; set; }
        public int Count { get; set; }
    }

    public class MissionConditionMetaStateDeathLimitHitPrototype : MissionConditionPrototype
    {
        public ulong MetaState { get; set; }
        public int Count { get; set; }
    }

    public class MissionConditionPublicEventIsActivePrototype : MissionConditionPrototype
    {
        public ulong Event { get; set; }
    }

    public class MissionConditionLogicFalsePrototype : MissionConditionPrototype
    {
    }

    public class MissionConditionLogicTruePrototype : MissionConditionPrototype
    {
    }

    public class MissionConditionMissionCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MissionPrototype { get; set; }
        public long Count { get; set; }
        public DistributionType CreditTo { get; set; }
        public ulong MissionKeyword { get; set; }
        public RegionPrototype WithinRegions { get; set; }
        public bool EvaluateOnRegionEnter { get; set; }
        public bool EvaluateOnReset { get; set; }
        public AreaPrototype WithinAreas { get; set; }
        public MissionShowObjsSettings ShowObjs { get; set; }
    }

    public class MissionConditionMissionFailedPrototype : MissionPlayerConditionPrototype
    {
        public ulong MissionPrototype { get; set; }
        public long Count { get; set; }
        public DistributionType CreditTo { get; set; }
        public ulong MissionKeyword { get; set; }
        public RegionPrototype WithinRegions { get; set; }
        public bool EvaluateOnRegionEnter { get; set; }
        public bool EvaluateOnReset { get; set; }
    }

    public class MissionConditionObjectiveCompletePrototype : MissionPlayerConditionPrototype
    {
        public long ObjectiveID { get; set; }
        public ulong MissionPrototype { get; set; }
        public bool EvaluateOnReset { get; set; }
        public long Count { get; set; }
        public DistributionType CreditTo { get; set; }
        public bool EvaluateOnRegionEnter { get; set; }
        public bool ShowCountFromTargetObjective { get; set; }
    }

    public class MissionConditionOrbPickUpPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class MissionConditionOrPrototype : MissionConditionListPrototype
    {
    }

    public class MissionConditionSpawnerDefeatedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
        public int Count { get; set; }
    }

    public class MissionConditionHealthRangePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
        public double HealthMinPct { get; set; }
        public double HealthMaxPct { get; set; }
    }

    public class MissionConditionHotspotContainsPrototype : MissionConditionPrototype
    {
        public int CountMax { get; set; }
        public int CountMin { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
        public EntityFilterPrototype TargetFilter { get; set; }
    }

    public class MissionConditionHotspotEnterPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class MissionConditionHotspotLeavePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class MissionConditionItemCollectPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
        public bool MustBeEquippableByAvatar { get; set; }
        public bool DestroyOnPickup { get; set; }
        public bool CountItemsOnMissionStart { get; set; }
    }

    public class MissionConditionItemEquipPrototype : MissionPlayerConditionPrototype
    {
        public ulong ItemPrototype { get; set; }
    }

    public class MissionConditionInventoryCapacityPrototype : MissionPlayerConditionPrototype
    {
        public ulong InventoryPrototype { get; set; }
        public int SlotsRemaining { get; set; }
    }

    public class MissionConditionKismetSeqFinishedPrototype : MissionPlayerConditionPrototype
    {
        public ulong KismetSeqPrototype { get; set; }
    }

    public class MissionConditionPowerEquipPrototype : MissionPlayerConditionPrototype
    {
        public ulong PowerPrototype { get; set; }
    }

    public class MissionConditionPartySizePrototype : MissionPlayerConditionPrototype
    {
        public int MaxSize { get; set; }
        public int MinSize { get; set; }
    }

    public class MissionConditionRegionBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; set; }
    }

    public class MissionConditionRegionContainsPrototype : MissionConditionPrototype
    {
        public int CountMax { get; set; }
        public int CountMin { get; set; }
        public ulong Region { get; set; }
        public EntityFilterPrototype TargetFilter { get; set; }
        public bool RegionIncludeChildren { get; set; }
        public RegionPrototype RegionsExclude { get; set; }
    }

    public class MissionConditionRegionEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; set; }
        public bool WaitForCinematicFinished { get; set; }
        public RegionKeywordPrototype Keywords { get; set; }
        public bool RegionIncludeChildren { get; set; }
    }

    public class MissionConditionRegionHasMatchPrototype : MissionPlayerConditionPrototype
    {
    }

    public class MissionConditionRegionLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; set; }
        public bool RegionIncludeChildren { get; set; }
    }

    public class MissionConditionRemoteNotificationPrototype : MissionPlayerConditionPrototype
    {
        public ulong DialogText { get; set; }
        public ulong WorldEntityPrototype { get; set; }
        public GameNotificationType NotificationType { get; set; }
        public ulong VOTrigger { get; set; }
    }

    public class MissionConditionTeamUpIsActivePrototype : MissionPlayerConditionPrototype
    {
        public ulong TeamUpPrototype { get; set; }
    }

    public class MissionConditionTeamUpIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public ulong TeamUpPrototype { get; set; }
    }

    public class MissionConditionThrowablePickUpPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class MissionConditionItemBuyPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class MissionConditionItemDonatePrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class MissionConditionItemCraftPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; set; }
        public EntityFilterPrototype EntityFilter { get; set; }
        public ulong UsingRecipe { get; set; }
    }

    public class MissionConditionClusterEnemiesClearedPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; set; }
        public bool OnlyCountMissionClusters { get; set; }
        public ulong[] SpawnedByMission { get; set; }
        public ulong[] SpecificClusters { get; set; }
        public RegionPrototype WithinRegions { get; set; }
        public AreaPrototype WithinAreas { get; set; }
        public bool PlayerKillerRequired { get; set; }
    }
}
