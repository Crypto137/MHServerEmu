using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Locomotion;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.GameData.Prototypes
{
    #region Enums

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

        //---

        public virtual bool Check(Agent agent, Entity target)
        {
            Verify.IsTrue(false, "Found an AIEntityAttributePrototype that does not override Check()!");
            return false;
        }
    }

    public class AIEntityAttributeHasKeywordPrototype : AIEntityAttributePrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype Keyword { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        {
            WorldEntity targetWorldEntity = target as WorldEntity;
            if (!Verify.IsNotNull(targetWorldEntity)) return false;

            bool hasKeyword = targetWorldEntity.HasKeyword(Keyword);
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return hasKeyword;
                case ComparisonOperatorType.NotEqualTo: return hasKeyword == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeHasConditionKeywordPrototype : AIEntityAttributePrototype
    {
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public KeywordPrototype ConditionKeyword { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        {
            WorldEntity targetWorldEntity = target as WorldEntity;
            if (!Verify.IsNotNull(targetWorldEntity)) return false;

            bool hasConditionKeyword = targetWorldEntity.HasConditionWithKeyword(ConditionKeyword);
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return hasConditionKeyword;
                case ComparisonOperatorType.NotEqualTo: return hasConditionKeyword == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsHostilePrototype : AIEntityAttributePrototype
    {
        //---

        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity)
                return false;

            bool isHostile = agent.IsHostileTo(targetWorldEntity);
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return isHostile;
                case ComparisonOperatorType.NotEqualTo: return isHostile == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsMeleePrototype : AIEntityAttributePrototype
    {
        //

        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity)
                return false;

            bool isMelee = targetWorldEntity.IsMelee();
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return isMelee;
                case ComparisonOperatorType.NotEqualTo: return isMelee == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsAvatarPrototype : AIEntityAttributePrototype
    {
        //---

        public override bool Check(Agent agent, Entity target)
        {
            bool isAvatar = target is Avatar;
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return isAvatar;
                case ComparisonOperatorType.NotEqualTo: return isAvatar == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsAISummonedByAvatarPrototype : AIEntityAttributePrototype
    {
        //---

        public override bool Check(Agent agent, Entity target)
        {
            bool summonedByAvatar = false;

            if (target is Agent targetAgent && targetAgent.AIController != null)            
            {
                ulong ownerId = target.Properties[PropertyEnum.PowerUserOverrideID];
                Avatar avatar = target.Game.EntityManager.GetEntity<Avatar>(ownerId);
                summonedByAvatar = avatar != null;
            }

            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return summonedByAvatar;
                case ComparisonOperatorType.NotEqualTo: return summonedByAvatar == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsPrototypeRefPrototype : AIEntityAttributePrototype
    {
        public PrototypeId ProtoRef { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        {
            bool isProtoRef = target.PrototypeDataRef == ProtoRef;
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return isProtoRef;
                case ComparisonOperatorType.NotEqualTo: return isProtoRef == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsPrototypePrototype : AIEntityAttributePrototype
    {
        public PrototypeId RefToPrototype { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        {            
            bool isPrototype = GameDatabase.DataDirectory.PrototypeIsAPrototype(target.PrototypeDataRef, RefToPrototype);
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return isPrototype;
                case ComparisonOperatorType.NotEqualTo: return isPrototype == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsSimulatedPrototype : AIEntityAttributePrototype
    {
        //---

        public override bool Check(Agent agent, Entity target)
        {
            bool isSimulated = target.IsSimulated;
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return isSimulated;
                case ComparisonOperatorType.NotEqualTo: return isSimulated == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsCurrentTargetEntityPrototype : AIEntityAttributePrototype
    {
        //---

        public override bool Check(Agent agent, Entity target)
        {
            AIController currentAgentController = agent.AIController;
            if (!Verify.IsNotNull(currentAgentController)) return false;

            WorldEntity currentTarget = currentAgentController.TargetEntity;
            if (currentTarget == null || currentTarget.IsInWorld == false)
                return false;

            bool isCurrentTarget = currentTarget.Id == target.Id;
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return isCurrentTarget;
                case ComparisonOperatorType.NotEqualTo: return isCurrentTarget == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsCurrentTargetEntityOfAgentOfTypePrototype : AIEntityAttributePrototype
    {
        public PrototypeId OtherAgentProtoRef { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        {
            if (target is not Agent targetAgent)
                return false;

            Game game = targetAgent.Game;
            if (!Verify.IsNotNull(game)) return false;
            Region entityRegion = targetAgent.Region;
            if (!Verify.IsNotNull(entityRegion)) return false;
            Cell entityCell = targetAgent.Cell;
            if (!Verify.IsNotNull(entityCell)) return false;

            using var entitiesHandle = ListPool<WorldEntity>.Instance.Get(out List<WorldEntity> entities);
            entityRegion.GetEntitiesInVolume(entities, entityCell.RegionBounds, new(EntityRegionSPContextFlags.PrimaryPartition));

            Agent otherAgent = null;
            foreach (WorldEntity entity in entities)
            {
                if (entity.PrototypeDataRef == OtherAgentProtoRef)
                {
                    otherAgent = entity as Agent;
                    break;
                }
            }

            if (otherAgent == null)
                return false;

            AIController otherAgentController = otherAgent.AIController;
            if (!Verify.IsNotNull(otherAgent, $"This entity {otherAgent} does not have AI"))
                return false;

            WorldEntity currentTarget = otherAgentController.TargetEntity;
            if (currentTarget == null || currentTarget.IsInWorld == false)
                return false;

            bool isCurrentTarget = currentTarget.Id == target.Id;
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return isCurrentTarget;
                case ComparisonOperatorType.NotEqualTo: return isCurrentTarget == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsSummonedByPowerPrototype : AIEntityAttributePrototype
    {
        public PrototypeId Power { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        {
            if (!Verify.IsTrue(Power != PrototypeId.Invalid)) return false;

            bool summonedByPower = target.Properties[PropertyEnum.PowerUserOverrideID] == Power;
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return summonedByPower;
                case ComparisonOperatorType.NotEqualTo: return summonedByPower == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeCanBePlayerOwnedPrototype : AIEntityAttributePrototype
    {
        //---

        public override bool Check(Agent agent, Entity target)
        {
            bool canBePlayerOwned = target.CanBePlayerOwned();
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return canBePlayerOwned;
                case ComparisonOperatorType.NotEqualTo: return canBePlayerOwned == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeHasBlackboardPropertyValuePrototype : AIEntityAttributePrototype
    {
        public PrototypeId PropertyInfoRef { get; protected set; }
        public int Value { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        { 
            if (target is not Agent targetAgent)
                return false;

            AIController aiController = targetAgent.AIController;
            if (aiController != null)
            {
                var index = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(PropertyInfoRef);
                if (index == PropertyEnum.Invalid) return false;

                int indexValue = aiController.Blackboard.PropertyCollection.GetProperty(index);
                switch (OperatorType)
                {
                    case ComparisonOperatorType.EqualTo:            return indexValue == Value;
                    case ComparisonOperatorType.GreaterThan:        return indexValue > Value;
                    case ComparisonOperatorType.GreaterThanEqualTo: return indexValue >= Value;
                    case ComparisonOperatorType.LessThan:           return indexValue < Value;
                    case ComparisonOperatorType.LessThanEqualTo:    return indexValue <= Value;
                    case ComparisonOperatorType.NotEqualTo:         return indexValue != Value;

                    default:
                        Verify.IsTrue(false, $"Unsupported operator type in {this}");
                        return false;
                }
            }
            else if (OperatorType == ComparisonOperatorType.NotEqualTo)
            {
                return true;
            }

            return false;
        }
    }

    public class AIEntityAttributeHasPropertyPrototype : AIEntityAttributePrototype
    {
        public PrototypeId PropertyInfoRef { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        {
            PropertyEnum index = GameDatabase.PropertyInfoTable.GetPropertyEnumFromPrototype(PropertyInfoRef);
            if (!Verify.IsTrue(index != PropertyEnum.Invalid)) return false;

            bool hasProperty = target.Properties.HasProperty(index);
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return hasProperty;
                case ComparisonOperatorType.NotEqualTo: return hasProperty == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeHasHealthValuePercentPrototype : AIEntityAttributePrototype
    {
        public float Value { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        {
            long health = target.Properties[PropertyEnum.Health];
            long healthMax = target.Properties[PropertyEnum.HealthMax];

            float healthValuePct = healthMax != 0 ? MathHelper.Ratio(health, healthMax) : 0.0f;

            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:            return healthValuePct == Value;
                case ComparisonOperatorType.GreaterThan:        return healthValuePct > Value;
                case ComparisonOperatorType.GreaterThanEqualTo: return healthValuePct >= Value;
                case ComparisonOperatorType.LessThan:           return healthValuePct < Value;
                case ComparisonOperatorType.LessThanEqualTo:    return healthValuePct <= Value;
                case ComparisonOperatorType.NotEqualTo:         return healthValuePct != Value;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeIsDestructiblePrototype : AIEntityAttributePrototype
    {
        //---

        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity)
                return false;

            bool isDestructible = targetWorldEntity.IsDestructible;
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return isDestructible;
                case ComparisonOperatorType.NotEqualTo: return isDestructible == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
    }

    public class AIEntityAttributeCanPathToPrototype : AIEntityAttributePrototype
    {
        public LocomotorMethod LocomotorMethod { get; protected set; }

        //---

        public override bool Check(Agent agent, Entity target)
        {
            if (target is not WorldEntity targetWorldEntity)
                return false;

            Region region = agent.Region;
            if (!Verify.IsNotNull(region)) return false;

            NaviPathResult pathResult = agent.CheckCanPathTo(targetWorldEntity.RegionLocation.Position, Locomotor.GetPathFlags(LocomotorMethod));

            bool canPathTo = pathResult == NaviPathResult.Success || pathResult == NaviPathResult.IncompletedPath;
            switch (OperatorType)
            {
                case ComparisonOperatorType.EqualTo:    return canPathTo;
                case ComparisonOperatorType.NotEqualTo: return canPathTo == false;

                default:
                    Verify.IsTrue(false, $"Unsupported operator type in {this}");
                    return false;
            }
        }
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
#if GAME_VERSION_1_53
        public PrototypeId PowerToActivate { get; protected set; }
#endif
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
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        public float FleeTimeVariance { get; protected set; }
        public float FleeHalfAngle { get; protected set; }
        public float FleeDistanceMin { get; protected set; }
        public bool FleeTowardAllies { get; protected set; }
        public float FleeTowardAlliesPercentChance { get; protected set; }
#endif
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
        [PrototypeField(PrototypeFieldType.PrototypeRefPtr)]
        public PowerPrototype Power { get; protected set; }
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

        //---

        public bool HasDifficultyTierRestriction(PrototypeId difficultyRef)
        {
            return DifficultyTierRestrictions.HasValue() && DifficultyTierRestrictions.Contains(difficultyRef);
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
#if GAME_VERSION_1_53
        public float ChanceToMoveInFrontOfAssistedEnt { get; protected set; }
#endif
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
}
