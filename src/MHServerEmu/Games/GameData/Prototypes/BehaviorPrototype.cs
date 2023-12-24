using MHServerEmu.Games.GameData.Calligraphy;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum]
    public enum ResourceType
    {
        Force = 0,
        Focus = 1,
        Fury = 2,
        Secondary_Pips = 3,
        Secondary_Gauge = 4,
    }

    [AssetEnum]
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

    [AssetEnum]
    public enum StackingApplicationStyleType
    {
        DontRefresh = 0,
        Refresh = 1,
        Recreate = 2,
        MatchDuration = 3,
        SingleStackAddDuration = 4,
        MultiStackAddDuration = 5,
    }

    [AssetEnum]
    public enum TeleportType
    {
        None = 0,
        AssistedEntity = 1,
        SpawnPosition = 2,
    }

    [AssetEnum]
    public enum SelectEntityType
    {
        None = 0,
        SelectAssistedEntity = 1,
        SelectInteractedEntity = 2,
        SelectTarget = 3,
        SelectTargetByAssistedEntitiesLastTarget = 4,
    }

    [AssetEnum]
    public enum SelectEntityPoolType
    {
        None = 0,
        AllEntitiesInCellOfAgent = 1,
        AllEntitiesInRegionOfAgent = 2,
        PotentialAlliesOfAgent = 3,
        PotentialEnemiesOfAgent = 4,
    }

    [AssetEnum]
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

    [AssetEnum]
    public enum FlankToType
    {
        AssistedEntity = 1,
        InteractEntity = 2,
        Target = 3,
    }

    [AssetEnum]
    public enum WanderBasePointType
    {
        CurrentPosition = 0,
        SpawnPoint = 1,
        TargetPosition = 2,
        None = 3,
    }

    [AssetEnum]
    public enum MoveToType
    {
        AssistedEntity = 0,
        DespawnPosition = 1,
        InteractEntity = 2,
        PathNode = 3,
        SpawnPosition = 4,
        Target = 5,
    }

    [AssetEnum]
    public enum MovementSpeedOverride
    {
        Default,
        Walk,
        Run,
    }

    #endregion

    public class BrainPrototype : Prototype
    {
    }

    public class ManaBehaviorPrototype : Prototype
    {
        public ulong DisplayName { get; private set; }
        public ResourceType MeterType { get; private set; }
        public ulong Powers { get; private set; }
        public bool StartsEmpty { get; private set; }
        public ulong Description { get; private set; }
        public ulong MeterColor { get; private set; }
        public ulong ResourceBarStyle { get; private set; }
        public ulong ResourcePipStyle { get; private set; }
        public bool DepleteOnDeath { get; private set; }
    }

    public class PrimaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public bool StartsWithRegenEnabled { get; private set; }
        public int RegenUpdateTimeMS { get; private set; }
        public EvalPrototype EvalOnEnduranceUpdate { get; private set; }
        public ManaType ManaType { get; private set; }
        public ulong BaseEndurancePerLevel { get; private set; }
        public bool RestoreToMaxOnLevelUp { get; private set; }
    }

    public class SecondaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public EvalPrototype EvalGetCurrentForDisplay { get; private set; }
        public EvalPrototype EvalGetCurrentPipsForDisplay { get; private set; }
        public EvalPrototype EvalGetMaxForDisplay { get; private set; }
        public EvalPrototype EvalGetMaxPipsForDisplay { get; private set; }
        public bool DepleteOnExitWorld { get; private set; }
        public bool ResetOnAvatarSwap { get; private set; }
    }

    public class AlliancePrototype : Prototype
    {
        public ulong HostileTo { get; private set; }
        public ulong FriendlyTo { get; private set; }
        public ulong WhileConfused { get; private set; }
        public ulong WhileControlled { get; private set; }
    }

    public class BotDefinitionEntryPrototype : Prototype
    {
        public ulong Avatar { get; private set; }
        public BehaviorProfilePrototype BehaviorProfile { get; private set; }
    }

    public class BotSettingsPrototype : Prototype
    {
        public BotDefinitionEntryPrototype[] BotDefinitions { get; private set; }
        public BehaviorProfilePrototype DefaultProceduralBotProfile { get; private set; }
    }

    public class AIEntityAttributePrototype : Prototype
    {
        public ComparisonOperatorType OperatorType { get; private set; }
    }

    public class AIEntityAttributeHasKeywordPrototype : AIEntityAttributePrototype
    {
        public ulong Keyword { get; private set; }
    }

    public class AIEntityAttributeHasConditionKeywordPrototype : AIEntityAttributePrototype
    {
        public ulong ConditionKeyword { get; private set; }
    }

    public class AIEntityAttributeIsHostilePrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeIsMeleePrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeIsAvatarPrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeIsAISummonedByAvatarPrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeIsPrototypeRefPrototype : AIEntityAttributePrototype
    {
        public ulong ProtoRef { get; private set; }
    }

    public class AIEntityAttributeIsPrototypePrototype : AIEntityAttributePrototype
    {
        public ulong RefToPrototype { get; private set; }
    }

    public class AIEntityAttributeIsSimulatedPrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeIsCurrentTargetEntityPrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeIsCurrentTargetEntityOfAgentOfTypePrototype : AIEntityAttributePrototype
    {
        public ulong OtherAgentProtoRef { get; private set; }
    }

    public class AIEntityAttributeIsSummonedByPowerPrototype : AIEntityAttributePrototype
    {
        public ulong Power { get; private set; }
    }

    public class AIEntityAttributeCanBePlayerOwnedPrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeHasBlackboardPropertyValuePrototype : AIEntityAttributePrototype
    {
        public ulong PropertyInfoRef { get; private set; }
        public int Value { get; private set; }
    }

    public class AIEntityAttributeHasPropertyPrototype : AIEntityAttributePrototype
    {
        public ulong PropertyInfoRef { get; private set; }
    }

    public class AIEntityAttributeHasHealthValuePercentPrototype : AIEntityAttributePrototype
    {
        public float Value { get; private set; }
    }

    public class AIEntityAttributeIsDestructiblePrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeCanPathToPrototype : AIEntityAttributePrototype
    {
        public LocomotorMethod LocomotorMethod { get; private set; }
    }

    public class MovementBehaviorPrototype : Prototype
    {
    }

    public class StrafeTargetPrototype : MovementBehaviorPrototype
    {
        public float StrafeDistanceMult { get; private set; }
    }

    public class RandomPositionAroundTargetPrototype : MovementBehaviorPrototype
    {
        public float StrafeAngle { get; private set; }
    }

    public class FixedRotationPrototype : MovementBehaviorPrototype
    {
        public float RotationSpeed { get; private set; }
        public float PivotAngle { get; private set; }
        public int MaxPivotTimeMS { get; private set; }
        public float PostPivotAcceleration { get; private set; }
    }

    public class StackingBehaviorPrototype : Prototype
    {
        public StackingApplicationStyleType ApplicationStyle { get; private set; }
        public int MaxNumStacks { get; private set; }
        public bool RemoveStackOnMaxNumStacksReached { get; private set; }
        public bool StacksFromDifferentCreators { get; private set; }
        public int NumStacksToApply { get; private set; }
        public ulong[] StacksByKeyword { get; private set; }
        public ulong StacksWithOtherPower { get; private set; }
    }

    public class DelayContextPrototype : Prototype
    {
        public int MaxDelayMS { get; private set; }
        public int MinDelayMS { get; private set; }
    }

    public class InteractContextPrototype : Prototype
    {
    }

    public class TeleportContextPrototype : Prototype
    {
        public TeleportType TeleportType { get; private set; }
    }

    public class SelectEntityContextPrototype : Prototype
    {
        public AIEntityAttributePrototype[] AttributeList { get; private set; }
        public float MaxDistanceThreshold { get; private set; }
        public float MinDistanceThreshold { get; private set; }
        public SelectEntityPoolType PoolType { get; private set; }
        public SelectEntityMethodType SelectionMethod { get; private set; }
        public ulong EntitiesPropertyForComparison { get; private set; }
        public SelectEntityType SelectEntityType { get; private set; }
        public bool LockEntityOnceSelected { get; private set; }
        public float CellOrRegionAABBScale { get; private set; }
        public ulong AlliancePriority { get; private set; }
    }

    public class FlankContextPrototype : Prototype
    {
        public float RangeMax { get; private set; }
        public float RangeMin { get; private set; }
        public bool StopAtFlankingWaypoint { get; private set; }
        public float ToTargetFlankingAngle { get; private set; }
        public float WaypointRadius { get; private set; }
        public int TimeoutMS { get; private set; }
        public bool FailOnTimeout { get; private set; }
        public bool RandomizeFlankingAngle { get; private set; }
        public FlankToType FlankTo { get; private set; }
    }

    public class FleeContextPrototype : Prototype
    {
        public float FleeTime { get; private set; }
        public float FleeTimeVariance { get; private set; }
        public float FleeHalfAngle { get; private set; }
        public float FleeDistanceMin { get; private set; }
        public bool FleeTowardAllies { get; private set; }
        public float FleeTowardAlliesPercentChance { get; private set; }
    }

    public class FlockContextPrototype : Prototype
    {
        public float RangeMax { get; private set; }
        public float RangeMin { get; private set; }
        public float SeparationWeight { get; private set; }
        public float AlignmentWeight { get; private set; }
        public float CohesionWeight { get; private set; }
        public float SeparationThreshold { get; private set; }
        public float AlignmentThreshold { get; private set; }
        public float CohesionThreshold { get; private set; }
        public float MaxSteeringForce { get; private set; }
        public float ForceToLeaderWeight { get; private set; }
        public bool SwitchLeaderOnCompletion { get; private set; }
        public bool ChooseRandomPointAsDestination { get; private set; }
        public WanderBasePointType WanderFromPointType { get; private set; }
        public float WanderRadius { get; private set; }
    }

    public class UseAffixPowerContextPrototype : Prototype
    {
    }

    public class UsePowerContextPrototype : Prototype
    {
        public ulong Power { get; private set; }
        public float TargetOffset { get; private set; }
        public bool RequireOriPriorToActivate { get; private set; }
        public float OrientationThreshold { get; private set; }
        public bool ForceIgnoreLOS { get; private set; }
        public float OffsetVarianceMagnitude { get; private set; }
        public bool ChooseRandomTargetPosition { get; private set; }
        public float OwnerOffset { get; private set; }
        public SelectEntityContextPrototype SecondaryTargetSelection { get; private set; }
        public bool TargetsWorldPosition { get; private set; }
        public bool ForceCheckTargetRegionLocation { get; private set; }
        public float TargetAngleOffset { get; private set; }
        public bool UseMainTargetForAOEActivation { get; private set; }
        public float MinDistanceFromOwner { get; private set; }
        public bool ForceInvalidTargetActivation { get; private set; }
        public bool AllowMovementClipping { get; private set; }
        public float MinDistanceToTarget { get; private set; }
        public float MaxDistanceToTarget { get; private set; }
        public bool IgnoreOutOfPositionFailure { get; private set; }
        public ulong[] DifficultyTierRestrictions { get; private set; }
    }

    public class MoveToContextPrototype : Prototype
    {
        public float LOSSweepPadding { get; private set; }
        public float RangeMax { get; private set; }
        public float RangeMin { get; private set; }
        public bool EnforceLOS { get; private set; }
        public MoveToType MoveTo { get; private set; }
        public PathMethod PathNodeSetMethod { get; private set; }
        public int PathNodeSetGroup { get; private set; }
        public MovementSpeedOverride MovementSpeed { get; private set; }
        public bool StopLocomotorOnMoveToFail { get; private set; }
    }

    public class OrbitContextPrototype : Prototype
    {
        public float ThetaInDegrees { get; private set; }
    }

    public class RotateContextPrototype : Prototype
    {
        public bool Clockwise { get; private set; }
        public int Degrees { get; private set; }
        public bool RotateTowardsTarget { get; private set; }
        public float SpeedOverride { get; private set; }
    }

    public class WanderContextPrototype : Prototype
    {
        public WanderBasePointType FromPoint { get; private set; }
        public float RangeMax { get; private set; }
        public float RangeMin { get; private set; }
        public MovementSpeedOverride MovementSpeed { get; private set; }
    }

    public class DespawnContextPrototype : Prototype
    {
        public bool DespawnOwner { get; private set; }
        public bool DespawnTarget { get; private set; }
        public bool UseKillInsteadOfDestroy { get; private set; }
    }

    public class TriggerSpawnersContextPrototype : Prototype
    {
        public bool DoPulse { get; private set; }
        public bool EnableSpawner { get; private set; }
        public ulong Spawners { get; private set; }
        public bool KillSummonedInventory { get; private set; }
        public bool SearchWholeRegion { get; private set; }
    }

    public class BehaviorProfilePrototype : Prototype
    {
        public float AggroDropChanceLOS { get; private set; }
        public float AggroDropDistance { get; private set; }
        public float AggroRangeAlly { get; private set; }
        public float AggroRangeHostile { get; private set; }
        public ulong Brain { get; private set; }
        public ulong[] EquippedPassivePowers { get; private set; }
        public bool IsBot { get; private set; }
        public int InterruptCooldownMS { get; private set; }
        public bool CanLeash { get; private set; }
        public ulong Properties { get; private set; }
        public bool AlwaysAggroed { get; private set; }
    }

    public class KismetSequencePrototype : Prototype
    {
        public ulong KismetSeqName { get; private set; }
        public bool KismetSeqBlocking { get; private set; }
        public bool AudioListenerAtCamera { get; private set; }
        public bool HideAvatarsDuringPlayback { get; private set; }
    }
}
