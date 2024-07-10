using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Games.Behavior.ProceduralAI;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Tables;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

    [AssetEnum((int)Force)]
    public enum ResourceType
    {
        Force = 0,
        Focus = 1,
        Fury = 2,
        Secondary_Pips = 3,
        Secondary_Gauge = 4,
    }

    [AssetEnum((int)None)]
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

    [AssetEnum((int)DontRefresh)]
    public enum StackingApplicationStyleType
    {
        DontRefresh = 0,
        Refresh = 1,
        Recreate = 2,
        MatchDuration = 3,
        SingleStackAddDuration = 4,
        MultiStackAddDuration = 5,
    }

    [AssetEnum((int)None)]
    public enum TeleportType
    {
        None = 0,
        AssistedEntity = 1,
        SpawnPosition = 2,
    }

    [AssetEnum((int)None)]
    public enum SelectEntityType
    {
        None = 0,
        SelectAssistedEntity = 1,
        SelectInteractedEntity = 2,
        SelectTarget = 3,
        SelectTargetByAssistedEntitiesLastTarget = 4,
    }

    [AssetEnum((int)None)]
    public enum SelectEntityPoolType
    {
        None = 0,
        AllEntitiesInCellOfAgent = 1,
        AllEntitiesInRegionOfAgent = 2,
        PotentialAlliesOfAgent = 3,
        PotentialEnemiesOfAgent = 4,
        // Not found in client
        ItemsAroundAgent = 0, 
    }

    [AssetEnum((int)None)]
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

    [AssetEnum((int)Target)]
    public enum FlankToType
    {
        AssistedEntity = 1,
        InteractEntity = 2,
        Target = 3,
    }

    [AssetEnum((int)None)]
    public enum WanderBasePointType
    {
        CurrentPosition = 0,
        SpawnPoint = 1,
        TargetPosition = 2,
        None = 3,
    }

    [AssetEnum((int)Target)]
    public enum MoveToType
    {
        AssistedEntity = 0,
        DespawnPosition = 1,
        InteractEntity = 2,
        PathNode = 3,
        SpawnPosition = 4,
        Target = 5,
    }

    [AssetEnum((int)Default)]
    public enum MovementSpeedOverride
    {
        Default,
        Walk,
        Run,
    }

    [AssetEnum((int)Set)]
    public enum BlackboardOperatorType
    {
        Add = 0,
        Div = 1,
        Mul = 2,
        Set = 3,
        Sub = 4,
        SetTargetId = 5,
        ClearTargetId = 6,
    }

    #endregion

    public class BrainPrototype : Prototype
    {
    }

    public class ManaBehaviorPrototype : Prototype
    {
        public LocaleStringId DisplayName { get; protected set; }
        public ResourceType MeterType { get; protected set; }
        public PrototypeId[] Powers { get; protected set; }
        public bool StartsEmpty { get; protected set; }
        public LocaleStringId Description { get; protected set; }
        public AssetId MeterColor { get; protected set; }
        public AssetId ResourceBarStyle { get; protected set; }
        public AssetId ResourcePipStyle { get; protected set; }
        public bool DepleteOnDeath { get; protected set; }
    }

    public class PrimaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public bool StartsWithRegenEnabled { get; protected set; }
        public int RegenUpdateTimeMS { get; protected set; }
        public EvalPrototype EvalOnEnduranceUpdate { get; protected set; }
        public ManaType ManaType { get; protected set; }
        public CurveId BaseEndurancePerLevel { get; protected set; }
        public bool RestoreToMaxOnLevelUp { get; protected set; }
    }

    public class SecondaryResourceManaBehaviorPrototype : ManaBehaviorPrototype
    {
        public EvalPrototype EvalGetCurrentForDisplay { get; protected set; }
        public EvalPrototype EvalGetCurrentPipsForDisplay { get; protected set; }
        public EvalPrototype EvalGetMaxForDisplay { get; protected set; }
        public EvalPrototype EvalGetMaxPipsForDisplay { get; protected set; }
        public bool DepleteOnExitWorld { get; protected set; }
        public bool ResetOnAvatarSwap { get; protected set; }
    }

    public class AlliancePrototype : Prototype
    {
        public PrototypeId[] HostileTo { get; protected set; }
        public PrototypeId[] FriendlyTo { get; protected set; }
        public PrototypeId WhileConfused { get; protected set; }
        public PrototypeId WhileControlled { get; protected set; }

        [DoNotCopy]
        public int EnumValue { get; private set; }

        public override void PostProcess()
        {
            base.PostProcess();

            BlueprintId blueprintRef = GameDatabase.DataDirectory.GetPrototypeBlueprintDataRef(DataRef);
            EnumValue = GameDatabase.DataDirectory.GetPrototypeEnumValue(DataRef, blueprintRef);
        }

        public static bool IsHostileToPlayerAlliance(AlliancePrototype allianceProto)
        {
            if (allianceProto == null
                || GameDatabase.GlobalsPrototype == null 
                || GameDatabase.GlobalsPrototype.PlayerAlliance == PrototypeId.Invalid)
                return false;

            AlliancePrototype playerAlliance = GameDatabase.GlobalsPrototype.PlayerAlliancePrototype;
            return playerAlliance.IsHostileTo(allianceProto);
        }

        public bool IsFriendlyTo(AlliancePrototype allianceProto)
        {
            if (allianceProto == null) return false;
            return GameDataTables.Instance.AllianceTable.IsFriendlyTo(this, allianceProto);
        }

        public bool IsHostileTo(AlliancePrototype allianceProto)
        {
            if (allianceProto == null) return false;
            return GameDataTables.Instance.AllianceTable.IsHostileTo(this, allianceProto);
        }
    }

    public class BotDefinitionEntryPrototype : Prototype
    {
        public PrototypeId Avatar { get; protected set; }
        public BehaviorProfilePrototype BehaviorProfile { get; protected set; }
    }

    public class BotSettingsPrototype : Prototype
    {
        public BotDefinitionEntryPrototype[] BotDefinitions { get; protected set; }
        public BehaviorProfilePrototype DefaultProceduralBotProfile { get; protected set; }
    }

    public class AIEntityAttributePrototype : Prototype
    {
        public ComparisonOperatorType OperatorType { get; protected set; }

        public virtual bool Check(Agent agent, Entity target)
        {
            return ProceduralAI.Logger.WarnReturn(false, "Found an AIEntityAttributePrototype that does not override Check()!");
        }
    }

    public class AIEntityAttributeHasKeywordPrototype : AIEntityAttributePrototype
    {
        public PrototypeId Keyword { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity) return false;

            bool hasKeyword = targetWorldEntity.HasKeyword(Keyword);
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => hasKeyword,
                ComparisonOperatorType.NotEqualTo => !hasKeyword,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeHasConditionKeywordPrototype : AIEntityAttributePrototype
    {
        public PrototypeId ConditionKeyword { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity) return false;

            bool hasConditionKeyword = targetWorldEntity.HasConditionWithKeyword(ConditionKeyword);
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => hasConditionKeyword,
                ComparisonOperatorType.NotEqualTo => !hasConditionKeyword,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsHostilePrototype : AIEntityAttributePrototype
    {
        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity) return false;

            bool isHostile = agent.IsHostileTo(targetWorldEntity);
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => isHostile,
                ComparisonOperatorType.NotEqualTo => !isHostile,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsMeleePrototype : AIEntityAttributePrototype
    {
        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity) return false;

            bool isMelle = targetWorldEntity.IsMelee();
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => isMelle,
                ComparisonOperatorType.NotEqualTo => !isMelle,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsAvatarPrototype : AIEntityAttributePrototype
    {
        public override bool Check(Agent agent, Entity target)
        {
            bool isAvatar = target is Avatar;
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => isAvatar,
                ComparisonOperatorType.NotEqualTo => !isAvatar,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsAISummonedByAvatarPrototype : AIEntityAttributePrototype
    {
        public override bool Check(Agent agent, Entity target)
        {
            bool summonedByAvatar = false;

            if (target is Agent targetAgent && targetAgent.AIController != null)            
            {
                ulong ownerId = target.Properties[PropertyEnum.PowerUserOverrideID];
                Avatar avatar = target.Game.EntityManager.GetEntity<Avatar>(ownerId);
                summonedByAvatar = avatar != null;
            }

            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => summonedByAvatar,
                ComparisonOperatorType.NotEqualTo => !summonedByAvatar,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsPrototypeRefPrototype : AIEntityAttributePrototype
    {
        public PrototypeId ProtoRef { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        {
            bool isProtoRef = target.PrototypeDataRef == ProtoRef;
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => isProtoRef,
                ComparisonOperatorType.NotEqualTo => !isProtoRef,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsPrototypePrototype : AIEntityAttributePrototype
    {
        public PrototypeId RefToPrototype { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        {            
            bool isPrototype = GameDatabase.DataDirectory.PrototypeIsAPrototype(target.PrototypeDataRef, RefToPrototype);
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => isPrototype,
                ComparisonOperatorType.NotEqualTo => !isPrototype,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsSimulatedPrototype : AIEntityAttributePrototype
    {
        public override bool Check(Agent agent, Entity target)
        {
            bool isSimulated = target.IsSimulated;
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => isSimulated,
                ComparisonOperatorType.NotEqualTo => !isSimulated,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsCurrentTargetEntityPrototype : AIEntityAttributePrototype
    {
        public override bool Check(Agent agent, Entity target)
        {
            var currentTarget = agent.AIController?.TargetEntity;
            if (currentTarget == null || currentTarget.IsInWorld == false) return false;

            bool isCurrentTarget = currentTarget.Id == target.Id;
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => isCurrentTarget,
                ComparisonOperatorType.NotEqualTo => !isCurrentTarget,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsCurrentTargetEntityOfAgentOfTypePrototype : AIEntityAttributePrototype
    {
        public PrototypeId OtherAgentProtoRef { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        {
            if (target is not Agent targetAgent) return false;
            Game game = targetAgent.Game;
            if (game == null) return false;
            Region region = targetAgent.Region;
            if (region == null) return false;
            Cell cell = targetAgent.Cell;
            if (cell == null) return false;
            List<WorldEntity> entities = new(); 
            region.GetEntitiesInVolume(entities, cell.RegionBounds, new(EntityRegionSPContextFlags.ActivePartition));

            Agent otherAgent = null;
            foreach (var entity in entities)
                if (entity.PrototypeDataRef == OtherAgentProtoRef)
                {
                    otherAgent = entity as Agent;
                    break;
                }

            var otherAgentController = otherAgent?.AIController;
            if (otherAgentController == null) ProceduralAI.Logger.WarnReturn(false, $"This entity {otherAgent} does not have AI");
            var currentTarget = otherAgentController.TargetEntity;
            if (currentTarget == null || currentTarget.IsInWorld == false) return false;

            bool isCurrentTarget = currentTarget.Id == target.Id;
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => isCurrentTarget,
                ComparisonOperatorType.NotEqualTo => !isCurrentTarget,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsSummonedByPowerPrototype : AIEntityAttributePrototype
    {
        public PrototypeId Power { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        {
            if (Power == PrototypeId.Invalid) return false;

            bool summonedByPower = target.Properties[PropertyEnum.PowerUserOverrideID] == Power;
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => summonedByPower,
                ComparisonOperatorType.NotEqualTo => !summonedByPower,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeCanBePlayerOwnedPrototype : AIEntityAttributePrototype
    {
        public override bool Check(Agent agent, Entity target)
        {
            bool canBePlayerOwned = target.CanBePlayerOwned();
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => canBePlayerOwned,
                ComparisonOperatorType.NotEqualTo => !canBePlayerOwned,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeHasBlackboardPropertyValuePrototype : AIEntityAttributePrototype
    {
        public PrototypeId PropertyInfoRef { get; protected set; }
        public int Value { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        { 
            if (target is not Agent targetAgent) return false;
            var controller = targetAgent.AIController;
            if (controller != null)
            {
                var index = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(PropertyInfoRef);
                if (index == PropertyEnum.Invalid) return false;

                int indexValue = controller.Blackboard.PropertyCollection.GetProperty(index);
                return OperatorType switch
                {
                    ComparisonOperatorType.EqualTo => indexValue == Value,
                    ComparisonOperatorType.NotEqualTo => indexValue != Value,
                    ComparisonOperatorType.LessThanEqualTo => indexValue <= Value,
                    ComparisonOperatorType.GreaterThanEqualTo => indexValue >= Value,
                    ComparisonOperatorType.LessThan => indexValue < Value,
                    ComparisonOperatorType.GreaterThan => indexValue > Value,
                    _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
                };
            }
            else if (OperatorType == ComparisonOperatorType.NotEqualTo)
                return true;

            return false;
        }
    }

    public class AIEntityAttributeHasPropertyPrototype : AIEntityAttributePrototype
    {
        public PrototypeId PropertyInfoRef { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        {
            var index = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(PropertyInfoRef);
            if (index == PropertyEnum.Invalid) return false;

            bool hasProperty = target.Properties.HasProperty(index);
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => hasProperty,
                ComparisonOperatorType.NotEqualTo => !hasProperty,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeHasHealthValuePercentPrototype : AIEntityAttributePrototype
    {
        public float Value { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        {
            long health = target.Properties[PropertyEnum.Health];
            long healthMax = target.Properties[PropertyEnum.HealthMax];

            float healthValuePct = 0.0f;
            if (healthMax != 0) healthValuePct = MathHelper.Ratio(health, healthMax);
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => healthValuePct == Value,
                ComparisonOperatorType.NotEqualTo => healthValuePct != Value,
                ComparisonOperatorType.LessThanEqualTo => healthValuePct <= Value,
                ComparisonOperatorType.GreaterThanEqualTo => healthValuePct >= Value,
                ComparisonOperatorType.LessThan => healthValuePct < Value,
                ComparisonOperatorType.GreaterThan => healthValuePct > Value,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeIsDestructiblePrototype : AIEntityAttributePrototype
    {
        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity) return false;

            bool isDestructible = targetWorldEntity.IsDestructible;
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => isDestructible,
                ComparisonOperatorType.NotEqualTo => !isDestructible,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class AIEntityAttributeCanPathToPrototype : AIEntityAttributePrototype
    {
        public LocomotorMethod LocomotorMethod { get; protected set; }

        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity) return false;
            Region region = agent.Region;
            if (region == null) return false;
            var pathResult = agent.CheckCanPathTo(targetWorldEntity.RegionLocation.Position, Locomotor.GetPathFlags(LocomotorMethod));

            bool canPathTo = pathResult == NaviPathResult.Success || pathResult == NaviPathResult.IncompletedPath;
            return OperatorType switch
            {
                ComparisonOperatorType.EqualTo => canPathTo,
                ComparisonOperatorType.NotEqualTo => !canPathTo,
                _ => ProceduralAI.Logger.WarnReturn(false, $"Unsupported operator type in {ToString()}"),
            };
        }
    }

    public class StackingBehaviorPrototype : Prototype
    {
        public StackingApplicationStyleType ApplicationStyle { get; protected set; }
        public int MaxNumStacks { get; protected set; }
        public bool RemoveStackOnMaxNumStacksReached { get; protected set; }
        public bool StacksFromDifferentCreators { get; protected set; }
        public int NumStacksToApply { get; protected set; }
        public PrototypeId[] StacksByKeyword { get; protected set; }
        public PrototypeId StacksWithOtherPower { get; protected set; }
    }

    public class DelayContextPrototype : Prototype
    {
        public int MaxDelayMS { get; protected set; }
        public int MinDelayMS { get; protected set; }
    }

    public class InteractContextPrototype : Prototype
    {
    }

    public class TeleportContextPrototype : Prototype
    {
        public TeleportType TeleportType { get; protected set; }
    }

    public class SelectEntityContextPrototype : Prototype
    {
        public AIEntityAttributePrototype[] AttributeList { get; protected set; }
        public float MaxDistanceThreshold { get; protected set; }
        public float MinDistanceThreshold { get; protected set; }
        public SelectEntityPoolType PoolType { get; protected set; }
        public SelectEntityMethodType SelectionMethod { get; protected set; }
        public PrototypeId EntitiesPropertyForComparison { get; protected set; }
        public SelectEntityType SelectEntityType { get; protected set; }
        public bool LockEntityOnceSelected { get; protected set; }
        public float CellOrRegionAABBScale { get; protected set; }
        public PrototypeId AlliancePriority { get; protected set; }
    }

    public class FlankContextPrototype : Prototype
    {
        public float RangeMax { get; protected set; }
        public float RangeMin { get; protected set; }
        public bool StopAtFlankingWaypoint { get; protected set; }
        public float ToTargetFlankingAngle { get; protected set; }
        public float WaypointRadius { get; protected set; }
        public int TimeoutMS { get; protected set; }
        public bool FailOnTimeout { get; protected set; }
        public bool RandomizeFlankingAngle { get; protected set; }
        public FlankToType FlankTo { get; protected set; }
    }

    public class FleeContextPrototype : Prototype
    {
        public float FleeTime { get; protected set; }
        public float FleeTimeVariance { get; protected set; }
        public float FleeHalfAngle { get; protected set; }
        public float FleeDistanceMin { get; protected set; }
        public bool FleeTowardAllies { get; protected set; }
        public float FleeTowardAlliesPercentChance { get; protected set; }
    }

    public class FlockContextPrototype : Prototype
    {
        public float RangeMax { get; protected set; }
        public float RangeMin { get; protected set; }
        public float SeparationWeight { get; protected set; }
        public float AlignmentWeight { get; protected set; }
        public float CohesionWeight { get; protected set; }
        public float SeparationThreshold { get; protected set; }
        public float AlignmentThreshold { get; protected set; }
        public float CohesionThreshold { get; protected set; }
        public float MaxSteeringForce { get; protected set; }
        public float ForceToLeaderWeight { get; protected set; }
        public bool SwitchLeaderOnCompletion { get; protected set; }
        public bool ChooseRandomPointAsDestination { get; protected set; }
        public WanderBasePointType WanderFromPointType { get; protected set; }
        public float WanderRadius { get; protected set; }
    }

    public class UseAffixPowerContextPrototype : Prototype
    {
    }

    public class UsePowerContextPrototype : Prototype
    {
        public PrototypeId Power { get; protected set; }
        public float TargetOffset { get; protected set; }
        public bool RequireOriPriorToActivate { get; protected set; }
        public float OrientationThreshold { get; protected set; }
        public bool ForceIgnoreLOS { get; protected set; }
        public float OffsetVarianceMagnitude { get; protected set; }
        public bool ChooseRandomTargetPosition { get; protected set; }
        public float OwnerOffset { get; protected set; }
        public SelectEntityContextPrototype SecondaryTargetSelection { get; protected set; }
        public bool TargetsWorldPosition { get; protected set; }
        public bool ForceCheckTargetRegionLocation { get; protected set; }
        public float TargetAngleOffset { get; protected set; }
        public bool UseMainTargetForAOEActivation { get; protected set; }
        public float MinDistanceFromOwner { get; protected set; }
        public bool ForceInvalidTargetActivation { get; protected set; }
        public bool AllowMovementClipping { get; protected set; }
        public float MinDistanceToTarget { get; protected set; }
        public float MaxDistanceToTarget { get; protected set; }
        public bool IgnoreOutOfPositionFailure { get; protected set; }
        public PrototypeId[] DifficultyTierRestrictions { get; protected set; }

        public bool HasDifficultyTierRestriction(PrototypeId difficultyRef)
        {
            if (DifficultyTierRestrictions.HasValue() && DifficultyTierRestrictions.Contains(difficultyRef))
                return true;
            return false;
        }
    }

    public class MoveToContextPrototype : Prototype
    {
        public float LOSSweepPadding { get; protected set; }
        public float RangeMax { get; protected set; }
        public float RangeMin { get; protected set; }
        public bool EnforceLOS { get; protected set; }
        public MoveToType MoveTo { get; protected set; }
        public PathMethod PathNodeSetMethod { get; protected set; }
        public int PathNodeSetGroup { get; protected set; }
        public MovementSpeedOverride MovementSpeed { get; protected set; }
        public bool StopLocomotorOnMoveToFail { get; protected set; }
    }

    public class OrbitContextPrototype : Prototype
    {
        public float ThetaInDegrees { get; protected set; }
    }

    public class RotateContextPrototype : Prototype
    {
        public bool Clockwise { get; protected set; }
        public int Degrees { get; protected set; }
        public bool RotateTowardsTarget { get; protected set; }
        public float SpeedOverride { get; protected set; }
    }

    public class WanderContextPrototype : Prototype
    {
        public WanderBasePointType FromPoint { get; protected set; }
        public float RangeMax { get; protected set; }
        public float RangeMin { get; protected set; }
        public MovementSpeedOverride MovementSpeed { get; protected set; }
    }

    public class DespawnContextPrototype : Prototype
    {
        public bool DespawnOwner { get; protected set; }
        public bool DespawnTarget { get; protected set; }
        public bool UseKillInsteadOfDestroy { get; protected set; }
    }

    public class TriggerSpawnersContextPrototype : Prototype
    {
        public bool DoPulse { get; protected set; }
        public bool EnableSpawner { get; protected set; }
        public PrototypeId[] Spawners { get; protected set; }
        public bool KillSummonedInventory { get; protected set; }
        public bool SearchWholeRegion { get; protected set; }
    }

    public class BehaviorProfilePrototype : Prototype
    {
        public float AggroDropChanceLOS { get; protected set; }
        public float AggroDropDistance { get; protected set; }
        public float AggroRangeAlly { get; protected set; }
        public float AggroRangeHostile { get; protected set; }
        public PrototypeId Brain { get; protected set; }
        public PrototypeId[] EquippedPassivePowers { get; protected set; }
        public bool IsBot { get; protected set; }
        public int InterruptCooldownMS { get; protected set; }
        public bool CanLeash { get; protected set; }
        public PrototypePropertyCollection Properties { get; protected set; }
        public bool AlwaysAggroed { get; protected set; }
    }

    public class KismetSequencePrototype : Prototype
    {
        public AssetId KismetSeqName { get; protected set; }
        public bool KismetSeqBlocking { get; protected set; }
        public bool AudioListenerAtCamera { get; protected set; }
        public bool HideAvatarsDuringPlayback { get; protected set; }
    }
}
