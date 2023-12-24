using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum EntitySelectorAttributeActions
    {
        None,
        DisableInteractions,
        EnableInteractions,
    }

    [AssetEnum]
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

    [AssetEnum]
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

    [AssetEnum]
    public enum LocomotorMethod
    {
        Ground = 1,
        Airborne = 2,
        TallGround = 3,
        Missile = 4,
        MissileSeeking = 5,
        HighFlying = 6,
    }

    [AssetEnum]
    public enum DialogStyle
    {
        ComputerTerminal = 1,
        NPCDialog = 2,
        OverheadText = 3,
    }

    [AssetEnum]
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

    [AssetEnum]
    public enum EntityAppearanceEnum    // Entity/Types/AppearanceEnum.type
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

    [AssetEnum]
    public enum HotspotOverlapEventTriggerType
    {
        None = 0,
        Allies = 1,
        Enemies = 2,
        All = 3,
    }

    [AssetEnum]
    public enum HotspotNegateByAllianceType
    {
        None = 0,
        Allies = 1,
        Enemies = 2,
        All = 3,
    }

    [AssetEnum]
    public enum SpawnerDefeatCriteria
    {
        Never = 0,
        MaxReachedAndNoHostileMobs = 1,
    }

    [AssetEnum]
    public enum FormationFacing // Populations/Blueprints/FacingEnum.type
    {
        None = 0,
        FaceParent = 0,
        FaceParentInverse = 1,
        FaceOrigin = 2,
        FaceOriginInverse = 3,
    }

    [AssetEnum]
    public enum SpawnFailBehavior
    {
        Fail = 0,
        RetryIgnoringBlackout = 1,
        RetryForce = 2,
    }

    [AssetEnum]
    public enum FactionColor
    {
        Default = 0,
        Red = 1,
        White = 2,
        Blue = 3,
    }

    [AssetEnum]
    public enum WaypointPOIType
    {
        HUB = 0,
        PCZ = 1,    // Public Combat Zone
        PI = 2,     // Private Instance
    }

    #endregion

    public class EntityPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; private set; }
        public StringId IconPath { get; private set; }                                  // A Entity/Types/EntityIconPathType.type
        public PrototypePropertyCollection Properties { get; private set; }             // Populated from mixins? Parsed from the game as ulong?
        public bool ReplicateToProximity { get; private set; }
        public bool ReplicateToParty { get; private set; }
        public bool ReplicateToOwner { get; private set; }
        public bool ReplicateToDiscovered { get; private set; }
        public EntityInventoryAssignmentPrototype[] Inventories { get; private set; }
        public EvalPrototype[] EvalOnCreate { get; private set; }
        public LocaleStringId DisplayNameInformal { get; private set; }
        public LocaleStringId DisplayNameShort { get; private set; }
        public bool ReplicateToTrader { get; private set; }
        public int LifespanMS { get; private set; }
        public StringId IconPathTooltipHeader { get; private set; }                     // A Entity/Types/EntityIconPathType.type
        public StringId IconPathHiRes { get; private set; }                             // A Entity/Types/EntityIconPathType.type
    }

    public class WorldEntityPrototype : EntityPrototype
    {
        public ulong Alliance { get; private set; }
        public BoundsPrototype Bounds { get; private set; }
        public ulong DialogText { get; private set; }
        public ulong UnrealClass { get; private set; }
        public ulong XPGrantedCurve { get; private set; }
        public bool HACKBuildMouseCollision { get; private set; }
        public ulong PreInteractPower { get; private set; }
        public DialogStyle DialogStyle { get; private set; }
        public WeightedTextEntryPrototype[] DialogTextList { get; private set; }
        public ulong[] Keywords { get; private set; }
        public DesignWorkflowState DesignState { get; private set; }
        public ulong Rank { get; private set; }
        public LocomotorMethod NaviMethod { get; private set; }
        public bool SnapToFloorOnSpawn { get; private set; }
        public bool AffectNavigation { get; private set; }
        public StateChangePrototype PostInteractState { get; private set; }
        public StateChangePrototype PostKilledState { get; private set; }
        public bool OrientToInteractor { get; private set; }
        public ulong TooltipInWorldTemplate { get; private set; }
        public bool InteractIgnoreBoundsForDistance { get; private set; }
        public float PopulationWeight { get; private set; }
        public bool VisibleByDefault { get; private set; }
        public int RemoveFromWorldTimerMS { get; private set; }
        public bool RemoveNavInfluenceOnKilled { get; private set; }
        public bool AlwaysSimulated { get; private set; }
        public bool XPIsShared { get; private set; }
        public ulong TutorialTip { get; private set; }
        public bool TrackingDisabled { get; private set; }
        public ulong[] ModifiersGuaranteed { get; private set; }
        public float InteractRangeBonus { get; private set; }
        public bool ShouldIgnoreMaxDeadBodies { get; private set; }
        public bool ModifierSetEnable { get; private set; }
        public bool LiveTuningDefaultEnabled { get; private set; }
        public bool UpdateOrientationWithParent { get; private set; }
        public bool MissionEntityDeathCredit { get; private set; }
        public bool HACKDiscoverInRegion { get; private set; }
        public bool CanCollideWithPowerUserItems { get; private set; }
        public bool ForwardOnHitProcsToOwner { get; private set; }
        public ObjectiveInfoPrototype ObjectiveInfo { get; private set; }
        public WorldEntityIconsPrototype Icons { get; private set; }
        public EntitySelectorActionPrototype[] EntitySelectorActions { get; private set; }
        public bool OverheadIndicator { get; private set; }
        public bool RequireCombatActiveForKillCredit { get; private set; }
        public bool ClonePerPlayer { get; private set; }
        public bool PrefetchMarkedAssets { get; private set; }
        public ulong MarvelModelRenderClass { get; private set; }
        public DesignWorkflowState DesignStatePS4 { get; private set; }
        public DesignWorkflowState DesignStateXboxOne { get; private set; }
    }

    public class StateChangePrototype : Prototype
    {
    }

    public class StateTogglePrototype : StateChangePrototype
    {
        public ulong StateA { get; private set; }
        public ulong StateB { get; private set; }
    }

    public class StateSetPrototype : StateChangePrototype
    {
        public ulong State { get; private set; }
    }

    public class WorldEntityIconsPrototype : Prototype
    {
        public ulong EdgeIcon { get; private set; }
        public ulong MapIcon { get; private set; }
        public ulong EdgeIconHiRes { get; private set; }
    }

    #region EntityAction

    public class EntityActionBasePrototype : Prototype
    {
        public int Weight { get; private set; }
    }

    public class EntityActionAIOverridePrototype : EntityActionBasePrototype
    {
        public ulong Power { get; private set; }
        public bool PowerRemove { get; private set; }
        public ulong Brain { get; private set; }
        public bool BrainRemove { get; private set; }
        public bool SelectorReferencedPowerRemove { get; private set; }
        public int AIAggroRangeOverrideAlly { get; private set; }
        public int AIAggroRangeOverrideEnemy { get; private set; }
        public int AIProximityRangeOverride { get; private set; }
        public int LifespanMS { get; private set; }
        public ulong LifespanEndPower { get; private set; }
    }

    public class EntityActionOverheadTextPrototype : EntityActionBasePrototype
    {
        public ulong Text { get; private set; }
        public int Duration { get; private set; }
    }

    public class EntityActionEventBroadcastPrototype : EntityActionBasePrototype
    {
        public EntitySelectorActionEventType EventToBroadcast { get; private set; }
        public int BroadcastRange { get; private set; }
    }

    public class EntityActionSpawnerTriggerPrototype : EntityActionBasePrototype
    {
        public bool EnableClusterLocalSpawner { get; private set; }
    }

    public class EntitySelectorActionBasePrototype : Prototype
    {
        public ulong[] AIOverrides { get; private set; }
        public EntityActionAIOverridePrototype[] AIOverridesList { get; private set; }
        public ulong[] OverheadTexts { get; private set; }
        public EntityActionOverheadTextPrototype[] OverheadTextsList { get; private set; }
        public ulong[] Rewards { get; private set; }
        public EntitySelectorAttributeActions[] AttributeActions { get; private set; }
        public HUDEntitySettingsPrototype HUDEntitySettingOverride { get; private set; }
    }

    public class EntitySelectorActionPrototype : EntitySelectorActionBasePrototype
    {
        public EntitySelectorActionEventType[] EventTypes { get; private set; }
        public int ReactionTimeMS { get; private set; }
        public EntitySelectorActionEventType[] CancelOnEventTypes { get; private set; }
        public ulong SpawnerTrigger { get; private set; }
        public ulong AllianceOverride { get; private set; }
        public ulong BroadcastEvent { get; private set; }
    }

    public class EntitySelectorActionSetPrototype : Prototype
    {
        public EntitySelectorActionPrototype[] EntitySelectorActions { get; private set; }
    }

    public class EntitySelectorPrototype : Prototype
    {
        public ulong[] Entities { get; private set; }
        public EntitySelectorActionPrototype[] EntitySelectorActions { get; private set; }
        public ulong EntitySelectorActionsTemplate { get; private set; }
        public ulong DefaultBrainOnSimulated { get; private set; }
        public bool IgnoreMissionOwnerForTargeting { get; private set; }
        public float DefaultAggroRangeAlly { get; private set; }
        public float DefaultAggroRangeHostile { get; private set; }
        public float DefaultProximityRangeHostile { get; private set; }
        public EvalPrototype EvalSpawnProperties { get; private set; }
        public bool SelectUniqueEntities { get; private set; }
    }

    public class EntityActionTimelineScriptActionPrototype : EntitySelectorActionBasePrototype
    {
        public ScriptRoleKeyEnum[] ScriptRoleKeys { get; private set; }
        public ulong SpawnerTrigger { get; private set; }
    }

    public class EntityActionTimelineScriptEventPrototype : Prototype
    {
        public ulong[] ActionsList { get; private set; }
        public EntityActionTimelineScriptActionPrototype[] ActionsVector { get; private set; }
        public int EventTime { get; private set; }
        public EntitySelectorActionEventType[] InterruptOnEventTypes { get; private set; }
    }

    public class EntityActionTimelineScriptPrototype : Prototype
    {
        public EntitySelectorActionEventType[] TriggerOnEventTypes { get; private set; }
        public EntitySelectorActionEventType[] CancelOnEventTypes { get; private set; }
        public EntityActionTimelineScriptEventPrototype[] ScriptEvents { get; private set; }
        public bool RunOnceOnly { get; private set; }
    }

    #endregion

    public class WeightedTextEntryPrototype : Prototype
    {
        public ulong Text { get; private set; }
        public long Weight { get; private set; }
    }

    public class TransitionPrototype : WorldEntityPrototype
    {
        public RegionTransitionType Type { get; private set; }
        public int SpawnOffset { get; private set; }
        public ulong Waypoint { get; private set; }
        public bool SupressBlackout { get; private set; }
        public bool ShowIndicator { get; private set; }
        public bool ShowConfirmationDialog { get; private set; }
        public ulong DirectTarget { get; private set; }
        public ulong[] RegionAffixesBySummonerRarity { get; private set; }
        public ulong ShowConfirmationDialogOverride { get; private set; }
        public ulong ShowConfirmationDialogTemplate { get; private set; }
        public ulong ShowConfirmationDialogEnemy { get; private set; }
    }

    public class EntityAppearancePrototype : Prototype
    {
        public EntityAppearanceEnum AppearanceEnum { get; private set; }
    }

    public class EntityStatePrototype : Prototype
    {
        public ulong Appearance { get; private set; }
        public ulong OnActivatePowers { get; private set; }
    }

    public class DoorEntityStatePrototype : EntityStatePrototype
    {
        public bool IsOpen { get; private set; }
    }

    public class InteractionSpecPrototype : Prototype
    {
    }

    public class ConnectionTargetEnableSpecPrototype : InteractionSpecPrototype
    {
        public ulong ConnectionTarget { get; private set; }
        public bool Enabled { get; private set; }
    }

    public class EntityBaseSpecPrototype : InteractionSpecPrototype
    {
        public EntityFilterPrototype EntityFilter { get; private set; }
    }

    public class EntityVisibilitySpecPrototype : EntityBaseSpecPrototype
    {
        public bool Visible { get; private set; }
    }

    public class EntityAppearanceSpecPrototype : EntityBaseSpecPrototype
    {
        public ulong Appearance { get; private set; }
        public ulong FailureReasonText { get; private set; }
        public TriBool InteractionEnabled { get; private set; }
    }

    public class HotspotDirectApplyToMissilesDataPrototype : Prototype
    {
        public bool AffectsAllyMissiles { get; private set; }
        public bool AffectsHostileMissiles { get; private set; }
        public EvalPrototype EvalPropertiesToApply { get; private set; }
        public bool AffectsReflectedMissilesOnly { get; private set; }
        public bool IsPermanent { get; private set; }
    }

    public class HotspotPrototype : WorldEntityPrototype
    {
        public PowerPrototype AppliesPowers { get; private set; }
        public PowerPrototype AppliesIntervalPowers { get; private set; }
        public int IntervalPowersTimeDelayMS { get; private set; }
        public bool IntervalPowersRandomTarget { get; private set; }
        public int IntervalPowersNumRandomTargets { get; private set; }
        public UINotificationPrototype UINotificationOnEnter { get; private set; }
        public int MaxSimultaneousTargets { get; private set; }
        public bool KillCreatorWhenHotspotIsEmpty { get; private set; }
        public ulong KismetSeq { get; private set; }
        public bool Negatable { get; private set; }
        public bool KillSelfWhenPowerApplied { get; private set; }
        public HotspotOverlapEventTriggerType OverlapEventsTriggerOn { get; private set; }
        public int OverlapEventsMaxTargets { get; private set; }
        public HotspotDirectApplyToMissilesDataPrototype DirectApplyToMissilesData { get; private set; }
        public int ApplyEffectsDelayMS { get; private set; }
        public ulong CameraSettings { get; private set; }
        public int MaxLifetimeTargets { get; private set; }
    }

    public class OverheadTextPrototype : Prototype
    {
        public EntityFilterFilterListPrototype OverheadTextEntityFilter { get; private set; }
        public ulong OverheadText { get; private set; }
    }

    public class SpawnerSequenceEntryPrototype : PopulationRequiredObjectPrototype
    {
        public bool OnKilledDefeatSpawner { get; private set; }
        public ulong OnDefeatAIOverride { get; private set; }
        public bool Unique { get; private set; }
        public OverheadTextPrototype[] OnSpawnOverheadTexts { get; private set; }
    }

    public class SpawnerPrototype : WorldEntityPrototype
    {
        public int SpawnLifetimeMax { get; private set; }
        public int SpawnDistanceMin { get; private set; }
        public int SpawnDistanceMax { get; private set; }
        public int SpawnIntervalMS { get; private set; }
        public int SpawnSimultaneousMax { get; private set; }
        public ulong SpawnedEntityInventory { get; private set; }
        public SpawnerSequenceEntryPrototype[] SpawnSequence { get; private set; }
        public bool SpawnsInheritMissionPrototype { get; private set; }
        public bool StartEnabled { get; private set; }
        public bool OnDestroyCleanupSpawnedEntities { get; private set; }
        public int SpawnIntervalVarianceMS { get; private set; }
        public ulong HotspotTrigger { get; private set; }
        public BannerMessagePrototype OnDefeatBannerMessage { get; private set; }
        public bool OnDefeatDefeatGroup { get; private set; }
        public SpawnerDefeatCriteria DefeatCriteria { get; private set; }
        public EvalPrototype EvalSpawnProperties { get; private set; }
        public FormationFacing SpawnFacing { get; private set; }
        public SpawnFailBehavior SpawnFailBehavior { get; private set; }
        public int DefeatTimeoutMS { get; private set; }
    }

    public class KismetSequenceEntityPrototype : WorldEntityPrototype
    {
        public ulong KismetSequence { get; private set; }
    }

    public class FactionPrototype : Prototype
    {
        public ulong IconPath { get; private set; }
        public ulong TextStyle { get; private set; }
        public FactionColor HealthColor { get; private set; }
    }

    public class WaypointPrototype : Prototype
    {
        public ulong Destination { get; private set; }
        public ulong Name { get; private set; }
        public bool SupressBannerMessage { get; private set; }
        public bool IsCheckpoint { get; private set; }
        public ulong WaypointGraph { get; private set; }
        public ulong WaypointGraphList { get; private set; }
        public ulong RequiresItem { get; private set; }
        public EvalPrototype EvalShouldDisplay { get; private set; }
        public ulong Tooltip { get; private set; }
        public bool IncludeWaypointPrefixInName { get; private set; }
        public bool StartLocked { get; private set; }
        public ulong DestinationBossEntities { get; private set; }
        public bool IsAccountWaypoint { get; private set; }
        public int MigrationUnlockedByLevel { get; private set; }
        public ulong MigrationUnlockedByChapters { get; private set; }
        public WaypointPOIType MapPOIType { get; private set; }
        public ulong MapConnectTo { get; private set; }
        public ulong MapDescription { get; private set; }
        public float MapPOIXCoord { get; private set; }
        public float MapPOIYCoord { get; private set; }
        public ulong MapImage { get; private set; }
        public ulong OpenToWaypointGraph { get; private set; }
        public ulong MapImageConsole { get; private set; }
        public ulong LocationImageConsole { get; private set; }
        public ulong ConsoleRegionDescription { get; private set; }
        public ulong ConsoleLocationName { get; private set; }
        public ulong ConsoleRegionType { get; private set; }
        public ulong ConsoleLevelRange { get; private set; }
        public LocalizedTextAndImagePrototype[] ConsoleRegionItems { get; private set; }
        public ulong ConsoleWaypointGraphList { get; private set; }
    }

    public class WaypointChapterPrototype : Prototype
    {
        public ulong Chapter { get; private set; }
        public ulong[] Waypoints { get; private set; }
    }

    public class WaypointGraphPrototype : Prototype
    {
        public WaypointChapterPrototype[] Chapters { get; private set; }
        public ulong DisplayName { get; private set; }
        public ulong MapDescription { get; private set; }
        public ulong MapImage { get; private set; }
        public ulong Tooltip { get; private set; }
    }

    public class CheckpointPrototype : Prototype
    {
        public ulong Destination { get; private set; }
    }
}
