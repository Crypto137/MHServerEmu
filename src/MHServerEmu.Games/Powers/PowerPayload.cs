using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.Powers
{
    public class PowerPayload : PowerEffectsPacket
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Game Game { get; private set; }

        public Vector3 TargetPosition { get; private set; }
        public TimeSpan MovementTime { get; private set; }
        public uint PowerRandomSeed { get; private set; }
        public uint FXRandomSeed { get; private set; }

        public float Range { get; private set; }
        public ulong RegionId { get; private set; }
        public AlliancePrototype OwnerAlliance { get; private set; }
        public int BeamSweepSlice { get; private set; }
        public TimeSpan ExecutionTime { get; private set; }

        public bool Initialize(Power power, PowerApplication powerApplication)
        {
            Game = power.Game;
            PowerPrototype = power.Prototype;

            PowerOwnerId = powerApplication.UserEntityId;
            TargetId = powerApplication.TargetEntityId;
            PowerOwnerPosition = powerApplication.UserPosition;
            TargetPosition = powerApplication.TargetPosition;
            MovementTime = powerApplication.MovementTime;
            PowerRandomSeed = powerApplication.PowerRandomSeed;
            FXRandomSeed = powerApplication.FXRandomSeed;

            // All payloads have to have valid owners on initialization
            WorldEntity powerOwner = Game.EntityManager.GetEntity<WorldEntity>(PowerOwnerId);
            if (powerOwner == null) return Logger.WarnReturn(false, "powerOwner == null");

            WorldEntity ultimateOwner = power.GetUltimateOwner();
            UltimateOwnerId = ultimateOwner != null ? ultimateOwner.Id : power.Owner.Id;

            // NOTE: Due to how physics work, user may no longer be where they were when collision / combo / proc activated.
            // In these cases we use application position for validation checks to work.
            PowerOwnerPosition = power.IsMissileEffect() || power.IsComboEffect() || power.IsProcEffect()
                ? powerApplication.UserPosition
                : powerOwner.RegionLocation.Position;

            // Snapshot properties of the power and its owner
            Power.SerializeEntityPropertiesForPowerPayload(powerOwner, Properties);
            Power.SerializePowerPropertiesForPowerPayload(power, Properties);

            Logger.Debug($"Initialize(): Properties for {power}:\n{Properties}");

            // Snapshot additional data used to determine targets
            Range = power.GetApplicationRange();
            RegionId = powerOwner.Region.Id;
            OwnerAlliance = powerOwner.Alliance;
            BeamSweepSlice = -1;        // TODO
            ExecutionTime = power.GetFullExecutionTime();

            return true;
        }

        public PowerResults GenerateResults(Power power, WorldEntity owner, WorldEntity target)
        {
            // TODO: Use snapshotted data for calculations

            bool isHostile = target != null && owner.IsHostileTo(target);

            PowerResults results = new();
            results.Init(PowerOwnerId, UltimateOwnerId, target.Id, PowerOwnerPosition, PowerPrototype, AssetId.Invalid, isHostile);

            float physicalDamage = CalculateDamage(power, DamageType.Physical, owner, target);
            float energyDamage = CalculateDamage(power, DamageType.Energy, owner, target);
            float mentalDamage = CalculateDamage(power, DamageType.Mental, owner, target);

            // Check crit chance / super crit chance and apply damage multiplier if needed
            if (CheckCritChance(power.Prototype, owner, target))
            {
                if (CheckSuperCritChance(power.Prototype, owner, target))
                    results.SetFlag(PowerResultFlags.SuperCritical, true);
                else
                    results.SetFlag(PowerResultFlags.Critical, true);

                float critDamageMult = Power.GetCritDamageMult(owner.Properties, target, results.TestFlag(PowerResultFlags.SuperCritical));

                physicalDamage *= critDamageMult;
                energyDamage *= critDamageMult;
                mentalDamage *= critDamageMult;
            }

            // Apply level scaling

            // Show unscaled damage client-side
            results.SetDamageForClient(DamageType.Physical, physicalDamage);
            results.SetDamageForClient(DamageType.Energy, energyDamage);
            results.SetDamageForClient(DamageType.Mental, mentalDamage);

            float levelScalingMult = 1f;
            if (owner.CombatLevel != target.CombatLevel)
            {
                if (owner.CanBePlayerOwned())   
                {
                    long unscaledTargetHealthMax = target.Properties[PropertyEnum.HealthMax];
                    long scaledTargetHealthMax = CalculateTargetHealthMaxForCombatLevel(target, owner.CombatLevel);
                    levelScalingMult = MathHelper.Ratio(unscaledTargetHealthMax, scaledTargetHealthMax);
                    Logger.Debug($"Scaling {unscaledTargetHealthMax} => {scaledTargetHealthMax} ({levelScalingMult} ratio)");
                }
                else if (target.CanBePlayerOwned()) // Enemy => Player
                {
                    // Effectively disable damage to players for now
                    physicalDamage = 1f;
                    energyDamage = 1f;
                    mentalDamage = 1f;
                }
            }

            physicalDamage *= levelScalingMult;
            energyDamage *= levelScalingMult;
            mentalDamage *= levelScalingMult;

            // Set damage
            results.Properties[PropertyEnum.Damage, (int)DamageType.Physical] = physicalDamage;
            results.Properties[PropertyEnum.Damage, (int)DamageType.Energy] = energyDamage;
            results.Properties[PropertyEnum.Damage, (int)DamageType.Mental] = mentalDamage;

            // Calculate and set healing
            float healing = CalculateHealing(power, target);
            results.Properties[PropertyEnum.Healing] = healing;
            results.HealingForClient = healing;

            return results;
        }

        #region Calculations

        private static float CalculateDamage(Power power, DamageType damageType, WorldEntity user, WorldEntity target)
        {
            // Current implementation is based on the DamageStats class from the client

            PropertyCollection powerProperties = power.Properties;

            // Calculate base damage
            float damageBase = powerProperties[PropertyEnum.DamageBase, (int)damageType];
            damageBase += powerProperties[PropertyEnum.DamageBaseBonus, (int)damageType];

            // Manually get DamageBasePerLevel and CombatLevel to make sure C# does not implicitly cast to the wrong type
            float damageBasePerLevel = powerProperties[PropertyEnum.DamageBasePerLevel, (int)damageType];
            int combatLevel = powerProperties[PropertyEnum.CombatLevel];
            damageBase += damageBasePerLevel * combatLevel;

            // Apply variance and damage tuning score
            float variance = powerProperties[PropertyEnum.DamageVariance];
            float damageTuningScore = power.Prototype.DamageTuningScore;

            float minDamage = damageBase * damageTuningScore * (1f - variance);
            float maxDamage = damageBase * damageTuningScore * (1f + variance);

            // No need to run bonus calculations if base damage is 0
            if (maxDamage == 0f)
                return 0f;

            // Calculate damage multiplier
            float damageMult = CalculateDamageMultiplier(power, damageType, user, target);

            // Apply scaling to base damage
            minDamage = MathF.Max(0f, minDamage * damageMult);
            maxDamage = MathF.Max(0f, maxDamage * damageMult);

            // Apply base damage bonuses not affected by multipliers
            float damageBaseUnmodified = powerProperties[PropertyEnum.DamageBaseUnmodified];

            // Manually get DamageBaseUnmodifiedPerRank and PowerRank to make sure C# does not implicitly cast to the wrong type
            float damageBaseUnmodifiedPerRank = powerProperties[PropertyEnum.DamageBaseUnmodifiedPerRank];
            int powerRank = powerProperties[PropertyEnum.PowerRank];
            damageBaseUnmodified += damageBaseUnmodifiedPerRank * powerRank;

            minDamage += damageBaseUnmodified;
            maxDamage += damageBaseUnmodified;

            // Get damage value within range and apply additional modifiers to it (e.g. crit and mitigation)
            float damage = power.Game.Random.NextFloat(minDamage, maxDamage);

            return damage;
        }

        private static float CalculateDamageMultiplier(Power power, DamageType damageType, WorldEntity user, WorldEntity target)
        {
            PowerPrototype powerProto = power.Prototype;
            PropertyCollection powerProperties = power.Properties;

            // TODO: team up damage scalar
            float teamUpDamageScalar = 1f;

            // Calculate additive and multiplicative damage bonuses
            float damageMult = 1f + powerProperties[PropertyEnum.DamageMult];

            if (user is Agent agentUser)
            {
                PropertyCollection userProperties = agentUser.Properties;

                // Owner bonuses
                float damageRating = agentUser.GetDamageRating(damageType);

                damageMult += userProperties[PropertyEnum.DamagePctBonus];
                damageMult += userProperties[PropertyEnum.DamagePctBonusByType, (int)damageType];
                damageMult += userProperties[PropertyEnum.DamageMult];

                // Power bonuses
                damageMult += powerProperties[PropertyEnum.DamageMultOnPower];

                float powerDmgBonusFromAtkSpdPct = powerProperties[PropertyEnum.PowerDmgBonusFromAtkSpdPct];
                if (powerDmgBonusFromAtkSpdPct > 0f)
                    damageMult += (power.GetAnimSpeed() - 1f) * powerDmgBonusFromAtkSpdPct;

                // Power specific bonuses
                foreach (var kvp in userProperties.IteratePropertyRange(PropertyEnum.DamageRatingBonusForPower))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId protoRefToCheck);
                    if (protoRefToCheck == PrototypeId.Invalid)
                    {
                        Logger.Warn("CalculateDamage(): protoRefToCheck == PrototypeId.Invalid");
                        continue;
                    }

                    if (power.PrototypeDataRef == protoRefToCheck)
                        damageRating += kvp.Value;
                }

                foreach (var kvp in userProperties.IteratePropertyRange(PropertyEnum.DamagePctBonusForPower))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId protoRefToCheck);
                    if (protoRefToCheck == PrototypeId.Invalid)
                    {
                        Logger.Warn("CalculateDamage(): protoRefToCheck == PrototypeId.Invalid");
                        continue;
                    }

                    if (power.PrototypeDataRef == protoRefToCheck)
                        damageMult += kvp.Value;
                }

                foreach (var kvp in userProperties.IteratePropertyRange(PropertyEnum.DamageMultForPower))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId protoRefToCheck);
                    if (protoRefToCheck == PrototypeId.Invalid)
                    {
                        Logger.Warn("CalculateDamage(): protoRefToCheck == PrototypeId.Invalid");
                        continue;
                    }

                    if (power.PrototypeDataRef == protoRefToCheck)
                        damageMult += kvp.Value;
                }

                // Keyword-specific bonuses

                foreach (var kvp in userProperties.IteratePropertyRange(PropertyEnum.DamageRatingBonusForPowerKeyword))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId protoRefToCheck);
                    if (protoRefToCheck == PrototypeId.Invalid)
                    {
                        Logger.Warn("CalculateDamage(): protoRefToCheck == PrototypeId.Invalid");
                        continue;
                    }

                    if (agentUser.HasPowerWithKeyword(powerProto, protoRefToCheck))
                        damageRating += kvp.Value;
                }

                foreach (var kvp in userProperties.IteratePropertyRange(PropertyEnum.DamagePctBonusForPowerKeyword))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId protoRefToCheck);
                    if (protoRefToCheck == PrototypeId.Invalid)
                    {
                        Logger.Warn("CalculateDamage(): protoRefToCheck == PrototypeId.Invalid");
                        continue;
                    }

                    if (agentUser.HasPowerWithKeyword(powerProto, protoRefToCheck))
                        damageMult += kvp.Value;
                }

                foreach (var kvp in userProperties.IteratePropertyRange(PropertyEnum.DamageMultForPowerKeyword))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId protoRefToCheck);
                    if (protoRefToCheck == PrototypeId.Invalid)
                    {
                        Logger.Warn("CalculateDamage(): protoRefToCheck == PrototypeId.Invalid");
                        continue;
                    }

                    if (agentUser.HasPowerWithKeyword(powerProto, protoRefToCheck))
                        damageMult += kvp.Value;
                }

                // Boss-specific bonuses
                RankPrototype targetRankProto = target.GetRankPrototype();
                if (targetRankProto.Rank == Rank.MiniBoss || targetRankProto.Rank == Rank.Boss || targetRankProto.Rank == Rank.GroupBoss)
                {
                    damageRating += userProperties[PropertyEnum.DamageRatingBonusVsBosses];
                    damageMult += userProperties[PropertyEnum.DamagePctBonusVsBosses];
                }

                // Calculate and apply multiplier from damage rating
                damageMult += Power.GetDamageRatingMult(damageRating, userProperties, target);
            }

            return damageMult * teamUpDamageScalar;
        }

        private static bool CheckCritChance(PowerPrototype powerProto, WorldEntity user, WorldEntity target)
        {
            // Skip power that can't crit
            if (powerProto.CanCrit == false || powerProto.Activation == PowerActivationType.Passive)
                return false;

            float critChance = Power.GetCritChance(powerProto, user.Properties, target, user.Id);
            return user.Game.Random.NextFloat() < critChance;
        }

        private static bool CheckSuperCritChance(PowerPrototype powerProto, WorldEntity user, WorldEntity target)
        {
            float superCritChance = Power.GetSuperCritChance(powerProto, user.Properties, target);
            return user.Game.Random.NextFloat() < superCritChance;
        }

        private static float CalculateHealing(Power power, WorldEntity target)
        {
            // Based on Rule_healing::GetValue()
            PropertyCollection powerProperties = power.Properties;
            PropertyCollection targetProperties = target.Properties;

            // Calculate flat healing
            float healingBase = powerProperties[PropertyEnum.HealingBase];
            healingBase += powerProperties[PropertyEnum.HealingBaseCurve];

            float healingMagnitude = powerProperties[PropertyEnum.HealingMagnitude];
            float healingVariance = powerProperties[PropertyEnum.HealingVariance];

            float minHealing = healingBase * healingMagnitude * (1f - healingVariance);
            float maxHealing = healingBase * healingMagnitude * (1f + healingVariance);

            float healing = power.Game.Random.NextFloat(minHealing, maxHealing);

            // Apply percentage healing
            float healingBasePct = powerProperties[PropertyEnum.HealingBasePct];
            if (healingBasePct > 0)
            {
                long targetHealthMax = targetProperties[PropertyEnum.HealthMaxOther];
                healing += targetHealthMax * healingBasePct;
            }

            // HACK: Increase healing to compensate for the lack of avatar stats
            healing *= 3f;

            return healing;
        }

        private static long CalculateTargetHealthMaxForCombatLevel(WorldEntity target, int combatLevel)
        {
            using PropertyCollection healthMaxProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            // Copy all properties involved in calculating HealthMax from the target
            PropertyInfo healthMaxPropertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.HealthMax);

            foreach (PropertyId dependencyPropertyId in healthMaxPropertyInfo.EvalDependencies)
                healthMaxProperties.CopyProperty(target.Properties, dependencyPropertyId);

            // Set CombatLevel to the level we are scaling to
            healthMaxProperties[PropertyEnum.CombatLevel] = combatLevel;

            // Set the HealthBase curve used by the target
            PropertyInfo healthBasePropertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.HealthBase);
            CurveId healthBaseCurveId = target.Properties.GetCurveIdForCurveProperty(PropertyEnum.HealthBase);

            healthMaxProperties.SetCurveProperty(PropertyEnum.HealthBase, healthBaseCurveId, PropertyEnum.CombatLevel,
                healthBasePropertyInfo, SetPropertyFlags.None, true);

            // Calculate the eval
            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, healthMaxProperties);

            return Eval.RunLong(healthMaxPropertyInfo.Eval, evalContext);
        }

        #endregion
    }
}
