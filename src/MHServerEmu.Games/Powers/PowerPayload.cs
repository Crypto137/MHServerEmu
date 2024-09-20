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
            UltimateOwnerId = ultimateOwner != null ? ultimateOwner.Id : power.Owner.Id;

            // NOTE: Due to how physics work, user may no longer be where they were when collision / combo / proc activated.
            // In these cases we use application position for validation checks to work.
            PowerOwnerPosition = power.IsMissileEffect() || power.IsComboEffect() || power.IsProcEffect()
                ? powerApplication.UserPosition
                : powerOwner.RegionLocation.Position;

            // Snapshot properties of the power and its owner
            Power.SerializeEntityPropertiesForPowerPayload(powerOwner, Properties);
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

        /// <summary>
        /// Calculates <see cref="PowerResults"/> for the provided <see cref="WorldEntity"/> target. 
        /// </summary>
        public void CalculatePowerResults(PowerResults results, WorldEntity target)
        {
            // Placeholder implementation for testing
            results.Properties.CopyPropertyRange(Properties, PropertyEnum.Damage);
            results.Properties.CopyPropertyRange(Properties, PropertyEnum.Healing);

            results.SetDamageForClient(DamageType.Physical, Properties[PropertyEnum.Damage, DamageType.Physical]);
            results.SetDamageForClient(DamageType.Energy, Properties[PropertyEnum.Damage, DamageType.Energy]);
            results.SetDamageForClient(DamageType.Mental, Properties[PropertyEnum.Damage, DamageType.Mental]);
            results.HealingForClient = Properties[PropertyEnum.Healing];
        }

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

            foreach (var kvp in powerOwner.Properties.IteratePropertyRange(PropertyEnum.DamagePctWeaken))
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

            float healing = healingBase * healingMagnitude * healingVariance;

            // HACK: Increase healing to compensate for the lack of healing over time
            healing *= 3f;

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

        //
        // OLD CODE BELOW
        //

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
