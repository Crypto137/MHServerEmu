using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class EntityPrototype : Prototype
    {
        public ulong DisplayName;
        public ulong IconPath;
        public ulong Properties;
        public bool ReplicateToProximity;
        public bool ReplicateToParty;
        public bool ReplicateToOwner;
        public bool ReplicateToDiscovered;
        public EntityInventoryAssignmentPrototype[] Inventories;
        public EvalPrototype[] EvalOnCreate;
        public ulong DisplayNameInformal;
        public ulong DisplayNameShort;
        public bool ReplicateToTrader;
        public int LifespanMS;
        public ulong IconPathTooltipHeader;
        public ulong IconPathHiRes;
        public EntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityPrototype), proto); }
    }

    public class WorldEntityPrototype : EntityPrototype
    {
        public ulong Alliance;
        public BoundsPrototype Bounds;
        public ulong DialogText;
        public ulong UnrealClass;
        public ulong XPGrantedCurve;
        public bool HACKBuildMouseCollision;
        public ulong PreInteractPower;
        public DialogStyle DialogStyle;
        public WeightedTextEntryPrototype[] DialogTextList;
        public ulong[] Keywords;
        public DesignWorkflowState DesignState;
        public ulong Rank;
        public Method NaviMethod;
        public bool SnapToFloorOnSpawn;
        public bool AffectNavigation;
        public StateChangePrototype PostInteractState;
        public StateChangePrototype PostKilledState;
        public bool OrientToInteractor;
        public ulong TooltipInWorldTemplate;
        public bool InteractIgnoreBoundsForDistance;
        public float PopulationWeight;
        public bool VisibleByDefault;
        public int RemoveFromWorldTimerMS;
        public bool RemoveNavInfluenceOnKilled;
        public bool AlwaysSimulated;
        public bool XPIsShared;
        public ulong TutorialTip;
        public bool TrackingDisabled;
        public ulong[] ModifiersGuaranteed;
        public float InteractRangeBonus;
        public bool ShouldIgnoreMaxDeadBodies;
        public bool ModifierSetEnable;
        public bool LiveTuningDefaultEnabled;
        public bool UpdateOrientationWithParent;
        public bool MissionEntityDeathCredit;
        public bool HACKDiscoverInRegion;
        public bool CanCollideWithPowerUserItems;
        public bool ForwardOnHitProcsToOwner;
        public ObjectiveInfoPrototype ObjectiveInfo;
        public WorldEntityIconsPrototype Icons;
        public EntitySelectorActionPrototype[] EntitySelectorActions;
        public bool OverheadIndicator;
        public bool RequireCombatActiveForKillCredit;
        public bool ClonePerPlayer;
        public bool PrefetchMarkedAssets;
        public ulong MarvelModelRenderClass;
        public DesignWorkflowState DesignStatePS4;
        public DesignWorkflowState DesignStateXboxOne;
        public WorldEntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WorldEntityPrototype), proto); }
    }

    public class StateChangePrototype : Prototype
    {
        public StateChangePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StateChangePrototype), proto); }
    }

    public class StateTogglePrototype : StateChangePrototype
    {
        public ulong StateA;
        public ulong StateB;
        public StateTogglePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StateTogglePrototype), proto); }
    }

    public class StateSetPrototype : StateChangePrototype
    {
        public ulong State;
        public StateSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StateSetPrototype), proto); }
    }

    public class WorldEntityIconsPrototype : Prototype
    {
        public ulong EdgeIcon;
        public ulong MapIcon;
        public ulong EdgeIconHiRes;
        public WorldEntityIconsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WorldEntityIconsPrototype), proto); }
    }

    #region EntityAction

    public class EntityActionBasePrototype : Prototype
    {
        public int Weight;
        public EntityActionBasePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityActionBasePrototype), proto); }
    }

    public class EntityActionAIOverridePrototype : EntityActionBasePrototype
    {
        public ulong Power;
        public bool PowerRemove;
        public ulong Brain;
        public bool BrainRemove;
        public bool SelectorReferencedPowerRemove;
        public int AIAggroRangeOverrideAlly;
        public int AIAggroRangeOverrideEnemy;
        public int AIProximityRangeOverride;
        public int LifespanMS;
        public ulong LifespanEndPower;
        public EntityActionAIOverridePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityActionAIOverridePrototype), proto); }
    }

    public class EntityActionOverheadTextPrototype : EntityActionBasePrototype
    {
        public ulong Text;
        public int Duration;
        public EntityActionOverheadTextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityActionOverheadTextPrototype), proto); }
    }

    public class EntityActionEventBroadcastPrototype : EntityActionBasePrototype
    {
        public EntitySelectorActionEventType EventToBroadcast;
        public int BroadcastRange;
        public EntityActionEventBroadcastPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityActionEventBroadcastPrototype), proto); }
    }

    public class EntityActionSpawnerTriggerPrototype : EntityActionBasePrototype
    {
        public bool EnableClusterLocalSpawner;
        public EntityActionSpawnerTriggerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityActionSpawnerTriggerPrototype), proto); }
    }

    public class EntitySelectorActionBasePrototype : Prototype
    {
        public ulong[] AIOverrides;
        public EntityActionAIOverridePrototype[] AIOverridesList;
        public ulong[] OverheadTexts;
        public EntityActionOverheadTextPrototype[] OverheadTextsList;
        public ulong[] Rewards;
        public EntitySelectorAttributeActions[] AttributeActions;
        public HUDEntitySettingsPrototype HUDEntitySettingOverride;
        public EntitySelectorActionBasePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntitySelectorActionBasePrototype), proto); }
    }

    public enum EntitySelectorAttributeActions {
	    None,
	    DisableInteractions,
	    EnableInteractions,
    }

    public class EntitySelectorActionPrototype : EntitySelectorActionBasePrototype
    {
        public EntitySelectorActionEventType[] EventTypes;
        public int ReactionTimeMS;
        public EntitySelectorActionEventType[] CancelOnEventTypes;
        public ulong SpawnerTrigger;
        public ulong AllianceOverride;
        public ulong BroadcastEvent;
        public EntitySelectorActionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntitySelectorActionPrototype), proto); }
    }


    public class EntitySelectorActionSetPrototype : Prototype
    {
        public EntitySelectorActionPrototype[] EntitySelectorActions;
        public EntitySelectorActionSetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntitySelectorActionSetPrototype), proto); }
    }

    [Flags]
    public enum EntitySelectorActionEventType
    {
	    None = 0,
	    OnDetectedEnemy = 1,
	    OnGotAttacked = 2,
	    OnGotDamaged = 4,
	    OnGotDefeated = 8,
	    OnGotKilled = 16,
	    OnAllyDetectedEnemy = 32,
	    OnAllyGotAttacked = 64,
	    OnAllyGotKilled = 128,
	    OnMetLeashDistance = 256,
	    OnEnteredCombat = 512,
	    OnExitedCombat = 1024,
	    OnKilledOther = 2048,
	    OnDetectedFriend = 4096,
	    OnSimulated = 8192,
	    OnEnemyProximity = 16384,
	    OnDetectedPlayer = 32768,
	    OnDetectedNonPlayer = 65536,
	    OnAllyDetectedPlayer = 131072,
	    OnAllyDetectedNonPlayer = 262144,
	    OnClusterEnemiesCleared = 524288,
	    OnPlayerInteract = 1048576,
	    OnPlayerProximity = 2097152,
	    OnGotAttackedByPlayer = 4194304,
	    OnAllyGotAttackedByPlayer = 8388608,
	    OnMissionBroadcast = 16777216,
    }


    public class EntitySelectorPrototype : Prototype
    {
        public ulong[] Entities;
        public EntitySelectorActionPrototype[] EntitySelectorActions;
        public ulong EntitySelectorActionsTemplate;
        public ulong DefaultBrainOnSimulated;
        public bool IgnoreMissionOwnerForTargeting;
        public float DefaultAggroRangeAlly;
        public float DefaultAggroRangeHostile;
        public float DefaultProximityRangeHostile;
        public EvalPrototype EvalSpawnProperties;
        public bool SelectUniqueEntities;
        public EntitySelectorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntitySelectorPrototype), proto); }
    }

    public class EntityActionTimelineScriptActionPrototype : EntitySelectorActionBasePrototype
    {
        public ScriptRoleKeyEnum[] ScriptRoleKeys;
        public ulong SpawnerTrigger;
        public EntityActionTimelineScriptActionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityActionTimelineScriptActionPrototype), proto); }
    }

    public enum ScriptRoleKeyEnum
    {
        Invalid = 0,
        FriendlyPassive01 = 1,
        FriendlyPassive02 = 2,
        FriendlyPassive03 = 3,
        FriendlyPassive04 = 4,
        FriendlyCombatant01 = 5,
        FriendlyCombatant02 = 6,
        FriendlyCombatant03 = 7,
        FriendlyCombatant04 = 8,
        HostileCombatant01 = 9,
        HostileCombatant02 = 10,
        HostileCombatant03 = 11,
        HostileCombatant04 = 12,
    }

    public class EntityActionTimelineScriptEventPrototype : Prototype
    {
        public ulong[] ActionsList;
        public EntityActionTimelineScriptActionPrototype[] ActionsVector;
        public int EventTime;
        public EntitySelectorActionEventType[] InterruptOnEventTypes;
        public EntityActionTimelineScriptEventPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityActionTimelineScriptEventPrototype), proto); }
    }

    public class EntityActionTimelineScriptPrototype : Prototype
    {
        public EntitySelectorActionEventType[] TriggerOnEventTypes;
        public EntitySelectorActionEventType[] CancelOnEventTypes;
        public EntityActionTimelineScriptEventPrototype[] ScriptEvents;
        public bool RunOnceOnly;
        public EntityActionTimelineScriptPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityActionTimelineScriptPrototype), proto); }
    }


    #endregion

    public class WeightedTextEntryPrototype : Prototype
    {
        public ulong Text;
        public ulong Weight;
        public WeightedTextEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WeightedTextEntryPrototype), proto); }
    }

    public enum Method {
	    Ground = 1,
	    Airborne = 2,
	    TallGround = 3,
	    Missile = 4,
	    MissileSeeking = 5,
	    HighFlying = 6,
    }

    public enum DialogStyle {
	    ComputerTerminal = 1,
	    NPCDialog = 2,
	    OverheadText = 3,
    }

    public class TransitionPrototype : WorldEntityPrototype
    {
        public RegionTransitionType Type;
        public int SpawnOffset;
        public ulong Waypoint;
        public bool SupressBlackout;
        public bool ShowIndicator;
        public bool ShowConfirmationDialog;
        public ulong DirectTarget;
        public ulong[] RegionAffixesBySummonerRarity;
        public ulong ShowConfirmationDialogOverride;
        public ulong ShowConfirmationDialogTemplate;
        public ulong ShowConfirmationDialogEnemy;
        public TransitionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TransitionPrototype), proto); }
    }

    public enum RegionTransitionType 
    {
	    Transition,
	    TransitionDirect,
	    Marker,
	    Waypoint,
	    TowerUp,
	    TowerDown,
	    PartyJoin,
	    Checkpoint,
	    TransitionDirectReturn,
	    ReturnToLastTown,
	    EndlessDown,
    }

    public class EntityAppearancePrototype : Prototype
    {
        public EntityAppearanceEnum AppearanceEnum;
        public EntityAppearancePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityAppearancePrototype), proto); }
    }
    public enum EntityAppearanceEnum
    {
        None = 0,
        Closed = 1,
        Destroyed = 2,
        Disabled = 3,
        Enabled = 4,
        Locked = 5,
        Open = 6,
        Dead = 7,
    }

    public class EntityStatePrototype : Prototype
    {
        public ulong Appearance;
        public ulong OnActivatePowers;
        public EntityStatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityStatePrototype), proto); }
    }

    public class DoorEntityStatePrototype : EntityStatePrototype
    {
        public bool IsOpen;
        public DoorEntityStatePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DoorEntityStatePrototype), proto); }
    }

    public class InteractionSpecPrototype : Prototype
    {
        public InteractionSpecPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InteractionSpecPrototype), proto); }
    }

    public class ConnectionTargetEnableSpecPrototype : InteractionSpecPrototype
    {
        public ulong ConnectionTarget;
        public bool Enabled;
        public ConnectionTargetEnableSpecPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ConnectionTargetEnableSpecPrototype), proto); }
    }

    public class EntityBaseSpecPrototype : InteractionSpecPrototype
    {
        public EntityFilterPrototype EntityFilter;
        public EntityBaseSpecPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityBaseSpecPrototype), proto); }
    }

    public class EntityVisibilitySpecPrototype : EntityBaseSpecPrototype
    {
        public bool Visible;
        public EntityVisibilitySpecPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityVisibilitySpecPrototype), proto); }
    }

    public class EntityAppearanceSpecPrototype : EntityBaseSpecPrototype
    {
        public ulong Appearance;
        public ulong FailureReasonText;
        public TriBool InteractionEnabled;
        public EntityAppearanceSpecPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityAppearanceSpecPrototype), proto); }
    }

    public class HotspotDirectApplyToMissilesDataPrototype : Prototype
    {
        public bool AffectsAllyMissiles;
        public bool AffectsHostileMissiles;
        public EvalPrototype EvalPropertiesToApply;
        public bool AffectsReflectedMissilesOnly;
        public bool IsPermanent;
        public HotspotDirectApplyToMissilesDataPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HotspotDirectApplyToMissilesDataPrototype), proto); }
    }

    public class HotspotPrototype : WorldEntityPrototype
    {
        public PowerPrototype AppliesPowers;
        public PowerPrototype AppliesIntervalPowers;
        public int IntervalPowersTimeDelayMS;
        public bool IntervalPowersRandomTarget;
        public int IntervalPowersNumRandomTargets;
        public UINotificationPrototype UINotificationOnEnter;
        public int MaxSimultaneousTargets;
        public bool KillCreatorWhenHotspotIsEmpty;
        public ulong KismetSeq;
        public bool Negatable;
        public bool KillSelfWhenPowerApplied;
        public HotspotOverlapEventTriggerType OverlapEventsTriggerOn;
        public int OverlapEventsMaxTargets;
        public HotspotDirectApplyToMissilesDataPrototype DirectApplyToMissilesData;
        public int ApplyEffectsDelayMS;
        public ulong CameraSettings;
        public int MaxLifetimeTargets;
        public HotspotPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(HotspotPrototype), proto); }
    }
    public enum HotspotOverlapEventTriggerType
    {
        None = 0,
        Allies = 1,
        Enemies = 2,
        All = 3,
    }

    public class OverheadTextPrototype : Prototype
    {
        public EntityFilterFilterListPrototype OverheadTextEntityFilter;
        public ulong OverheadText;
        public OverheadTextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OverheadTextPrototype), proto); }
    }

    public class SpawnerSequenceEntryPrototype : PopulationRequiredObjectPrototype
    {
        public bool OnKilledDefeatSpawner;
        public ulong OnDefeatAIOverride;
        public bool Unique;
        public OverheadTextPrototype[] OnSpawnOverheadTexts;
        public SpawnerSequenceEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SpawnerSequenceEntryPrototype), proto); }
    }

    public class SpawnerPrototype : WorldEntityPrototype
    {
        public int SpawnLifetimeMax;
        public int SpawnDistanceMin;
        public int SpawnDistanceMax;
        public int SpawnIntervalMS;
        public int SpawnSimultaneousMax;
        public ulong SpawnedEntityInventory;
        public SpawnerSequenceEntryPrototype[] SpawnSequence;
        public bool SpawnsInheritMissionPrototype;
        public bool StartEnabled;
        public bool OnDestroyCleanupSpawnedEntities;
        public int SpawnIntervalVarianceMS;
        public ulong HotspotTrigger;
        public BannerMessagePrototype OnDefeatBannerMessage;
        public bool OnDefeatDefeatGroup;
        public SpawnerDefeatCriteria DefeatCriteria;
        public EvalPrototype EvalSpawnProperties;
        public FormationFacingEnum SpawnFacing;
        public SpawnFailBehavior SpawnFailBehavior;
        public int DefeatTimeoutMS;
        public SpawnerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SpawnerPrototype), proto); }
    }
    public enum SpawnerDefeatCriteria
    {
        Never = 0,
        MaxReachedAndNoHostileMobs = 1,
    }
    public enum FormationFacingEnum
    {
        None = 0,
        FaceParent = 0,
        FaceParentInverse = 1,
        FaceOrigin = 2,
        FaceOriginInverse = 3,
    }
    public enum SpawnFailBehavior
    {
        Fail = 0,
        RetryIgnoringBlackout = 1,
        RetryForce = 2,
    }
    public class KismetSequenceEntityPrototype : WorldEntityPrototype
    {
        public ulong KismetSequence;
        public KismetSequenceEntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(KismetSequenceEntityPrototype), proto); }
    }

    public class FactionPrototype : Prototype
    {
        public ulong IconPath;
        public ulong TextStyle;
        public FactionColor HealthColor;
        public FactionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FactionPrototype), proto); }
    }
    public enum FactionColor
    {
        Default = 0,
        Red = 1,
        White = 2,
        Blue = 3,
    }
    public class WaypointPrototype : Prototype
    {
        public ulong Destination;
        public ulong Name;
        public bool SupressBannerMessage;
        public bool IsCheckpoint;
        public ulong WaypointGraph;
        public ulong WaypointGraphList;
        public ulong RequiresItem;
        public EvalPrototype EvalShouldDisplay;
        public ulong Tooltip;
        public bool IncludeWaypointPrefixInName;
        public bool StartLocked;
        public ulong DestinationBossEntities;
        public bool IsAccountWaypoint;
        public int MigrationUnlockedByLevel;
        public ulong MigrationUnlockedByChapters;
        public WaypointPOIType MapPOIType;
        public ulong MapConnectTo;
        public ulong MapDescription;
        public float MapPOIXCoord;
        public float MapPOIYCoord;
        public ulong MapImage;
        public ulong OpenToWaypointGraph;
        public ulong MapImageConsole;
        public ulong LocationImageConsole;
        public ulong ConsoleRegionDescription;
        public ulong ConsoleLocationName;
        public ulong ConsoleRegionType;
        public ulong ConsoleLevelRange;
        public LocalizedTextAndImagePrototype[] ConsoleRegionItems;
        public ulong ConsoleWaypointGraphList;
        public WaypointPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WaypointPrototype), proto); }
    }

    public enum WaypointPOIType
    {
        HUB = 0,
        PCZ = 1,
        PI = 2,
    }

    public class WaypointChapterPrototype : Prototype
    {
        public ulong Chapter;
        public ulong[] Waypoints;
        public WaypointChapterPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WaypointChapterPrototype), proto); }
    }

    public class WaypointGraphPrototype : Prototype
    {
        public WaypointChapterPrototype[] Chapters;
        public ulong DisplayName;
        public ulong MapDescription;
        public ulong MapImage;
        public ulong Tooltip;
        public WaypointGraphPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WaypointGraphPrototype), proto); }
    }

    public class CheckpointPrototype : Prototype
    {
        public ulong Destination;
        public CheckpointPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CheckpointPrototype), proto); }
    }

}
