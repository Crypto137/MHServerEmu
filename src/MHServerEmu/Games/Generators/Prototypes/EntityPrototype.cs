using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class EntityPrototype : Prototype
    {
        public ulong DisplayName;
        public ulong DisplayNameInformal;
        public ulong DisplayNameShort;
        public EvalPrototype[] EvalOnCreate;
        public ulong IconPath;
        public ulong IconPathHiRes;
        public ulong IconPathTooltipHeader;
        public int LifespanMS;
        public ulong Properties;
        public bool ReplicateToOwner;
        public bool ReplicateToParty;
        public bool ReplicateToProximity;
        public bool ReplicateToDiscovered;
        public bool ReplicateToTrader;
        public EntityInventoryAssignmentPrototype[] Inventories;

        public EntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntityPrototype), proto); }
    }

    public class WorldEntityPrototype : EntityPrototype
    {
        public Method NaviMethod;
        public bool AffectNavigation;
        public ulong Alliance;
        public bool RequireCombatActiveForKillCredit;
        public BoundsPrototype Bounds;
        public bool CanCollideWithPowerUserItems;
        public int RemoveFromWorldTimerMS;
        public DesignWorkflowState DesignState;
        public DialogStyle DialogStyle;
        public ulong DialogText;
        public WeightedTextEntryPrototype[] DialogTextList;
        public EntitySelectorActionPrototype[] EntitySelectorActions;
        public bool ForwardOnHitProcsToOwner;
        public bool LiveTuningDefaultEnabled;
        public bool HACKBuildMouseCollision;
        public float InteractRangeBonus;
        public ulong[] Keywords;
        public bool MissionEntityDeathCredit;
        public bool PrefetchMarkedAssets;
        public ulong PreInteractPower;
        public bool OrientToInteractor;
        public bool SnapToFloorOnSpawn;
        public ulong UnrealClass;
        public ulong XPGrantedCurve;
        public bool XPIsShared;
        public bool ModifierSetEnable;
        public ulong[] ModifiersGuaranteed;
        public WorldEntityIconsPrototype Icons;
        public ObjectiveInfoPrototype ObjectiveInfo;
        public bool OverheadIndicator;
        public ulong Rank;
        public bool RemoveNavInfluenceOnKilled;
        public StateChangePrototype PostInteractState;
        public StateChangePrototype PostKilledState;
        public ulong TooltipInWorldTemplate;
        public bool InteractIgnoreBoundsForDistance;
        public float PopulationWeight;
        public bool VisibleByDefault;
        public bool AlwaysSimulated;
        public ulong TutorialTip;
        public bool TrackingDisabled;
        public bool ShouldIgnoreMaxDeadBodies;
        public bool UpdateOrientationWithParent;
        public bool HACKDiscoverInRegion;
        public bool ClonePerPlayer;
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

    public class WorldEntityIconsPrototype : EntityPrototype
    {
        public ulong EdgeIcon;
        public ulong EdgeIconHiRes;
        public ulong MapIcon;

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
        public ulong HUDEntitySettingOverride;
        public ulong[] Rewards;
        public EntitySelectorAttributeActions[] AttributeActions;
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
        public EntitySelectorActionEventType[] CancelOnEventTypes;
        public int ReactionTimeMS;
        public ulong SpawnerTrigger;
        public ulong AllianceOverride;
        public ulong BroadcastEvent;
        public EntitySelectorActionPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(EntitySelectorActionPrototype), proto); }
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
        public bool ShowConfirmationDialog;
        public ulong ShowConfirmationDialogOverride;
        public TranslationPrototype ShowConfirmationDialogTemplate;
        public ulong ShowConfirmationDialogEnemy;
        public bool ShowIndicator;
        public ulong DirectTarget;
        public ulong[] RegionAffixesBySummonerRarity;

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
}
