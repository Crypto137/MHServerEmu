namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    public enum ResourceType
    {
        Force = 0,
        Focus = 1,
        Fury = 2,
        Secondary_Pips = 3,
        Secondary_Gauge = 4,
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

    public enum StackingApplicationStyleType
    {
        DontRefresh = 0,
        Refresh = 1,
        Recreate = 2,
        MatchDuration = 3,
        SingleStackAddDuration = 4,
        MultiStackAddDuration = 5,
    }

    public enum TeleportType
    {
        None = 0,
        AssistedEntity = 1,
        SpawnPosition = 2,
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

    public enum FlankToType
    {
        AssistedEntity = 1,
        InteractEntity = 2,
        Target = 3,
    }

    public enum WanderBasePointType
    {
        CurrentPosition = 0,
        SpawnPoint = 1,
        TargetPosition = 2,
        None = 3,
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

    #endregion

    public class BrainPrototype : Prototype
    {
    }

    public class ManaBehaviorPrototype : Prototype
    {
        public ulong DisplayName { get; set; }
        public ResourceType MeterType { get; set; }
        public ulong Powers { get; set; }
        public bool StartsEmpty { get; set; }
        public ulong Description { get; set; }
        public ulong MeterColor { get; set; }
        public ulong ResourceBarStyle { get; set; }
        public ulong ResourcePipStyle { get; set; }
        public bool DepleteOnDeath { get; set; }
    }

    public class PrimaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public bool StartsWithRegenEnabled { get; set; }
        public int RegenUpdateTimeMS { get; set; }
        public EvalPrototype EvalOnEnduranceUpdate { get; set; }
        public ManaType ManaType { get; set; }
        public ulong BaseEndurancePerLevel { get; set; }
        public bool RestoreToMaxOnLevelUp { get; set; }
    }

    public class SecondaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public EvalPrototype EvalGetCurrentForDisplay { get; set; }
        public EvalPrototype EvalGetCurrentPipsForDisplay { get; set; }
        public EvalPrototype EvalGetMaxForDisplay { get; set; }
        public EvalPrototype EvalGetMaxPipsForDisplay { get; set; }
        public bool DepleteOnExitWorld { get; set; }
        public bool ResetOnAvatarSwap { get; set; }
    }

    public class AlliancePrototype : Prototype
    {
        public ulong HostileTo { get; set; }
        public ulong FriendlyTo { get; set; }
        public ulong WhileConfused { get; set; }
        public ulong WhileControlled { get; set; }
    }

    public class BotDefinitionEntryPrototype : Prototype
    {
        public ulong Avatar { get; set; }
        public BehaviorProfilePrototype BehaviorProfile { get; set; }
    }

    public class BotSettingsPrototype : Prototype
    {
        public BotDefinitionEntryPrototype[] BotDefinitions { get; set; }
        public BehaviorProfilePrototype DefaultProceduralBotProfile { get; set; }
    }

    public class AIEntityAttributePrototype : Prototype
    {
        public ComparisonOperatorType OperatorType { get; set; }
    }

    public class AIEntityAttributeHasKeywordPrototype : AIEntityAttributePrototype
    {
        public ulong Keyword { get; set; }
    }

    public class AIEntityAttributeHasConditionKeywordPrototype : AIEntityAttributePrototype
    {
        public ulong ConditionKeyword { get; set; }
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
        public ulong ProtoRef { get; set; }
    }

    public class AIEntityAttributeIsPrototypePrototype : AIEntityAttributePrototype
    {
        public ulong RefToPrototype { get; set; }
    }

    public class AIEntityAttributeIsSimulatedPrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeIsCurrentTargetEntityPrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeIsCurrentTargetEntityOfAgentOfTypePrototype : AIEntityAttributePrototype
    {
        public ulong OtherAgentProtoRef { get; set; }
    }

    public class AIEntityAttributeIsSummonedByPowerPrototype : AIEntityAttributePrototype
    {
        public ulong Power { get; set; }
    }

    public class AIEntityAttributeCanBePlayerOwnedPrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeHasBlackboardPropertyValuePrototype : AIEntityAttributePrototype
    {
        public ulong PropertyInfoRef { get; set; }
        public int Value { get; set; }
    }

    public class AIEntityAttributeHasPropertyPrototype : AIEntityAttributePrototype
    {
        public ulong PropertyInfoRef { get; set; }
    }

    public class AIEntityAttributeHasHealthValuePercentPrototype : AIEntityAttributePrototype
    {
        public float Value { get; set; }
    }

    public class AIEntityAttributeIsDestructiblePrototype : AIEntityAttributePrototype
    {
    }

    public class AIEntityAttributeCanPathToPrototype : AIEntityAttributePrototype
    {
        public Method LocomotorMethod { get; set; }
    }

    public class MovementBehaviorPrototype : Prototype
    {
    }

    public class StrafeTargetPrototype : MovementBehaviorPrototype
    {
        public float StrafeDistanceMult { get; set; }
    }

    public class RandomPositionAroundTargetPrototype : MovementBehaviorPrototype
    {
        public float StrafeAngle { get; set; }
    }

    public class FixedRotationPrototype : MovementBehaviorPrototype
    {
        public float RotationSpeed { get; set; }
        public float PivotAngle { get; set; }
        public int MaxPivotTimeMS { get; set; }
        public float PostPivotAcceleration { get; set; }
    }

    public class StackingBehaviorPrototype : Prototype
    {
        public StackingApplicationStyleType ApplicationStyle { get; set; }
        public int MaxNumStacks { get; set; }
        public bool RemoveStackOnMaxNumStacksReached { get; set; }
        public bool StacksFromDifferentCreators { get; set; }
        public int NumStacksToApply { get; set; }
        public ulong[] StacksByKeyword { get; set; }
        public ulong StacksWithOtherPower { get; set; }
    }

    public class DelayContextPrototype : Prototype
    {
        public int MaxDelayMS { get; set; }
        public int MinDelayMS { get; set; }
    }

    public class InteractContextPrototype : Prototype
    {
    }

    public class TeleportContextPrototype : Prototype
    {
        public TeleportType TeleportType { get; set; }
    }

    public class SelectEntityContextPrototype : Prototype
    {
        public AIEntityAttributePrototype[] AttributeList { get; set; }
        public float MaxDistanceThreshold { get; set; }
        public float MinDistanceThreshold { get; set; }
        public SelectEntityPoolType PoolType { get; set; }
        public SelectEntityMethodType SelectionMethod { get; set; }
        public ulong EntitiesPropertyForComparison { get; set; }
        public SelectEntityTypeType SelectEntityType { get; set; }
        public bool LockEntityOnceSelected { get; set; }
        public float CellOrRegionAABBScale { get; set; }
        public ulong AlliancePriority { get; set; }
    }

    public class FlankContextPrototype : Prototype
    {
        public float RangeMax { get; set; }
        public float RangeMin { get; set; }
        public bool StopAtFlankingWaypoint { get; set; }
        public float ToTargetFlankingAngle { get; set; }
        public float WaypointRadius { get; set; }
        public int TimeoutMS { get; set; }
        public bool FailOnTimeout { get; set; }
        public bool RandomizeFlankingAngle { get; set; }
        public FlankToType FlankTo { get; set; }
    }

    public class FleeContextPrototype : Prototype
    {
        public float FleeTime { get; set; }
        public float FleeTimeVariance { get; set; }
        public float FleeHalfAngle { get; set; }
        public float FleeDistanceMin { get; set; }
        public bool FleeTowardAllies { get; set; }
        public float FleeTowardAlliesPercentChance { get; set; }
    }

    public class FlockContextPrototype : Prototype
    {
        public float RangeMax { get; set; }
        public float RangeMin { get; set; }
        public float SeparationWeight { get; set; }
        public float AlignmentWeight { get; set; }
        public float CohesionWeight { get; set; }
        public float SeparationThreshold { get; set; }
        public float AlignmentThreshold { get; set; }
        public float CohesionThreshold { get; set; }
        public float MaxSteeringForce { get; set; }
        public float ForceToLeaderWeight { get; set; }
        public bool SwitchLeaderOnCompletion { get; set; }
        public bool ChooseRandomPointAsDestination { get; set; }
        public WanderBasePointType WanderFromPointType { get; set; }
        public float WanderRadius { get; set; }
    }

    public class UseAffixPowerContextPrototype : Prototype
    {
    }

    public class UsePowerContextPrototype : Prototype
    {
        public ulong Power { get; set; }
        public float TargetOffset { get; set; }
        public bool RequireOriPriorToActivate { get; set; }
        public float OrientationThreshold { get; set; }
        public bool ForceIgnoreLOS { get; set; }
        public float OffsetVarianceMagnitude { get; set; }
        public bool ChooseRandomTargetPosition { get; set; }
        public float OwnerOffset { get; set; }
        public SelectEntityContextPrototype SecondaryTargetSelection { get; set; }
        public bool TargetsWorldPosition { get; set; }
        public bool ForceCheckTargetRegionLocation { get; set; }
        public float TargetAngleOffset { get; set; }
        public bool UseMainTargetForAOEActivation { get; set; }
        public float MinDistanceFromOwner { get; set; }
        public bool ForceInvalidTargetActivation { get; set; }
        public bool AllowMovementClipping { get; set; }
        public float MinDistanceToTarget { get; set; }
        public float MaxDistanceToTarget { get; set; }
        public bool IgnoreOutOfPositionFailure { get; set; }
        public ulong[] DifficultyTierRestrictions { get; set; }
    }

    public class MoveToContextPrototype : Prototype
    {
        public float LOSSweepPadding { get; set; }
        public float RangeMax { get; set; }
        public float RangeMin { get; set; }
        public bool EnforceLOS { get; set; }
        public MoveToType MoveTo { get; set; }
        public PathMethod PathNodeSetMethod { get; set; }
        public int PathNodeSetGroup { get; set; }
        public MovementSpeedOverride MovementSpeed { get; set; }
        public bool StopLocomotorOnMoveToFail { get; set; }
    }

    public class OrbitContextPrototype : Prototype
    {
        public float ThetaInDegrees { get; set; }
    }

    public class RotateContextPrototype : Prototype
    {
        public bool Clockwise { get; set; }
        public int Degrees { get; set; }
        public bool RotateTowardsTarget { get; set; }
        public float SpeedOverride { get; set; }
    }

    public class WanderContextPrototype : Prototype
    {
        public WanderBasePointType FromPoint { get; set; }
        public float RangeMax { get; set; }
        public float RangeMin { get; set; }
        public MovementSpeedOverride MovementSpeed { get; set; }
    }

    public class DespawnContextPrototype : Prototype
    {
        public bool DespawnOwner { get; set; }
        public bool DespawnTarget { get; set; }
        public bool UseKillInsteadOfDestroy { get; set; }
    }

    public class TriggerSpawnersContextPrototype : Prototype
    {
        public bool DoPulse { get; set; }
        public bool EnableSpawner { get; set; }
        public ulong Spawners { get; set; }
        public bool KillSummonedInventory { get; set; }
        public bool SearchWholeRegion { get; set; }
    }

    public class BehaviorProfilePrototype : Prototype
    {
        public float AggroDropChanceLOS { get; set; }
        public float AggroDropDistance { get; set; }
        public float AggroRangeAlly { get; set; }
        public float AggroRangeHostile { get; set; }
        public ulong Brain { get; set; }
        public ulong[] EquippedPassivePowers { get; set; }
        public bool IsBot { get; set; }
        public int InterruptCooldownMS { get; set; }
        public bool CanLeash { get; set; }
        public ulong Properties { get; set; }
        public bool AlwaysAggroed { get; set; }
    }

    public class KismetSequencePrototype : Prototype
    {
        public ulong KismetSeqName { get; set; }
        public bool KismetSeqBlocking { get; set; }
        public bool AudioListenerAtCamera { get; set; }
        public bool HideAvatarsDuringPlayback { get; set; }
    }
}
