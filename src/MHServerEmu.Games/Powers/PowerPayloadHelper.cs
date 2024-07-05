using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Powers
{
    public static class PowerPayloadHelper
    {
        public static float CalculateDamage(Power power, DamageType damageType)
        {
            // Current implementation is based on the DamageStats class from the client

            PropertyCollection properties = power.Properties;

            // Calculate base damage
            float damageBase = properties[PropertyEnum.DamageBase, (int)damageType];
            damageBase += properties[PropertyEnum.DamageBaseBonus, (int)damageType];

            // Manually get DamageBasePerLevel and CombatLevel to make sure C# does not implicitly cast to the wrong type
            float damageBasePerLevel = properties[PropertyEnum.DamageBasePerLevel, (int)damageType];
            int combatLevel = properties[PropertyEnum.CombatLevel];
            damageBase += damageBasePerLevel * combatLevel;

            // Apply variance and damage tuning score
            float variance = properties[PropertyEnum.DamageVariance];
            float damageTuningScore = power.Prototype.DamageTuningScore;

            float minDamage = damageBase * damageTuningScore * (1f - variance);
            float maxDamage = damageBase * damageTuningScore * (1f + variance);

            // Apply multipliers

            // TODO: team up damage scalar
            float teamUpDamageScalar = 1f;

            // TODO: damage multiplier
            float damageMult = 1f;

            minDamage = MathF.Max(0f, minDamage * damageMult * teamUpDamageScalar);
            maxDamage = MathF.Max(0f, maxDamage * damageMult * teamUpDamageScalar);

            // Apply base damage bonuses not affected by multipliers
            float damageBaseUnmodified = properties[PropertyEnum.DamageBaseUnmodified];

            // Manually get DamageBaseUnmodifiedPerRank and PowerRank to make sure C# does not implicitly cast to the wrong type
            float damageBaseUnmodifiedPerRank = properties[PropertyEnum.DamageBaseUnmodifiedPerRank];
            int powerRank = properties[PropertyEnum.PowerRank];
            damageBaseUnmodified += damageBaseUnmodifiedPerRank * powerRank;

            minDamage += damageBaseUnmodified;
            maxDamage += damageBaseUnmodified;

            return power.Game.Random.NextFloat(minDamage, maxDamage);
        }
    }
}
