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
        public ulong ItemPrototype { get; private set; }
        public long Num { get; private set; }
        public bool Remove { get; private set; }
    }

    public class MissionConditionPrototype : Prototype
    {
        public StoryNotificationPrototype StoryNotification { get; private set; }
        public bool NoTrackingOptimization { get; private set; }
        public long MissionConditionGuid { get; private set; }
    }

    public class MissionPlayerConditionPrototype : MissionConditionPrototype
    {
        public bool PartyMembersGetCredit { get; private set; }
        public double OpenMissionContributionValue { get; private set; }
    }

    public class MissionConditionListPrototype : MissionConditionPrototype
    {
        public MissionConditionPrototype[] Conditions { get; private set; }
    }

    public class MissionConditionAndPrototype : MissionConditionListPrototype
    {
    }

    public class MissionConditionActiveChapterPrototype : MissionPlayerConditionPrototype
    {
        public ulong Chapter { get; private set; }
    }

    public class MissionConditionPowerPointsRemainingPrototype : MissionPlayerConditionPrototype
    {
        public int MinPoints { get; private set; }
        public int MaxPoints { get; private set; }
    }

    public class MissionConditionAreaBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype { get; private set; }
    }

    public class MissionConditionAreaContainsPrototype : MissionConditionPrototype
    {
        public ulong Area { get; private set; }
        public int CountMax { get; private set; }
        public int CountMin { get; private set; }
        public EntityFilterPrototype TargetFilter { get; private set; }
    }

    public class MissionConditionAreaEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; private set; }
        public ulong AreaPrototype { get; private set; }
    }

    public class MissionConditionAreaLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype { get; private set; }
    }

    public class MissionConditionAvatarIsActivePrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype { get; private set; }
    }

    public class MissionConditionAvatarIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype { get; private set; }
    }

    public class MissionConditionAvatarLevelUpPrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype { get; private set; }
        public long Level { get; private set; }
    }

    public class MissionConditionAvatarUsedPowerPrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype { get; private set; }
        public ulong AvatarPrototype { get; private set; }
        public ulong PowerPrototype { get; private set; }
        public ulong RegionPrototype { get; private set; }
        public ulong WithinHotspot { get; private set; }
        public int Count { get; private set; }
        public EntityFilterPrototype TargetFilter { get; private set; }
        public int WithinSeconds { get; private set; }
    }

    public class MissionConditionCellEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong[] Cells { get; private set; }
        public ulong[] Regions { get; private set; }
    }

    public class MissionConditionCellLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong[] Cells { get; private set; }
    }

    public class MissionConditionCohortPrototype : MissionConditionPrototype
    {
        public ulong CohortPrototype { get; private set; }
        public ulong ExperimentPrototype { get; private set; }
    }

    public class MissionConditionCountPrototype : MissionConditionListPrototype
    {
        public long Count { get; private set; }
    }

    public class MissionConditionCurrencyCollectedPrototype : MissionPlayerConditionPrototype
    {
        public long AmountRequired { get; private set; }
        public ulong CurrencyType { get; private set; }
    }

    public class MissionConditionEmotePerformedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EmoteAvatarFilter { get; private set; }
        public ulong EmotePower { get; private set; }
        public bool ObserverAvatarsOnly { get; private set; }
        public EntityFilterPrototype ObserverFilter { get; private set; }
        public int ObserverRadius { get; private set; }
    }

    public class MissionConditionEntityAggroPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class MissionConditionEntityDamagedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
        public ulong EncounterResource { get; private set; }
        public bool LimitToDamageFromPlayerOMOnly { get; private set; }
    }

    public class MissionConditionEntityDeathPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
        public double OpenMissionContribValueDamage { get; private set; }
        public double OpenMissionContribValueTanking { get; private set; }
        public ulong EncounterResource { get; private set; }
        public int DelayDeathMS { get; private set; }
        public bool EncounterResourceValidate { get; private set; }
        public int WithinSeconds { get; private set; }
        public bool MustBeTaggedByPlayer { get; private set; }
        public HUDEntitySettingsPrototype EntityHUDSettingOverride { get; private set; }
    }

    public class MissionConditionEntityInteractPrototype : MissionPlayerConditionPrototype
    {
        public ulong Cinematic { get; private set; }
        public long Count { get; private set; }
        public ulong DialogText { get; private set; }
        public WeightedTextEntryPrototype[] DialogTextList { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
        public LootTablePrototype[] GiveItems { get; private set; }
        public bool IsTurnInNPC { get; private set; }
        public OnInteractAction OnInteract { get; private set; }
        public MissionItemRequiredEntryPrototype[] RequiredItems { get; private set; }
        public ulong WithinHotspot { get; private set; }
        public ulong OnInteractBehavior { get; private set; }
        public MissionActionPrototype[] OnInteractEntityActions { get; private set; }
        public IncrementalActionEntryPrototype[] OnIncrementalActions { get; private set; }
        public ulong DialogTextWhenInventoryFull { get; private set; }
        public bool DropLootOnGround { get; private set; }
        public int WithinSeconds { get; private set; }
        public DialogStyle DialogStyle { get; private set; }
        public HUDEntitySettingsPrototype EntityHUDSettingOverride { get; private set; }
        public bool ShowRewards { get; private set; }
        public VOCategory VoiceoverCategory { get; private set; }
    }

    public class MissionConditionFactionPrototype : MissionPlayerConditionPrototype
    {
        public ulong Faction { get; private set; }
        public bool EventOnly { get; private set; }
    }

    public class MissionConditionGlobalEventCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong Event { get; private set; }
    }

    public class MissionConditionMemberOfEventTeamPrototype : MissionPlayerConditionPrototype
    {
        public ulong Team { get; private set; }
    }

    public class MissionConditionMetaGameCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MetaGame { get; private set; }
        public MetaGameCompleteType CompleteType { get; private set; }
        public int Count { get; private set; }
    }

    public class MissionConditionMetaStateCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MetaState { get; private set; }
        public int MinDifficulty { get; private set; }
        public ulong RequiredAffix { get; private set; }
        public ulong RequiredRarity { get; private set; }
        public int WaveNum { get; private set; }
        public int Count { get; private set; }
    }

    public class MissionConditionMetaStateDeathLimitHitPrototype : MissionConditionPrototype
    {
        public ulong MetaState { get; private set; }
        public int Count { get; private set; }
    }

    public class MissionConditionPublicEventIsActivePrototype : MissionConditionPrototype
    {
        public ulong Event { get; private set; }
    }

    public class MissionConditionLogicFalsePrototype : MissionConditionPrototype
    {
    }

    public class MissionConditionLogicTruePrototype : MissionConditionPrototype
    {
    }

    public class MissionConditionMissionCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MissionPrototype { get; private set; }
        public long Count { get; private set; }
        public DistributionType CreditTo { get; private set; }
        public ulong MissionKeyword { get; private set; }
        public RegionPrototype WithinRegions { get; private set; }
        public bool EvaluateOnRegionEnter { get; private set; }
        public bool EvaluateOnReset { get; private set; }
        public AreaPrototype WithinAreas { get; private set; }
        public MissionShowObjsSettings ShowObjs { get; private set; }
    }

    public class MissionConditionMissionFailedPrototype : MissionPlayerConditionPrototype
    {
        public ulong MissionPrototype { get; private set; }
        public long Count { get; private set; }
        public DistributionType CreditTo { get; private set; }
        public ulong MissionKeyword { get; private set; }
        public RegionPrototype WithinRegions { get; private set; }
        public bool EvaluateOnRegionEnter { get; private set; }
        public bool EvaluateOnReset { get; private set; }
    }

    public class MissionConditionObjectiveCompletePrototype : MissionPlayerConditionPrototype
    {
        public long ObjectiveID { get; private set; }
        public ulong MissionPrototype { get; private set; }
        public bool EvaluateOnReset { get; private set; }
        public long Count { get; private set; }
        public DistributionType CreditTo { get; private set; }
        public bool EvaluateOnRegionEnter { get; private set; }
        public bool ShowCountFromTargetObjective { get; private set; }
    }

    public class MissionConditionOrbPickUpPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class MissionConditionOrPrototype : MissionConditionListPrototype
    {
    }

    public class MissionConditionSpawnerDefeatedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
        public int Count { get; private set; }
    }

    public class MissionConditionHealthRangePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
        public double HealthMinPct { get; private set; }
        public double HealthMaxPct { get; private set; }
    }

    public class MissionConditionHotspotContainsPrototype : MissionConditionPrototype
    {
        public int CountMax { get; private set; }
        public int CountMin { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
        public EntityFilterPrototype TargetFilter { get; private set; }
    }

    public class MissionConditionHotspotEnterPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class MissionConditionHotspotLeavePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class MissionConditionItemCollectPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
        public bool MustBeEquippableByAvatar { get; private set; }
        public bool DestroyOnPickup { get; private set; }
        public bool CountItemsOnMissionStart { get; private set; }
    }

    public class MissionConditionItemEquipPrototype : MissionPlayerConditionPrototype
    {
        public ulong ItemPrototype { get; private set; }
    }

    public class MissionConditionInventoryCapacityPrototype : MissionPlayerConditionPrototype
    {
        public ulong InventoryPrototype { get; private set; }
        public int SlotsRemaining { get; private set; }
    }

    public class MissionConditionKismetSeqFinishedPrototype : MissionPlayerConditionPrototype
    {
        public ulong KismetSeqPrototype { get; private set; }
    }

    public class MissionConditionPowerEquipPrototype : MissionPlayerConditionPrototype
    {
        public ulong PowerPrototype { get; private set; }
    }

    public class MissionConditionPartySizePrototype : MissionPlayerConditionPrototype
    {
        public int MaxSize { get; private set; }
        public int MinSize { get; private set; }
    }

    public class MissionConditionRegionBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; private set; }
    }

    public class MissionConditionRegionContainsPrototype : MissionConditionPrototype
    {
        public int CountMax { get; private set; }
        public int CountMin { get; private set; }
        public ulong Region { get; private set; }
        public EntityFilterPrototype TargetFilter { get; private set; }
        public bool RegionIncludeChildren { get; private set; }
        public RegionPrototype RegionsExclude { get; private set; }
    }

    public class MissionConditionRegionEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; private set; }
        public bool WaitForCinematicFinished { get; private set; }
        public RegionKeywordPrototype Keywords { get; private set; }
        public bool RegionIncludeChildren { get; private set; }
    }

    public class MissionConditionRegionHasMatchPrototype : MissionPlayerConditionPrototype
    {
    }

    public class MissionConditionRegionLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; private set; }
        public bool RegionIncludeChildren { get; private set; }
    }

    public class MissionConditionRemoteNotificationPrototype : MissionPlayerConditionPrototype
    {
        public ulong DialogText { get; private set; }
        public ulong WorldEntityPrototype { get; private set; }
        public GameNotificationType NotificationType { get; private set; }
        public ulong VOTrigger { get; private set; }
    }

    public class MissionConditionTeamUpIsActivePrototype : MissionPlayerConditionPrototype
    {
        public ulong TeamUpPrototype { get; private set; }
    }

    public class MissionConditionTeamUpIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public ulong TeamUpPrototype { get; private set; }
    }

    public class MissionConditionThrowablePickUpPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class MissionConditionItemBuyPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class MissionConditionItemDonatePrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class MissionConditionItemCraftPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; private set; }
        public EntityFilterPrototype EntityFilter { get; private set; }
        public ulong UsingRecipe { get; private set; }
    }

    public class MissionConditionClusterEnemiesClearedPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; private set; }
        public bool OnlyCountMissionClusters { get; private set; }
        public ulong[] SpawnedByMission { get; private set; }
        public ulong[] SpecificClusters { get; private set; }
        public RegionPrototype WithinRegions { get; private set; }
        public AreaPrototype WithinAreas { get; private set; }
        public bool PlayerKillerRequired { get; private set; }
    }
}
