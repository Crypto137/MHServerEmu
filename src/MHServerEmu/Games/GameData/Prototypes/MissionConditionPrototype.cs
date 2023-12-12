
namespace MHServerEmu.Games.GameData.Prototypes
{
    public class MissionItemRequiredEntryPrototype : Prototype
    {
        public ulong ItemPrototype;
        public long Num;
        public bool Remove;
        public MissionItemRequiredEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionItemRequiredEntryPrototype), proto); }
    }

    public class MissionConditionPrototype : Prototype
    {
        public StoryNotificationPrototype StoryNotification;
        public bool NoTrackingOptimization;
        public long MissionConditionGuid;
        public MissionConditionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionPrototype), proto); }
    }

    public class MissionPlayerConditionPrototype : MissionConditionPrototype
    {
        public bool PartyMembersGetCredit;
        public double OpenMissionContributionValue;
        public MissionPlayerConditionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionPlayerConditionPrototype), proto); }
    }

    public class MissionConditionListPrototype : MissionConditionPrototype
    {
        public MissionConditionPrototype[] Conditions;
        public MissionConditionListPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionListPrototype), proto); }
    }

    public class MissionConditionAndPrototype : MissionConditionListPrototype
    {
        public MissionConditionAndPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionAndPrototype), proto); }
    }

    public class MissionConditionActiveChapterPrototype : MissionPlayerConditionPrototype
    {
        public ulong Chapter;
        public MissionConditionActiveChapterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionActiveChapterPrototype), proto); }
    }

    public class MissionConditionPowerPointsRemainingPrototype : MissionPlayerConditionPrototype
    {
        public int MinPoints;
        public int MaxPoints;
        public MissionConditionPowerPointsRemainingPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionPowerPointsRemainingPrototype), proto); }
    }

    public class MissionConditionAreaBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype;
        public MissionConditionAreaBeginTravelToPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionAreaBeginTravelToPrototype), proto); }
    }

    public class MissionConditionAreaContainsPrototype : MissionConditionPrototype
    {
        public ulong Area;
        public int CountMax;
        public int CountMin;
        public EntityFilterPrototype TargetFilter;
        public MissionConditionAreaContainsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionAreaContainsPrototype), proto); }
    }

    public class MissionConditionAreaEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype;
        public ulong AreaPrototype;
        public MissionConditionAreaEnterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionAreaEnterPrototype), proto); }
    }

    public class MissionConditionAreaLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype;
        public MissionConditionAreaLeavePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionAreaLeavePrototype), proto); }
    }

    public class MissionConditionAvatarIsActivePrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype;
        public MissionConditionAvatarIsActivePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionAvatarIsActivePrototype), proto); }
    }

    public class MissionConditionAvatarIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype;
        public MissionConditionAvatarIsUnlockedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionAvatarIsUnlockedPrototype), proto); }
    }

    public class MissionConditionAvatarLevelUpPrototype : MissionPlayerConditionPrototype
    {
        public ulong AvatarPrototype;
        public long Level;
        public MissionConditionAvatarLevelUpPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionAvatarLevelUpPrototype), proto); }
    }

    public class MissionConditionAvatarUsedPowerPrototype : MissionPlayerConditionPrototype
    {
        public ulong AreaPrototype;
        public ulong AvatarPrototype;
        public ulong PowerPrototype;
        public ulong RegionPrototype;
        public ulong WithinHotspot;
        public int Count;
        public EntityFilterPrototype TargetFilter;
        public int WithinSeconds;
        public MissionConditionAvatarUsedPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionAvatarUsedPowerPrototype), proto); }
    }

    public class MissionConditionCellEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong[] Cells;
        public ulong[] Regions;
        public MissionConditionCellEnterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionCellEnterPrototype), proto); }
    }

    public class MissionConditionCellLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong[] Cells;
        public MissionConditionCellLeavePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionCellLeavePrototype), proto); }
    }

    public class MissionConditionCohortPrototype : MissionConditionPrototype
    {
        public ulong CohortPrototype;
        public ulong ExperimentPrototype;
        public MissionConditionCohortPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionCohortPrototype), proto); }
    }

    public class MissionConditionCountPrototype : MissionConditionListPrototype
    {
        public long Count;
        public MissionConditionCountPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionCountPrototype), proto); }
    }

    public class MissionConditionCurrencyCollectedPrototype : MissionPlayerConditionPrototype
    {
        public long AmountRequired;
        public ulong CurrencyType;
        public MissionConditionCurrencyCollectedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionCurrencyCollectedPrototype), proto); }
    }

    public class MissionConditionEmotePerformedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EmoteAvatarFilter;
        public ulong EmotePower;
        public bool ObserverAvatarsOnly;
        public EntityFilterPrototype ObserverFilter;
        public int ObserverRadius;
        public MissionConditionEmotePerformedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionEmotePerformedPrototype), proto); }
    }

    public class MissionConditionEntityAggroPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter;
        public MissionConditionEntityAggroPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionEntityAggroPrototype), proto); }
    }

    public class MissionConditionEntityDamagedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter;
        public ulong EncounterResource;
        public bool LimitToDamageFromPlayerOMOnly;
        public MissionConditionEntityDamagedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionEntityDamagedPrototype), proto); }
    }

    public class MissionConditionEntityDeathPrototype : MissionPlayerConditionPrototype
    {
        public long Count;
        public EntityFilterPrototype EntityFilter;
        public double OpenMissionContribValueDamage;
        public double OpenMissionContribValueTanking;
        public ulong EncounterResource;
        public int DelayDeathMS;
        public bool EncounterResourceValidate;
        public int WithinSeconds;
        public bool MustBeTaggedByPlayer;
        public HUDEntitySettingsPrototype EntityHUDSettingOverride;
        public MissionConditionEntityDeathPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionEntityDeathPrototype), proto); }
    }

    public class MissionConditionEntityInteractPrototype : MissionPlayerConditionPrototype
    {
        public ulong Cinematic;
        public long Count;
        public ulong DialogText;
        public WeightedTextEntryPrototype[] DialogTextList;
        public EntityFilterPrototype EntityFilter;
        public LootTablePrototype[] GiveItems;
        public bool IsTurnInNPC;
        public OnInteractAction OnInteract;
        public MissionItemRequiredEntryPrototype[] RequiredItems;
        public ulong WithinHotspot;
        public ulong OnInteractBehavior;
        public MissionActionPrototype[] OnInteractEntityActions;
        public IncrementalActionEntryPrototype[] OnIncrementalActions;
        public ulong DialogTextWhenInventoryFull;
        public bool DropLootOnGround;
        public int WithinSeconds;
        public DialogStyle DialogStyle;
        public HUDEntitySettingsPrototype EntityHUDSettingOverride;
        public bool ShowRewards;
        public VOCategory VoiceoverCategory;
        public MissionConditionEntityInteractPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionEntityInteractPrototype), proto); }
    }
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
    public enum OnInteractAction
    {
        Despawn = 1,
        Disable = 2,
    }
    public class MissionConditionFactionPrototype : MissionPlayerConditionPrototype
    {
        public ulong Faction;
        public bool EventOnly;
        public MissionConditionFactionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionFactionPrototype), proto); }
    }

    public class MissionConditionGlobalEventCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong Event;
        public MissionConditionGlobalEventCompletePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionGlobalEventCompletePrototype), proto); }
    }

    public class MissionConditionMemberOfEventTeamPrototype : MissionPlayerConditionPrototype
    {
        public ulong Team;
        public MissionConditionMemberOfEventTeamPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionMemberOfEventTeamPrototype), proto); }
    }

    public class MissionConditionMetaGameCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MetaGame;
        public MetaGameCompleteType CompleteType;
        public int Count;
        public MissionConditionMetaGameCompletePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionMetaGameCompletePrototype), proto); }
    }
    public enum MetaGameCompleteType
    {
        Success = 0,
        Failure = 1,
        DidNotParticipate = 2,
    }
    public class MissionConditionMetaStateCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MetaState;
        public int MinDifficulty;
        public ulong RequiredAffix;
        public ulong RequiredRarity;
        public int WaveNum;
        public int Count;
        public MissionConditionMetaStateCompletePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionMetaStateCompletePrototype), proto); }
    }

    public class MissionConditionMetaStateDeathLimitHitPrototype : MissionConditionPrototype
    {
        public ulong MetaState;
        public int Count;
        public MissionConditionMetaStateDeathLimitHitPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionMetaStateDeathLimitHitPrototype), proto); }
    }

    public class MissionConditionPublicEventIsActivePrototype : MissionConditionPrototype
    {
        public ulong Event;
        public MissionConditionPublicEventIsActivePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionPublicEventIsActivePrototype), proto); }
    }

    public class MissionConditionLogicFalsePrototype : MissionConditionPrototype
    {
        public MissionConditionLogicFalsePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionLogicFalsePrototype), proto); }
    }

    public class MissionConditionLogicTruePrototype : MissionConditionPrototype
    {
        public MissionConditionLogicTruePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionLogicTruePrototype), proto); }
    }

    public class MissionConditionMissionCompletePrototype : MissionPlayerConditionPrototype
    {
        public ulong MissionPrototype;
        public long Count;
        public DistributionType CreditTo;
        public ulong MissionKeyword;
        public RegionPrototype WithinRegions;
        public bool EvaluateOnRegionEnter;
        public bool EvaluateOnReset;
        public AreaPrototype WithinAreas;
        public MissionShowObjsSettings ShowObjs;
        public MissionConditionMissionCompletePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionMissionCompletePrototype), proto); }
    }

    public enum MissionShowObjsSettings
    {
        FromTargetMission = 0,
        FromThisMission = 1,
        SuppressAllObjs = 2,
    }

    public class MissionConditionMissionFailedPrototype : MissionPlayerConditionPrototype
    {
        public ulong MissionPrototype;
        public long Count;
        public DistributionType CreditTo;
        public ulong MissionKeyword;
        public RegionPrototype WithinRegions;
        public bool EvaluateOnRegionEnter;
        public bool EvaluateOnReset;
        public MissionConditionMissionFailedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionMissionFailedPrototype), proto); }
    }

    public class MissionConditionObjectiveCompletePrototype : MissionPlayerConditionPrototype
    {
        public long ObjectiveID;
        public ulong MissionPrototype;
        public bool EvaluateOnReset;
        public long Count;
        public DistributionType CreditTo;
        public bool EvaluateOnRegionEnter;
        public bool ShowCountFromTargetObjective;
        public MissionConditionObjectiveCompletePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionObjectiveCompletePrototype), proto); }
    }

    public class MissionConditionOrbPickUpPrototype : MissionPlayerConditionPrototype
    {
        public long Count;
        public EntityFilterPrototype EntityFilter;
        public MissionConditionOrbPickUpPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionOrbPickUpPrototype), proto); }
    }

    public class MissionConditionOrPrototype : MissionConditionListPrototype
    {
        public MissionConditionOrPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionOrPrototype), proto); }
    }

    public class MissionConditionSpawnerDefeatedPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter;
        public int Count;
        public MissionConditionSpawnerDefeatedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionSpawnerDefeatedPrototype), proto); }
    }

    public class MissionConditionHealthRangePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter;
        public double HealthMinPct;
        public double HealthMaxPct;
        public MissionConditionHealthRangePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionHealthRangePrototype), proto); }
    }

    public class MissionConditionHotspotContainsPrototype : MissionConditionPrototype
    {
        public int CountMax;
        public int CountMin;
        public EntityFilterPrototype EntityFilter;
        public EntityFilterPrototype TargetFilter;
        public MissionConditionHotspotContainsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionHotspotContainsPrototype), proto); }
    }

    public class MissionConditionHotspotEnterPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter;
        public EntityFilterPrototype EntityFilter;
        public MissionConditionHotspotEnterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionHotspotEnterPrototype), proto); }
    }

    public class MissionConditionHotspotLeavePrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype TargetFilter;
        public EntityFilterPrototype EntityFilter;
        public MissionConditionHotspotLeavePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionHotspotLeavePrototype), proto); }
    }

    public class MissionConditionItemCollectPrototype : MissionPlayerConditionPrototype
    {
        public long Count;
        public EntityFilterPrototype EntityFilter;
        public bool MustBeEquippableByAvatar;
        public bool DestroyOnPickup;
        public bool CountItemsOnMissionStart;
        public MissionConditionItemCollectPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionItemCollectPrototype), proto); }
    }

    public class MissionConditionItemEquipPrototype : MissionPlayerConditionPrototype
    {
        public ulong ItemPrototype;
        public MissionConditionItemEquipPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionItemEquipPrototype), proto); }
    }

    public class MissionConditionInventoryCapacityPrototype : MissionPlayerConditionPrototype
    {
        public ulong InventoryPrototype;
        public int SlotsRemaining;
        public MissionConditionInventoryCapacityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionInventoryCapacityPrototype), proto); }
    }

    public class MissionConditionKismetSeqFinishedPrototype : MissionPlayerConditionPrototype
    {
        public ulong KismetSeqPrototype;
        public MissionConditionKismetSeqFinishedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionKismetSeqFinishedPrototype), proto); }
    }

    public class MissionConditionPowerEquipPrototype : MissionPlayerConditionPrototype
    {
        public ulong PowerPrototype;
        public MissionConditionPowerEquipPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionPowerEquipPrototype), proto); }
    }

    public class MissionConditionPartySizePrototype : MissionPlayerConditionPrototype
    {
        public int MaxSize;
        public int MinSize;
        public MissionConditionPartySizePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionPartySizePrototype), proto); }
    }

    public class MissionConditionRegionBeginTravelToPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype;
        public MissionConditionRegionBeginTravelToPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionRegionBeginTravelToPrototype), proto); }
    }

    public class MissionConditionRegionContainsPrototype : MissionConditionPrototype
    {
        public int CountMax;
        public int CountMin;
        public ulong Region;
        public EntityFilterPrototype TargetFilter;
        public bool RegionIncludeChildren;
        public RegionPrototype RegionsExclude;
        public MissionConditionRegionContainsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionRegionContainsPrototype), proto); }
    }

    public class MissionConditionRegionEnterPrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype;
        public bool WaitForCinematicFinished;
        public RegionKeywordPrototype Keywords;
        public bool RegionIncludeChildren;
        public MissionConditionRegionEnterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionRegionEnterPrototype), proto); }
    }

    public class MissionConditionRegionHasMatchPrototype : MissionPlayerConditionPrototype
    {
        public MissionConditionRegionHasMatchPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionRegionHasMatchPrototype), proto); }
    }

    public class MissionConditionRegionLeavePrototype : MissionPlayerConditionPrototype
    {
        public ulong RegionPrototype;
        public bool RegionIncludeChildren;
        public MissionConditionRegionLeavePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionRegionLeavePrototype), proto); }
    }

    public class MissionConditionRemoteNotificationPrototype : MissionPlayerConditionPrototype
    {
        public ulong DialogText;
        public ulong WorldEntityPrototype;
        public GameNotificationType NotificationType;
        public ulong VOTrigger;
        public MissionConditionRemoteNotificationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionRemoteNotificationPrototype), proto); }
    }

    public class MissionConditionTeamUpIsActivePrototype : MissionPlayerConditionPrototype
    {
        public ulong TeamUpPrototype;
        public MissionConditionTeamUpIsActivePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionTeamUpIsActivePrototype), proto); }
    }

    public class MissionConditionTeamUpIsUnlockedPrototype : MissionPlayerConditionPrototype
    {
        public ulong TeamUpPrototype;
        public MissionConditionTeamUpIsUnlockedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionTeamUpIsUnlockedPrototype), proto); }
    }

    public class MissionConditionThrowablePickUpPrototype : MissionPlayerConditionPrototype
    {
        public EntityFilterPrototype EntityFilter;
        public MissionConditionThrowablePickUpPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionThrowablePickUpPrototype), proto); }
    }

    public class MissionConditionItemBuyPrototype : MissionPlayerConditionPrototype
    {
        public long Count;
        public EntityFilterPrototype EntityFilter;
        public MissionConditionItemBuyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionItemBuyPrototype), proto); }
    }

    public class MissionConditionItemDonatePrototype : MissionPlayerConditionPrototype
    {
        public long Count;
        public EntityFilterPrototype EntityFilter;
        public MissionConditionItemDonatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionItemDonatePrototype), proto); }
    }

    public class MissionConditionItemCraftPrototype : MissionPlayerConditionPrototype
    {
        public long Count;
        public EntityFilterPrototype EntityFilter;
        public ulong UsingRecipe;
        public MissionConditionItemCraftPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionItemCraftPrototype), proto); }
    }

    public class MissionConditionClusterEnemiesClearedPrototype : MissionPlayerConditionPrototype
    {
        public long Count;
        public bool OnlyCountMissionClusters;
        public ulong[] SpawnedByMission;
        public ulong[] SpecificClusters;
        public RegionPrototype WithinRegions;
        public AreaPrototype WithinAreas;
        public bool PlayerKillerRequired;
        public MissionConditionClusterEnemiesClearedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MissionConditionClusterEnemiesClearedPrototype), proto); }
    }

}
