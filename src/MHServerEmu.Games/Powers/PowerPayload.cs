using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    public class PowerPayload : PowerEffectsPacket
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public void Init(ulong powerOwnerId, ulong ultimateOwnerId, ulong targetId, Vector3 powerOwnerPosition, PowerPrototype powerProto)
        {
            PowerOwnerId = powerOwnerId;
            UltimateOwnerId = ultimateOwnerId;
            TargetId = targetId;
            PowerOwnerPosition = powerOwnerPosition;
            PowerPrototype = powerProto;
        }

        public PowerResults GenerateResults(Power power, WorldEntity owner, WorldEntity target)
        {
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

            // Set damage
            results.Properties[PropertyEnum.Damage, (int)DamageType.Physical] = physicalDamage;
            results.Properties[PropertyEnum.Damage, (int)DamageType.Energy] = energyDamage;
            results.Properties[PropertyEnum.Damage, (int)DamageType.Mental] = mentalDamage;

            results.SetDamageForClient(DamageType.Physical, physicalDamage);
            results.SetDamageForClient(DamageType.Energy, energyDamage);
            results.SetDamageForClient(DamageType.Mental, mentalDamage);

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

        #endregion
    }
}
