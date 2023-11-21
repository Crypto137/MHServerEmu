using MHServerEmu.Games.GameData.Prototypes;
using static MHServerEmu.Games.Generators.Navi.PathCache;

namespace MHServerEmu.Games.Generators.Prototypes
{

    public class BrainPrototype : Prototype
    {
        public BrainPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BrainPrototype), proto); }
    }

    public class ManaBehaviorPrototype : Prototype
    {
        public ulong DisplayName;
        public ResourceType MeterType;
        public ulong Powers;
        public bool StartsEmpty;
        public ulong Description;
        public ulong MeterColor;
        public ulong ResourceBarStyle;
        public ulong ResourcePipStyle;
        public bool DepleteOnDeath;
        public ManaBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ManaBehaviorPrototype), proto); }
    }
    public enum ResourceType
    {
        Force = 0,
        Focus = 1,
        Fury = 2,
        Secondary_Pips = 3,
        Secondary_Gauge = 4,
    }

    public class PrimaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public bool StartsWithRegenEnabled;
        public int RegenUpdateTimeMS;
        public EvalPrototype EvalOnEnduranceUpdate;
        public ManaType ManaType;
        public ulong BaseEndurancePerLevel;
        public bool RestoreToMaxOnLevelUp;
        public PrimaryResourceManaBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(PrimaryResourceManaBehaviorPrototype), proto); }
    }

    public class SecondaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public EvalPrototype EvalGetCurrentForDisplay;
        public EvalPrototype EvalGetCurrentPipsForDisplay;
        public EvalPrototype EvalGetMaxForDisplay;
        public EvalPrototype EvalGetMaxPipsForDisplay;
        public bool DepleteOnExitWorld;
        public bool ResetOnAvatarSwap;
        public SecondaryResourceManaBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SecondaryResourceManaBehaviorPrototype), proto); }
    }

    public class AlliancePrototype : Prototype
    {
        public ulong HostileTo;
        public ulong FriendlyTo;
        public ulong WhileConfused;
        public ulong WhileControlled;
        public AlliancePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AlliancePrototype), proto); }
    }

    public class BotDefinitionEntryPrototype : Prototype
    {
        public ulong Avatar;
        public BehaviorProfilePrototype BehaviorProfile;
        public BotDefinitionEntryPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BotDefinitionEntryPrototype), proto); }
    }

    public class BotSettingsPrototype : Prototype
    {
        public BotDefinitionEntryPrototype[] BotDefinitions;
        public BehaviorProfilePrototype DefaultProceduralBotProfile;
        public BotSettingsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BotSettingsPrototype), proto); }
    }

    public class AIEntityAttributePrototype : Prototype
    {
        public ComparisonOperatorType OperatorType;
        public AIEntityAttributePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributePrototype), proto); }
    }
    public enum ComparisonOperatorType
    {
        EqualTo = 0,
        GreaterThan = 1,
        GreaterThanEqualTo = 2,
        LessThan = 3,
        LessThanEqualTo = 4,
        NotEqualTo = 5,
        None = 6,
    }
    public class AIEntityAttributeHasKeywordPrototype : AIEntityAttributePrototype
    {
        public ulong Keyword;
        public AIEntityAttributeHasKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeHasKeywordPrototype), proto); }
    }

    public class AIEntityAttributeHasConditionKeywordPrototype : AIEntityAttributePrototype
    {
        public ulong ConditionKeyword;
        public AIEntityAttributeHasConditionKeywordPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeHasConditionKeywordPrototype), proto); }
    }

    public class AIEntityAttributeIsHostilePrototype : AIEntityAttributePrototype
    {
        public AIEntityAttributeIsHostilePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsHostilePrototype), proto); }
    }

    public class AIEntityAttributeIsMeleePrototype : AIEntityAttributePrototype
    {
        public AIEntityAttributeIsMeleePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsMeleePrototype), proto); }
    }

    public class AIEntityAttributeIsAvatarPrototype : AIEntityAttributePrototype
    {
        public AIEntityAttributeIsAvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsAvatarPrototype), proto); }
    }

    public class AIEntityAttributeIsAISummonedByAvatarPrototype : AIEntityAttributePrototype
    {
        public AIEntityAttributeIsAISummonedByAvatarPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsAISummonedByAvatarPrototype), proto); }
    }

    public class AIEntityAttributeIsPrototypeRefPrototype : AIEntityAttributePrototype
    {
        public ulong ProtoRef;
        public AIEntityAttributeIsPrototypeRefPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsPrototypeRefPrototype), proto); }
    }

    public class AIEntityAttributeIsPrototypePrototype : AIEntityAttributePrototype
    {
        public ulong RefToPrototype;
        public AIEntityAttributeIsPrototypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsPrototypePrototype), proto); }
    }

    public class AIEntityAttributeIsSimulatedPrototype : AIEntityAttributePrototype
    {
        public AIEntityAttributeIsSimulatedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsSimulatedPrototype), proto); }
    }

    public class AIEntityAttributeIsCurrentTargetEntityPrototype : AIEntityAttributePrototype
    {
        public AIEntityAttributeIsCurrentTargetEntityPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsCurrentTargetEntityPrototype), proto); }
    }

    public class AIEntityAttributeIsCurrentTargetEntityOfAgentOfTypePrototype : AIEntityAttributePrototype
    {
        public ulong OtherAgentProtoRef;
        public AIEntityAttributeIsCurrentTargetEntityOfAgentOfTypePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsCurrentTargetEntityOfAgentOfTypePrototype), proto); }
    }

    public class AIEntityAttributeIsSummonedByPowerPrototype : AIEntityAttributePrototype
    {
        public ulong Power;
        public AIEntityAttributeIsSummonedByPowerPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsSummonedByPowerPrototype), proto); }
    }

    public class AIEntityAttributeCanBePlayerOwnedPrototype : AIEntityAttributePrototype
    {
        public AIEntityAttributeCanBePlayerOwnedPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeCanBePlayerOwnedPrototype), proto); }
    }

    public class AIEntityAttributeHasBlackboardPropertyValuePrototype : AIEntityAttributePrototype
    {
        public ulong PropertyInfoRef;
        public int Value;
        public AIEntityAttributeHasBlackboardPropertyValuePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeHasBlackboardPropertyValuePrototype), proto); }
    }

    public class AIEntityAttributeHasPropertyPrototype : AIEntityAttributePrototype
    {
        public ulong PropertyInfoRef;
        public AIEntityAttributeHasPropertyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeHasPropertyPrototype), proto); }
    }

    public class AIEntityAttributeHasHealthValuePercentPrototype : AIEntityAttributePrototype
    {
        public float Value;
        public AIEntityAttributeHasHealthValuePercentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeHasHealthValuePercentPrototype), proto); }
    }

    public class AIEntityAttributeIsDestructiblePrototype : AIEntityAttributePrototype
    {
        public AIEntityAttributeIsDestructiblePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeIsDestructiblePrototype), proto); }
    }

    public class AIEntityAttributeCanPathToPrototype : AIEntityAttributePrototype
    {
        public Method LocomotorMethod;
        public AIEntityAttributeCanPathToPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AIEntityAttributeCanPathToPrototype), proto); }
    }


    public class MovementBehaviorPrototype : Prototype
    {
        public MovementBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MovementBehaviorPrototype), proto); }
    }

    public class StrafeTargetPrototype : MovementBehaviorPrototype
    {
        public float StrafeDistanceMult;
        public StrafeTargetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StrafeTargetPrototype), proto); }
    }

    public class RandomPositionAroundTargetPrototype : MovementBehaviorPrototype
    {
        public float StrafeAngle;
        public RandomPositionAroundTargetPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RandomPositionAroundTargetPrototype), proto); }
    }

    public class FixedRotationPrototype : MovementBehaviorPrototype
    {
        public float RotationSpeed;
        public float PivotAngle;
        public int MaxPivotTimeMS;
        public float PostPivotAcceleration;
        public FixedRotationPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FixedRotationPrototype), proto); }
    }

    public class StackingBehaviorPrototype : Prototype
    {
        public StackingApplicationStyleType ApplicationStyle;
        public int MaxNumStacks;
        public bool RemoveStackOnMaxNumStacksReached;
        public bool StacksFromDifferentCreators;
        public int NumStacksToApply;
        public ulong[] StacksByKeyword;
        public ulong StacksWithOtherPower;
        public StackingBehaviorPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(StackingBehaviorPrototype), proto); }
    }
    public enum StackingApplicationStyleType
    {
        DontRefresh = 0,
        Refresh = 1,
        Recreate = 2,
        MatchDuration = 3,
        SingleStackAddDuration = 4,
        MultiStackAddDuration = 5,
    }

    public class DelayContextPrototype : Prototype
    {
        public int MaxDelayMS;
        public int MinDelayMS;
        public DelayContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DelayContextPrototype), proto); }
    }

    public class InteractContextPrototype : Prototype
    {
        public InteractContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(InteractContextPrototype), proto); }
    }

    public class TeleportContextPrototype : Prototype
    {
        public TeleportType TeleportType;
        public TeleportContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TeleportContextPrototype), proto); }
    }
    public enum TeleportType
    {
        None = 0,
        AssistedEntity = 1,
        SpawnPosition = 2,
    }
    public class SelectEntityContextPrototype : Prototype
    {
        public AIEntityAttributePrototype[] AttributeList;
        public float MaxDistanceThreshold;
        public float MinDistanceThreshold;
        public SelectEntityPoolType PoolType;
        public SelectEntityMethodType SelectionMethod;
        public ulong EntitiesPropertyForComparison;
        public SelectEntityTypeType SelectEntityType;
        public bool LockEntityOnceSelected;
        public float CellOrRegionAABBScale;
        public ulong AlliancePriority;
        public SelectEntityContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(SelectEntityContextPrototype), proto); }
    }
    public enum SelectEntityTypeType
    {
        None = 0,
        SelectAssistedEntity = 1,
        SelectInteractedEntity = 2,
        SelectTarget = 3,
        SelectTargetByAssistedEntitiesLastTarget = 4,
    }
    public enum SelectEntityPoolType
    {
        None = 0,
        AllEntitiesInCellOfAgent = 1,
        AllEntitiesInRegionOfAgent = 2,
        PotentialAlliesOfAgent = 3,
        PotentialEnemiesOfAgent = 4,
    }
    public enum SelectEntityMethodType
    {
        None = 0,
        ClosestEntity = 1,
        FarthestEntity = 2,
        FirstFound = 4,
        HighestValueOfProperty = 5,
        LowestValueOfProperty = 6,
        MostDamageInTimeInterval = 7,
        RandomEntity = 8,
        Self = 9,
    }
    public class FlankContextPrototype : Prototype
    {
        public float RangeMax;
        public float RangeMin;
        public bool StopAtFlankingWaypoint;
        public float ToTargetFlankingAngle;
        public float WaypointRadius;
        public int TimeoutMS;
        public bool FailOnTimeout;
        public bool RandomizeFlankingAngle;
        public FlankToType FlankTo;
        public FlankContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FlankContextPrototype), proto); }
    }
    public enum FlankToType
    {
        AssistedEntity = 1,
        InteractEntity = 2,
        Target = 3,
    }
    public class FleeContextPrototype : Prototype
    {
        public float FleeTime;
        public float FleeTimeVariance;
        public float FleeHalfAngle;
        public float FleeDistanceMin;
        public bool FleeTowardAllies;
        public float FleeTowardAlliesPercentChance;
        public FleeContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FleeContextPrototype), proto); }
    }

    public class FlockContextPrototype : Prototype
    {
        public float RangeMax;
        public float RangeMin;
        public float SeparationWeight;
        public float AlignmentWeight;
        public float CohesionWeight;
        public float SeparationThreshold;
        public float AlignmentThreshold;
        public float CohesionThreshold;
        public float MaxSteeringForce;
        public float ForceToLeaderWeight;
        public bool SwitchLeaderOnCompletion;
        public bool ChooseRandomPointAsDestination;
        public WanderBasePointType WanderFromPointType;
        public float WanderRadius;
        public FlockContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(FlockContextPrototype), proto); }
    }
    public enum WanderBasePointType
    {
        CurrentPosition = 0,
        SpawnPoint = 1,
        TargetPosition = 2,
        None = 3,
    }
    public class UseAffixPowerContextPrototype : Prototype
    {
        public UseAffixPowerContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UseAffixPowerContextPrototype), proto); }
    }

    public class UsePowerContextPrototype : Prototype
    {
        public ulong Power;
        public float TargetOffset;
        public bool RequireOriPriorToActivate;
        public float OrientationThreshold;
        public bool ForceIgnoreLOS;
        public float OffsetVarianceMagnitude;
        public bool ChooseRandomTargetPosition;
        public float OwnerOffset;
        public SelectEntityContextPrototype SecondaryTargetSelection;
        public bool TargetsWorldPosition;
        public bool ForceCheckTargetRegionLocation;
        public float TargetAngleOffset;
        public bool UseMainTargetForAOEActivation;
        public float MinDistanceFromOwner;
        public bool ForceInvalidTargetActivation;
        public bool AllowMovementClipping;
        public float MinDistanceToTarget;
        public float MaxDistanceToTarget;
        public bool IgnoreOutOfPositionFailure;
        public ulong[] DifficultyTierRestrictions;
        public UsePowerContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(UsePowerContextPrototype), proto); }
    }

    public class MoveToContextPrototype : Prototype
    {
        public float LOSSweepPadding;
        public float RangeMax;
        public float RangeMin;
        public bool EnforceLOS;
        public MoveToType MoveTo;
        public PathMethod PathNodeSetMethod;
        public int PathNodeSetGroup;
        public MovementSpeedOverride MovementSpeed;
        public bool StopLocomotorOnMoveToFail;
        public MoveToContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(MoveToContextPrototype), proto); }
    }
    public enum MoveToType
    {
        AssistedEntity = 0,
        DespawnPosition = 1,
        InteractEntity = 2,
        PathNode = 3,
        SpawnPosition = 4,
        Target = 5,
    }
    public enum MovementSpeedOverride
    {
        Default,
        Walk,
        Run,
    }

    public class OrbitContextPrototype : Prototype
    {
        public float ThetaInDegrees;
        public OrbitContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(OrbitContextPrototype), proto); }
    }

    public class RotateContextPrototype : Prototype
    {
        public bool Clockwise;
        public int Degrees;
        public bool RotateTowardsTarget;
        public float SpeedOverride;
        public RotateContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RotateContextPrototype), proto); }
    }

    public class WanderContextPrototype : Prototype
    {
        public WanderBasePointType FromPoint;
        public float RangeMax;
        public float RangeMin;
        public MovementSpeedOverride MovementSpeed;
        public WanderContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(WanderContextPrototype), proto); }
    }

    public class DespawnContextPrototype : Prototype
    {
        public bool DespawnOwner;
        public bool DespawnTarget;
        public bool UseKillInsteadOfDestroy;
        public DespawnContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(DespawnContextPrototype), proto); }
    }

    public class TriggerSpawnersContextPrototype : Prototype
    {
        public bool DoPulse;
        public bool EnableSpawner;
        public ulong Spawners;
        public bool KillSummonedInventory;
        public bool SearchWholeRegion;
        public TriggerSpawnersContextPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(TriggerSpawnersContextPrototype), proto); }
    }

    public class BehaviorProfilePrototype : Prototype
    {
        public float AggroDropChanceLOS;
        public float AggroDropDistance;
        public float AggroRangeAlly;
        public float AggroRangeHostile;
        public ulong Brain;
        public ulong[] EquippedPassivePowers;
        public bool IsBot;
        public int InterruptCooldownMS;
        public bool CanLeash;
        public ulong Properties;
        public bool AlwaysAggroed;
        public BehaviorProfilePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(BehaviorProfilePrototype), proto); }
    }

    public class KismetSequencePrototype : Prototype
    {
        public ulong KismetSeqName;
        public bool KismetSeqBlocking;
        public bool AudioListenerAtCamera;
        public bool HideAvatarsDuringPlayback;
        public KismetSequencePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(KismetSequencePrototype), proto); }
    }
}
