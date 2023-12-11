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
        public LocaleStringId DisplayName { get; set; }
        public StringId IconPath { get; set; }                                  // A Entity/Types/EntityIconPathType.type
        public PrototypePropertyCollection Properties { get; set; }             // Populated from mixins? Parsed from the game as ulong?
        public bool ReplicateToProximity { get; set; }
        public bool ReplicateToParty { get; set; }
        public bool ReplicateToOwner { get; set; }
        public bool ReplicateToDiscovered { get; set; }
        public EntityInventoryAssignmentPrototype[] Inventories { get; set; }
        public EvalPrototype[] EvalOnCreate { get; set; }
        public LocaleStringId DisplayNameInformal { get; set; }
        public LocaleStringId DisplayNameShort { get; set; }
        public bool ReplicateToTrader { get; set; }
        public int LifespanMS { get; set; }
        public StringId IconPathTooltipHeader { get; set; }                     // A Entity/Types/EntityIconPathType.type
        public StringId IconPathHiRes { get; set; }                             // A Entity/Types/EntityIconPathType.type
    }

    public class WorldEntityPrototype : EntityPrototype
    {
        public ulong Alliance { get; set; }
        public BoundsPrototype Bounds { get; set; }
        public ulong DialogText { get; set; }
        public ulong UnrealClass { get; set; }
        public ulong XPGrantedCurve { get; set; }
        public bool HACKBuildMouseCollision { get; set; }
        public ulong PreInteractPower { get; set; }
        public DialogStyle DialogStyle { get; set; }
        public WeightedTextEntryPrototype[] DialogTextList { get; set; }
        public ulong[] Keywords { get; set; }
        public DesignWorkflowState DesignState { get; set; }
        public ulong Rank { get; set; }
        public LocomotorMethod NaviMethod { get; set; }
        public bool SnapToFloorOnSpawn { get; set; }
        public bool AffectNavigation { get; set; }
        public StateChangePrototype PostInteractState { get; set; }
        public StateChangePrototype PostKilledState { get; set; }
        public bool OrientToInteractor { get; set; }
        public ulong TooltipInWorldTemplate { get; set; }
        public bool InteractIgnoreBoundsForDistance { get; set; }
        public float PopulationWeight { get; set; }
        public bool VisibleByDefault { get; set; }
        public int RemoveFromWorldTimerMS { get; set; }
        public bool RemoveNavInfluenceOnKilled { get; set; }
        public bool AlwaysSimulated { get; set; }
        public bool XPIsShared { get; set; }
        public ulong TutorialTip { get; set; }
        public bool TrackingDisabled { get; set; }
        public ulong[] ModifiersGuaranteed { get; set; }
        public float InteractRangeBonus { get; set; }
        public bool ShouldIgnoreMaxDeadBodies { get; set; }
        public bool ModifierSetEnable { get; set; }
        public bool LiveTuningDefaultEnabled { get; set; }
        public bool UpdateOrientationWithParent { get; set; }
        public bool MissionEntityDeathCredit { get; set; }
        public bool HACKDiscoverInRegion { get; set; }
        public bool CanCollideWithPowerUserItems { get; set; }
        public bool ForwardOnHitProcsToOwner { get; set; }
        public ObjectiveInfoPrototype ObjectiveInfo { get; set; }
        public WorldEntityIconsPrototype Icons { get; set; }
        public EntitySelectorActionPrototype[] EntitySelectorActions { get; set; }
        public bool OverheadIndicator { get; set; }
        public bool RequireCombatActiveForKillCredit { get; set; }
        public bool ClonePerPlayer { get; set; }
        public bool PrefetchMarkedAssets { get; set; }
        public ulong MarvelModelRenderClass { get; set; }
        public DesignWorkflowState DesignStatePS4 { get; set; }
        public DesignWorkflowState DesignStateXboxOne { get; set; }
    }

    public class StateChangePrototype : Prototype
    {
    }

    public class StateTogglePrototype : StateChangePrototype
    {
        public ulong StateA { get; set; }
        public ulong StateB { get; set; }
    }

    public class StateSetPrototype : StateChangePrototype
    {
        public ulong State { get; set; }
    }

    public class WorldEntityIconsPrototype : Prototype
    {
        public ulong EdgeIcon { get; set; }
        public ulong MapIcon { get; set; }
        public ulong EdgeIconHiRes { get; set; }
    }

    #region EntityAction

    public class EntityActionBasePrototype : Prototype
    {
        public int Weight { get; set; }
    }

    public class EntityActionAIOverridePrototype : EntityActionBasePrototype
    {
        public ulong Power { get; set; }
        public bool PowerRemove { get; set; }
        public ulong Brain { get; set; }
        public bool BrainRemove { get; set; }
        public bool SelectorReferencedPowerRemove { get; set; }
        public int AIAggroRangeOverrideAlly { get; set; }
        public int AIAggroRangeOverrideEnemy { get; set; }
        public int AIProximityRangeOverride { get; set; }
        public int LifespanMS { get; set; }
        public ulong LifespanEndPower { get; set; }
    }

    public class EntityActionOverheadTextPrototype : EntityActionBasePrototype
    {
        public ulong Text { get; set; }
        public int Duration { get; set; }
    }

    public class EntityActionEventBroadcastPrototype : EntityActionBasePrototype
    {
        public EntitySelectorActionEventType EventToBroadcast { get; set; }
        public int BroadcastRange { get; set; }
    }

    public class EntityActionSpawnerTriggerPrototype : EntityActionBasePrototype
    {
        public bool EnableClusterLocalSpawner { get; set; }
    }

    public class EntitySelectorActionBasePrototype : Prototype
    {
        public ulong[] AIOverrides { get; set; }
        public EntityActionAIOverridePrototype[] AIOverridesList { get; set; }
        public ulong[] OverheadTexts { get; set; }
        public EntityActionOverheadTextPrototype[] OverheadTextsList { get; set; }
        public ulong[] Rewards { get; set; }
        public EntitySelectorAttributeActions[] AttributeActions { get; set; }
        public HUDEntitySettingsPrototype HUDEntitySettingOverride { get; set; }
    }

    public class EntitySelectorActionPrototype : EntitySelectorActionBasePrototype
    {
        public EntitySelectorActionEventType[] EventTypes { get; set; }
        public int ReactionTimeMS { get; set; }
        public EntitySelectorActionEventType[] CancelOnEventTypes { get; set; }
        public ulong SpawnerTrigger { get; set; }
        public ulong AllianceOverride { get; set; }
        public ulong BroadcastEvent { get; set; }
    }

    public class EntitySelectorActionSetPrototype : Prototype
    {
        public EntitySelectorActionPrototype[] EntitySelectorActions { get; set; }
    }

    public class EntitySelectorPrototype : Prototype
    {
        public ulong[] Entities { get; set; }
        public EntitySelectorActionPrototype[] EntitySelectorActions { get; set; }
        public ulong EntitySelectorActionsTemplate { get; set; }
        public ulong DefaultBrainOnSimulated { get; set; }
        public bool IgnoreMissionOwnerForTargeting { get; set; }
        public float DefaultAggroRangeAlly { get; set; }
        public float DefaultAggroRangeHostile { get; set; }
        public float DefaultProximityRangeHostile { get; set; }
        public EvalPrototype EvalSpawnProperties { get; set; }
        public bool SelectUniqueEntities { get; set; }
    }

    public class EntityActionTimelineScriptActionPrototype : EntitySelectorActionBasePrototype
    {
        public ScriptRoleKeyEnum[] ScriptRoleKeys { get; set; }
        public ulong SpawnerTrigger { get; set; }
    }

    public class EntityActionTimelineScriptEventPrototype : Prototype
    {
        public ulong[] ActionsList { get; set; }
        public EntityActionTimelineScriptActionPrototype[] ActionsVector { get; set; }
        public int EventTime { get; set; }
        public EntitySelectorActionEventType[] InterruptOnEventTypes { get; set; }
    }

    public class EntityActionTimelineScriptPrototype : Prototype
    {
        public EntitySelectorActionEventType[] TriggerOnEventTypes { get; set; }
        public EntitySelectorActionEventType[] CancelOnEventTypes { get; set; }
        public EntityActionTimelineScriptEventPrototype[] ScriptEvents { get; set; }
        public bool RunOnceOnly { get; set; }
    }

    #endregion

    public class WeightedTextEntryPrototype : Prototype
    {
        public ulong Text { get; set; }
        public long Weight { get; set; }
    }

    public class TransitionPrototype : WorldEntityPrototype
    {
        public RegionTransitionType Type { get; set; }
        public int SpawnOffset { get; set; }
        public ulong Waypoint { get; set; }
        public bool SupressBlackout { get; set; }
        public bool ShowIndicator { get; set; }
        public bool ShowConfirmationDialog { get; set; }
        public ulong DirectTarget { get; set; }
        public ulong[] RegionAffixesBySummonerRarity { get; set; }
        public ulong ShowConfirmationDialogOverride { get; set; }
        public ulong ShowConfirmationDialogTemplate { get; set; }
        public ulong ShowConfirmationDialogEnemy { get; set; }
    }

    public class EntityAppearancePrototype : Prototype
    {
        public EntityAppearanceEnum AppearanceEnum { get; set; }
    }

    public class EntityStatePrototype : Prototype
    {
        public ulong Appearance { get; set; }
        public ulong OnActivatePowers { get; set; }
    }

    public class DoorEntityStatePrototype : EntityStatePrototype
    {
        public bool IsOpen { get; set; }
    }

    public class InteractionSpecPrototype : Prototype
    {
    }

    public class ConnectionTargetEnableSpecPrototype : InteractionSpecPrototype
    {
        public ulong ConnectionTarget { get; set; }
        public bool Enabled { get; set; }
    }

    public class EntityBaseSpecPrototype : InteractionSpecPrototype
    {
        public EntityFilterPrototype EntityFilter { get; set; }
    }

    public class EntityVisibilitySpecPrototype : EntityBaseSpecPrototype
    {
        public bool Visible { get; set; }
    }

    public class EntityAppearanceSpecPrototype : EntityBaseSpecPrototype
    {
        public ulong Appearance { get; set; }
        public ulong FailureReasonText { get; set; }
        public TriBool InteractionEnabled { get; set; }
    }

    public class HotspotDirectApplyToMissilesDataPrototype : Prototype
    {
        public bool AffectsAllyMissiles { get; set; }
        public bool AffectsHostileMissiles { get; set; }
        public EvalPrototype EvalPropertiesToApply { get; set; }
        public bool AffectsReflectedMissilesOnly { get; set; }
        public bool IsPermanent { get; set; }
    }

    public class HotspotPrototype : WorldEntityPrototype
    {
        public PowerPrototype AppliesPowers { get; set; }
        public PowerPrototype AppliesIntervalPowers { get; set; }
        public int IntervalPowersTimeDelayMS { get; set; }
        public bool IntervalPowersRandomTarget { get; set; }
        public int IntervalPowersNumRandomTargets { get; set; }
        public UINotificationPrototype UINotificationOnEnter { get; set; }
        public int MaxSimultaneousTargets { get; set; }
        public bool KillCreatorWhenHotspotIsEmpty { get; set; }
        public ulong KismetSeq { get; set; }
        public bool Negatable { get; set; }
        public bool KillSelfWhenPowerApplied { get; set; }
        public HotspotOverlapEventTriggerType OverlapEventsTriggerOn { get; set; }
        public int OverlapEventsMaxTargets { get; set; }
        public HotspotDirectApplyToMissilesDataPrototype DirectApplyToMissilesData { get; set; }
        public int ApplyEffectsDelayMS { get; set; }
        public ulong CameraSettings { get; set; }
        public int MaxLifetimeTargets { get; set; }
    }

    public class OverheadTextPrototype : Prototype
    {
        public EntityFilterFilterListPrototype OverheadTextEntityFilter { get; set; }
        public ulong OverheadText { get; set; }
    }

    public class SpawnerSequenceEntryPrototype : PopulationRequiredObjectPrototype
    {
        public bool OnKilledDefeatSpawner { get; set; }
        public ulong OnDefeatAIOverride { get; set; }
        public bool Unique { get; set; }
        public OverheadTextPrototype[] OnSpawnOverheadTexts { get; set; }
    }

    public class SpawnerPrototype : WorldEntityPrototype
    {
        public int SpawnLifetimeMax { get; set; }
        public int SpawnDistanceMin { get; set; }
        public int SpawnDistanceMax { get; set; }
        public int SpawnIntervalMS { get; set; }
        public int SpawnSimultaneousMax { get; set; }
        public ulong SpawnedEntityInventory { get; set; }
        public SpawnerSequenceEntryPrototype[] SpawnSequence { get; set; }
        public bool SpawnsInheritMissionPrototype { get; set; }
        public bool StartEnabled { get; set; }
        public bool OnDestroyCleanupSpawnedEntities { get; set; }
        public int SpawnIntervalVarianceMS { get; set; }
        public ulong HotspotTrigger { get; set; }
        public BannerMessagePrototype OnDefeatBannerMessage { get; set; }
        public bool OnDefeatDefeatGroup { get; set; }
        public SpawnerDefeatCriteria DefeatCriteria { get; set; }
        public EvalPrototype EvalSpawnProperties { get; set; }
        public FormationFacing SpawnFacing { get; set; }
        public SpawnFailBehavior SpawnFailBehavior { get; set; }
        public int DefeatTimeoutMS { get; set; }
    }

    public class KismetSequenceEntityPrototype : WorldEntityPrototype
    {
        public ulong KismetSequence { get; set; }
    }

    public class FactionPrototype : Prototype
    {
        public ulong IconPath { get; set; }
        public ulong TextStyle { get; set; }
        public FactionColor HealthColor { get; set; }
    }

    public class WaypointPrototype : Prototype
    {
        public ulong Destination { get; set; }
        public ulong Name { get; set; }
        public bool SupressBannerMessage { get; set; }
        public bool IsCheckpoint { get; set; }
        public ulong WaypointGraph { get; set; }
        public ulong WaypointGraphList { get; set; }
        public ulong RequiresItem { get; set; }
        public EvalPrototype EvalShouldDisplay { get; set; }
        public ulong Tooltip { get; set; }
        public bool IncludeWaypointPrefixInName { get; set; }
        public bool StartLocked { get; set; }
        public ulong DestinationBossEntities { get; set; }
        public bool IsAccountWaypoint { get; set; }
        public int MigrationUnlockedByLevel { get; set; }
        public ulong MigrationUnlockedByChapters { get; set; }
        public WaypointPOIType MapPOIType { get; set; }
        public ulong MapConnectTo { get; set; }
        public ulong MapDescription { get; set; }
        public float MapPOIXCoord { get; set; }
        public float MapPOIYCoord { get; set; }
        public ulong MapImage { get; set; }
        public ulong OpenToWaypointGraph { get; set; }
        public ulong MapImageConsole { get; set; }
        public ulong LocationImageConsole { get; set; }
        public ulong ConsoleRegionDescription { get; set; }
        public ulong ConsoleLocationName { get; set; }
        public ulong ConsoleRegionType { get; set; }
        public ulong ConsoleLevelRange { get; set; }
        public LocalizedTextAndImagePrototype[] ConsoleRegionItems { get; set; }
        public ulong ConsoleWaypointGraphList { get; set; }
    }

    public class WaypointChapterPrototype : Prototype
    {
        public ulong Chapter { get; set; }
        public ulong[] Waypoints { get; set; }
    }

    public class WaypointGraphPrototype : Prototype
    {
        public WaypointChapterPrototype[] Chapters { get; set; }
        public ulong DisplayName { get; set; }
        public ulong MapDescription { get; set; }
        public ulong MapImage { get; set; }
        public ulong Tooltip { get; set; }
    }

    public class CheckpointPrototype : Prototype
    {
        public ulong Destination { get; set; }
    }
}
