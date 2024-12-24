using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.Powers
{
    /// <summary>
    /// Snapshots the state of a <see cref="Power"/> and its owner and calculates effects to be applied as <see cref="PowerResults"/>.
    /// </summary>
    public class PowerPayload : PowerEffectsPacket
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Game Game { get; private set; }

        public bool IsPlayerPayload { get; private set; }
        public PrototypeId PowerProtoRef { get; private set; }
        public AssetId PowerAssetRefOverride { get; private set; }

        public Vector3 TargetPosition { get; private set; }
        public TimeSpan MovementTime { get; private set; }
        public TimeSpan VariableActivationTime { get; private set; }
        public uint PowerRandomSeed { get; private set; }
        public uint FXRandomSeed { get; private set; }

        public float Range { get; private set; }
        public ulong RegionId { get; private set; }
        public AlliancePrototype OwnerAlliance { get; private set; }
        public int BeamSweepSlice { get; private set; }
        public TimeSpan ExecutionTime { get; private set; }

        public KeywordsMask KeywordsMask { get; private set; }

        public EventGroup PendingEvents { get; } = new();

        public int CombatLevel { get => Properties[PropertyEnum.CombatLevel]; }

        public PowerActivationSettings ActivationSettings { get => new(TargetId, TargetPosition, PowerOwnerPosition); }

        /// <summary>
        /// Initializes this <see cref="PowerPayload"/> from a <see cref="PowerApplication"/> and snapshots
        /// the state of the <see cref="Power"/> and its owner.
        /// </summary>
        public bool Init(Power power, PowerApplication powerApplication)
        {
            Game = power.Game;
            PowerPrototype = power.Prototype;
            PowerProtoRef = power.Prototype.DataRef;

            PowerOwnerId = powerApplication.UserEntityId;
            TargetId = powerApplication.TargetEntityId;
            PowerOwnerPosition = powerApplication.UserPosition;
            TargetPosition = powerApplication.TargetPosition;
            MovementTime = powerApplication.MovementTime;
            VariableActivationTime = powerApplication.VariableActivationTime;
            PowerRandomSeed = powerApplication.PowerRandomSeed;
            FXRandomSeed = powerApplication.FXRandomSeed;

            // All payloads have to have valid owners on initialization
            WorldEntity powerOwner = Game.EntityManager.GetEntity<WorldEntity>(PowerOwnerId);
            if (powerOwner == null) return Logger.WarnReturn(false, "powerOwner == null");

            WorldEntity ultimateOwner = power.GetUltimateOwner();
            if (ultimateOwner != null)
            {
                UltimateOwnerId = ultimateOwner.Id;
                IsPlayerPayload = ultimateOwner.CanBePlayerOwned();
            }
            else
            {
                UltimateOwnerId = powerOwner.Id;
                IsPlayerPayload = powerOwner.CanBePlayerOwned();
            }

            // NOTE: Due to how physics work, user may no longer be where they were when collision / combo / proc activated.
            // In these cases we use application position for validation checks to work.
            PowerOwnerPosition = power.IsMissileEffect() || power.IsComboEffect() || power.IsProcEffect()
                ? powerApplication.UserPosition
                : powerOwner.RegionLocation.Position;

            // Snapshot properties of the power and its owner
            Power.SerializeEntityPropertiesForPowerPayload(power.GetPayloadPropertySourceEntity(), Properties);
            Power.SerializePowerPropertiesForPowerPayload(power, Properties);

            // Snapshot additional data used to determine targets
            Range = power.GetApplicationRange();
            RegionId = powerOwner.Region.Id;
            OwnerAlliance = powerOwner.Alliance;
            BeamSweepSlice = -1;        // TODO
            ExecutionTime = power.GetFullExecutionTime();
            KeywordsMask = power.KeywordsMask.Copy<KeywordsMask>();

            // TODO: visuals override
            PowerAssetRefOverride = AssetId.Invalid;

            // Snapshot additional properties to recalculate initial damage for enemy DCL scaling
            if (IsPlayerPayload == false)
            {
                CopyCurvePropertyRange(power.Properties, PropertyEnum.DamageBase);
                CopyCurvePropertyRange(power.Properties, PropertyEnum.DamageBasePerLevel);

                Properties.CopyPropertyRange(power.Properties, PropertyEnum.DamageBaseBonus);
                Properties.CopyProperty(power.Properties, PropertyEnum.DamageMagnitude);
                Properties.CopyProperty(power.Properties, PropertyEnum.DamageVariance);
                Properties.CopyPropertyRange(power.Properties, PropertyEnum.DamageBaseUnmodified);
                Properties.CopyPropertyRange(power.Properties, PropertyEnum.DamageBaseUnmodifiedPerRank);
            }

            return true;
        }

        /// <summary>
        /// Calculates properties for this <see cref="PowerPayload"/> that do not require a target.
        /// </summary>
        public void CalculateInitialProperties(Power power)
        {
            CalculateInitialDamage(power.Properties);
            CalculateInitialDamageBonuses(power);
            CalculateInitialDamagePenalties();
            CalculateInitialHealing(power.Properties);
            CalculateInitialResourceChange(power.Properties);
        }

        public void InitPowerResultsForTarget(PowerResults results, WorldEntity target)
        {
            bool isHostile = OwnerAlliance != null && OwnerAlliance.IsHostileTo(target.Alliance);

            results.Init(PowerOwnerId, UltimateOwnerId, target.Id, PowerOwnerPosition, PowerPrototype,
                PowerAssetRefOverride, isHostile);
        }

        public void RecalculateInitialDamageForCombatLevel(int combatLevel)
        {
            Properties[PropertyEnum.CombatLevel] = combatLevel;
            CalculateInitialDamage(Properties);
        }

        /// <summary>
        /// Calculates <see cref="PowerResults"/> for the provided <see cref="WorldEntity"/> target. 
        /// </summary>
        public void CalculatePowerResults(PowerResults targetResults, PowerResults userResults, WorldEntity target, bool calculateForTarget)
        {
            if (calculateForTarget)
            {
                CalculateResultDamage(targetResults, target);
                CalculateResultHealing(targetResults, target);

                CalculateResultConditionsToRemove(targetResults, target);
            }

            if (targetResults.IsDodged == false)
                CalculateResultConditionsToAdd(targetResults, target, calculateForTarget);

            // Copy extra properties
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.CreatorEntityAssetRefBase);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.CreatorEntityAssetRefCurrent);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.NoExpOnDeath);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.NoLootDrop);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.OnKillDestroyImmediate);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.ProcRecursionDepth);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.SetTargetLifespanMS);
        }

        #region Initial Calculations

        /// <summary>
        /// Calculates damage properties for this <see cref="PowerPayload"/> that do not require a target.
        /// </summary>
        /// <remarks>
        /// Affected properties: Damage, DamageBaseUnmodified.
        /// </remarks>
        private bool CalculateInitialDamage(PropertyCollection powerProperties)
        {
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateDamage(): powerProto == null");

            for (int damageType = 0; damageType < (int)DamageType.NumDamageTypes; damageType++)
            {
                // Calculate base damage
                float damageBase = powerProperties[PropertyEnum.DamageBase, damageType];
                damageBase += powerProperties[PropertyEnum.DamageBaseBonus];
                damageBase += (float)powerProperties[PropertyEnum.DamageBasePerLevel, damageType] * (int)Properties[PropertyEnum.CombatLevel];

                // Calculate variable activation time bonus (for hold and release powers)
                if (VariableActivationTime > TimeSpan.Zero)
                {
                    SecondaryActivateOnReleasePrototype secondaryActivateProto = GetSecondaryActivateOnReleasePrototype();

                    if (secondaryActivateProto != null &&
                        secondaryActivateProto.DamageIncreaseType == (DamageType)damageType &&
                        secondaryActivateProto.DamageIncreasePerSecond != CurveId.Invalid)
                    {
                        Curve damageIncreaseCurve = secondaryActivateProto.DamageIncreasePerSecond.AsCurve();
                        if (damageIncreaseCurve != null)
                        {
                            float damageIncrease = damageIncreaseCurve.GetAt(Properties[PropertyEnum.PowerRank]);
                            float timeMult = (float)Math.Min(VariableActivationTime.TotalMilliseconds, secondaryActivateProto.MaxReleaseTimeMS) * 0.001f;
                            damageBase += damageIncrease * timeMult;
                        }
                    }
                }

                // Calculate variance / tuning score multipliers
                float damageVariance = powerProperties[PropertyEnum.DamageVariance];
                float damageVarianceMult = (1f - damageVariance) + (damageVariance * 2f * Game.Random.NextFloat());

                float damageTuningScore = powerProto.DamageTuningScore;

                // Calculate damage
                float damage = damageBase * damageTuningScore * damageVarianceMult;
                if (damage > 0f)
                    Properties[PropertyEnum.Damage, damageType] = damage;

                // Calculate unmodified damage (flat damage unaffected by bonuses)
                float damageBaseUnmodified = powerProperties[PropertyEnum.DamageBaseUnmodified, damageType];
                damageBaseUnmodified += (float)powerProperties[PropertyEnum.DamageBaseUnmodifiedPerRank, damageType] * (int)Properties[PropertyEnum.PowerRank];

                Properties[PropertyEnum.DamageBaseUnmodified, damageType] = damageBaseUnmodified;
            }

            return true;
        }

        /// <summary>
        /// Calculates damage bonus properties for this <see cref="PowerPayload"/> that do not require a target.
        /// </summary>
        /// <remarks>
        /// Affected properties: PayloadDamageMultTotal, PayloadDamagePctModifierTotal, and PayloadDamageRatingTotal.
        /// </remarks>
        private bool CalculateInitialDamageBonuses(Power power)
        {
            WorldEntity powerOwner = Game.EntityManager.GetEntity<WorldEntity>(PowerOwnerId);
            if (powerOwner == null) return Logger.WarnReturn(false, "CalculateUserDamageBonuses(): powerOwner == null");

            PropertyCollection ownerProperties = powerOwner.Properties;

            // DamageMult
            float damageMult = Properties[PropertyEnum.DamageMult];

            // Apply bonus damage mult from attack speed
            float powerDmgBonusFromAtkSpdPct = Properties[PropertyEnum.PowerDmgBonusFromAtkSpdPct];
            if (powerDmgBonusFromAtkSpdPct > 0f)
                damageMult += powerDmgBonusFromAtkSpdPct * (power.GetAnimSpeed() - 1f);

            // For some weird reason this is not copied from power on initialization
            damageMult += power.Properties[PropertyEnum.DamageMultOnPower];

            // DamagePct
            float damagePct = Properties[PropertyEnum.DamagePctBonus];
            damagePct += Properties[PropertyEnum.DamagePctBonus];

            // DamageRating
            float damageRating = powerOwner.GetDamageRating();

            // Power / keyword specific bonuses
            Span<PropertyEnum> damageBonusProperties = stackalloc PropertyEnum[]
            {
                PropertyEnum.DamageMultForPower,
                PropertyEnum.DamageMultForPowerKeyword,
                PropertyEnum.DamagePctBonusForPower,
                PropertyEnum.DamagePctBonusForPowerKeyword,
                PropertyEnum.DamageRatingBonusForPower,
                PropertyEnum.DamageRatingBonusForPowerKeyword,
            };

            foreach (PropertyEnum propertyEnum in damageBonusProperties)
            {
                foreach (var kvp in ownerProperties.IteratePropertyRange(propertyEnum))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId protoRefToCheck);
                    if (protoRefToCheck == PrototypeId.Invalid)
                    {
                        Logger.Warn($"CalculateOwnerDamageBonuses(): Invalid param proto ref for {propertyEnum}");
                        continue;
                    }

                    // Filter power-specific bonuses
                    if (propertyEnum == PropertyEnum.DamageMultForPower || propertyEnum == PropertyEnum.DamagePctBonusForPower ||
                        propertyEnum == PropertyEnum.DamageRatingBonusForPower)
                    {
                        if (protoRefToCheck != PowerProtoRef)
                            continue;
                    }

                    // Filter keyword-specific bonuses
                    if (propertyEnum == PropertyEnum.DamageMultForPowerKeyword || propertyEnum == PropertyEnum.DamagePctBonusForPowerKeyword ||
                        propertyEnum == PropertyEnum.DamageRatingBonusForPowerKeyword)
                    {
                        if (HasKeyword(protoRefToCheck.As<KeywordPrototype>()) == false)
                            continue;
                    }

                    if (propertyEnum == PropertyEnum.DamageMultForPower || propertyEnum == PropertyEnum.DamageMultForPowerKeyword)
                    {
                        damageMult += kvp.Value;
                    }
                    else if (propertyEnum == PropertyEnum.DamagePctBonusForPower || propertyEnum == PropertyEnum.DamagePctBonusForPowerKeyword)
                    {
                        damagePct += kvp.Value;
                    }
                    else if (propertyEnum == PropertyEnum.DamageRatingBonusForPower || propertyEnum == PropertyEnum.DamageRatingBonusForPowerKeyword)
                    {
                        damageRating += kvp.Value;
                    }
                }
            }

            // Secondary resource bonuses
            if (power.CanUseSecondaryResourceEffects())
            {
                damagePct += power.Properties[PropertyEnum.SecondaryResourceDmgBnsPct];
                damageRating += power.Properties[PropertyEnum.SecondaryResourceDmgBns];
            }

            // Apply damage bonus for the number of powers on cooldown if needed.
            if (ownerProperties.HasProperty(PropertyEnum.DamageMultPowerCdKwd))
            {
                // Get the number of cooldowns from the most responsible power user because
                // this may be a missile / hotspot / summon power.
                WorldEntity mostResponsiblePowerUser = powerOwner.GetMostResponsiblePowerUser<WorldEntity>();

                foreach (var kvp in ownerProperties.IteratePropertyRange(PropertyEnum.DamageMultPowerCdKwd))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);
                    if (keywordProtoRef == PrototypeId.Invalid)
                    {
                        Logger.Warn($"CalculateOwnerDamageBonuses(): Invalid keyword param proto ref for {kvp.Key.Enum}");
                        continue;
                    }

                    KeywordPrototype keywordProto = keywordProtoRef.As<KeywordPrototype>();

                    int numPowersOnCooldown = 0;

                    foreach (var recordKvp in mostResponsiblePowerUser.PowerCollection)
                    {
                        Power recordPower = recordKvp.Value.Power;
                        if (recordPower.HasKeyword(keywordProto) && recordPower.IsOnCooldown())
                            numPowersOnCooldown++;
                    }

                    if (numPowersOnCooldown > 0)
                        damageMult += (float)kvp.Value * numPowersOnCooldown;

                }
            }

            // Set all damage bonus properties
            Properties[PropertyEnum.PayloadDamageMultTotal, DamageType.Any] = damageMult;
            Properties[PropertyEnum.PayloadDamagePctModifierTotal, DamageType.Any] = damagePct;
            Properties[PropertyEnum.PayloadDamageRatingTotal, DamageType.Any] = damageRating;

            // Apply damage type-specific bonuses
            for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
            {
                float damagePctBonusByType = powerOwner.Properties[PropertyEnum.DamagePctBonusByType, damageType];
                float damageRatingBonusByType = powerOwner.Properties[PropertyEnum.DamageRatingBonusByType, damageType];

                Properties[PropertyEnum.PayloadDamagePctModifierTotal, damageType] = damagePctBonusByType;
                Properties[PropertyEnum.PayloadDamageRatingTotal, damageType] = damageRatingBonusByType;
            }

            return true;
        }

        /// <summary>
        /// Calculates damage penalty (weaken) properties for this <see cref="PowerPayload"/> that do not require a target.
        /// </summary>
        /// <remarks>
        /// Affected properties: PayloadDamagePctWeakenTotal.
        /// </remarks>
        private bool CalculateInitialDamagePenalties()
        {
            WorldEntity powerOwner = Game.EntityManager.GetEntity<WorldEntity>(PowerOwnerId);
            if (powerOwner == null) return Logger.WarnReturn(false, "CalculateOwnerDamagePenalties(): powerOwner == null");

            // Apply weaken pct (maybe we should a separate CalculateOwnerDamagePenalties method for this?)

            float damagePctWeaken = powerOwner.Properties[PropertyEnum.DamagePctWeaken];

            foreach (var kvp in powerOwner.Properties.IteratePropertyRange(PropertyEnum.DamagePctWeakenForPowerKeyword))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);
                if (keywordProtoRef == PrototypeId.Invalid)
                {
                    Logger.Warn($"CalculateOwnerDamagePenalties(): Invalid param keyword proto ref for {keywordProtoRef}");
                    continue;
                }

                if (HasKeyword(keywordProtoRef.As<KeywordPrototype>()) == false)
                    continue;

                damagePctWeaken += kvp.Value;
            }

            Properties[PropertyEnum.PayloadDamagePctWeakenTotal, DamageType.Any] = damagePctWeaken;
            return true;
        }

        /// <summary>
        /// Calculates healing properties for this <see cref="PowerPayload"/> that do not require a target.
        /// </summary>
        /// <remarks>
        /// Affected properties: Healing, HealingBasePct.
        /// </remarks>
        private bool CalculateInitialHealing(PropertyCollection powerProperties)
        {
            // Calculate healing
            float healingBase = powerProperties[PropertyEnum.HealingBase];
            healingBase += powerProperties[PropertyEnum.HealingBaseCurve];

            float healingMagnitude = powerProperties[PropertyEnum.HealingMagnitude];

            float healingVariance = powerProperties[PropertyEnum.DamageVariance];
            float healingVarianceMult = (1f - healingVariance) + (healingVariance * 2f * Game.Random.NextFloat());

            float healing = healingBase * healingMagnitude * healingVarianceMult;

            // Set properties
            Properties[PropertyEnum.Healing] = healing;
            Properties.CopyProperty(powerProperties, PropertyEnum.HealingBasePct);

            return true;
        }

        /// <summary>
        /// Calculates resource change properties for this <see cref="PowerPayload"/> that do not require a target.
        /// </summary>
        /// <remarks>
        /// Affected properties: EnduranceChange, SecondaryResourceChange.
        /// </remarks>
        private bool CalculateInitialResourceChange(PropertyCollection powerProperties)
        {
            // Primary resource / endurance (spirit, etc.)
            foreach (var kvp in powerProperties.IteratePropertyRange(PropertyEnum.EnduranceChangeBase))
            {
                Property.FromParam(kvp.Key, 0, out int manaType);
                Properties[PropertyEnum.EnduranceChange, manaType] = kvp.Value;
            }

            // Secondary resource
            Properties[PropertyEnum.SecondaryResourceChange] = powerProperties[PropertyEnum.SecondaryResourceChangeBase];

            return true;
        }

        #endregion

        #region Result Calculations

        private bool CalculateResultDamage(PowerResults results, WorldEntity target)
        {
            // Placeholder implementation for testing
            Span<float> damage = stackalloc float[(int)DamageType.NumDamageTypes];
            damage.Clear();

            // Check crit / brutal strike chance
            if (CheckCritChance(target))
            {
                if (CheckSuperCritChance(target))
                    results.SetFlag(PowerResultFlags.SuperCritical, true);
                else
                    results.SetFlag(PowerResultFlags.Critical, true);
            }

            // Boss-specific bonuses (TODO: clean this up)
            RankPrototype targetRankProto = target.GetRankPrototype();
            float damagePctBonusVsBosses = 0f;
            float damageRatingBonusVsBosses = 0f;

            if (targetRankProto.IsRankBossOrMiniBoss)
            {
                damagePctBonusVsBosses += Properties[PropertyEnum.DamagePctBonusVsBosses];
                damageRatingBonusVsBosses += Properties[PropertyEnum.DamageRatingBonusVsBosses];
            }

            // TODO: team up damage scalar
            float teamUpDamageScalar = 1f;

            for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
            {
                damage[(int)damageType] = Properties[PropertyEnum.Damage, damageType];

                // DamageMult
                float damageMult = 1f;
                damageMult += Properties[PropertyEnum.PayloadDamageMultTotal, DamageType.Any];
                damageMult += Properties[PropertyEnum.PayloadDamageMultTotal, damageType];
                damageMult = MathF.Max(damageMult, 0f);

                damage[(int)damageType] *= damageMult;

                // DamagePct + DamageRating
                float damagePct = 1f;
                damagePct += Properties[PropertyEnum.PayloadDamagePctModifierTotal, DamageType.Any];
                damagePct += Properties[PropertyEnum.PayloadDamagePctModifierTotal, damageType];
                damagePct += damagePctBonusVsBosses;
                
                float damageRating = Properties[PropertyEnum.PayloadDamageRatingTotal, DamageType.Any];
                damageRating += Properties[PropertyEnum.PayloadDamageRatingTotal, damageType];
                damageRating += damageRatingBonusVsBosses;

                damagePct += Power.GetDamageRatingMult(damageRating, Properties, target);
                damagePct = MathF.Max(damagePct, 0f);

                damage[(int)damageType] *= damagePct;

                // DamagePctWeaken
                float damagePctWeaken = 1f;
                damagePctWeaken -= Properties[PropertyEnum.PayloadDamagePctWeakenTotal, DamageType.Any];
                damagePctWeaken -= Properties[PropertyEnum.PayloadDamagePctWeakenTotal, damageType];
                damagePctWeaken = MathF.Max(damagePctWeaken, 0f);

                damage[(int)damageType] *= damagePctWeaken;

                // Team-up damage scaling
                damage[(int)damageType] *= teamUpDamageScalar;

                // Add flat damage bonuses not affected by modifiers
                damage[(int)damageType] += Properties[PropertyEnum.DamageBaseUnmodified, damageType];

                results.Properties[PropertyEnum.Damage, damageType] = damage[(int)damageType];
            }

            CalculateResultDamageCriticalModifier(results, target);

            CalculateResultDamageMetaGameModifier(results, target);

            CalculateResultDamageLevelScaling(results, target);

            return true;
        }

        private bool CalculateResultDamageCriticalModifier(PowerResults results, WorldEntity target)
        {
            // Not critical
            if (results.TestFlag(PowerResultFlags.Critical) == false && results.TestFlag(PowerResultFlags.SuperCritical) == false)
                return true;

            float critDamageMult = Power.GetCritDamageMult(Properties, target, results.TestFlag(PowerResultFlags.SuperCritical));

            // Store damage values in a temporary span so that we don't modify the results' collection while iterating
            // Remove this if our future optimized implementation does not require this.
            Span<float> damage = stackalloc float[(int)DamageType.NumDamageTypes];

            foreach (var kvp in results.Properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                Property.FromParam(PropertyEnum.Damage, 0, out int damageType);
                if (damageType < (int)DamageType.NumDamageTypes)
                    damage[damageType] = kvp.Value;
            }

            for (int i = 0; i < (int)DamageType.NumDamageTypes; i++)
                results.Properties[PropertyEnum.Damage, i] = damage[i] * critDamageMult;

            return true;
        }

        private bool CalculateResultDamageMetaGameModifier(PowerResults results, WorldEntity target)
        {
            float damageMetaGameBossResistance = target.Properties[PropertyEnum.DamageMetaGameBossResistance];
            if (damageMetaGameBossResistance == 0f)
                return true;

            float mult = 1f - damageMetaGameBossResistance;

            // NOTE: damageMetaGameBossResistance > 0f = damage reduction
            //       damageMetaGameBossResistance < 0f = damage increase
            if (damageMetaGameBossResistance > 0f)
            {
                mult += Properties[PropertyEnum.DamageMetaGameBossPenetration];
                mult = Math.Clamp(mult, 0f, 1f);
            }

            if (mult == 1f)
                return true;

            for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
                results.Properties[PropertyEnum.Damage, damageType] *= mult;

            return true;
        }

        private bool CalculateResultDamageLevelScaling(PowerResults results, WorldEntity target)
        {
            // Apply player->enemy damage scaling
            float levelScalingMult = 1f;
            if (CombatLevel != target.CombatLevel && IsPlayerPayload && target.CanBePlayerOwned() == false)
            {
                long unscaledTargetHealthMax = target.Properties[PropertyEnum.HealthMax];
                long scaledTargetHealthMax = CalculateTargetHealthMaxForCombatLevel(target, CombatLevel);
                levelScalingMult = MathHelper.Ratio(unscaledTargetHealthMax, scaledTargetHealthMax);
            }

            Span<float> damage = stackalloc float[(int)DamageType.NumDamageTypes];
            int i = 0;
            foreach (var kvp in results.Properties.IteratePropertyRange(PropertyEnum.Damage))
                damage[i++] = kvp.Value;

            for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
            {
                if (levelScalingMult != 1f)
                    results.Properties[PropertyEnum.Damage, damageType] = damage[(int)damageType] * levelScalingMult;

                // Show unscaled damage numbers to the client
                // TODO: Hide region difficulty multipliers using this as well
                results.SetDamageForClient(damageType, damage[(int)damageType]);
            }

            return true;
        }

        private bool CalculateResultHealing(PowerResults results, WorldEntity target)
        {
            float healing = Properties[PropertyEnum.Healing];

            // HACK: Increase medkit healing to compensate for the lack of healing over time
            if (results.PowerPrototype.DataRef == GameDatabase.GlobalsPrototype.AvatarHealPower)
                healing *= 2f;

            // Pct healing
            float healingBasePct = Properties[PropertyEnum.HealingBasePct];
            if (healingBasePct > 0f)
            {
                long targetHealthMax = target.Properties[PropertyEnum.HealthMax];
                healing += targetHealthMax * healingBasePct;
            }

            if (healing > 0f)
            {
                results.Properties[PropertyEnum.Healing] = healing;
                results.HealingForClient = healing;
            }

            return true;
        }

        private bool CalculateResultConditionsToAdd(PowerResults results, WorldEntity target, bool isTargetResult)
        {
            if (PowerPrototype.AppliesConditions == null && PowerPrototype.ConditionsByRef.IsNullOrEmpty())
                return true;

            ConditionCollection conditionCollection = target?.ConditionCollection;
            if (conditionCollection == null) return Logger.WarnReturn(false, "CalculateResultConditionsToAdd(): conditionCollection == null");

            WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(results.PowerOwnerId);
            WorldEntity ultimateOwner = Game.EntityManager.GetEntity<WorldEntity>(results.UltimateOwnerId);

            if (PowerPrototype.AppliesConditions != null)
            {
                foreach (var entry in PowerPrototype.AppliesConditions)
                {
                    ConditionPrototype mixinConditionProto = entry.Prototype as ConditionPrototype;
                    if (mixinConditionProto == null)
                    {
                        Logger.Warn("CalculateResultConditionsToAdd(): mixinConditionProto == null");
                        continue;
                    }

                    CalculateResultConditionsToAddHelper(results, target, owner, ultimateOwner, isTargetResult, conditionCollection, mixinConditionProto);
                }
            }

            if (PowerPrototype.ConditionsByRef.HasValue())
            {
                foreach (PrototypeId conditionProtoRef in PowerPrototype.ConditionsByRef)
                {
                    ConditionPrototype conditionByRefProto = conditionProtoRef.As<ConditionPrototype>();
                    if (conditionByRefProto == null)
                    {
                        Logger.Warn("CalculateResultConditionsToAdd(): conditionByRefProto == null");
                        continue;
                    }

                    CalculateResultConditionsToAddHelper(results, target, owner, ultimateOwner, isTargetResult, conditionCollection, conditionByRefProto);
                }
            }

            for (int i = 0; i < results.ConditionAddList.Count; i++)
            {
                Condition condition = results.ConditionAddList[i];

                if (owner != null)
                {
                    Power power = owner.GetPower(PowerProtoRef);
                    power?.TrackCondition(target.Id, condition);
                }
                else if (condition.Duration == TimeSpan.Zero)
                {
                    Logger.Warn($"CalculateResultConditionsToAdd(): No owner to cancel infinite condition for {PowerPrototype}");
                }
            }

            return true;
        }

        private bool CalculateResultConditionsToAddHelper(PowerResults results, WorldEntity target, WorldEntity owner, WorldEntity ultimateOwner,
            bool calculateForTarget, ConditionCollection conditionCollection, ConditionPrototype conditionProto)
        {
            // Make sure the condition matches the scope for the current results
            if ((conditionProto.Scope == ConditionScopeType.Target && calculateForTarget == false) ||
                (conditionProto.Scope == ConditionScopeType.User && calculateForTarget))
            {
                return false;
            }

            // Check for condition immunities
            foreach (var kvp in target.Properties.IteratePropertyRange(PropertyEnum.ImmuneToConditionWithKwd))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);
                if (keywordProtoRef != PrototypeId.Invalid && conditionProto.HasKeyword(keywordProtoRef))
                    return false;
            }

            // Roll the chance to apply
            float chanceToApply = conditionProto.GetChanceToApplyConditionEffects(Properties, target, conditionCollection, PowerProtoRef, ultimateOwner);
            if (Game.Random.NextFloat() >= chanceToApply)
                return false;

            // Apply the conditon
            CalculateConditionDuration(conditionProto, owner, target, out TimeSpan duration);
            
            if ((PowerPrototype is MovementPowerPrototype movementPowerProto && movementPowerProto.IsTravelPower == false) ||
                (conditionProto.Properties != null && conditionProto.Properties[PropertyEnum.Knockback]))
            {
                // TODO: calculate duration for conditions that last as long as the entity is moving
                return false;
            }

            Condition condition = conditionCollection.AllocateCondition();
            condition.InitializeFromPower(conditionCollection.NextConditionId, this, conditionProto, duration);
            results.AddConditionToAdd(condition);

            return true;
        }

        private bool CalculateResultConditionsToRemove(PowerResults results, WorldEntity target)
        {
            bool removedAny = false;

            ConditionCollection conditionCollection = target?.ConditionCollection;
            if (conditionCollection == null)
                return removedAny;

            // Remove conditions created by specified powers
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.RemoveConditionsOfPower))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                Property.FromParam(kvp.Key, 1, out int maxStacksToRemove);

                removedAny |= CalculateResultConditionsToRemoveHelper(results, conditionCollection, ConditionFilter.IsConditionOfPowerFunc, powerProtoRef, maxStacksToRemove);
            }

            // Remove conditions with specified keywords
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.RemoveConditionsWithKeyword))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);
                Property.FromParam(kvp.Key, 1, out int maxStacksToRemove);

                removedAny |= CalculateResultConditionsToRemoveHelper(results, conditionCollection, ConditionFilter.IsConditionWithKeywordFunc, keywordProtoRef, maxStacksToRemove);
            }

            // Remove conditions that have specified properties
            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.RemoveConditionsWithPropertyOfType))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId propertyProtoRef);
                Property.FromParam(kvp.Key, 1, out int maxStacksToRemove);

                PropertyEnum propertyEnum = propertyInfoTable.GetPropertyEnumFromPrototype(propertyProtoRef);

                removedAny |= CalculateResultConditionsToRemoveHelper(results, conditionCollection, ConditionFilter.IsConditionWithPropertyOfTypeFunc, propertyEnum, maxStacksToRemove);
            }

            // Remove conditions of the specified type (no params here)
            AssetId conditionTypeAssetRef = Properties[PropertyEnum.RemoveConditionsOfType];
            if (conditionTypeAssetRef != AssetId.Invalid)
            {
                ConditionType conditionType = (ConditionType)AssetDirectory.Instance.GetEnumValue(conditionTypeAssetRef);
                if (conditionType != ConditionType.Neither)
                    removedAny |= CalculateResultConditionsToRemoveHelper(results, conditionCollection, ConditionFilter.IsConditionOfTypeFunc, conditionType, 0);
            }

            return removedAny;
        }

        private bool CalculateResultConditionsToRemoveHelper<T>(PowerResults results, ConditionCollection conditionColleciton,
            ConditionFilter.Func<T> filterFunc, T filterArg, int maxStacksToRemove = 0)
        {
            int numRemoved = 0;

            foreach (Condition condition in conditionColleciton)
            {
                if (filterFunc(condition, filterArg) == false)
                    continue;

                results.AddConditionToRemove(condition.Id);
                numRemoved++;

                if (maxStacksToRemove > 0 && numRemoved == maxStacksToRemove)
                    break;
            }

            return numRemoved > 0;
        }

        #endregion

        #region Bouncing

        public bool TryBounce()
        {
            // TODO
            return false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Returns the <see cref="SecondaryActivateOnReleasePrototype"/> for this <see cref="PowerPayload"/>.
        /// Returns <see langword="null"/> if it does not have one.
        /// </summary>
        private SecondaryActivateOnReleasePrototype GetSecondaryActivateOnReleasePrototype()
        {
            if (PowerPrototype == null) return null;

            var secondaryActivateProto = PowerPrototype.ExtraActivation as SecondaryActivateOnReleasePrototype;
            if (secondaryActivateProto == null && VariableActivationTime > TimeSpan.Zero)
            {
                // Missiles will need to look for their creator power for their secondary activate effect
                PrototypeId creatorPowerProtoRef = Properties[PropertyEnum.CreatorPowerPrototype];
                if (creatorPowerProtoRef != PrototypeId.Invalid)
                {
                    PowerPrototype creatorPowerProto = creatorPowerProtoRef.As<PowerPrototype>();
                    secondaryActivateProto = creatorPowerProto.ExtraActivation as SecondaryActivateOnReleasePrototype;
                }
            }

            return secondaryActivateProto;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PowerPayload"/> has the specified keyword.
        /// </summary>
        private bool HasKeyword(KeywordPrototype keywordProto)
        {
            return keywordProto != null && KeywordPrototype.TestKeywordBit(KeywordsMask, keywordProto);
        }

        /// <summary>
        /// Copies all curve properties that use the specified <see cref="PropertyEnum"/> from the provided <see cref="PropertyCollection"/>.
        /// </summary>
        private bool CopyCurvePropertyRange(PropertyCollection source, PropertyEnum propertyEnum)
        {
            // Move this to PropertyCollection if it's used somewhere else as well

            PropertyInfo propertyInfo = GameDatabase.PropertyInfoTable.LookupPropertyInfo(propertyEnum);
            if (propertyInfo.IsCurveProperty == false)
                return Logger.WarnReturn(false, $"CopyCurvePropertyRange(): {propertyEnum} is not a curve property");

            foreach (var kvp in source.IteratePropertyRange(propertyEnum))
            {
                CurveId curveId = source.GetCurveIdForCurveProperty(kvp.Key);
                PropertyId indexProperty = source.GetIndexPropertyIdForCurveProperty(kvp.Key);

                Properties.SetCurveProperty(kvp.Key, curveId, indexProperty, propertyInfo, SetPropertyFlags.None, true);
            }

            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PowerPayload"/>'s hit should be critical.
        /// </summary>
        private bool CheckCritChance(WorldEntity target)
        {
            // Skip power that can't crit
            if (PowerPrototype.CanCrit == false || PowerPrototype.Activation == PowerActivationType.Passive)
                return false;

            // Check if the crit is guaranteed by a keyword
            if (PowerPrototype.Keywords.HasValue())
            {
                foreach (PrototypeId keywordProtoRef in PowerPrototype.Keywords)
                {
                    if (Properties[PropertyEnum.CritAlwaysOnKeywordAttack, keywordProtoRef])
                        return true;

                    if (target.Properties[PropertyEnum.CritAlwaysOnGotHitKeyword, keywordProtoRef])
                        return true;
                }
            }

            // Override target level if needed
            int targetLevelOverride = -1;
            if (IsPlayerPayload && target.CanBePlayerOwned() == false)
                targetLevelOverride = CombatLevel;

            // Calculate and check crit chance
            float critChance = Power.GetCritChance(PowerPrototype, Properties, target, PowerOwnerId, PrototypeId.Invalid, targetLevelOverride);            
            return Game.Random.NextFloat() < critChance;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PowerPayload"/>'s hit should be super critical (brutal strike).
        /// </summary>
        private bool CheckSuperCritChance(WorldEntity target)
        {
            // Override target level if needed
            int targetLevelOverride = -1;
            if (IsPlayerPayload && target.CanBePlayerOwned() == false)
                targetLevelOverride = CombatLevel;

            // Calculate and check super crit chance
            float superCritChance = Power.GetSuperCritChance(PowerPrototype, Properties, target);
            return Game.Random.NextFloat() < superCritChance;
        }

        private bool CalculateConditionDuration(ConditionPrototype conditionProto, WorldEntity owner, WorldEntity target, out TimeSpan duration)
        {
            duration = conditionProto.GetDuration(Properties, owner, PowerProtoRef, target);

            // TODO: Apply modifiers to the base duration we get from the prototype

            if (duration < TimeSpan.Zero)
                return Logger.WarnReturn(false, $"CalculateConditionDuration(): Negative duration for {PowerPrototype}");

            return true;
        }

        /// <summary>
        /// Returns the HealthMax value of the provided <see cref="WorldEntity"/> adjusted for the specified combat level.
        /// </summary>
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
