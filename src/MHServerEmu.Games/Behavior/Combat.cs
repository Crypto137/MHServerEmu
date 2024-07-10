using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Behavior.StaticAI;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Behavior
{
    public class Combat
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static List<WorldEntity> GetTargetsInRange(Agent aggressor, float rangeMax, float rangeMin, CombatTargetType targetType,
            CombatTargetFlags flags, AIEntityAttributePrototype[] attributes)
        {
            List<WorldEntity> targets = new ();
            if (aggressor == null) return targets;
            if (aggressor.Region == null)
            {
                Logger.Warn($"Agent not in region when trying to count targets in range! Agent: {aggressor}");
                return targets;
            }

            Sphere volume = new (aggressor.RegionLocation.Position, rangeMax + aggressor.Bounds.GetRadius());
            foreach (WorldEntity target in aggressor.Region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.ActivePartition)))
            {
                if (target == null) continue;
                if (ValidTarget(aggressor.Game, aggressor, target, targetType, false, flags)
                    && (attributes == null || PassesAttributesCheck(aggressor, target, attributes))
                    && (rangeMin == 0f || (aggressor.GetDistanceTo(target, true) >= rangeMin)))
                    targets.Add(target);
            }

            return targets;
        }

        public static int GetNumTargetsInRange(Agent aggressor, float rangeMax, float rangeMin, CombatTargetType targetType,
            CombatTargetFlags flags, AIEntityAttributePrototype[] attributes = null)
        {
            int numTargets = 0;
            if (aggressor == null) return numTargets;
            if (aggressor.Region == null)
            {
                Logger.Warn($"Agent not in region when trying to count targets in range! Agent: {aggressor}");
                return numTargets;
            }
            
            Sphere volume = new (aggressor.RegionLocation.Position, rangeMax + aggressor.Bounds.GetRadius());
            foreach (WorldEntity target in aggressor.Region.IterateEntitiesInVolume(volume, new(EntityRegionSPContextFlags.ActivePartition)))
            {
                if (target == null) continue;
                if (ValidTarget(aggressor.Game, aggressor, target, targetType, false, flags)
                    && (attributes == null || PassesAttributesCheck(aggressor, target, attributes)) 
                    && (rangeMin == 0.0f || (aggressor.GetDistanceTo(target, true) >= rangeMin)))
                    numTargets++;
            }

            return numTargets;
        }

        private static bool PassesAttributesCheck(Agent aggressor, WorldEntity target, AIEntityAttributePrototype[] attributes)
        {
            foreach (var attrib in attributes)
                if (attrib != null && attrib.Check(aggressor, target) == false)
                    return false;
            return true;
        }

        public static bool ValidTarget(Game game, Agent aggressor, WorldEntity target, CombatTargetType targetType, bool inTarget, 
            CombatTargetFlags flags = CombatTargetFlags.None, AlliancePrototype allianceOverride = null, float aggroRangeOverride = 0.0f)
        {
            if (game == null || aggressor == null || target == null || aggressor == target) return false;
            if (target.IsInWorld == false) return false;

            if (flags.HasFlag(CombatTargetFlags.CheckAgent) && target is not Agent) return false;
            if (flags.HasFlag(CombatTargetFlags.CheckItem) && target is not Item) return false;

            AlliancePrototype allianceProto = allianceOverride ?? aggressor.Alliance;
            if (flags.HasFlag(CombatTargetFlags.IgnoreTargetable) == false)
            {
                if (target.IsTargetable(aggressor) == false && target.HasAITargetableOverride == false) return false;
                Player player = target.GetOwnerOfType<Player>();
                if (player != null && player.IsTargetable(allianceProto) == false) return false;
            }

            if (flags.HasFlag(CombatTargetFlags.IgnoreDead) == false && target.IsDead) return false;
            if (flags.HasFlag(CombatTargetFlags.CheckInvulnerable) && target.Properties[PropertyEnum.Invulnerable]) return false;

            if (flags.HasFlag(CombatTargetFlags.IgnoreStealth) == false)
            {
                float stealth = target.Properties[PropertyEnum.Stealth];
                if (stealth > 0.0f)
                {
                    stealth -= target.Properties[PropertyEnum.StealthPenalty];
                    if (stealth > 0.0f)
                    {
                        stealth -= aggressor.Properties[PropertyEnum.StealthDetection];
                        if (stealth > 0.0f)
                            return false;
                    }
                }
            }

            AIController aggressorsController = aggressor.AIController;
            if (aggressorsController == null) return false;

            float aggroRange = 0.0f;
            if (flags.HasFlag(CombatTargetFlags.IgnoreHostile) == false)
            {
                switch (targetType)
                {
                    case CombatTargetType.Hostile:
                        if (aggressor.IsHostileTo(target, allianceProto) == false) return false;
                        aggroRange = (aggroRangeOverride > 0.0f) ? aggroRangeOverride : aggressorsController.AggroRangeHostile;
                        break;
                    case CombatTargetType.Ally:
                        if (aggressor.IsFriendlyTo(target, allianceProto) == false) return false;
                        aggroRange = (aggroRangeOverride > 0.0f) ? aggroRangeOverride : aggressorsController.AggroRangeHostile;
                        break;
                    default:
                        Logger.Warn("Invalid combat target type.");
                        break;
                }
            }

            if (inTarget)         
            {            
                BehaviorBlackboard aggressorBlackboard = aggressorsController.Blackboard;
                if (flags.HasFlag(CombatTargetFlags.IgnoreAggroDropRange) == false)
                    if (CheckAggroDistance(aggressor, target, aggressorBlackboard.PropertyCollection[PropertyEnum.AIAggroDropRange]) == false)
                        return false;

                if (flags.HasFlag(CombatTargetFlags.IgnoreAggroLOSChance) == false && flags.HasFlag(CombatTargetFlags.IgnoreLOS) == false)
                {
                    float chance = aggressorBlackboard.PropertyCollection[PropertyEnum.AIAggroDropByLOSChance];
                    if (chance > 0.0f && aggressor.LineOfSightTo(target) == false)
                        if (game.Random.NextFloat() < chance) return false;
                }
            }
            else
            {
                if (flags.HasFlag(CombatTargetFlags.IgnoreAggroDistance) == false && flags.HasFlag(CombatTargetFlags.IgnoreHostile) == false)
                    if (CheckAggroDistance(aggressor, target, aggroRange) == false) return false;

                if (flags.HasFlag(CombatTargetFlags.IgnoreLOS) == false)
                    if (aggressor.LineOfSightTo(target) == false) return false;
            }

            return true;
        }

        private static bool CheckAggroDistance(Agent aggressor, WorldEntity target, float distance)
        {
            if (distance > 0.0)
            {
                float distanceSq = distance * distance;
                if (distanceSq < Vector3.DistanceSquared2D(aggressor.RegionLocation.Position, target.RegionLocation.Position)) 
                    return false;
            }
            return true;
        }

        public static ulong GetClosestValidHostileTarget(Agent aggressor, float aggroRange)
        {
            if (aggressor == null || aggroRange <= 0.0f) return 0;
            var region = aggressor.Region;
            if (region == null) return 0;

            var aggressorPosition = aggressor.RegionLocation.Position;
            float closestDistanceSq = float.MaxValue;
            ulong closestTargetId = 0;
            Sphere volume = new (aggressorPosition, aggroRange);
            foreach (var target in region.IterateEntitiesInVolume(volume, new (EntityRegionSPContextFlags.ActivePartition)))
            {
                if (target == null) continue;
                if (ValidTarget(aggressor.Game, aggressor, target, CombatTargetType.Hostile, false))
                {               
                    var distanceSq = Vector3.DistanceSquared(aggressorPosition, target.RegionLocation.Position);
                    if (distanceSq < closestDistanceSq)
                    {
                        closestDistanceSq = distanceSq;
                        closestTargetId = target.Id;
                    }
                }
            }
            return closestTargetId;
        }

        public static bool GetValidTargetsInSphere(Agent aggressor, float aggroRange, List<ulong> targets, CombatTargetType targetType, 
            in SelectEntity.SelectEntityContext selectionContext, ref WorldEntity bestTargetSoFar, ref float bestValue, CombatTargetFlags flags)
        {
            if (aggressor == null || aggroRange <= 0.0f) return false;
            Region region = aggressor.Region;
            if (region == null) return false;
            Game game = aggressor.Game;
            if (game == null) return false;

            Sphere volume = new (aggressor.RegionLocation.Position, aggroRange);
            foreach (WorldEntity worldEntity in region.IterateEntitiesInVolume(volume, new (EntityRegionSPContextFlags.ActivePartition)))
            {
                if (worldEntity == null) continue;
                if (ValidTarget(game, aggressor, worldEntity, targetType, false, flags, null, aggroRange))
                {
                    targets.Add(worldEntity.Id);  
                    if (SelectEntity.EntityMatchesSelectionCriteria(selectionContext, worldEntity, ref bestTargetSoFar, ref bestValue))
                        break;
                }
            }

            return targets.Count > 0;
        }
    }

    [Flags]
    public enum CombatTargetFlags
    {
        None = 0,
        Flag0 = 1 << 0,
        CheckInvulnerable    = 1 << 1,
        CheckAgent           = 1 << 2,
        CheckItem            = 1 << 3,
        IgnoreAggroDistance  = 1 << 4,
        IgnoreAggroDropRange = 1 << 5,
        IgnoreAggroLOSChance = 1 << 6,
        IgnoreStealth        = 1 << 7,
        IgnoreDead           = 1 << 8,
        IgnoreHostile        = 1 << 9,
        IgnoreLOS            = 1 << 10,
        IgnoreTargetable     = 1 << 11,
    }

    public enum CombatTargetType
    {
        Hostile,
        Ally
    }
}
