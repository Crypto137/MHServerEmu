using Gazillion;
using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Behavior;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.MetaGames;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Powers
{
    /// <summary>
    /// Snapshots the state of a <see cref="Power"/> and its owner and calculates effects to be applied as <see cref="PowerResults"/>.
    /// </summary>
    public class PowerPayload : PowerEffectsPacket
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        [ThreadStatic]
        public static PowerPayload ReusableTickerPayload;

        private readonly Dictionary<ulong, int> _hitCountDict = new();

        private ulong _propertySourceEntityId;
        private WorldEntityPrototype _powerOwnerProto;
        private WorldEntityPrototype _ultimatePowerOwnerProto;

        public Game Game { get; private set; }

        public bool IsPlayerPayload { get; private set; }
        public PrototypeId PowerProtoRef { get; private set; }
        public AssetId PowerAssetRefOverride { get; private set; }

        public Vector3 UltimateOwnerPosition { get; private set; }
        public Vector3 TargetPosition { get; private set; }
        public Vector3 TargetEntityPosition { get; private set; }
        public TimeSpan MovementTime { get; private set; }
        public TimeSpan VariableActivationTime { get; private set; }
        public int PowerRandomSeed { get; private set; }
        public int FXRandomSeed { get; private set; }

        public float Range { get; private set; }
        public ulong RegionId { get; private set; }
        public AlliancePrototype OwnerAlliance { get; private set; }
        public TimeSpan ExecutionTime { get; private set; }
        public Action<PowerPayload> DeliverAction { get; set; }
        public EventGroup PendingEvents { get; } = new();

        public int CombatLevel { get => Properties[PropertyEnum.CombatLevel]; }
        public int AOESweepTick { get => Properties[PropertyEnum.AOESweepTick]; }
        public TimeSpan AOESweepRate { get => TimeSpan.FromMilliseconds((int)Properties[PropertyEnum.AOESweepRateMS]); }
        public bool IsTeamUpAwaySource { get => Properties[PropertyEnum.IsTeamUpAwaySource]; }

        public PowerActivationSettings ActivationSettings { get => new(TargetId, TargetPosition, PowerOwnerPosition); }

        public PowerPayload() { }

        #region Data Management

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

            _powerOwnerProto = powerOwner.WorldEntityPrototype;

            // Get ultimate owner id. We can't use Power.GetUltimateOwner() here because
            // we need the id even if the ultimate owner no longer exists to be able to create stack ids.
            ulong ultimateOwnerId = power.Owner.PowerUserOverrideId;
            if (ultimateOwnerId == Entity.InvalidId)
                ultimateOwnerId = power.Owner.Id;

            UltimateOwnerId = ultimateOwnerId;

            // Get additional information from the ultimate owner if it's available
            WorldEntity ultimateOwner = Game.EntityManager.GetEntity<WorldEntity>(ultimateOwnerId);
            if (ultimateOwner != null && ultimateOwner.IsInWorld)
            {
                UltimateOwnerPosition = ultimateOwner.RegionLocation.Position;
                _ultimatePowerOwnerProto = ultimateOwner.WorldEntityPrototype;
            }

            IsPlayerPayload = ultimateOwner != null ? ultimateOwner.CanBePlayerOwned() : powerOwner.CanBePlayerOwned();

            // Record that current position of the target (which may be different from the target position of this power)
            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(TargetId);
            if (target != null && target.IsInWorld)
                TargetEntityPosition = target.RegionLocation.Position;

            // NOTE: Due to how physics work, user may no longer be where they were when collision / combo / proc activated.
            // In these cases we use application position for validation checks to work.
            PowerOwnerPosition = power.IsMissileEffect() || power.IsComboEffect() || power.IsProcEffect()
                ? powerApplication.UserPosition
                : powerOwner.RegionLocation.Position;

            // Snapshot properties of the power and its owner
            WorldEntity propertySourceEntity = power.GetPayloadPropertySourceEntity(ultimateOwner);
            if (propertySourceEntity == null) return Logger.WarnReturn(false, "Init(): propertySourceEntity == null");

            // Save property source owner id for later calculations
            _propertySourceEntityId = propertySourceEntity != powerOwner ? propertySourceEntity.Id : powerOwner.Id;

            Power.SerializeEntityPropertiesForPowerPayload(propertySourceEntity, Properties);
            Power.SerializePowerPropertiesForPowerPayload(power, Properties);

            // Team-up passive flag
            Properties[PropertyEnum.IsTeamUpAwaySource] = power.IsTeamUpPassivePowerWhileAway;

            // Snapshot additional data used to determine targets
            Range = power.GetApplicationRange();
            RegionId = powerOwner.Region.Id;
            OwnerAlliance = powerOwner.Alliance;
            ExecutionTime = power.GetFullExecutionTime();
            SetKeywordsMask(power.KeywordsMask);

            // Beam sweep data
            if (power.GetTargetingShape() == TargetingShapeType.BeamSweep)
            {
                Properties.CopyProperty(power.Properties, PropertyEnum.AOESweepRateMS);
                Properties[PropertyEnum.AOESweepTick] = powerApplication.BeamSweepTick;
            }

            // Apply visual overrides (use the ultimate owner for missile overrides)
            WorldEntity assetSourceEntity;
            if (power.IsMissileEffect() && powerOwner.GetOriginalWorldAsset() == AssetId.Invalid && ultimateOwner != null)
                assetSourceEntity = ultimateOwner;
            else
                assetSourceEntity = powerOwner;

            AssetId creatorEntityAssetRefBase = assetSourceEntity.GetOriginalWorldAsset();
            AssetId creatorEntityAssetRefCurrent = assetSourceEntity.GetEntityWorldAsset();
            Properties[PropertyEnum.CreatorEntityAssetRefBase] = creatorEntityAssetRefBase;
            Properties[PropertyEnum.CreatorEntityAssetRefCurrent] = creatorEntityAssetRefCurrent;

            AssetId powerAssetRef = PowerPrototype.GetUnrealClass(creatorEntityAssetRefBase, creatorEntityAssetRefCurrent);
            if (powerAssetRef != PowerPrototype.PowerUnrealClass)
                PowerAssetRefOverride = powerAssetRef;

            // Rank for difficulty scaling (prioritize creator rank)
            PrototypeId rankProtoRef = powerOwner.Properties[PropertyEnum.CreatorRank];
            if (rankProtoRef == PrototypeId.Invalid)
                rankProtoRef = powerOwner.Properties[PropertyEnum.Rank];

            Properties[PropertyEnum.Rank] = rankProtoRef;

            // Set scenario affixes
            if (powerApplication.ItemSourceId != Entity.InvalidId)
            {
                var item = Game.EntityManager.GetEntity<Item>(powerApplication.ItemSourceId);
                item?.SetScenarioProperties(Properties);
            }

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

            // Initialize bouncing
            int bounceCount = power.Properties[PropertyEnum.BounceCount];
            if (bounceCount > 0)
            {
                Properties[PropertyEnum.BounceCountPayload] = bounceCount;
                Properties[PropertyEnum.BounceRangePayload] = power.GetRange();
                Properties[PropertyEnum.BounceSpeedPayload] = power.GetProjectileSpeed(PowerOwnerPosition, TargetPosition);
                Properties[PropertyEnum.PayloadSkipRangeCheck] = true;
                Properties[PropertyEnum.PowerPreviousTargetsID, 0] = TargetId;
            }
            else if (powerApplication.SkipRangeCheck)
            {
                // Copy range skip from the application if we didn't get it from bouncing
                Properties[PropertyEnum.PayloadSkipRangeCheck] = true;
            }

            // Movement speed override (movement power / knockbacks)
            if (PowerPrototype is not MovementPowerPrototype movementPowerProto || movementPowerProto.ConstantMoveTime == false)
                Properties.CopyProperty(power.Properties, PropertyEnum.MovementSpeedOverride);

            // Snapshot properties from triggering power results
            // TODO: Do we need full power results here? We should be able to get away with just the properties
            
            // Set proc recursion depth
            if (powerApplication.PowerResults != null)
            {
                int procRecursionDepth = powerApplication.PowerResults.Properties[PropertyEnum.ProcRecursionDepth];
                if (power.IsProcEffect())
                    procRecursionDepth++;
                Properties[PropertyEnum.ProcRecursionDepth] = procRecursionDepth;
            }

            // Snapshot damage for conversion (e.g. barrier primary resources)
            if (power.Properties.HasProperty(PropertyEnum.DamageConvertToCondition))
            {
                if (powerApplication.PowerResults != null)
                {
                    foreach (var kvp in powerApplication.PowerResults.Properties.IteratePropertyRange(PropertyEnum.Damage))
                    {
                        Property.FromParam(kvp.Key, 0, out int damageType);
                        Properties[PropertyEnum.DamageIncoming, damageType] = kvp.Value;
                    }
                }
                else
                {
                    Logger.Warn("Init(): powerApplication.PowerResults == null");
                }
            }

            power.OnPayloadInit(this);

            return true;
        }

        public void OnDeliverPayload()
        {
            DeliverAction?.Invoke(this);
        }

        /// <summary>
        /// Initiates a dummy <see cref="PowerPayload"/> for applying over time effects.
        /// </summary>
        public void Init(Game game)
        {
            Game = game;
            Properties.Clear();
        }

        /// <summary>
        /// Initializes an instance of <see cref="PowerResults"/> using data from this <see cref="PowerPayload"/>.
        /// </summary>
        public void InitPowerResultsForTarget(PowerResults results, WorldEntity target)
        {
            bool isHostile = OwnerAlliance != null && OwnerAlliance.IsHostileTo(target.Alliance);

            results.Init(PowerOwnerId, UltimateOwnerId, target.Id, PowerOwnerPosition, PowerPrototype,
                PowerAssetRefOverride, isHostile);
        }

        /// <summary>
        /// Updates the target of this <see cref="PowerPayload"/>.
        /// </summary>
        public void UpdateTarget(ulong targetId, Vector3 targetPosition)
        {
            TargetId = targetId;
            TargetPosition = targetPosition;
        }

        /// <summary>
        /// Increments the number of times the specified target has been hit by this <see cref="PowerPayload"/>.
        /// </summary>
        public void IncrementHitCount(ulong targetId)
        {
            _hitCountDict.TryGetValue(targetId, out int count);
            _hitCountDict[targetId] = ++count;
        }

        /// <summary>
        /// Returns the number of times the specified target has been hit by this <see cref="PowerPayload"/>.
        /// </summary>
        public int GetHitCount(ulong targetId)
        {
            _hitCountDict.TryGetValue(targetId, out int count);
            return count;
        }

        /// <summary>
        /// Recalculates initial damage properties of this <see cref="PowerPayload"/> for the specified combat level.
        /// </summary>
        public void RecalculateInitialDamageForCombatLevel(int combatLevel)
        {
            Properties[PropertyEnum.CombatLevel] = combatLevel;
            CalculateInitialDamage(Properties);
        }

        /// <summary>
        /// Clears calculated damage, damage accumulation, healing, and resource changes from this <see cref="PowerPayload"/>.
        /// </summary>
        public void ClearResult()
        {
            Properties.RemovePropertyRange(PropertyEnum.Damage);
            Properties.RemovePropertyRange(PropertyEnum.DamageAccumulationChange);
            Properties.RemovePropertyRange(PropertyEnum.EnduranceChange);
            Properties.RemoveProperty(PropertyEnum.Healing);
            Properties.RemoveProperty(PropertyEnum.SecondaryResourceChange);
        }

        #endregion

        #region Initial Calculations

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

                // Calculate variance / tuning score / magnitude multipliers
                float damageVariance = powerProperties[PropertyEnum.DamageVariance];
                float damageVarianceMult = (1f - damageVariance) + (damageVariance * 2f * Game.Random.NextFloat());

                float damageTuningScore = powerProto.DamageTuningScore;

                float damageMagnitude = powerProperties[PropertyEnum.DamageMagnitude];

                // Calculate damage
                float damage = damageBase * damageTuningScore * damageVarianceMult * damageMagnitude;
                if (damage > 0f)
                    Properties[PropertyEnum.Damage, damageType] = damage;

                // Calculate unmodified damage (flat damage not affected by bonuses)
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
            WorldEntity powerOwner = Game.EntityManager.GetEntity<WorldEntity>(_propertySourceEntityId);
            if (powerOwner == null) return Logger.WarnReturn(false, "CalculateInitialDamageBonuses(): powerOwner == null");

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

            // NOTE: In some cases DamagePctBonus can potentially exist on both the owner
            // and the power, so when copying properties powers will override their owners.
            // For this reason we need to sum them manually here.
            float damagePct = power.Properties[PropertyEnum.DamagePctBonus];
            damagePct += ownerProperties[PropertyEnum.DamagePctBonus];

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
                        Logger.Warn($"CalculateInitialDamageBonuses(): Invalid param proto ref for {propertyEnum}");
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
                        Logger.Warn($"CalculateInitialDamageBonuses(): Invalid keyword param proto ref for {kvp.Key.Enum}");
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
                float damagePctBonusByType = ownerProperties[PropertyEnum.DamagePctBonusByType, damageType];
                Properties.AdjustProperty(damagePctBonusByType, new(PropertyEnum.PayloadDamagePctModifierTotal, damageType));

                float damageRatingBonusByType = ownerProperties[PropertyEnum.DamageRatingBonusByType, damageType];
                Properties.AdjustProperty(damageRatingBonusByType, new(PropertyEnum.PayloadDamageRatingTotal, damageType));
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

        #region Over Time Calculations

        /// <summary>
        /// Calculates properties for a tick of an over time effect.
        /// </summary>
        public void CalculateOverTimeProperties(WorldEntity target, PropertyCollection overTimeProperties, float timeSeconds, bool calculateDamage)
        {
            if (calculateDamage)
                CalculateOverTimeDamage(target, overTimeProperties, timeSeconds);

            CalculateOverTimeHealing(target, overTimeProperties, timeSeconds);
            CalculateOverTimeResourceChange(target, overTimeProperties, timeSeconds);
            CalculateOverTimeDamageAccumulationChange(overTimeProperties, timeSeconds);
        }

        /// <summary>
        /// Calculates damage for a tick of an over time effect.
        /// </summary>
        private bool CalculateOverTimeDamage(WorldEntity target, PropertyCollection overTimeProperties, float timeSeconds)
        {
            // DoTs require a full power payload for calculations
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateOverTimeDamage(): powerProto == null");

            long targetHealthMax = target.Properties[PropertyEnum.HealthMax];

            for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
            {
                // Calculate base damage
                float damageOverTimeBasePerLevel = overTimeProperties[PropertyEnum.DamageOverTimeBasePerLevel, damageType];
                float damageOverTimeBaseBonus = overTimeProperties[PropertyEnum.DamageOverTimeBaseBonus, damageType];
                float bonus = damageOverTimeBasePerLevel * CombatLevel + damageOverTimeBaseBonus;

                float damage = CalculateOverTimeValue(overTimeProperties, new(PropertyEnum.DamageOverTimeBase, damageType),
                    PropertyEnum.DamageOverTimeVariance, PropertyEnum.DamageOverTimeMagnitude, bonus);

                // Apply tuning score
                damage *= powerProto.DamageTuningScore;

                // Apply health pct based damage
                damage += targetHealthMax * (float)overTimeProperties[PropertyEnum.DamageOverTimePctTargetHealthMax, damageType];

                // Apply time multiplier
                damage *= timeSeconds;

                // Set value
                if (damage > 0f)
                    Properties[PropertyEnum.Damage, damageType] = damage;

                // ----
                // Calculate flat unmodified damage (not affected by scaling)
                float damageUnmodified = overTimeProperties[PropertyEnum.DamageOverTimeBaseUnmodified, damageType];

                // Apply per-rank unmodified damage
                float damageOverTimeBaseUnmodPerRank = overTimeProperties[PropertyEnum.DamageOverTimeBaseUnmodPerRank, damageType];
                int powerRank = Properties[PropertyEnum.PowerRank];
                damageUnmodified += damageOverTimeBaseUnmodPerRank * powerRank;

                // Apply time multiplioer
                damageUnmodified *= timeSeconds;

                // Set value
                if (damageUnmodified > 0f)
                    Properties[PropertyEnum.DamageBaseUnmodified, damageType] = damageUnmodified;
            }

            return true;
        }

        /// <summary>
        /// Calculates healing for a tick of an over time effect.
        /// </summary>
        private void CalculateOverTimeHealing(WorldEntity target, PropertyCollection overTimeProperties, float timeSeconds)
        {
            // Check if our target can receive healing

            // CanHeal can be overriden with PowerForceHealing
            if (target.CanHeal == false && Properties[PropertyEnum.PowerForceHealing] == false)
                return;

            // Do not heal if at max health
            long health = target.Properties[PropertyEnum.Health];
            long healthMax = target.Properties[PropertyEnum.HealthMax];

            if (health >= healthMax)
                return;

            float healing = 0f;

            // Flat bonus
            healing += CalculateOverTimeValue(overTimeProperties, PropertyEnum.HealingOverTimeBase,
                PropertyEnum.HealingOverTimeVariance, PropertyEnum.HealingOverTimeMagnitude);

            // Pct bonus
            float healthPct = CalculateOverTimeValue(overTimeProperties, PropertyEnum.HealingOverTimeBasePct,
                PropertyEnum.HealingOverTimeVariance, PropertyEnum.HealingOverTimeMagnitude);
            healing += healthMax * healthPct;

            // Time multiplier
            healing *= timeSeconds;

            // Set
            Properties[PropertyEnum.Healing] = healing;
        }

        /// <summary>
        /// Calculates primary and secondary resource change for a tick of an over time effect.
        /// </summary>
        private void CalculateOverTimeResourceChange(WorldEntity target, PropertyCollection overTimeProperties, float timeSeconds)
        {
            if (target is not Avatar avatar)
                return;

            // Endurance
            bool hasAllChange = HasOverTimeEnduranceChange(overTimeProperties, ManaType.TypeAll);

            for (ManaType manaType = ManaType.Type1; manaType < ManaType.NumTypes; manaType++)
            {
                if (hasAllChange == false && HasOverTimeEnduranceChange(overTimeProperties, manaType) == false)
                    continue;

                float enduranceChange = 0f;

                // Flat bonus
                enduranceChange += CalculateOverTimeValue(overTimeProperties, new(PropertyEnum.EnduranceCOTBase, manaType),
                    new(PropertyEnum.EnduranceCOTVariance, manaType), new(PropertyEnum.EnduranceCOTMagnitude, manaType));

                enduranceChange += CalculateOverTimeValue(overTimeProperties, new(PropertyEnum.EnduranceCOTBase, ManaType.TypeAll),
                    new(PropertyEnum.EnduranceCOTVariance, ManaType.TypeAll), new(PropertyEnum.EnduranceCOTMagnitude, ManaType.TypeAll));

                // Pct bonus
                float enduranceMax = target.Properties[PropertyEnum.EnduranceMax, manaType];

                float endurancePct = CalculateOverTimeValue(overTimeProperties, new(PropertyEnum.EnduranceCOTPctBase, manaType),
                    new(PropertyEnum.EnduranceCOTVariance, manaType), new(PropertyEnum.EnduranceCOTMagnitude, manaType));
                enduranceChange += enduranceMax * endurancePct;

                float endurancePctAll = CalculateOverTimeValue(overTimeProperties, new(PropertyEnum.EnduranceCOTPctBase, ManaType.TypeAll),
                    new(PropertyEnum.EnduranceCOTVariance, ManaType.TypeAll), new(PropertyEnum.EnduranceCOTMagnitude, ManaType.TypeAll));
                enduranceChange += enduranceMax * endurancePctAll;

                // Time multiplier
                enduranceChange *= timeSeconds;

                // Set if the change results in a gain or decay
                float endurance = target.Properties[PropertyEnum.Endurance, manaType];

                if ((enduranceChange > 0f && endurance < enduranceMax && avatar.CanGainOrRegenEndurance(manaType)) ||
                    (enduranceChange < 0f && endurance > 0f))
                {
                    Properties[PropertyEnum.EnduranceChange, manaType] = enduranceChange;
                }
            }

            // Secondary resources
            float secondaryResourceChange = overTimeProperties[PropertyEnum.SecondaryResourceCOTBase];

            // Pct bonus
            float secondaryResourceMax = target.Properties[PropertyEnum.SecondaryResourceMax];

            secondaryResourceChange += secondaryResourceMax * overTimeProperties[PropertyEnum.SecondaryResourceCOTPct];

            // Time multiplier
            secondaryResourceChange *= timeSeconds;

            if (secondaryResourceChange != 0f)
                Properties[PropertyEnum.SecondaryResourceChange] = secondaryResourceChange;
        }

        private void CalculateOverTimeDamageAccumulationChange(PropertyCollection overTimeProperties, float timeSeconds)
        {
            foreach (var kvp in overTimeProperties.IteratePropertyRange(PropertyEnum.DamageAccumulationCOT))
            {
                Property.FromParam(kvp.Key, 0, out int damageTypeValue);
                DamageType damageType = (DamageType)damageTypeValue;

                Properties[PropertyEnum.DamageAccumulationChange, damageType] = kvp.Value * timeSeconds;
            }
        }

        #endregion

        #region Result Calculations

        /// <summary>
        /// Calculates <see cref="PowerResults"/> for the provided <see cref="WorldEntity"/> target. 
        /// </summary>
        public void CalculatePowerResults(PowerResults targetResults, PowerResults userResults, WorldEntity target, bool calculateForTarget)
        {
            if (calculateForTarget)
            {
                // Flag for resurrection if needed
                if (Properties[PropertyEnum.IsResurrectionPower])
                    targetResults.SetFlag(PowerResultFlags.Resurrect, true);

                // Check dodge chance (dodge is full mitigation, so don't bother calculating other stuff if dodged)
                if (CheckDodgeChance(target))
                {
                    targetResults.SetFlag(PowerResultFlags.Dodged, true);
                }
                else
                {
                    // Block is partial mitigation, so continue the calculations even if blocked
                    if (CheckBlockChance(target))
                        targetResults.SetFlag(PowerResultFlags.Blocked, true);

                    // Check if this is an instant kill (deals damage equal to the target's current health).
                    // Instant kills override normal damage calculations.
                    if (Properties[PropertyEnum.InstantKill])
                        targetResults.SetFlag(PowerResultFlags.InstantKill, true);
                    else
                        CalculateResultDamage(targetResults, target);

                    CalculateResultHealing(targetResults, target);
                    CalculateResultResourceChanges(targetResults, target);
                    CalculateResultDamageAccumulation(targetResults);
                }

                // Dodging can still remove conditions
                CalculateResultConditionsToRemove(targetResults, target);
            }

            if (targetResults.IsDodged == false)
            {
                CalculateResultConditionsToAdd(targetResults, target, calculateForTarget);
                CalculateResultNegativeStatusRemoval(targetResults, target);
            }

            // Check Teleport property
            if (calculateForTarget)
                CalculateResultTeleport(targetResults, target);

            // Copy extra properties
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.CreatorEntityAssetRefBase);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.CreatorEntityAssetRefCurrent);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.NoExpOnDeath);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.NoLootDrop);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.OnKillDestroyImmediate);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.ProcRecursionDepth);
            targetResults.Properties.CopyProperty(Properties, PropertyEnum.SetTargetLifespanMS);

            // Add hit reaction if needed (NOTE: some conditions applied before take priority over hit reactions)
            if (calculateForTarget)
                CalculateResultHitReaction(targetResults, target);
        }

        public void CalculatePowerResultsOverTime(PowerResults targetResults, WorldEntity target, bool calculateDamage)
        {
            if (calculateDamage)
                CalculateResultDamage(targetResults, target);

            CalculateResultHealing(targetResults, target);
            CalculateResultResourceChanges(targetResults, target);
            CalculateResultDamageAccumulation(targetResults);
        }

        private bool CalculateResultDamage(PowerResults results, WorldEntity target)
        {
            Span<float> damageValues = stackalloc float[(int)DamageType.NumDamageTypes];
            damageValues.Clear();

            // Get base damage from properties
            bool hasBaseDamage = false;
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                Property.FromParam(kvp.Key, 0, out int damageType);
                if (damageType >= damageValues.Length)
                    continue;

                float damage = kvp.Value;
                damageValues[damageType] = kvp.Value;
                hasBaseDamage |= damage > 0f;
            }

            // Add DamageBasePctTargetHealth if needed
            if (Properties.HasProperty(PropertyEnum.DamageBasePctTargetHealthCur) || Properties.HasProperty(PropertyEnum.DamageBasePctTargetHealthMax))
            {
                long health = target.Properties[PropertyEnum.Health];
                long healthMax = target.Properties[PropertyEnum.HealthMax];

                for (int damageType = 0; damageType < (int)DamageType.NumDamageTypes; damageType++)
                {
                    float pctTargetHealthDamage = 0f;
                    pctTargetHealthDamage += health * (float)Properties[PropertyEnum.DamageBasePctTargetHealthCur, damageType];
                    pctTargetHealthDamage += health * (float)Properties[PropertyEnum.DamageBasePctTargetHealthMax, damageType];

                    damageValues[damageType] += pctTargetHealthDamage;
                    hasBaseDamage |= pctTargetHealthDamage > 0f;
                }
            }

            // Check if we have DamageBaseUnmodified
            hasBaseDamage |= Properties.HasProperty(PropertyEnum.DamageBaseUnmodified);

            // Don't do other calculations if there is no base damage
            if (hasBaseDamage == false)
                return true;

            // Check if this target can be affected by this payload
            if (CheckUnaffected(target))
            {
                // Stop damage calculations if unaffected
                results.SetFlag(PowerResultFlags.Unaffected, true);
                return true;
            }

            // Check crit / brutal strike chance
            if (CheckCritChance(target))
            {
                if (CheckSuperCritChance(target))
                    results.SetFlag(PowerResultFlags.SuperCritical, true);
                else
                    results.SetFlag(PowerResultFlags.Critical, true);
            }

            // Copy payload damage bonus properties to results to apply target-specific modifiers to them
            PropertyCollection resultProperties = results.Properties;
            resultProperties.CopyPropertyRange(Properties, PropertyEnum.PayloadDamageMultTotal);
            resultProperties.CopyPropertyRange(Properties, PropertyEnum.PayloadDamagePctModifierTotal);
            resultProperties.CopyPropertyRange(Properties, PropertyEnum.PayloadDamagePctWeakenTotal);
            resultProperties.CopyPropertyRange(Properties, PropertyEnum.PayloadDamageRatingTotal);

            // Calculate target-specific damage bonuses (these will modify PayloadDamage bonuses copied above)
            CalculateResultDamageRankBonus(results, target);
            CalculateResultDamageAggroBonus(results, target);
            CalculateResultDamageTargetKeywordBonus(results, target);
            CalculateResultDamagePowerBonus(results, target);
            CalculateResultDamageNearbyDistanceBonus(results, target);
            CalculateResultDamageRangedDistanceBonus(results, target);

            // Team-ups deal too much damage at lower levels, so they need to have a scalar applied to their damage
            float teamUpDamageScalar = 1f;
            if (_powerOwnerProto is AgentTeamUpPrototype || (_ultimatePowerOwnerProto != null && _ultimatePowerOwnerProto is AgentTeamUpPrototype) || IsTeamUpAwaySource)
                teamUpDamageScalar = GameDatabase.DifficultyGlobalsPrototype.GetTeamUpDamageScalar(CombatLevel);

            // Get live tuning multiplier for mobs
            float liveTuningMultiplier = 1f;
            if (_ultimatePowerOwnerProto != null)
                liveTuningMultiplier = LiveTuningManager.GetLiveWorldEntityTuningVar(_ultimatePowerOwnerProto, WorldEntityTuningVar.eWETV_MobPowerDamage);

            for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
            {
                // DamageMult
                float damageMult = 1f;
                damageMult += resultProperties[PropertyEnum.PayloadDamageMultTotal, DamageType.Any];
                damageMult += resultProperties[PropertyEnum.PayloadDamageMultTotal, damageType];
                damageMult = MathF.Max(damageMult, 0f);

                damageValues[(int)damageType] *= damageMult;

                // DamagePct + DamageRating
                float damagePct = 1f;
                damagePct += resultProperties[PropertyEnum.PayloadDamagePctModifierTotal, DamageType.Any];
                damagePct += resultProperties[PropertyEnum.PayloadDamagePctModifierTotal, damageType];
                
                float damageRating = resultProperties[PropertyEnum.PayloadDamageRatingTotal, DamageType.Any];
                damageRating += resultProperties[PropertyEnum.PayloadDamageRatingTotal, damageType];

                damagePct += Power.GetDamageRatingMult(damageRating, Properties, target);
                damagePct = MathF.Max(damagePct, 0f);

                damageValues[(int)damageType] *= damagePct;

                // DamagePctWeaken
                float damagePctWeaken = 1f;
                damagePctWeaken -= resultProperties[PropertyEnum.PayloadDamagePctWeakenTotal, DamageType.Any];
                damagePctWeaken -= resultProperties[PropertyEnum.PayloadDamagePctWeakenTotal, damageType];
                damagePctWeaken = MathF.Max(damagePctWeaken, 0f);

                damageValues[(int)damageType] *= damagePctWeaken;

                // Team-up damage scaling
                damageValues[(int)damageType] *= teamUpDamageScalar;

                // Live tuning multiplier for mobs
                damageValues[(int)damageType] *= liveTuningMultiplier;

                // Add flat damage bonuses not affected by modifiers
                damageValues[(int)damageType] += Properties[PropertyEnum.DamageBaseUnmodified, damageType];

                results.Properties[PropertyEnum.Damage, damageType] = damageValues[(int)damageType];
            }

            // Apply crit
            CalculateResultDamageCriticalModifier(results, target);

            // Check OnGotDamaged procs prior to damage mitigation
            float healthDelta = 0f;

            foreach (var kvp in results.Properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                float damageByType = kvp.Value;
                healthDelta -= damageByType;

                Property.FromParam(kvp.Key, 0, out int damageType);

                ProcTriggerType triggerType = (DamageType)damageType switch
                {
                    DamageType.Physical => ProcTriggerType.OnGotDamagedPhysicalPriorResist,
                    DamageType.Energy   => ProcTriggerType.OnGotDamagedEnergyPriorResist,
                    DamageType.Mental   => ProcTriggerType.OnGotDamagedMentalPriorResist,
                    _                   => ProcTriggerType.None
                };

                if (triggerType == ProcTriggerType.None)
                {
                    Logger.Warn("CalculateResultDamage(): triggerType == ProcTriggerType.None");
                    continue;
                }

                target.TryActivateOnGotDamagedProcs(triggerType, results, -damageByType);
            }

            target.TryActivateOnGotDamagedProcs(ProcTriggerType.OnGotDamagedPriorResist, results, healthDelta);

            // Apply other modifiers
            CalculateResultDamagePvPBoost(results, target);

            CalculateResultDamageSplitBetweenTargets(results);

            CalculateResultDamagePvPScaling(results, target);

            CalculateResultDamageDifficultyScaling(results, target, out float difficultyMult);

            CalculateResultDamageLiveTuningModifier(results);

            CalculateResultDamageBounceModifier(results, target);

            CalculateResultDamageBonusReservoir(results);

            CalculateResultDamageVulnerabilityModifier(results, target);

            CalculateResultDamageBlockModifier(results, target);

            CalculateResultDamageDefenseModifier(results, target);

            CalculateResultDamagePctResistModifier(results, target);

            CalculateResultDamageShieldModifier(results, target);   // DamageShield needs to be applied before DamageConversion (e.g. Psylocke's barrier)

            CalculateResultDamageConversion(results, target, difficultyMult);

            CalculateResultDamageMetaGameModifier(results, target);

            CalculateResultDamagePvPReduction(results, target);

            CalculateResultDamageLevelScaling(results, target, difficultyMult);

            CalculateResultDamageTransfer(results, target);

            // Flag as NoDamage if we don't have damage and this isn't a DoT (this will make the client display 0)
            if (results.TestFlag(PowerResultFlags.OverTime) == false)
            {
                bool hasResultDamage = false;
                foreach (var kvp in results.Properties.IteratePropertyRange(PropertyEnum.Damage))
                    hasResultDamage |= MathHelper.RoundToInt(kvp.Value) > 0;

                results.SetFlag(PowerResultFlags.NoDamage, hasResultDamage == false);
            }

            return true;
        }

        private bool CalculateResultDamageCriticalModifier(PowerResults results, WorldEntity target)
        {
            // Not critical
            if (results.TestFlag(PowerResultFlags.Critical) == false && results.TestFlag(PowerResultFlags.SuperCritical) == false)
                return true;

            float critDamageMult = Power.GetCritDamageMult(Properties, target, results.TestFlag(PowerResultFlags.SuperCritical));
            ApplyDamageMultiplier(results.Properties, critDamageMult);

            return true;
        }

        private bool CalculateResultDamageRankBonus(PowerResults results, WorldEntity target)
        {
            RankPrototype targetRankProto = target.GetRankPrototype();
            if (targetRankProto == null) return Logger.WarnReturn(false, "CalculateResultDamageRankModifier(): targetRankProto == null");

            float damageMult = 0f;
            float damagePct = 0f;
            float damageRating = 0f;

            // DamageMultVsRank
            CalculateResultDamageRankBonusHelper(ref damageMult, Properties, PropertyEnum.DamageMultVsRank, targetRankProto);

            // DamagePctBonusVsRank
            CalculateResultDamageRankBonusHelper(ref damagePct, Properties, PropertyEnum.DamagePctBonusVsRank, targetRankProto);

            // DamageRatingBonusVsRank
            CalculateResultDamageRankBonusHelper(ref damageRating, Properties, PropertyEnum.DamageRatingBonusVsRank, targetRankProto);

            // BonusVsBosses
            if (targetRankProto.IsRankBossOrMiniBoss)
            {
                damagePct += Properties[PropertyEnum.DamagePctBonusVsBosses];
                damageRating += Properties[PropertyEnum.DamageRatingBonusVsBosses];
            }

            if (damageMult != 0f)
                results.Properties.AdjustProperty(damageMult, new(PropertyEnum.PayloadDamageMultTotal, DamageType.Any));

            if (damagePct != 0f)
                results.Properties.AdjustProperty(damagePct, new(PropertyEnum.PayloadDamagePctModifierTotal, DamageType.Any));

            if (damageRating != 0f)
                results.Properties.AdjustProperty(damageRating, new(PropertyEnum.PayloadDamageRatingTotal, DamageType.Any));

            return true;
        }

        private static void CalculateResultDamageRankBonusHelper(ref float value, PropertyCollection properties, PropertyEnum propertyEnum, RankPrototype targetRankProto)
        {
            foreach (var kvp in properties.IteratePropertyRange(propertyEnum))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId paramRankProtoRef);
                RankPrototype paramRankProto = paramRankProtoRef.As<RankPrototype>();
                if (paramRankProto == null)
                {
                    Logger.Warn("CalculateResultDamageRankModifierHelper(): paramRankProto == null");
                    continue;
                }

                if (paramRankProto.Rank == targetRankProto.Rank)
                    value += kvp.Value;
            }
        }

        private void CalculateResultDamageAggroBonus(PowerResults results, WorldEntity target)
        {
            float damagePctBonusVsAggroed = Properties[PropertyEnum.DamagePctBonusVsAggroed];
            float damagePctBonusVsUnaware = Properties[PropertyEnum.DamagePctBonusVsUnaware];
            float damageRatingBonusVsAggroed = Properties[PropertyEnum.DamageRatingBonusVsAggroed];
            float damageRatingBonusVsUnaware = Properties[PropertyEnum.DamageRatingBonusVsUnaware];

            // Check if there is anything to apply
            if (damagePctBonusVsAggroed == 0f && damagePctBonusVsUnaware == 0f && damageRatingBonusVsAggroed == 0f && damageRatingBonusVsUnaware == 0f)
                return;

            // Only agents can be aggroed
            if (target is not Agent agent)
                return;

            // Check if this agent has AI
            AIController aiController = agent.AIController;
            if (aiController == null)
                return;

            // Apply aggro or unaware bonus
            WorldEntity targetOfTarget = aiController.TargetEntity;
            if (targetOfTarget != null && targetOfTarget.Id == UltimateOwnerId)
            {
                results.Properties.AdjustProperty(damagePctBonusVsAggroed, new(PropertyEnum.PayloadDamagePctModifierTotal, DamageType.Any));
                results.Properties.AdjustProperty(damageRatingBonusVsAggroed, new(PropertyEnum.PayloadDamageRatingTotal, DamageType.Any));
            }
            else
            {
                results.Properties.AdjustProperty(damagePctBonusVsUnaware, new(PropertyEnum.PayloadDamagePctModifierTotal, DamageType.Any));
                results.Properties.AdjustProperty(damageRatingBonusVsUnaware, new(PropertyEnum.PayloadDamageRatingTotal, DamageType.Any));
            }
        }

        private bool CalculateResultDamageTargetKeywordBonus(PowerResults results, WorldEntity target)
        {
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateResultDamageKeywordModifier(): powerProto == null");

            float damageMult = 0f;
            float damagePct = 0f;
            float damageRating = 0f;

            // DamageMultVsKeyword
            CalculateResultDamageKeywordBonusHelper(ref damageMult, Properties, PropertyEnum.DamageMultVsKeyword, target);

            // DamagePctBonusVsConditionKeyword
            CalculateResultDamageKeywordBonusHelper(ref damagePct, Properties, PropertyEnum.DamagePctBonusVsConditionKeyword, target);

            // DamageRatingBonusVsConditionKeyword
            CalculateResultDamageKeywordBonusHelper(ref damageRating, Properties, PropertyEnum.DamageRatingBonusVsConditionKeyword, target);

            // DamageMultVsKeywordForPowerKwd
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.DamageMultVsKeywordForPowerKwd))
            {
                // Check target keyword
                Property.FromParam(kvp.Key, 0, out PrototypeId targetKeywordProtoRef);
                KeywordPrototype targetKeywordProto = targetKeywordProtoRef.As<KeywordPrototype>();

                if (target.HasKeyword(targetKeywordProto) == false && target.HasConditionWithKeyword(targetKeywordProto) == false)
                    continue;

                // Check power keyword
                Property.FromParam(kvp.Key, 1, out PrototypeId powerKeywordProtoRef);
                KeywordPrototype powerKeywordProto = powerKeywordProtoRef.As<KeywordPrototype>();
                
                if (HasKeyword(powerKeywordProto) == false)
                    continue;

                damageMult += kvp.Value;
            }

            if (damageMult != 0f)
                results.Properties.AdjustProperty(damageMult, new(PropertyEnum.PayloadDamageMultTotal, DamageType.Any));

            if (damagePct != 0f)
                results.Properties.AdjustProperty(damagePct, new(PropertyEnum.PayloadDamagePctModifierTotal, DamageType.Any));

            if (damageRating != 0f)
                results.Properties.AdjustProperty(damageRating, new(PropertyEnum.PayloadDamageRatingTotal, DamageType.Any));

            return true;
        }

        private static void CalculateResultDamageKeywordBonusHelper(ref float value, PropertyCollection properties, PropertyEnum propertyEnum, WorldEntity target)
        {
            foreach (var kvp in properties.IteratePropertyRange(propertyEnum))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);
                KeywordPrototype keywordProto = keywordProtoRef.As<KeywordPrototype>();

                if (target.HasKeyword(keywordProto) == false && target.HasConditionWithKeyword(keywordProto) == false)
                    continue;

                value += kvp.Value;
            }
        }

        private void CalculateResultDamagePowerBonus(PowerResults results, WorldEntity target)
        {
            float damageRating = 0f;

            // Apply damage rating bonuses for specific powers / power keywords set on the target (e.g. via conditions)

            // DamageRatingBonusForPowerVsTarget
            foreach (var kvp in target.Properties.IteratePropertyRange(PropertyEnum.DamageRatingBonusForPowerVsTarget))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId powerProtoRef);
                if (powerProtoRef == PrototypeId.Invalid)
                {
                    Logger.Warn("CalculateResultDamagePowerBonus(): powerProtoRef == PrototypeId.Invalid");
                    continue;
                }

                if (powerProtoRef != PowerProtoRef)
                    continue;

                damageRating += kvp.Value;
            }

            // DamageRatingBonusForPowerKeywordVsTarget
            foreach (var kvp in target.Properties.IteratePropertyRange(PropertyEnum.DamageRatingBonusForPowerKeywordVsTarget))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);

                if (HasKeyword(keywordProtoRef.As<KeywordPrototype>()) == false)
                    continue;

                damageRating += kvp.Value;
            }

            if (damageRating != 0f)
                results.Properties.AdjustProperty(damageRating, new(PropertyEnum.PayloadDamageRatingTotal, DamageType.Any));
        }

        private bool CalculateResultDamageNearbyDistanceBonus(PowerResults results, WorldEntity target)
        {
            if (target.IsInWorld == false) return Logger.WarnReturn(false, "CalculateResultDamageNearbyDistanceBonus(): target.IsInWorld == false");

            float damageRating = 0f;

            Vector3 userPosition = UltimateOwnerId == Entity.InvalidId ? PowerOwnerPosition : UltimateOwnerPosition;

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.DamageRatingBonusWithinDist))
            {
                // On the cosmological scale, it's all nearby.
                Property.FromParam(kvp.Key, 0, out int nearbyThreshold);
                float distanceSquared = Vector3.DistanceSquared(userPosition, target.RegionLocation.Position);
                if (distanceSquared > (nearbyThreshold * nearbyThreshold))
                    continue;

                damageRating += kvp.Value;
            }

            if (damageRating != 0f)
                results.Properties.AdjustProperty(damageRating, new(PropertyEnum.PayloadDamageRatingTotal, DamageType.Any));

            return true;
        }

        private bool CalculateResultDamageRangedDistanceBonus(PowerResults results, WorldEntity target)
        {
            if (target.IsInWorld == false) return Logger.WarnReturn(false, "CalculateResultDamageRangedDistanceBonus(): target.IsInWorld == false");

            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateResultDamageRangedDistanceBonus(): powerProto == null");

            // This bonus applies only to powers keyworded as ranged
            if (powerProto.HasKeyword(GameDatabase.KeywordGlobalsPrototype.RangedPowerKeywordPrototype) == false)
                return true;

            float damagePct = 0f;

            WorldEntity user;
            Vector3 userPosition;

            if (UltimateOwnerId == Entity.InvalidId)
            {
                user = Game.EntityManager.GetEntity<WorldEntity>(PowerOwnerId);
                userPosition = PowerOwnerPosition;
            }
            else
            {
                user = Game.EntityManager.GetEntity<WorldEntity>(UltimateOwnerId);
                userPosition = UltimateOwnerPosition;
            }

            // Fail silently here if no valid user position (TODO: see if there is anything else wrong that's causing this)
            if (userPosition == Vector3.Zero)
                return false;   //return Logger.WarnReturn(false, $"CalculateResultDamageRangedDistanceBonus(): No valid user position for powerProto=[{powerProto}], user=[{user}]");

            CalculateResultDamageRangedDistanceBonusHelper(ref damagePct, PropertyEnum.DamagePctBonusDistanceClose, user, userPosition, target);
            CalculateResultDamageRangedDistanceBonusHelper(ref damagePct, PropertyEnum.DamagePctBonusDistanceFar, user, userPosition, target);

            if (damagePct != 0f)
                results.Properties.AdjustProperty(damagePct, new(PropertyEnum.PayloadDamagePctModifierTotal, DamageType.Any));

            return true;
        }

        private void CalculateResultDamageRangedDistanceBonusHelper(ref float value, PropertyEnum propertyEnum, WorldEntity user, Vector3 userPosition, WorldEntity target)
        {
            float maxDistanceBonus = Properties[propertyEnum];
            if (maxDistanceBonus == 0f)
                return;

            // Calculate the distance between the user and the target
            float minRange = target.Bounds.Radius + (user != null ? user.Bounds.Radius : 0f);
            float maxRange = Range + Properties[PropertyEnum.MissileRange];

            float distance = Vector3.Length(target.RegionLocation.Position - userPosition);
            distance = MathHelper.ClampNoThrow(distance, minRange, maxRange);

            // Calculate distance bonus multiplier excluding the min range
            float distanceBonusMult = (distance - minRange) / (maxRange - minRange);

            // Invert the multiplier if we are applying a close distance bonus
            if (propertyEnum == PropertyEnum.DamagePctBonusDistanceClose)
                distanceBonusMult = 1f - distanceBonusMult;

            value += maxDistanceBonus * distanceBonusMult;
        }

        private bool CalculateResultDamagePvPBoost(PowerResults results, WorldEntity target)
        {
            Region region = target.Region;
            if (region == null) return Logger.WarnReturn(false, "CalculateResultDamagePvPBoost(): region == null");

            PvP pvp = region.GetPvPMatch();
            if (pvp == null)
                return true;

            // This is dumb and should probably never be enabled.
            if (Game.CustomGameOptions.ApplyHiddenPvPDamageModifiers == false)
                return true;

            // Only avatar-originating damage gets boosted in PvP.
            Avatar avatar = Game.EntityManager.GetEntity<Avatar>(UltimateOwnerId);
            if (avatar == null)
                return true;

            PvPPrototype pvpProto = pvp.PvPPrototype;
            PropertyCollection avatarProps = avatar.Properties;

            float boostPct = 1f;

            boostPct += pvpProto.GetDamageBoostForKDPct(avatarProps[PropertyEnum.PvPRecentKDRatio]);
            boostPct += pvpProto.GetDamageBoostForNoobs(avatarProps[PropertyEnum.PvPMatchCount]);
            boostPct += pvpProto.GetDamageBoostForWinPct(avatarProps[PropertyEnum.PvPRecentWinLossRatio]);

            if (Game.InfinitySystemEnabled == false)
            {
                Player player = avatar.GetOwnerOfType<Player>();
                if (player != null)
                {
                    float omegaPct = (float)player.GetOmegaPoints() / GameDatabase.AdvancementGlobalsPrototype.OmegaPointsCap;
                    boostPct += pvpProto.GetDamageBoostForOmegaPct(omegaPct);
                }
            }

            ApplyDamageMultiplier(results.Properties, boostPct);
            return true;
        }

        private bool CalculateResultDamageSplitBetweenTargets(PowerResults results)
        {
            // Used for SurturRaid (including Rogue's stolen power for Lord Brimstone) and MoleMan
            if (Properties[PropertyEnum.DamageSplitBetweenTargets] == false)
                return true;

            int targetsHit = Math.Max(1, Properties[PropertyEnum.TargetsHit]);
            if (targetsHit == 1)
                return true;

            float splitMult = 1f / targetsHit;

            ApplyDamageMultiplier(results.Properties, splitMult);
            return true;
        }

        private bool CalculateResultDamagePvPScaling(PowerResults results, WorldEntity target)
        {
            float pvpDamageMult = 1f;

            // Damage modifiers that apply when the target is in a PvP match
            if (target.IsInPvPMatch)
            {
                pvpDamageMult *= target.Properties[PropertyEnum.PvPIncomingDamageMult];
                pvpDamageMult *= Properties[PropertyEnum.PvPOutgoingDamageMult];
            }

            // Damage modifiers that apply when both the owner and the target are players
            if (IsPlayerPayload && target.CanBePlayerOwned())
            {
                DifficultyGlobalsPrototype difficultyGlobals = GameDatabase.DifficultyGlobalsPrototype;
                pvpDamageMult *= difficultyGlobals.PvPDamageMultiplier;

                Curve pvpDamageScalarFromLevelCurve = difficultyGlobals.PvPDamageScalarFromLevelCurve.AsCurve();
                if (pvpDamageScalarFromLevelCurve != null)
                    pvpDamageMult *= pvpDamageScalarFromLevelCurve.GetAt(CombatLevel);
            }

            ApplyDamageMultiplier(results.Properties, pvpDamageMult);
            return true;
        }

        private bool CalculateResultDamageDifficultyScaling(PowerResults results, WorldEntity target, out float difficultyMult)
        {
            difficultyMult = 1f;

            // Do not apply difficulty scaling to player vs player or mob vs mob damage
            if (target.CanBePlayerOwned() == IsPlayerPayload)
                return true;

            TuningTable tuningTable = target.Region?.TuningTable;
            if (tuningTable == null) return Logger.WarnReturn(false, "CalculateResultDamageDifficultyScaling(): tuningTable == null");

            // Scaling differs based on the rank of the target
            RankPrototype rankProto = IsPlayerPayload
                ? target.GetRankPrototype()
                : GameDatabase.GetPrototype<RankPrototype>(Properties[PropertyEnum.Rank]);
            if (rankProto == null) return Logger.WarnReturn(false, "CalculateResultDamageDifficultyScaling(): rankProto == null");

            difficultyMult = tuningTable.GetDamageMultiplier(IsPlayerPayload, rankProto.Rank, target.RegionLocation.Position);
            
            ApplyDamageMultiplier(results.Properties, difficultyMult);
            return true;
        }

        private bool CalculateResultDamageLiveTuningModifier(PowerResults results)
        {
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateResultDamageLiveTuningModifier(): powerProto == null");

            Region region = Game.RegionManager.GetRegion(RegionId);
            if (region == null) return Logger.WarnReturn(false, "CalculateResultDamageLiveTuningModifier(): region == null");

            PowerTuningVar tuningVar = region.ContainsPvPMatch() ? PowerTuningVar.ePTV_PowerDamagePVP : PowerTuningVar.ePTV_PowerDamagePVE;
            float tuningDamageMult = LiveTuningManager.GetLivePowerTuningVar(powerProto, tuningVar);

            ApplyDamageMultiplier(results.Properties, tuningDamageMult);
            return true;
        }

        private bool CalculateResultDamageBounceModifier(PowerResults results, WorldEntity target)
        {
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateResultDamageBounceModifier(): powerProto == null");

            Curve curve = powerProto.BounceDamagePctToSameIdCurve.AsCurve();
            if (curve == null)
                return true;

            int hitCount = GetHitCount(target.Id);
            float bounceMult = 1f + Math.Max(-1f, curve.GetAt(hitCount));

            ApplyDamageMultiplier(results.Properties, bounceMult);
            return true;
        }

        private bool CalculateResultDamageBonusReservoir(PowerResults results)
        {
            // PropertyEnum.DamageBonusReservoir - appears to be unused in 1.48/1.52
            // Was used for Iron Man's Shield Overload in 1.10
            return true;
        }

        private bool CalculateResultDamageVulnerabilityModifier(PowerResults results, WorldEntity target)
        {
            // NOTE: For vulnerability we pick the highest multiplier out of generic, power-specific and PvP.
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateResultDamageVulnerabilityModifier(): powerProto == null");

            // PvP vulnerability
            float damagePctVulnerabilityPvP = target.IsInPvPMatch ? target.Properties[PropertyEnum.DamagePctVulnerabilityPvP] : 0f;

            Span<float> damageValues = stackalloc float[(int)DamageType.NumDamageTypes];
            damageValues.Clear();

            // Calculate damage amplification by vulnerability
            foreach (var kvp in results.Properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                float damage = kvp.Value;
                if (damage == 0f)
                    continue;

                Property.FromParam(kvp.Key, 0, out int damageTypeValue);
                DamageType damageType = (DamageType)damageTypeValue;

                // Generic vulnerability
                float damagePctVulnerability = target.Properties[PropertyEnum.DamagePctVulnerability, damageType];
                damagePctVulnerability += target.Properties[PropertyEnum.DamagePctVulnerability, DamageType.Any];

                // Power-specific vulnerability
                float damagePctVulnerabilityVsPower = target.Properties[PropertyEnum.DamagePctVulnerabilityVsPower, powerProto.DataRef];
                float damagePctVulnerabilityVsPowerKwd = 0f;
                
                foreach (var kwdKvp in target.Properties.IteratePropertyRange(PropertyEnum.DamagePctVulnerabilityVsPowerKwd))
                {
                    Property.FromParam(kwdKvp.Key, 0, out PrototypeId keywordProtoRef);
                    if (powerProto.HasKeyword(keywordProtoRef.As<KeywordPrototype>()) == false)
                        continue;

                    damagePctVulnerabilityVsPowerKwd = Math.Max(damagePctVulnerabilityVsPowerKwd, kwdKvp.Value);
                }

                // Pick the highest vulnerabililty pct
                float pickedDamagePctVulnerability = damagePctVulnerability;
                pickedDamagePctVulnerability = Math.Max(pickedDamagePctVulnerability, damagePctVulnerabilityPvP);
                pickedDamagePctVulnerability = Math.Max(pickedDamagePctVulnerability, damagePctVulnerabilityVsPower);
                pickedDamagePctVulnerability = Math.Max(pickedDamagePctVulnerability, damagePctVulnerabilityVsPowerKwd);

                float vulnerabilityMult = 1f + pickedDamagePctVulnerability;
                damageValues[(int)damageType] = damage * vulnerabilityMult;
            }

            // Set amplified damage
            for (int i = 0; i < damageValues.Length; i++)
            {
                float damage = damageValues[i];
                if (damage == 0f)
                    continue;

                results.Properties[PropertyEnum.Damage, i] = damage;
            }

            return true;
        }

        private void CalculateResultDamageBlockModifier(PowerResults results, WorldEntity target)
        {
            if (results.IsBlocked == false)
                return;

            float blockDamageMult = 1f;
            blockDamageMult -= target.Properties[PropertyEnum.BlockDamageReductionPct];
            blockDamageMult += target.Properties[PropertyEnum.BlockDamageReductionPctMod];
            blockDamageMult = Math.Clamp(blockDamageMult, 0f, 1f);

            ApplyDamageMultiplier(results.Properties, blockDamageMult);
        }

        private bool CalculateResultDamageDefenseModifier(PowerResults results, WorldEntity target)
        {
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateResultDamageLiveTuningModifier(): powerProto == null");

            Span<float> damageValues = stackalloc float[(int)DamageType.NumDamageTypes];
            damageValues.Clear();

            // Calculate damage mitigation by defense
            foreach (var kvp in results.Properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                float damage = kvp.Value;
                if (damage == 0f)
                    continue;

                Property.FromParam(kvp.Key, 0, out int damageTypeValue);
                DamageType damageType = (DamageType)damageTypeValue;

                // Get base damage rating
                float defenseRating = target.GetDefenseRating(damageType);

                // Calculate defense penetration (it looks like defense penetration may not be used in 1.52, need to investigate this further)
                float defensePenetration = Properties[PropertyEnum.DefensePenetration, damageType];
                defensePenetration += Properties[PropertyEnum.DefensePenetration, DamageType.Any];

                // Keyworded penetration
                foreach (var kwdKdp in Properties.IteratePropertyRange(PropertyEnum.DefensePenetrationKwd, (int)damageType))
                {
                    Property.FromParam(kwdKdp.Key, 1, out PrototypeId keywordProtoRef);
                    if (powerProto.HasKeyword(keywordProtoRef.As<KeywordPrototype>()) == false)
                        continue;

                    defensePenetration += kvp.Value;
                }

                // Variable activation time penetration (all powers seem to use ResistancePenetrationZero for this in 1.52)
                TimeSpan activationTime = VariableActivationTime;
                if (activationTime > TimeSpan.Zero)
                {
                    SecondaryActivateOnReleasePrototype secondaryActivateProto = GetSecondaryActivateOnReleasePrototype();
                    if (secondaryActivateProto != null && secondaryActivateProto.DefensePenetrationIncrPerSec != CurveId.Invalid &&
                        secondaryActivateProto.DefensePenetrationType == damageType)
                    {
                        Curve curve = secondaryActivateProto.DefensePenetrationIncrPerSec.AsCurve();
                        if (curve == null) return Logger.WarnReturn(false, "CalculateResultDamageDefenseModifier(): curve == null");

                        float timePenetrationBase = curve.GetAt(Properties[PropertyEnum.PowerRank]);
                        float activationTimeMS = Math.Min((float)activationTime.TotalMilliseconds, secondaryActivateProto.MaxReleaseTimeMS);

                        defensePenetration += timePenetrationBase * activationTimeMS * 0.001f;
                    }
                }

                // Penetration pct
                float defensePenetrationPct = Properties[PropertyEnum.DefensePenetrationPct, damageType];
                defensePenetrationPct += Properties[PropertyEnum.DefensePenetrationPct, DamageType.Any];

                // Keyworded penetration pct
                foreach (var kwdKdp in Properties.IteratePropertyRange(PropertyEnum.DefensePenetrationPctKwd, (int)damageType))
                {
                    Property.FromParam(kwdKdp.Key, 1, out PrototypeId keywordProtoRef);
                    if (powerProto.HasKeyword(keywordProtoRef.As<KeywordPrototype>()) == false)
                        continue;

                    defensePenetrationPct += kvp.Value;
                }

                // Apply penetration (defense rating cannot become negative)
                if (defensePenetration != 0f || defensePenetrationPct != 0f)
                    Logger.Debug($"CalculateResultDamageDefenseModifier(): Found defense penetration for power {powerProto}");

                defenseRating = Math.Max(defenseRating - defensePenetration, 0f);
                defenseRating *= Math.Clamp(1f - defensePenetrationPct, 0f, 1f);

                // Apply damage reduction
                float damageReductionPct = target.GetDamageReductionPct(defenseRating, Properties, powerProto);
                float damageReductionMult = 1f - Math.Clamp(damageReductionPct, 0f, 1f);

                damageValues[(int)damageType] = damage * damageReductionMult;
            }

            // Set mitigated damage
            for (int i = 0; i < damageValues.Length; i++)
                results.Properties[PropertyEnum.Damage, i] = damageValues[i];

            return true;
        }

        private bool CalculateResultDamagePctResistModifier(PowerResults results, WorldEntity target)
        {
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateResultDamagePctResistModifier(): powerProto == null");

            Span<float> damageValues = stackalloc float[(int)DamageType.NumDamageTypes];
            damageValues.Clear();

            // Calculate damage mitigation by DamagePctResist
            foreach (var kvp in results.Properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                float damage = kvp.Value;
                if (damage == 0f)
                    continue;

                Property.FromParam(kvp.Key, 0, out int damageTypeValue);
                DamageType damageType = (DamageType)damageTypeValue;

                // DamagePctResist
                float damagePctResist = target.Properties[PropertyEnum.DamagePctResist, damageType];
                damagePctResist += target.Properties[PropertyEnum.DamagePctResist, DamageType.Any];

                // DamagePctResistFromGear
                damagePctResist += target.Properties[PropertyEnum.DamagePctResistFromGear, damageType];
                damagePctResist += target.Properties[PropertyEnum.DamagePctResistFromGear, DamageType.Any];

                // DamagePctResistVsPower / DamagePctResistVsPowerKeyword
                float damagePctResistVsPower = target.Properties[PropertyEnum.DamagePctResistVsPower, powerProto.DataRef];
                float damagePctResistVsPowerKeyword = 0f;

                foreach (var powerKeywordKvp in target.Properties.IteratePropertyRange(PropertyEnum.DamagePctResistVsPowerKeyword))
                {
                    Property.FromParam(powerKeywordKvp.Key, 0, out PrototypeId keywordProtoRef);
                    KeywordPrototype keywordProto = keywordProtoRef.As<KeywordPrototype>();
                    if (keywordProto == null)
                    {
                        Logger.Warn("CalculateResultDamagePctResistModifier(): keywordProto == null");
                        continue;
                    }

                    if (powerProto.HasKeyword(keywordProto))
                        damagePctResistVsPowerKeyword = Math.Max(damagePctResistVsPowerKeyword, powerKeywordKvp.Value);
                }

                damagePctResist += Math.Max(damagePctResistVsPower, damagePctResistVsPowerKeyword);

                // DamagePctResistFromAngle / DamagePctResistFromDistance
                if (target.IsInWorld)
                {
                    float damagePctResistFromPosition = 0f;

                    Vector3 ownerPosition = Power.IsMissileEffect(powerProto) ? UltimateOwnerPosition : PowerOwnerPosition;

                    foreach (var angleKvp in target.Properties.IteratePropertyRange(PropertyEnum.DamagePctResistFromAngle))
                    {
                        Property.FromParam(angleKvp.Key, 0, out int angle);
                        if (WorldEntity.CheckWithinAngle(target.RegionLocation.Position, target.Forward, ownerPosition, angle))
                            damagePctResistFromPosition = Math.Max(damagePctResistFromPosition, angleKvp.Value);
                    }

                    foreach (var distanceKvp in target.Properties.IteratePropertyRange(PropertyEnum.DamagePctResistFromDistance))
                    {
                        Property.FromParam(distanceKvp.Key, 0, out int distanceThreshold);
                        if (distanceThreshold <= 0)
                            continue;

                        float distanceSquared = Vector3.DistanceSquared(target.RegionLocation.Position, ownerPosition);
                        if (distanceSquared > (distanceThreshold * distanceThreshold))
                            damagePctResistFromPosition = Math.Max(damagePctResistFromPosition, distanceKvp.Value);
                    }

                    damagePctResist += damagePctResistFromPosition;
                }
                else
                {
                    Logger.Warn("CalculateResultDamagePctResistModifier(): target.IsInWorld == false");
                }

                // DamagePctResistVsRank
                float damagePctResistVsRank = 0f;
                PrototypeId powerOwnerRankProtoRef = Properties[PropertyEnum.Rank];
                foreach (var rankKvp in target.Properties.IteratePropertyRange(PropertyEnum.DamagePctResistVsRank))
                {
                    Property.FromParam(rankKvp.Key, 0, out PrototypeId paramRankProtoRef);
                    if (powerOwnerRankProtoRef == paramRankProtoRef)
                        damagePctResistVsRank = Math.Max(damagePctResistVsRank, rankKvp.Value);
                }

                damagePctResist += Math.Clamp(damagePctResistVsRank, 0f, 1f);

                // Apply damage pct resist
                float damagePctResistMult = 1f - Math.Clamp(damagePctResist, 0f, 1f);

                damageValues[(int)damageType] = damage * damagePctResistMult;
            }

            // Set mitigated damage
            for (int i = 0; i < damageValues.Length; i++)
                results.Properties[PropertyEnum.Damage, i] = damageValues[i];

            return true;
        }

        private bool CalculateResultDamageShieldModifier(PowerResults results, WorldEntity target)
        {
            ConditionCollection conditionCollection = target.ConditionCollection;
            if (conditionCollection == null)
                return true;

            List<ulong> conditionCheckList = ListPool<ulong>.Instance.Get();

            foreach (Condition condition in conditionCollection.IterateConditions(true))
            {
                bool expirationCheckNeeded = false;
                PropertyCollection conditionProperties = condition.Properties;

                for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
                {
                    float damageTotal = results.Properties[PropertyEnum.Damage, damageType];
                    if (damageTotal <= 0f)
                        continue;

                    float damageShieldPercent = conditionProperties[PropertyEnum.DamageShieldPercent, damageType];
                    damageShieldPercent += conditionProperties[PropertyEnum.DamageShieldPercent, DamageType.Any];
                    if (damageShieldPercent == 0f)
                        continue;

                    float damageShielded = damageTotal * damageShieldPercent;

                    expirationCheckNeeded |= ApplyDamageToShield(target, conditionProperties, damageType, damageTotal, ref damageShielded);

                    if (conditionProperties[PropertyEnum.DamageShieldRegensFromDamage, damageType] == false)
                        expirationCheckNeeded |= ApplyDamageToShield(target, conditionProperties, DamageType.Any, damageTotal, ref damageShielded);

                    results.Properties[PropertyEnum.Damage, damageType] = damageTotal - damageShielded;
                }

                if (expirationCheckNeeded)
                    conditionCheckList.Add(condition.Id);
            }

            while (conditionCheckList.Count > 0)
            {
                int index = conditionCheckList.Count - 1;
                Condition condition = conditionCollection.GetCondition(conditionCheckList[index]);
                conditionCheckList.RemoveAt(index);

                if (condition == null)
                    continue;

                PropertyCollection conditionProperties = condition.Properties;

                bool isExpired = false;

                for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
                    isExpired |= CheckDamageShieldExpiration(target, conditionProperties, damageType);

                isExpired |= CheckDamageShieldExpiration(target, conditionProperties, DamageType.Any);

                if (isExpired && conditionProperties[PropertyEnum.DamageShieldRemoveWhenExpired])
                    results.AddConditionToRemove(condition.Id);
            }

            ListPool<ulong>.Instance.Return(conditionCheckList);
            return true;
        }

        private void CalculateResultDamageConversion(PowerResults results, WorldEntity target, float difficultyMult)
        {
            // Prioritize ultimate owner for damage conversion
            WorldEntity user = Game.EntityManager.GetEntity<WorldEntity>(UltimateOwnerId);
            user ??= Game.EntityManager.GetEntity<WorldEntity>(PowerOwnerId);

            for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
            {
                float damage = results.Properties[PropertyEnum.Damage, damageType];
                if (damage == 0f)
                    continue;

                float convertedDamage = target.ApplyDamageConversion(damage, damageType, results, user, Properties, difficultyMult);
                if (convertedDamage != damage)
                    results.Properties[PropertyEnum.Damage, damageType] = convertedDamage;
            }
        }

        private bool CalculateResultDamageMetaGameModifier(PowerResults results, WorldEntity target)
        {
            float damageMetaGameBossResistance = target.Properties[PropertyEnum.DamageMetaGameBossResistance];
            if (damageMetaGameBossResistance == 0f)
                return true;

            float metaGameMult = 1f - damageMetaGameBossResistance;

            // NOTE: damageMetaGameBossResistance > 0f = damage reduction
            //       damageMetaGameBossResistance < 0f = damage increase
            if (damageMetaGameBossResistance > 0f)
            {
                metaGameMult += Properties[PropertyEnum.DamageMetaGameBossPenetration];
                metaGameMult = Math.Clamp(metaGameMult, 0f, 1f);
            }

            ApplyDamageMultiplier(results.Properties, metaGameMult);
            return true;
        }

        private bool CalculateResultDamagePvPReduction(PowerResults results, WorldEntity target)
        {
            Region region = target.Region;
            if (region == null) return Logger.WarnReturn(false, "CalculateResultDamagePvPBoost(): region == null");

            PvP pvp = region.GetPvPMatch();
            if (pvp == null)
                return true;

            // This is dumb and should probably never be enabled.
            if (Game.CustomGameOptions.ApplyHiddenPvPDamageModifiers == false)
                return true;

            Avatar avatar = target.GetSelfOrOwnerOfType<Avatar>();
            if (avatar == null)
                return true;

            PvPPrototype pvpProto = pvp.PvPPrototype;
            PropertyCollection avatarProps = avatar.Properties;

            float damageReduction = 1f;

            damageReduction *= pvpProto.GetDamageReductionForKDPct(avatarProps[PropertyEnum.PvPRecentKDRatio]);
            damageReduction *= pvpProto.GetDamageReductionForNoobs(avatarProps[PropertyEnum.PvPMatchCount]);
            damageReduction *= pvpProto.GetDamageReductionForWinPct(avatarProps[PropertyEnum.PvPRecentWinLossRatio]);

            if (Game.InfinitySystemEnabled == false)
            {
                Player player = avatar.GetOwnerOfType<Player>();
                if (player != null)
                {
                    float omegaPct = (float)player.GetOmegaPoints() / GameDatabase.AdvancementGlobalsPrototype.OmegaPointsCap;
                    damageReduction *= pvpProto.GetDamageReductionForOmegaPct(omegaPct);
                }
            }

            ApplyDamageMultiplier(results.Properties, damageReduction);
            return true;
        }

        private bool CalculateResultDamageLevelScaling(PowerResults results, WorldEntity target, float difficultyMult)
        {
            // Calculate player->mob damage scaling
            float levelScalingMult = 1f;
            bool isPlayerToMob = IsPlayerPayload && target.CanBePlayerOwned() == false;

            if (CombatLevel != target.CombatLevel && isPlayerToMob)
            {
                long unscaledTargetHealthMax = target.Properties[PropertyEnum.HealthMax];
                long scaledTargetHealthMax = CalculateTargetHealthMaxForCombatLevel(target, CombatLevel);
                levelScalingMult = MathHelper.Ratio(unscaledTargetHealthMax, scaledTargetHealthMax);
            }

            Span<float> damageValues = stackalloc float[(int)DamageType.NumDamageTypes];
            GetDamageValues(results.Properties, damageValues);

            for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
            {
                // NOTE: Damage numbers sent to the client are faked to make it seem like
                // mob health changes due to difficulty / level scaling, but it is actually
                // player damage getting reduced.

                float damage = damageValues[(int)damageType];

                // Apply player->mob damage scaling
                if (levelScalingMult != 1f)
                    results.Properties[PropertyEnum.Damage, damageType] = damage * levelScalingMult;

                // Hide the difficulty multiplier
                damage /= difficultyMult;

                // Set fake client damage
                results.SetDamageForClient(damageType, damage);
            }

            return true;
        }

        private bool CalculateResultDamageTransfer(PowerResults results, WorldEntity target)
        {
            EntityManager entityManager = Game.EntityManager;

            Span<float> transferredDamageCurrent = stackalloc float[(int)DamageType.NumDamageTypes];
            Span<float> transferredDamageCurrentClient = stackalloc float[(int)DamageType.NumDamageTypes];

            Span<float> transferredDamageTotal = stackalloc float[(int)DamageType.NumDamageTypes];
            transferredDamageTotal.Clear();

            // Apply damage transfer from conditions
            foreach (Condition condition in target.ConditionCollection)
            {
                ulong transferTargetId = condition.Properties[PropertyEnum.DamageTransferID];
                if (transferTargetId == Entity.InvalidId)
                    continue;

                // Transferring to itself can cause a loop
                if (transferTargetId == target.Id)
                {
                    Logger.Warn($"CalculateResultDamageTransfer(): Target [{target}] is attempting to transfer damage to itself");
                    continue;
                }

                WorldEntity transferTarget = entityManager.GetEntity<WorldEntity>(transferTargetId);
                if (transferTarget == null || transferTarget.IsInWorld == false)
                    continue;

                // Check transfer chance
                if (Game.Random.NextFloat() > condition.Properties[PropertyEnum.DamageTransferChance])
                    continue;

                results.TransferToId = transferTargetId;

                float damageTransferPct = condition.Properties[PropertyEnum.DamageTransferPct];
                if (damageTransferPct <= 0f)
                    continue;

                // Accumulate damage transfer
                float damageTransferMax = condition.Properties[PropertyEnum.DamageTransferMax];

                transferredDamageCurrent.Clear();
                transferredDamageCurrentClient.Clear();

                foreach (var kvp in results.Properties.IteratePropertyRange(PropertyEnum.Damage))
                {
                    Property.FromParam(kvp.Key, 0, out int damageTypeValue);
                    DamageType damageType = (DamageType)damageTypeValue;

                    float damage = kvp.Value;
                    transferredDamageCurrent[damageTypeValue] = damage * damageTransferPct;

                    if (damageTransferMax > 0f)
                        transferredDamageCurrent[damageTypeValue] = MathHelper.ClampNoThrow(transferredDamageCurrent[damageTypeValue], 0f, damageTransferMax);

                    if (transferredDamageCurrent[damageTypeValue] <= 0f)
                        continue;

                    transferredDamageTotal[damageTypeValue] += transferredDamageCurrent[damageTypeValue];

                    float damageForClient = results.GetDamageForClient(damageType);
                    float damageForClientRatio = transferredDamageCurrent[damageTypeValue] / damage;
                    transferredDamageCurrentClient[damageTypeValue] = damageForClient * damageForClientRatio;
                }

                // Apply damage transfer
                PowerResults transferResults = new();

                transferResults.Init(results.PowerOwnerId, results.UltimateOwnerId, transferTarget.Id,
                    results.PowerOwnerPosition, results.PowerPrototype, results.PowerAssetRefOverride, true);
                transferResults.SetKeywordsMask(KeywordsMask);
                transferResults.SetFlag(PowerResultFlags.OverTime, results.TestFlag(PowerResultFlags.OverTime));

                for (DamageType damageType = 0; damageType < DamageType.NumDamageTypes; damageType++)
                {
                    transferResults.Properties[PropertyEnum.Damage, damageType] = transferredDamageCurrent[(int)damageType];
                    transferResults.SetDamageForClient(damageType, transferredDamageCurrentClient[(int)damageType]);
                }

                transferTarget.ApplyDamageTransferPowerResults(transferResults);
            }

            // Remove transferred damage from the results for this target
            for (DamageType type = 0; type < DamageType.NumDamageTypes; type++)
            {
                float transferredDamageForType = transferredDamageTotal[(int)type];
                if (transferredDamageForType <= 0f)
                    continue;

                float damage = results.Properties[PropertyEnum.Damage, type];
                if (damage <= 0f)
                    continue;

                // Reduce actual damage
                results.Properties[PropertyEnum.Damage, type] = damage - transferredDamageForType;

                // Reduce client facing fake damage
                float transferRatio = Math.Min(1f - (transferredDamageForType / damage), 1f);
                float damageForClient = results.GetDamageForClient(type) * transferRatio;
                results.SetDamageForClient(type, damageForClient);
            }

            return true;
        }

        private bool CalculateResultHealing(PowerResults results, WorldEntity target)
        {
            // Check if our target can receive healing

            // DisableHealthGain has the highest priority
            if (target.Properties[PropertyEnum.DisableHealthGain])
                return false;

            // CanHeal can be overriden with PowerForceHealing
            if (target.CanHeal == false && Properties[PropertyEnum.PowerForceHealing] == false)
                return false;

            // Calculate healing amount

            // Start with the previously calculated base healing value
            float healing = Properties[PropertyEnum.Healing];

            // Apply target-specific multiplier
            float targetHealingReceivedMult = target.Properties[PropertyEnum.HealingReceivedMult];

            // Accumulate keyword-based multiplier bonus if we have a power (there may not be one if this is a ticker payload)
            if (PowerProtoRef != PrototypeId.Invalid)
            {
                foreach (var kvp in target.Properties.IteratePropertyRange(PropertyEnum.HealingReceivedMultPowerKeyword))
                {
                    Property.FromParam(kvp.Key, 0, out PrototypeId keywordProtoRef);

                    if (KeywordsMask.HasKeyword(keywordProtoRef.As<KeywordPrototype>()))
                        targetHealingReceivedMult += kvp.Value;
                }
            }

            healing *= 1f + targetHealingReceivedMult;

            // Pct-based healing
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

        private bool CalculateResultResourceChanges(PowerResults results, WorldEntity target)
        {
            // Primary resource (endurance / spirit)
            for (ManaType manaType = 0; manaType < ManaType.NumTypes; manaType++)
            {
                float enduranceChange = Properties[PropertyEnum.EnduranceChange, manaType];
                enduranceChange += Properties[PropertyEnum.EnduranceChange, ManaType.TypeAll];

                float enduranceChangePct = Properties[PropertyEnum.EnduranceChangePct, manaType];
                enduranceChangePct += Properties[PropertyEnum.EnduranceChangePct, ManaType.TypeAll];

                if (enduranceChangePct != 0f)
                    enduranceChange += target.Properties[PropertyEnum.EnduranceMax] * enduranceChangePct;

                results.Properties[PropertyEnum.EnduranceChange, manaType] = enduranceChange;
            }

            // Secondary resource
            float secondaryResourceChange = Properties[PropertyEnum.SecondaryResourceChange];

            float secondaryResourceChangePct = Properties[PropertyEnum.SecondaryResourceChangePct];
            if (secondaryResourceChangePct != 0f)
                secondaryResourceChange += target.Properties[PropertyEnum.SecondaryResourceMax] * secondaryResourceChangePct;

            results.Properties[PropertyEnum.SecondaryResourceChange] = secondaryResourceChange;

            return true;
        }

        private bool CalculateResultDamageAccumulation(PowerResults results)
        {
            // Start with the precalculated damage accumulation change (e.g. from an over time effect)
            results.Properties.CopyPropertyRange(Properties, PropertyEnum.DamageAccumulationChange);

            // Apply base change - is this used?
            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.DamageAccumulationChangeBase))
            {
                Property.FromParam(kvp.Key, 0, out int damageTypeValue);
                DamageType damageType = (DamageType)damageTypeValue;

                results.Properties.AdjustProperty((float)kvp.Value, new(PropertyEnum.DamageAccumulationChange, damageType));
            }

            return true;
        }

        private bool CalculateResultConditionsToAdd(PowerResults results, WorldEntity target, bool calculateForTarget)
        {
            if (PowerPrototype.AppliesConditions == null && PowerPrototype.ConditionsByRef.IsNullOrEmpty())
                return true;

            ConditionCollection conditionCollection = target?.ConditionCollection;
            if (conditionCollection == null) return Logger.WarnReturn(false, "CalculateResultConditionsToAdd(): conditionCollection == null");

            WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(results.PowerOwnerId);
            WorldEntity ultimateOwner = Game.EntityManager.GetEntity<WorldEntity>(results.UltimateOwnerId);

            if (PowerPrototype.AppliesConditions != null || PowerPrototype.ConditionsByRef.HasValue())
            {
                TimeSpan? movementDuration = null;
                if (CalculateMovementDurationForCondition(target, calculateForTarget, out TimeSpan movementDurationValue))
                    movementDuration = movementDurationValue;

                // Early out if this movement power doesn't have a movement duration available
                if (PowerPrototype is MovementPowerPrototype movementPowerProto && movementPowerProto.IsTravelPower == false && movementDuration.HasValue == false)
                    return true;

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

                        CalculateResultConditionsToAddHelper(results, target, owner, ultimateOwner, calculateForTarget,
                            conditionCollection, mixinConditionProto, movementDuration);
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

                        CalculateResultConditionsToAddHelper(results, target, owner, ultimateOwner, calculateForTarget,
                            conditionCollection, conditionByRefProto, movementDuration);
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
            }

            return true;
        }

        private bool CalculateResultConditionsToAddHelper(PowerResults results, WorldEntity target, WorldEntity owner, WorldEntity ultimateOwner,
            bool calculateForTarget, ConditionCollection conditionCollection, ConditionPrototype conditionProto, TimeSpan? movementDuration)
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

            // Calculate conditions properties (these will be shared by all stacks)
            using PropertyCollection conditionProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            Condition.GenerateConditionProperties(conditionProperties, conditionProto, Properties, owner ?? ultimateOwner, target, Game);

            // Calculate duration
            if (CalculateResultConditionDuration(results, target, owner, calculateForTarget, conditionProto, conditionProperties, movementDuration, out TimeSpan conditionDuration) == false)
                return false;

            // Calculate the number of stacks to apply and modify duration if needed
            int numStacksToApply = CalculateConditionNumStacksToApply(target, ultimateOwner, conditionCollection, conditionProto, ref conditionDuration);

            // Apply the calculated number of stacks
            for (int i = 0; i < numStacksToApply; i++)
            {
                Condition condition = ConditionCollection.AllocateCondition();
                condition.InitializeFromPower(conditionCollection.NextConditionId, this, conditionProto, conditionDuration, conditionProperties);
                CalculateResultConditionExtraProperties(results, target, condition);    // Sets properties specific to this stack
                results.AddConditionToAdd(condition);
            }

            return true;
        }

        private void CalculateResultNegativeStatusRemoval(PowerResults results, WorldEntity target)
        {
            List<ulong> negativeStatusConditionsToRemove = ListPool<ulong>.Instance.Get();
            ConditionCollection conditionCollection = target.ConditionCollection;

            float negStatusClearChancePctAll = Properties[PropertyEnum.PowerClearsNegStatusChancePctAll];
            if (negStatusClearChancePctAll > 0f)
            {
                // Roll for all possible negative statuses
                foreach (PrototypeId negativeStatusProtoRef in GameDatabase.GlobalsPrototype.NegStatusEffectList)
                {
                    float netStatusClearChance = negStatusClearChancePctAll + Properties[PropertyEnum.PowerClearsNegStatusChancePct, negativeStatusProtoRef];

                    if (netStatusClearChance == 0f)
                        continue;

                    if (Game.Random.NextFloat() < netStatusClearChance)
                        conditionCollection.GetNegativeStatusConditions(negativeStatusProtoRef, negativeStatusConditionsToRemove);
                }
            }
            else
            {
                // Roll individually for each status effect property
                foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.PowerClearsNegStatusChancePct))
                {
                    float netStatusClearChance = kvp.Value;

                    if (netStatusClearChance == 0f)
                        continue;

                    Property.FromParam(kvp.Key, 0, out PrototypeId negativeStatusProtoRef);

                    if (Game.Random.NextFloat() < netStatusClearChance)
                        conditionCollection.GetNegativeStatusConditions(negativeStatusProtoRef, negativeStatusConditionsToRemove);
                }
            }

            foreach (ulong conditionId in negativeStatusConditionsToRemove)
                results.AddConditionToRemove(conditionId);

            ListPool<ulong>.Instance.Return(negativeStatusConditionsToRemove);
        }

        private bool CalculateResultConditionDuration(PowerResults results, WorldEntity target, WorldEntity owner, bool calculateForTarget,
            ConditionPrototype conditionProto, PropertyCollection conditionProperties, TimeSpan? movementDuration, out TimeSpan conditionDuration)
        {
            conditionDuration = conditionProto.GetDuration(Properties, owner, PowerProtoRef, target);

            if ((PowerPrototype is MovementPowerPrototype movementPowerProto && movementPowerProto.IsTravelPower == false) ||
                (conditionProto.Properties != null && conditionProto.Properties[PropertyEnum.Knockback]))
            {
                // Movement and knockback condition last for as long as the movement is happening
                if (movementDuration.HasValue)
                {
                    if (movementDuration <= TimeSpan.Zero)
                        return Logger.WarnReturn(false, $"CalculateResultConditionDuration(): Calculated movement duration is <= TimeSpan.Zero, which would result in an infinite condition.\nowner=[{owner}]\ntarget=[{target}]");

                    conditionDuration = movementDuration.Value;
                }
                else
                {
                    return false;
                }
            }

            if (conditionDuration > TimeSpan.Zero)
            {
                // Finite conditions

                if (calculateForTarget)
                {
                    // Resist only targeted conditions
                    ApplyConditionDurationResistances(target, conditionProto, conditionProperties, ref conditionDuration);

                    if (conditionDuration > TimeSpan.Zero)
                    {
                        // Make sure the condition is at least 1 ms long to avoid rounding to 0, turning it into an infinite condition
                        conditionDuration = Clock.Max(conditionDuration, TimeSpan.FromMilliseconds(1));
                    }
                    else
                    {
                        results.SetFlag(PowerResultFlags.Resisted, true);
                        return false;
                    }
                }

                // Apply bonuses to everything
                ApplyConditionDurationBonuses(ref conditionDuration);
            }
            else if (conditionDuration == TimeSpan.Zero)
            {
                // Infinite conditions

                // Check if this condition can be applied (for targeted conditions only)
                if (calculateForTarget)
                {
                    bool canApply = true;

                    List<PrototypeId> negativeStatusList = ListPool<PrototypeId>.Instance.Get();
                    if (Condition.IsANegativeStatusEffect(conditionProperties, negativeStatusList))
                    {
                        if (CanApplyConditionToTarget(target, conditionProperties, negativeStatusList) == false)
                        {
                            results.SetFlag(PowerResultFlags.Resisted, true);
                            canApply = false;
                        }
                    }

                    ListPool<PrototypeId>.Instance.Return(negativeStatusList);
                    if (canApply == false)
                        return false;
                }

                // Needs to have an owner that can remove it
                if (owner == null)
                    return false;

                // If this is a hotspot condition, make sure the target is still being overlapped
                if (owner is Hotspot hotspot && hotspot.IsOverlappingPowerTarget(target.Id) == false)
                    return false;

                // Do not apply self-targeted conditions if its creator power is no longer available and it removes conditions on end
                PowerPrototype powerProto = PowerPrototype;
                if (owner.Id == target.Id && owner.GetPower(PowerProtoRef) == null && (powerProto.CancelConditionsOnEnd || powerProto.CancelConditionsOnUnassign))
                    return false;
            }
            else
            {
                // Negative duration should never happen
                return Logger.WarnReturn(false, $"CalculateConditionDuration(): Negative duration for {PowerPrototype}");
            }

            return true;
        }

        private bool CalculateResultConditionExtraProperties(PowerResults results, WorldEntity target, Condition condition)
        {
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateResultConditionExtraProperties(): powerProto == null");

            PropertyCollection conditionProps = condition.Properties;

            // NoEntityCollideException
            if (powerProto is MovementPowerPrototype movementPowerProto)
            {
                if (movementPowerProto.UserNoEntityCollide && movementPowerProto.NoCollideIncludesTarget == false)
                    conditionProps[PropertyEnum.NoEntityCollideException] = TargetId;
            }

            // Knockback
            if (conditionProps[PropertyEnum.Knockback])
                CalculateResultConditionKnockbackProperties(results, target, condition);

            // Taunt
            if (conditionProps[PropertyEnum.Taunted] && PowerOwnerId != Entity.InvalidId)
                conditionProps[PropertyEnum.TauntersID] = PowerOwnerId;

            // Damage Transfer
            if (conditionProps[PropertyEnum.DamageTransferChance] > 0f)
                conditionProps[PropertyEnum.DamageTransferID] = PowerOwnerId;

            // InformsHitInfoToAlly
            if (conditionProps[PropertyEnum.InformsHitInfoToAlly])
            {
                // This appears to be unused, log this in case it somehow pops up somewhere.
                Logger.Debug($"CalculateResultConditionExtraProperties(): InformsHitInfoToAlly on target [{target}]");
                conditionProps[PropertyEnum.InformsHitInfoToAllyId] = UltimateOwnerId;
            }

            // TargetedCritBonus
            if (conditionProps[PropertyEnum.TargetedCritBonus] > 0f)
                conditionProps[PropertyEnum.TargetedCritBonusId] = UltimateOwnerId;

            // XPTransfer
            if (conditionProps[PropertyEnum.XPTransfer])
                conditionProps[PropertyEnum.XPTransferToID] = UltimateOwnerId;

            // Procs
            CalculateResultConditionProcProperties(results, target, condition.Properties);

            // Add a reference to this payload if there is anything that needs ticking after all properties are set
            if (conditionProps.HasOverTimeProperties())
                condition.PropertyTickerPayload = this;

            return true;
        }

        private bool CalculateResultConditionKnockbackProperties(PowerResults results, WorldEntity target, Condition condition)
        {
            // powerProto is validated in CalculateResultConditionExtraProperties() above
            PowerPrototype powerProto = PowerPrototype;

            float knockbackDistance = 0f;
            Vector3 knockbackSourcePosition = Vector3.Zero;

            if (powerProto is MovementPowerPrototype)
            {
                if (target.Id != PowerOwnerId)
                {
                    Vector3 offsetFromTarget = TargetPosition - TargetEntityPosition;
                    knockbackDistance = Vector3.Length2D(offsetFromTarget);
                    knockbackSourcePosition = TargetEntityPosition - offsetFromTarget;
                }
            }
            else
            {
                knockbackDistance = Power.GetKnockbackDistance(target, PowerOwnerId, powerProto, Properties);

                if (Power.TargetsAOE(powerProto) && Power.IsOwnerCenteredAOE(powerProto) == false)
                    knockbackSourcePosition = TargetPosition;
                else
                    knockbackSourcePosition = Properties[PropertyEnum.KnockbackSourceUseUltimateOwner] ? UltimateOwnerPosition : PowerOwnerPosition;
            }

            float movementSpeedOverrideBase = Properties[PropertyEnum.MovementSpeedOverride];
            if (movementSpeedOverrideBase <= 0f) return Logger.WarnReturn(false, "CalculateResultConditionExtraProperties(): movementSpeedOverrideBase <= 0f");

            float knockbackTimeBase = MathF.Abs(knockbackDistance) / movementSpeedOverrideBase;
            if (Segment.IsNearZero(knockbackTimeBase))
                return false;

            // knockbackTime is adjusted for condition resistance compared to base
            float knockbackTimeResult = MathF.Min((float)condition.Duration.TotalSeconds, knockbackTimeBase);
            float knockbackSpeedResult;
            float knockbackAccelerationResult;

            float conditionMovementSpeedOverride;

            switch ((int)Properties[PropertyEnum.KnockbackMovementType])
            {
                default:    // Constant
                    knockbackAccelerationResult = 0f;
                    knockbackSpeedResult = knockbackDistance / knockbackTimeBase;
                    conditionMovementSpeedOverride = movementSpeedOverrideBase;
                    break;

                case 1:     // Accelerate
                    knockbackAccelerationResult = 2f * knockbackDistance / (knockbackTimeBase * knockbackTimeBase);
                    knockbackSpeedResult = 0f;
                    conditionMovementSpeedOverride = MathF.Abs(knockbackAccelerationResult * knockbackTimeResult);
                    break;

                case 2:     // Decelerate
                    knockbackAccelerationResult = -2f * knockbackDistance / (knockbackTimeBase * knockbackTimeBase);
                    knockbackSpeedResult = -knockbackAccelerationResult * knockbackTimeResult;
                    conditionMovementSpeedOverride = movementSpeedOverrideBase;
                    break;
            }

            // Record knockback in the results for it to be applied via entity physics
            results.Properties[PropertyEnum.Knockback] = true;
            results.Properties[PropertyEnum.KnockbackTimeResult] = knockbackTimeResult;
            results.Properties[PropertyEnum.KnockbackSpeedResult] = knockbackSpeedResult;
            results.Properties[PropertyEnum.KnockbackAccelerationResult] = knockbackAccelerationResult;
            results.Properties.CopyProperty(Properties, PropertyEnum.KnockbackReverseTargetOri);
            results.KnockbackSourcePosition = knockbackSourcePosition;

            condition.Properties[PropertyEnum.MovementSpeedOverride] = conditionMovementSpeedOverride;

            return true;
        }

        private void CalculateResultConditionProcProperties(PowerResults results, WorldEntity target, PropertyCollection conditionProperties)
        {
            // Store properties to set in a temporary dictionary to avoid modifying property collections during iteration
            Dictionary<PropertyId, PropertyValue> propertiesToSet = DictionaryPool<PropertyId, PropertyValue>.Instance.Get();

            // Triggering refs and ranks
            int rank = conditionProperties[PropertyEnum.PowerRank];
            foreach (var kvp in conditionProperties.IteratePropertyRange(Property.ProcPropertyTypesAll))
            {
                Property.FromParam(kvp.Key, 1, out PrototypeId procPowerProtoRef);
                propertiesToSet[new(PropertyEnum.TriggeringPowerRef, procPowerProtoRef)] = PowerProtoRef;
                propertiesToSet[new(PropertyEnum.ProcPowerRank, procPowerProtoRef)] = rank;
            }

            // Caster (user) overrides
            foreach (var kvp in conditionProperties.IteratePropertyRange(PropertyEnum.ProcActivatedByCondCreator))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId procPowerProtoRef);
                propertiesToSet[new(PropertyEnum.ProcCasterOverride, procPowerProtoRef)] = UltimateOwnerId;
            }

            // Target overrides
            foreach (var kvp in conditionProperties.IteratePropertyRange(PropertyEnum.ProcTargetsConditionCreator))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId procPowerProtoRef);
                propertiesToSet[new(PropertyEnum.ProcTargetOverride, procPowerProtoRef)] = UltimateOwnerId;
            }

            foreach (var kvp in conditionProperties.IteratePropertyRange(PropertyEnum.ProcTargetsConditionOwner))
            {
                Property.FromParam(kvp.Key, 0, out PrototypeId procPowerProtoRef);
                propertiesToSet[new(PropertyEnum.ProcTargetOverride, procPowerProtoRef)] = target.Id;
            }

            // Set properties
            foreach (var kvp in propertiesToSet)
                conditionProperties[kvp.Key] = kvp.Value;

            DictionaryPool<PropertyId, PropertyValue>.Instance.Return(propertiesToSet);
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

                KeywordPrototype keywordProto = keywordProtoRef.As<KeywordPrototype>();

                removedAny |= CalculateResultConditionsToRemoveHelper(results, conditionCollection, ConditionFilter.IsConditionWithKeywordFunc, keywordProto, maxStacksToRemove);
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

        private void CalculateResultTeleport(PowerResults results, WorldEntity target)
        {
            if (target.Properties[PropertyEnum.NoForcedMovement]) return;
            if (target.Locomotor == null) return;

            foreach (var kvp in Properties.IteratePropertyRange(PropertyEnum.Teleport))
            {
                float distance = kvp.Value;
                if (distance < 0) return;

                Property.FromParam(kvp.Key, 0, out int ownerId);

                var targetLocation = target.RegionLocation;
                var region = targetLocation.Region;
                if (region == null) return;

                bool teleport;
                var teleportRef = GameDatabase.PropertyInfoTable.LookupPropertyInfo(PropertyEnum.Teleport).PrototypeDataRef;

                if (Properties[PropertyEnum.IgnoreNegativeStatusResist])
                    teleport = true;
                else if (target.Properties[PropertyEnum.CCResistAlwaysAll] || target.Properties[PropertyEnum.CCResistAlways, teleportRef])
                    teleport = false;
                else
                {
                    int ccResistScore = target.Properties[PropertyEnum.CCResistScore, teleportRef] + target.Properties[PropertyEnum.CCResistScoreAll];
                    float resistPercent = target.GetNegStatusResistPercent(ccResistScore, Properties);
                    teleport = resistPercent != 1.0f;
                }

                if (teleport)
                {
                    var targetPositon = targetLocation.Position;
                    var ownerPosition = PowerOwnerPosition;

                    var startPosition = ownerId != 0 ? ownerPosition : targetPositon;

                    var dir = Vector3.Normalize2D(targetPositon - ownerPosition);
                    targetPositon = startPosition + dir * distance;
                    var teleportPositon = targetPositon;

                    var result = region.NaviMesh.FindPointOnLineToOccupy(ref teleportPositon, startPosition, targetPositon,
                        distance, target.Bounds, target.Locomotor.PathFlags, BlockingCheckFlags.CheckGroundMovementPowers, false);

                    if (result != Navi.PointOnLineResult.Failed)
                    {
                        results.Flags |= PowerResultFlags.Teleport;
                        results.TeleportPosition = teleportPositon;
                    }
                }

                return;
            }
        }

        private bool CalculateResultHitReaction(PowerResults results, WorldEntity target)
        {
            // Only agents can have hit reactions
            if (target is not Agent targetAgent)
                return false;

            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateResultHitReaction(): powerProto == null");

            // Check if this power can cause a hit react to this particular target
            if (Power.CanCauseHitReact(powerProto, targetAgent) == false)
                return false;

            // Check if there are any conditions that will be added that override hit reacts
            for (int i = 0; i < results.ConditionAddList.Count; i++)
            {
                if (results.ConditionAddList[i].OverridesHitReactConditions())
                    return false;
            }

            // Check if there is any damage
            bool hasDamage = false;

            foreach (var kvp in results.Properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                if (kvp.Value > 0f)
                {
                    hasDamage = true;
                    break;
                }
            }

            if (hasDamage == false)
                return false;

            // Check eval
            EvalPrototype interruptChanceFormula = GameDatabase.CombatGlobalsPrototype.EvalInterruptChanceFormulaPrototype; 

            using EvalContextData evalContext = ObjectPoolManager.Instance.Get<EvalContextData>();
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, Properties);
            evalContext.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Entity, target.Properties);
            evalContext.SetReadOnlyVar_ProtoRefVectorPtr(EvalContext.Var1, powerProto.Keywords);

            if (Eval.RunBool(interruptChanceFormula, evalContext) == false)
                return false;

            // All checks passed, now we add the hit reaction condition

            // agentProto should have already been validated in Power.CanCauseHitReact()
            ConditionPrototype conditionProto = targetAgent.AgentPrototype.HitReactCondition.As<ConditionPrototype>();
            if (conditionProto == null) return Logger.WarnReturn(false, "CalculateResultHitReaction(): conditionProto == null");

            ConditionCollection conditionCollection = targetAgent.ConditionCollection;

            WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(PowerOwnerId);
            WorldEntity ultimateOwner = Game.EntityManager.GetEntity<WorldEntity>(UltimateOwnerId);

            // Generate condition data
            TimeSpan duration = conditionProto.GetDuration(Properties, ultimateOwner);

            using PropertyCollection conditionProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            Condition.GenerateConditionProperties(conditionProperties, conditionProto, Properties, owner, target, Game);

            // Create, initialize, and add the condition
            Condition condition = ConditionCollection.AllocateCondition();
            condition.InitializeFromPower(conditionCollection.NextConditionId, this, conditionProto, duration, conditionProperties, false);
            results.AddConditionToAdd(condition);

            targetAgent.StartHitReactionCooldown();

            return true;
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
        /// Retrieves damage values from a <see cref="PropertyCollection"/> and writes them to the provided <see cref="Span{T}"/>.
        /// </summary>
        private static void GetDamageValues(PropertyCollection properties, Span<float> damage)
        {
            damage.Clear();
            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.Damage))
            {
                Property.FromParam(kvp.Key, 0, out int damageType);
                if (damageType >= damage.Length)
                    continue;

                damage[damageType] = kvp.Value;
            }
        }

        /// <summary>
        /// Applies the provided multiplier to all <see cref="PropertyEnum.Damage"/> properties on this <see cref="PowerPayload"/>.
        /// </summary>
        private static void ApplyDamageMultiplier(PropertyCollection properties, float multiplier)
        {
            // No need to apply multipliers of 1
            if (Segment.EpsilonTest(multiplier, 1f))
                return;

            // Store damage values in a temporary span so that we don't modify the collection while iterating
            // Remove this if our future optimized implementation does not require this.
            int numDamageTypes = (int)DamageType.NumDamageTypes;
            Span<float> damageValues = stackalloc float[numDamageTypes];
            GetDamageValues(properties, damageValues);

            for (int i = 0; i < numDamageTypes; i++)
            {
                float damage = damageValues[i];
                if (damage == 0f)
                    continue;

                properties[PropertyEnum.Damage, i] = damage * multiplier;
            }
        }

        /// <summary>
        /// Copies all curve properties that use the specified <see cref="PropertyEnum"/> from the provided <see cref="PropertyCollection"/> to this <see cref="PowerPayload"/>.
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
                targetLevelOverride = target.GetDynamicCombatLevel(CombatLevel);

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
                targetLevelOverride = target.GetDynamicCombatLevel(CombatLevel);

            // Calculate and check super crit chance
            float superCritChance = Power.GetSuperCritChance(PowerPrototype, Properties, target);
            return Game.Random.NextFloat() < superCritChance;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PowerPayload"/>'s hit should dodged.
        /// </summary>
        private bool CheckDodgeChance(WorldEntity target)
        {
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CheckDodgeChance(): powerProto == null");

            // Some powers cannot be dodged
            if (powerProto.CanBeDodged == false)
                return false;

            // Cannot dodge powers from friendly entities
            AlliancePrototype allianceProto = OwnerAlliance;
            if (allianceProto == null || allianceProto.IsHostileTo(target.Alliance) == false)
                return false;

            // Check dodge chance
            float dodgeChance = Power.GetDodgeChance(powerProto, Properties, target.Properties, UltimateOwnerId);
            return Game.Random.NextFloat() < dodgeChance;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PowerPayload"/>'s hit should blocked.
        /// </summary>
        private bool CheckBlockChance(WorldEntity target)
        {
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CheckBlockChance(): powerProto == null");

            // Some powers cannot be blocked
            if (powerProto.CanBeBlocked == false)
                return false;

            // Cannot block powers from friendly entities
            AlliancePrototype allianceProto = OwnerAlliance;
            if (allianceProto == null || allianceProto.IsHostileTo(target.Alliance) == false)
                return false;

            // Check if the target is guaranteed to block
            if (target.Properties[PropertyEnum.BlockAlways])
                return true;

            // Check block chance
            float blockChance = Power.GetBlockChance(powerProto, Properties, target.Properties, UltimateOwnerId);
            return Game.Random.NextFloat() < blockChance;
        }

        private bool CheckUnaffected(WorldEntity target)
        {
            AlliancePrototype allianceProto = OwnerAlliance;

            // Self-targeted powers always affect their targets
            if (target.Id == UltimateOwnerId)
                return false;

            // Check target invulnerability
            if (target.Properties[PropertyEnum.Invulnerable])
            {
                // Invulnerability affects only hostile targets
                if (allianceProto == null || allianceProto.IsHostileTo(target.Alliance))
                    return true;
            }
            
            // Check keyworded invulnerability
            foreach (var kvp in target.Properties.IteratePropertyRange(PropertyEnum.InvulnExceptWithPowerKwd))
            {
                PrototypeId keywordProtoRef = kvp.Value;
                if (HasKeyword(keywordProtoRef.As<KeywordPrototype>()) == false)
                    return true;
            }

            // Check player targetability
            Player player = target.GetOwnerOfType<Player>();
            if (player != null && player.IsTargetable(allianceProto) == false)
                return true;

            // All checks passed, this target is affectable
            return false;
        }

        /// <summary>
        /// Helper function for calculating random over time values.
        /// </summary>
        private float CalculateOverTimeValue(PropertyCollection overTimeProperties, PropertyId baseProp, PropertyId varianceProp, PropertyId magnitudeProp, float bonus = 0f)
        {
            // Helper function for calculating over time values
            float variance = overTimeProperties[varianceProp];
            float varianceMult = (1f - variance) + (variance * 2f * Game.Random.NextFloat());
            return (overTimeProperties[baseProp] + bonus) * varianceMult * overTimeProperties[magnitudeProp];
        }

        /// <summary>
        /// Returns <see langword="true"/> if this <see cref="PowerPayload"/> applies endurance changes over time.
        /// </summary>
        private static bool HasOverTimeEnduranceChange(PropertyCollection overTimeProperties, ManaType manaType)
        {
            return overTimeProperties.HasProperty(new PropertyId(PropertyEnum.EnduranceCOTBase, manaType)) ||
                   overTimeProperties.HasProperty(new PropertyId(PropertyEnum.EnduranceCOTPctBase, manaType));
        }

        /// <summary>
        /// Calculates the duration of movement for this power for conditions that last for as long as movement is happening (e.g. knockbacks).
        /// </summary>
        private bool CalculateMovementDurationForCondition(WorldEntity target, bool calculateForTarget, out TimeSpan movementDuration)
        {
            movementDuration = default;

            if (calculateForTarget == false)
            {
                // Self-applied condition
                movementDuration = MovementTime;

                // Add lag compensation for avatars, since avatar movement is client-authoritative
                if (movementDuration > TimeSpan.Zero && target is Avatar)
                    movementDuration += TimeSpan.FromMilliseconds(150);

                return movementDuration > TimeSpan.Zero;
            }

            // Targeted condition (e.g. knockback)
            PowerPrototype powerProto = PowerPrototype;
            if (powerProto == null) return Logger.WarnReturn(false, "CalculateMovementDurationForCondition(): powerProto == null");

            float knockbackDistance = MathF.Abs(Power.GetKnockbackDistance(target, PowerOwnerId, powerProto, Properties, TargetPosition));
            float movementSpeedOverride = Properties[PropertyEnum.MovementSpeedOverride];

            if (knockbackDistance <= 0f || movementSpeedOverride <= 0f)
                return false;

            movementDuration = TimeSpan.FromMilliseconds(knockbackDistance / movementSpeedOverride * 1000f);
            return movementDuration > TimeSpan.Zero;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the condition with the specified properties can be applied to the provided <see cref="WorldEntity"/>.
        /// </summary>
        private bool CanApplyConditionToTarget(WorldEntity target, PropertyCollection conditionProperties, List<PrototypeId> negativeStatusList)
        {
            PropertyCollection targetProperties = target.Properties;

            // Skip checks if the condition ignores resists and the target isn't immune to resist ignores
            if (conditionProperties[PropertyEnum.IgnoreNegativeStatusResist] && targetProperties[PropertyEnum.CCAlwaysCheckResist] == false)
                return true;

            // Check for general invulnerability
            if (targetProperties[PropertyEnum.Invulnerable])
                return false;

            // Check for immunity to all negative status effects
            if (targetProperties[PropertyEnum.NegStatusImmunity] || targetProperties[PropertyEnum.CCResistAlwaysAll])
                return false;

            // Check for immunity to negative status effects applied by this condition
            foreach (PrototypeId negativeStatus in negativeStatusList)
            {
                if (targetProperties[PropertyEnum.CCResistAlways, negativeStatus])
                    return false;
            }

            // Do not apply knockbacks when a target is immobilized
            if (conditionProperties[PropertyEnum.Knockback] && (target.IsImmobilized || target.IsSystemImmobilized))
                return false;

            // Make sure the target is targetable
            Player player = target.GetOwnerOfType<Player>();
            if (player != null && player.IsTargetable(OwnerAlliance) == false)
                return false;

            // All good, can apply
            return true;
        }

        /// <summary>
        /// Applies condition duration reduction to the provided <see cref="TimeSpan"/>.
        /// </summary>
        private void ApplyConditionDurationResistances(WorldEntity target, ConditionPrototype conditionProto, PropertyCollection conditionProperties, ref TimeSpan duration)
        {
            PropertyCollection targetProperties = target.Properties;

            // Do not resist conditions without negative status effects
            List<PrototypeId> negativeStatusList = ListPool<PrototypeId>.Instance.Get();
            if (Condition.IsANegativeStatusEffect(conditionProperties, negativeStatusList) == false)
                goto end;

            // Do not resist if the condition ignores resists and the target isn't immune to resist ignores
            if (conditionProperties[PropertyEnum.IgnoreNegativeStatusResist] && targetProperties[PropertyEnum.CCAlwaysCheckResist] == false)
                goto end;

            // Check for immunities
            if (CanApplyConditionToTarget(target, conditionProperties, negativeStatusList) == false)
            {
                duration = TimeSpan.Zero;
                goto end;
            }

            // Calculate and apply CCResistScore (tenacity)

            // Start with resist to all
            int ccResistScore = targetProperties[PropertyEnum.CCResistScoreAll];

            // Add resistances to specific negative statuses
            foreach (PrototypeId negativeStatus in negativeStatusList)
                ccResistScore += targetProperties[PropertyEnum.CCResistScore, negativeStatus];
            
            // Add resistances to specific keywords
            if (conditionProto.Keywords.HasValue())
            {
                foreach (PrototypeId keywordProtoRef in conditionProto.Keywords)
                    ccResistScore += targetProperties[PropertyEnum.CCResistScoreKwd, keywordProtoRef];
            }

            // Adjust CCResistScore for region difficulty
            WorldEntity ultimateOwner = Game.EntityManager.GetEntity<WorldEntity>(UltimateOwnerId);
            if (ultimateOwner != null && ultimateOwner.GetOwnerOfType<Player> != null)
                ccResistScore += CalculateRegionCCResistScore(target, conditionProperties);

            // Apply resist score
            float resistMult = 1f - target.GetNegStatusResistPercent(ccResistScore, Properties);
            duration *= resistMult;

            // Apply StatusResistByDuration properties
            ApplyStatusResistByDuration(target, conditionProto, conditionProperties, ref duration);

            end:
            ListPool<PrototypeId>.Instance.Return(negativeStatusList);
        }

        /// <summary>
        /// Returns CCResistScore (tenacity) for the provided <see cref="WorldEntity"/> target based on its rank and region difficulty.
        /// </summary>
        private int CalculateRegionCCResistScore(WorldEntity target, PropertyCollection conditionProperties)
        {
            // Entities have varying difficulty modifiers to their CCResistScore based on their rank
            RankPrototype rankProto = target?.GetRankPrototype();
            if (rankProto == null) return Logger.WarnReturn(0, "CalculateRegionCCResistScore(): rankProto == null");

            TuningPrototype tuningProto = target.Region?.TuningTable?.Prototype;
            if (tuningProto == null) return Logger.WarnReturn(0, "CalculateRegionCCResistScore(): tuningProto == null");

            if (tuningProto.NegativeStatusCurves.HasValue() == false)
                return 0;

            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;

            // Find all curves relevant to this condition and pick the highest resist score out of them
            int score = 0;
            foreach (NegStatusPropCurveEntryPrototype entry in tuningProto.NegativeStatusCurves)
            {
                PropertyEnum statusProperty = propertyInfoTable.GetPropertyEnumFromPrototype(entry.NegStatusProp);
                if (conditionProperties[statusProperty] == false)
                    continue;

                CurveId curveRef = entry.GetCurveRefForRank(rankProto.Rank);
                if (curveRef == CurveId.Invalid)
                    continue;

                Curve curve = curveRef.AsCurve();
                if (curve == null) return Logger.WarnReturn(0, "CalculateRegionCCResistScore(): curve == null");

                int level = Math.Clamp(target.CombatLevel, curve.MinPosition, curve.MaxPosition);
                score = Math.Max(curve.GetIntAt(level), score);
            }

            return score;
        }

        /// <summary>
        /// Applies status effect resistance to the provided <see cref="TimeSpan"/>.
        /// </summary>
        private void ApplyStatusResistByDuration(WorldEntity target, ConditionPrototype conditionProto, PropertyCollection conditionProperties, ref TimeSpan duration)
        {
            // Need a valid duration
            if (duration <= TimeSpan.Zero)
                return;

            // Get non-conditional resistance
            long resistMS = target.Properties[PropertyEnum.StatusResistByDurationMSAll];
            float resistPct = target.Properties[PropertyEnum.StatusResistByDurationPctAll];

            // Find the highest conditional bonuses
            long resistMSBonus = 0;
            float resistPctBonus = 0f;

            PropertyInfoTable propertyInfoTable = GameDatabase.PropertyInfoTable;

            foreach (var kvp in target.Properties.IteratePropertyRange(Property.StatusResistByDurationConditional))
            {
                PropertyEnum propertyEnum = kvp.Key.Enum;
                Property.FromParam(kvp.Key, 0, out PrototypeId protoRefToCheck);

                // Check if this property is applicable
                switch (propertyEnum)
                {
                    case PropertyEnum.StatusResistByDurationMS:
                    case PropertyEnum.StatusResistByDurationPct:
                        // Validate that this is boolean property
                        PropertyInfoPrototype propertyInfoProto = protoRefToCheck.As<PropertyInfoPrototype>();
                        if (propertyInfoProto == null || propertyInfoProto.Type != PropertyDataType.Boolean)
                        {
                            Logger.Warn("ApplyStatusResistByDuration(): propertyInfoProto == null || propertyInfoProto.Type != PropertyDataType.Boolean");
                            continue;
                        }

                        // Check for the specified flag property
                        PropertyEnum paramProperty = propertyInfoTable.GetPropertyEnumFromPrototype(protoRefToCheck);
                        if (conditionProperties[paramProperty] == false)
                            continue;

                        break;

                    case PropertyEnum.StatusResistByDurationMSKwd:
                    case PropertyEnum.StatusResistByDurationPctKwd:
                        // Check for the specified keyword
                        if (conditionProto.HasKeyword(protoRefToCheck) == false)
                            continue;
                        break;

                    default:
                        continue;
                }

                // Update bonus values (pick the highest one)
                switch (propertyEnum)
                {
                    case PropertyEnum.StatusResistByDurationMS:
                    case PropertyEnum.StatusResistByDurationMSKwd:
                        resistMSBonus = Math.Max(kvp.Value, resistMSBonus);
                        break;

                    case PropertyEnum.StatusResistByDurationPct:
                    case PropertyEnum.StatusResistByDurationPctKwd:
                        resistPctBonus = MathF.Max(kvp.Value, resistPctBonus);
                        break;
                }
            }

            // Apply status resist
            duration -= TimeSpan.FromMilliseconds(resistMS + resistMSBonus);
            duration *= 1f - (resistPct + resistPctBonus);
            duration = Clock.Max(duration, TimeSpan.Zero);
        }

        /// <summary>
        /// Applies condition duration bonuses to the provided <see cref="TimeSpan"/>.
        /// </summary>
        private void ApplyConditionDurationBonuses(ref TimeSpan duration)
        {
            if (PowerPrototype?.OmniDurationBonusExclude == false)
            {
                WorldEntity ultimateOwner = Game.EntityManager.GetEntity<WorldEntity>(UltimateOwnerId);
                if (ultimateOwner != null)
                {
                    duration *= 1f + ultimateOwner.Properties[PropertyEnum.OmniDurationBonusPct];
                    duration = Clock.Max(duration, TimeSpan.FromMilliseconds(1));
                }
            }

            duration += TimeSpan.FromMilliseconds((int)Properties[PropertyEnum.StatusDurationBonusMS]);
        }

        /// <summary>
        /// Returns the number of condition stacks to apply to the provided <see cref="WorldEntity"/>.
        /// Depending on the power's stacking behavior can also modify the provided duration <see cref="TimeSpan"/>.
        /// </summary>
        private int CalculateConditionNumStacksToApply(WorldEntity target, WorldEntity ultimateOwner,
            ConditionCollection conditionCollection, ConditionPrototype conditionProto, ref TimeSpan duration)
        {
            ulong creatorPlayerId = ultimateOwner is Avatar avatar ? avatar.OwnerPlayerDbId : 0;

            ConditionCollection.StackId stackId = ConditionCollection.MakeConditionStackId(PowerPrototype,
                conditionProto, UltimateOwnerId, creatorPlayerId, out StackingBehaviorPrototype stackingBehaviorProto);

            if (stackId.PrototypeRef == PrototypeId.Invalid) return Logger.WarnReturn(0, "CalculateConditionNumStacksToApply(): stackId.PrototypeRef == PrototypeId.Invalid");

            List<ulong> refreshList = ListPool<ulong>.Instance.Get();
            List<ulong> removeList = ListPool<ulong>.Instance.Get();

            int numStacksToApply = conditionCollection.GetStackApplicationData(stackId, stackingBehaviorProto,
                Properties[PropertyEnum.PowerRank], out TimeSpan longestTimeRemaining, removeList, refreshList);

            // Remove conditions
            foreach (ulong conditionId in removeList)
                conditionCollection.RemoveCondition(conditionId);

            // Modify duration and refresh conditions
            // NOTE: The order is important here because refreshing uses the duration
            StackingApplicationStyleType applicationStyle = stackingBehaviorProto.ApplicationStyle;

            if (applicationStyle == StackingApplicationStyleType.MatchDuration && longestTimeRemaining > TimeSpan.Zero)
                duration = longestTimeRemaining;

            if (refreshList.Count > 0)
            {
                bool refreshedAny = false;
                bool refreshedAnyNegativeStatus = false;
                ulong negativeStatusId = 0;

                foreach (ulong conditionId in refreshList)
                {
                    Condition condition = conditionCollection.GetCondition(conditionId);
                    if (condition == null)
                        continue;

                    TimeSpan durationDelta = TimeSpan.Zero;
                    if (applicationStyle == StackingApplicationStyleType.SingleStackAddDuration || applicationStyle == StackingApplicationStyleType.MultiStackAddDuration)
                        durationDelta = duration;

                    bool refreshedThis = conditionCollection.RefreshCondition(conditionId, PowerOwnerId, durationDelta);
                    refreshedAny |= refreshedThis;

                    if (refreshedThis && refreshedAnyNegativeStatus == false && condition.IsANegativeStatusEffect())
                    {
                        refreshedAnyNegativeStatus = true;
                        negativeStatusId = conditionId;
                    }
                }

                if (refreshedAnyNegativeStatus)
                    target.OnNegativeStatusEffectApplied(negativeStatusId);
            }

            if (applicationStyle == StackingApplicationStyleType.MultiStackAddDuration)
                duration += longestTimeRemaining;

            ListPool<ulong>.Instance.Return(refreshList);
            ListPool<ulong>.Instance.Return(removeList);
            return numStacksToApply;
        }

        #region Damage Shield

        private static bool ApplyDamageToShield(WorldEntity target, PropertyCollection conditionProperties, DamageType damageType, float damageTotal, ref float damageShielded)
        {
            bool expirationCheckNeeded = false;

            float damageAccumulationLimit = target.GetDamageAccumulationLimit(conditionProperties, damageType);
            int numHitLimit = conditionProperties[PropertyEnum.DamageShieldNumHitLimit, damageType];

            float damageAccumulationRemaining = 0f;
            if (damageAccumulationLimit > 0f)
                damageAccumulationRemaining = damageAccumulationLimit - conditionProperties[PropertyEnum.DamageAccumulation, damageType];

            if (conditionProperties[PropertyEnum.DamageShieldRegensFromDamage, damageType])
            {
                // Regen shield from damage if needed
                conditionProperties.AdjustProperty(-damageShielded, new(PropertyEnum.DamageAccumulation, damageType));

                if (damageType != DamageType.Any)
                    conditionProperties.AdjustProperty(-damageShielded, new(PropertyEnum.DamageAccumulation, DamageType.Any));
            }
            else if (damageAccumulationLimit > 0f && damageShielded > 0f && (numHitLimit > 0 || numHitLimit == -1))
            {
                // Accumulate damage if this is a finite shield (DamageAccumulationLimit > 0f or NumHitLimit > 0)
                damageShielded = Math.Min(damageShielded, Math.Min(damageTotal, damageAccumulationRemaining));
                conditionProperties.AdjustProperty(damageShielded, new(PropertyEnum.DamageAccumulation, damageType));

                if (conditionProperties[PropertyEnum.DamageAccumulation, damageType] >= damageAccumulationLimit)
                    expirationCheckNeeded = true;

                if (numHitLimit > -1)
                {
                    conditionProperties.AdjustProperty(-1, new(PropertyEnum.DamageShieldNumHitLimit, damageType));
                    expirationCheckNeeded = true;
                }
            }

            return expirationCheckNeeded;
        }

        private static bool CheckDamageShieldExpiration(WorldEntity target, PropertyCollection conditionProperties, DamageType damageType)
        {
            bool isExpired = false;

            float damageAccumulationLimit = target.GetDamageAccumulationLimit(conditionProperties, damageType);

            if (damageAccumulationLimit > 0f && conditionProperties[PropertyEnum.DamageAccumulation, damageType] >= damageAccumulationLimit)
                isExpired = true;

            if (conditionProperties[PropertyEnum.DamageShieldNumHitLimit, damageType] == 0)
                isExpired = true;

            return isExpired;
        }

        #endregion

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
            healthMaxProperties[PropertyEnum.CombatLevel] = target.GetDynamicCombatLevel(combatLevel);

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
