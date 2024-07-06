using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    public static class PowerPayloadHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static float CalculateDamage(Power power, DamageType damageType, WorldEntity user, WorldEntity target)
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

            // Calculate damage multiplier
            float damageMult = CalculateDamageMultiplier(power, damageType, user, target);
            Logger.Debug($"CalculateDamage(): damageMult = {damageMult}f");

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

            return power.Game.Random.NextFloat(minDamage, maxDamage);
        }

        public static float CalculateDamageMultiplier(Power power, DamageType damageType, WorldEntity user, WorldEntity target)
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
    }
}
