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
        public ulong ItemPrototype { get; protected set; }
        public long Num { get; protected set; }
        public bool Remove { get; protected set; }
    }

    public class MissionConditionPrototype : Prototype
    {
        public StoryNotificationPrototype StoryNotification { get; protected set; }
        public bool NoTrackingOptimization { get; protected set; }
        public long MissionConditionGuid { get; protected set; }
    }

    public class MissionPlayerConditionPrototype : MissionConditionPrototype
    {
        public bool PartyMembersGetCredit { get; protected set; }
        public double OpenMissionContributionValue { get; protected set; }
    }

    public class MissionConditionListPrototype : MissionConditionPrototype
    {
        public MissionConditionPrototype[] Conditions { get; protected set; }
    }

    public class MissionConditionAndPrototype : MissionConditionListPrototype
    {
    }

    public class MissionConditionActiveChapterPrototype : MissionPlayerConditionPrototype
    {
        public ulong Chapter { get; protected set; }
    }

    public class MissionConditionPowerPointsRemainingPrototype : MissionPlayerConditionPrototype
    {
        public int MinPoints { get; protected set; }
        public int MaxPoints { get; protected set; }
    }

    public class MissionConditionAreaBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype { get; protected set; }
    }

    public class MissionConditionAreaContainsPrototype : MissionConditionPrototype
    {
        public ulong Area { get; protected set; }
        public int CountMax { get; protected set; }
        public int CountMin { get; protected set; }
        public EntityFilterPrototype TargetFilter { get; protected set; }
    }

    public class MissionConditionAreaEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; protected set; }
        public ulong AreaPrototype { get; protected set; }
    }

    public class MissionConditionAreaLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype { get; protected set; }
    }

    public class MissionConditionAvatarIsActivePrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype { get; protected set; }
    }

    public class MissionConditionAvatarIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype { get; protected set; }
    }

    public class MissionConditionAvatarLevelUpPrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype { get; protected set; }
        public long Level { get; protected set; }
    }

    public class MissionConditionAvatarUsedPowerPrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype { get; protected set; }
        public ulong AvatarPrototype { get; protected set; }
        public ulong PowerPrototype { get; protected set; }
        public ulong RegionPrototype { get; protected set; }
        public ulong WithinHotspot { get; protected set; }
        public int Count { get; protected set; }
        public EntityFilterPrototype TargetFilter { get; protected set; }
        public int WithinSeconds { get; protected set; }
    }

    public class MissionConditionCellEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong[] Cells { get; protected set; }
        public ulong[] Regions { get; protected set; }
    }

    public class MissionConditionCellLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong[] Cells { get; protected set; }
    }

    public class MissionConditionCohortPrototype : MissionConditionPrototype
    {
        public ulong CohortPrototype { get; protected set; }
        public ulong ExperimentPrototype { get; protected set; }
    }

    public class MissionConditionCountPrototype : MissionConditionListPrototype
    {
        public long Count { get; protected set; }
    }

    public class MissionConditionCurrencyCollectedPrototype : MissionPlayerConditionPrototype
    {
        public long AmountRequired { get; protected set; }
        public ulong CurrencyType { get; protected set; }
    }

    public class MissionConditionEmotePerformedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EmoteAvatarFilter { get; protected set; }
        public ulong EmotePower { get; protected set; }
        public bool ObserverAvatarsOnly { get; protected set; }
        public EntityFilterPrototype ObserverFilter { get; protected set; }
        public int ObserverRadius { get; protected set; }
    }

    public class MissionConditionEntityAggroPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
    }

    public class MissionConditionEntityDamagedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public ulong EncounterResource { get; protected set; }
        public bool LimitToDamageFromPlayerOMOnly { get; protected set; }
    }

    public class MissionConditionEntityDeathPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public double OpenMissionContribValueDamage { get; protected set; }
        public double OpenMissionContribValueTanking { get; protected set; }
        public ulong EncounterResource { get; protected set; }
        public int DelayDeathMS { get; protected set; }
        public bool EncounterResourceValidate { get; protected set; }
        public int WithinSeconds { get; protected set; }
        public bool MustBeTaggedByPlayer { get; protected set; }
        public HUDEntitySettingsPrototype EntityHUDSettingOverride { get; protected set; }
    }

    public class MissionConditionEntityInteractPrototype : MissionPlayerConditionPrototype
    {
        public ulong Cinematic { get; protected set; }
        public long Count { get; protected set; }
        public ulong DialogText { get; protected set; }
        public WeightedTextEntryPrototype[] DialogTextList { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public LootTablePrototype[] GiveItems { get; protected set; }
        public bool IsTurnInNPC { get; protected set; }
        public OnInteractAction OnInteract { get; protected set; }
        public MissionItemRequiredEntryPrototype[] RequiredItems { get; protected set; }
        public ulong WithinHotspot { get; protected set; }
        public ulong OnInteractBehavior { get; protected set; }
        public MissionActionPrototype[] OnInteractEntityActions { get; protected set; }
        public IncrementalActionEntryPrototype[] OnIncrementalActions { get; protected set; }
        public ulong DialogTextWhenInventoryFull { get; protected set; }
        public bool DropLootOnGround { get; protected set; }
        public int WithinSeconds { get; protected set; }
        public DialogStyle DialogStyle { get; protected set; }
        public HUDEntitySettingsPrototype EntityHUDSettingOverride { get; protected set; }
        public bool ShowRewards { get; protected set; }
        public VOCategory VoiceoverCategory { get; protected set; }
    }

    public class MissionConditionFactionPrototype : MissionPlayerConditionPrototype
    {
        public ulong Faction { get; protected set; }
        public bool EventOnly { get; protected set; }
    }

    public class MissionConditionGlobalEventCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong Event { get; protected set; }
    }

    public class MissionConditionMemberOfEventTeamPrototype : MissionPlayerConditionPrototype
    {
        public ulong Team { get; protected set; }
    }

    public class MissionConditionMetaGameCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MetaGame { get; protected set; }
        public MetaGameCompleteType CompleteType { get; protected set; }
        public int Count { get; protected set; }
    }

    public class MissionConditionMetaStateCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MetaState { get; protected set; }
        public int MinDifficulty { get; protected set; }
        public ulong RequiredAffix { get; protected set; }
        public ulong RequiredRarity { get; protected set; }
        public int WaveNum { get; protected set; }
        public int Count { get; protected set; }
    }

    public class MissionConditionMetaStateDeathLimitHitPrototype : MissionConditionPrototype
    {
        public ulong MetaState { get; protected set; }
        public int Count { get; protected set; }
    }

    public class MissionConditionPublicEventIsActivePrototype : MissionConditionPrototype
    {
        public ulong Event { get; protected set; }
    }

    public class MissionConditionLogicFalsePrototype : MissionConditionPrototype
    {
    }

    public class MissionConditionLogicTruePrototype : MissionConditionPrototype
    {
    }

    public class MissionConditionMissionCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MissionPrototype { get; protected set; }
        public long Count { get; protected set; }
        public DistributionType CreditTo { get; protected set; }
        public ulong MissionKeyword { get; protected set; }
        public ulong[] WithinRegions { get; protected set; }        // VectorPrototypeRefPtr RegionPrototype
        public bool EvaluateOnRegionEnter { get; protected set; }
        public bool EvaluateOnReset { get; protected set; }
        public ulong[] WithinAreas { get; protected set; }          // VectorPrototypeRefPtr AreaPrototype
        public MissionShowObjsSettings ShowObjs { get; protected set; }
    }

    public class MissionConditionMissionFailedPrototype : MissionPlayerConditionPrototype
    {
        public ulong MissionPrototype { get; protected set; }
        public long Count { get; protected set; }
        public DistributionType CreditTo { get; protected set; }
        public ulong MissionKeyword { get; protected set; }
        public ulong[] WithinRegions { get; protected set; }    // VectorPrototypeRefPtr RegionPrototype
        public bool EvaluateOnRegionEnter { get; protected set; }
        public bool EvaluateOnReset { get; protected set; }
    }

    public class MissionConditionObjectiveCompletePrototype : MissionPlayerConditionPrototype
    {
        public long ObjectiveID { get; protected set; }
        public ulong MissionPrototype { get; protected set; }
        public bool EvaluateOnReset { get; protected set; }
        public long Count { get; protected set; }
        public DistributionType CreditTo { get; protected set; }
        public bool EvaluateOnRegionEnter { get; protected set; }
        public bool ShowCountFromTargetObjective { get; protected set; }
    }

    public class MissionConditionOrbPickUpPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
    }

    public class MissionConditionOrPrototype : MissionConditionListPrototype
    {
    }

    public class MissionConditionSpawnerDefeatedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public int Count { get; protected set; }
    }

    public class MissionConditionHealthRangePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public double HealthMinPct { get; protected set; }
        public double HealthMaxPct { get; protected set; }
    }

    public class MissionConditionHotspotContainsPrototype : MissionConditionPrototype
    {
        public int CountMax { get; protected set; }
        public int CountMin { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public EntityFilterPrototype TargetFilter { get; protected set; }
    }

    public class MissionConditionHotspotEnterPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
    }

    public class MissionConditionHotspotLeavePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
    }

    public class MissionConditionItemCollectPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public bool MustBeEquippableByAvatar { get; protected set; }
        public bool DestroyOnPickup { get; protected set; }
        public bool CountItemsOnMissionStart { get; protected set; }
    }

    public class MissionConditionItemEquipPrototype : MissionPlayerConditionPrototype
    {
        public ulong ItemPrototype { get; protected set; }
    }

    public class MissionConditionInventoryCapacityPrototype : MissionPlayerConditionPrototype
    {
        public ulong InventoryPrototype { get; protected set; }
        public int SlotsRemaining { get; protected set; }
    }

    public class MissionConditionKismetSeqFinishedPrototype : MissionPlayerConditionPrototype
    {
        public ulong KismetSeqPrototype { get; protected set; }
    }

    public class MissionConditionPowerEquipPrototype : MissionPlayerConditionPrototype
    {
        public ulong PowerPrototype { get; protected set; }
    }

    public class MissionConditionPartySizePrototype : MissionPlayerConditionPrototype
    {
        public int MaxSize { get; protected set; }
        public int MinSize { get; protected set; }
    }

    public class MissionConditionRegionBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; protected set; }
    }

    public class MissionConditionRegionContainsPrototype : MissionConditionPrototype
    {
        public int CountMax { get; protected set; }
        public int CountMin { get; protected set; }
        public ulong Region { get; protected set; }
        public EntityFilterPrototype TargetFilter { get; protected set; }
        public bool RegionIncludeChildren { get; protected set; }
        public ulong[] RegionsExclude { get; protected set; }   // VectorPrototypeRefPtr RegionPrototype
    }

    public class MissionConditionRegionEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; protected set; }
        public bool WaitForCinematicFinished { get; protected set; }
        public ulong[] Keywords { get; protected set; }  // VectorPrototypeRefPtr RegionKeywordPrototype
        public bool RegionIncludeChildren { get; protected set; }
    }

    public class MissionConditionRegionHasMatchPrototype : MissionPlayerConditionPrototype
    {
    }

    public class MissionConditionRegionLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype { get; protected set; }
        public bool RegionIncludeChildren { get; protected set; }
    }

    public class MissionConditionRemoteNotificationPrototype : MissionPlayerConditionPrototype
    {
        public ulong DialogText { get; protected set; }
        public ulong WorldEntityPrototype { get; protected set; }
        public GameNotificationType NotificationType { get; protected set; }
        public ulong VOTrigger { get; protected set; }
    }

    public class MissionConditionTeamUpIsActivePrototype : MissionPlayerConditionPrototype
    {
        public ulong TeamUpPrototype { get; protected set; }
    }

    public class MissionConditionTeamUpIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public ulong TeamUpPrototype { get; protected set; }
    }

    public class MissionConditionThrowablePickUpPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter { get; protected set; }
    }

    public class MissionConditionItemBuyPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
    }

    public class MissionConditionItemDonatePrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
    }

    public class MissionConditionItemCraftPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public EntityFilterPrototype EntityFilter { get; protected set; }
        public ulong UsingRecipe { get; protected set; }
    }

    public class MissionConditionClusterEnemiesClearedPrototype : MissionPlayerConditionPrototype
    {
        public long Count { get; protected set; }
        public bool OnlyCountMissionClusters { get; protected set; }
        public ulong[] SpawnedByMission { get; protected set; }
        public ulong[] SpecificClusters { get; protected set; }
        public ulong[] WithinRegions { get; protected set; }    // VectorPrototypeRefPtr RegionPrototype
        public ulong[] WithinAreas { get; protected set; }      // VectorPrototypeRefPtr AreaPrototype
        public bool PlayerKillerRequired { get; protected set; }
    }
}
