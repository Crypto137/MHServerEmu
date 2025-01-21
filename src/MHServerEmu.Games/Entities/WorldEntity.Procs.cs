using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Calligraphy.Attributes;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    [AssetEnum((int)None)]
    public enum ProcTriggerType
    {
        None = 0,
        OnAnyHit = 1,
        OnAnyHitForPctHealth = 2,
        OnAnyHitTargetHealthBelowPct = 3,
        OnBlock = 4,
        OnCollide = 5,
        OnCollideEntity = 6,
        OnCollideWorldGeo = 7,
        OnConditionEnd = 8,
        OnConditionStackCount = 9,
        OnCrit = 10,
        OnGotDamagedByCrit = 11,
        OnDeath = 12,
        OnDodge = 13,
        OnEnduranceAbove = 14,
        OnEnduranceBelow = 15,
        OnGotAttacked = 16,
        OnGotDamaged = 17,
        OnGotDamagedPriorResist = 18,
        OnGotDamagedEnergy = 19,
        OnGotDamagedEnergyPriorResist = 20,
        OnGotDamagedForPctHealth = 21,
        OnGotDamagedHealthBelowPct = 22,
        OnGotDamagedMental = 23,
        OnGotDamagedMentalPriorResist = 24,
        OnGotDamagedPhysical = 25,
        OnGotDamagedPhysicalPriorResist = 26,
        OnGotDamagedBySuperCrit = 27,
        OnHealthAbove = 28,
        OnHealthAboveToggle = 29,
        OnHealthBelow = 30,
        OnHealthBelowToggle = 31,
        OnInCombat = 32,
        OnInteractedWith = 33,
        OnInteractedWithOutOfUses = 34,
        OnKillAlly = 35,
        OnKillDestructible = 36,
        OnKillOther = 37,
        OnKillOtherCritical = 38,
        OnKillOtherSuperCrit = 39,
        OnKnockdownEnd = 40,
        OnLifespanExpired = 41,
        OnLootPickup = 42,
        OnMissileAbsorbed = 43,
        OnMovementStarted = 44,
        OnMovementStopped = 45,
        OnNegStatusApplied = 46,
        OnOrbPickup = 47,
        OnOutCombat = 48,
        OnOverlapBegin = 49,
        OnPetDeath = 50,
        OnPetHit = 51,
        OnPowerHit = 52,
        OnPowerHitEnergy = 53,
        OnPowerHitMental = 54,
        OnPowerHitNormal = 55,
        OnPowerHitNotOverTime = 56,
        OnPowerHitPhysical = 57,
        OnPowerUseComboEffect = 58,
        OnPowerUseConsumable = 59,
        OnPowerUseGameFunction = 60,
        OnPowerUseNormal = 61,
        OnPowerUseProcEffect = 62,
        OnRunestonePickup = 63,
        OnSecondaryResourceEmpty = 64,
        OnSecondaryResourcePipGain = 65,
        OnSecondaryResourcePipLoss = 66,
        OnSecondaryResourcePipMax = 67,
        OnSecondaryResourcePipZero = 68,
        OnSkillshotReflect = 69,
        OnSummonPet = 70,
        OnSuperCrit = 71,
        OnMissileHit = 72,
        OnHotspotNegated = 73,
        OnControlledEntityReleased = 74,
    }

    public partial class WorldEntity
    {
        #region Handlers

        // Handlers are ordered by ProcTriggerType enum

        // NOTE: Activating procs will likely modify the property collections of affected entities and conditions,
        // so we need to copy proc properties to a temporary collection for iteration.

        // Potentially this can be optimized by refactoring property collections to allow modification during iteration,
        // but this may create unnecessary garbage collection pressure, so more investigation is required before doing this.

        public virtual void TryActivateOnHitProcs(ProcTriggerType triggerType, PowerResults powerResults)   // 1-3, 10, 52-57, 71
        {
            if (IsInWorld == false)
                return;

            if (TryForwardOnHitProcsToOwner(triggerType, powerResults))
                return;

            // Check if our target can trigger procs
            WorldEntity target = null;
            if (powerResults.TargetId != InvalidId)
            {
                target = Game.EntityManager.GetEntity<WorldEntity>(powerResults.TargetId);
                if (target != null && target.CanTriggerOtherProcs(triggerType) == false)
                    return;
            }

            // Get proc chance multiplier for this power
            PowerPrototype powerProto = powerResults.PowerPrototype;
            float procChanceMultiplier = powerProto.OnHitProcChanceMultiplier;

            // Copy proc properties to a temporary collection for iteration
            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyword procs
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)triggerType))
            {
                int param;

                // Calculate param value
                switch (triggerType)
                {
                    case ProcTriggerType.OnAnyHitForPctHealth:
                        if (target == null)
                            continue;

                        float damage = 0f;
                        foreach (var damageKvp in powerResults.Properties.IteratePropertyRange(PropertyEnum.Damage))
                            damage += damageKvp.Value;

                        float pctHealth = damage / Math.Max(target.Properties[PropertyEnum.HealthMax], 1L);
                        param = (int)(pctHealth * 100f);

                        break;

                    case ProcTriggerType.OnAnyHitTargetHealthBelowPct:
                        if (target == null)
                            continue;

                        damage = 0f;
                        foreach (var damageKvp in powerResults.Properties.IteratePropertyRange(PropertyEnum.Damage))
                            damage += damageKvp.Value;

                        float healthAfterDamage = (long)target.Properties[PropertyEnum.Health] - damage;
                        if (healthAfterDamage > 0f)
                        {
                            pctHealth = healthAfterDamage / Math.Max(target.Properties[PropertyEnum.HealthMax], 1L);
                            param = (int)(pctHealth * 100f);
                        }
                        else
                        {
                            param = 100;
                        }

                        break;

                    default:
                        param = 0;
                        break;
                }

                if (CheckProc(kvp, out Power procPower, param, procChanceMultiplier) == false)
                    continue;

                // Check for recursion (this will also null check procPower)
                if (CheckOnHitRecursion(procPower, powerResults.PowerPrototype) == false)
                    continue;

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                settings.PowerResults = powerResults;

                if (target != null && target.IsInWorld)
                {
                    settings.TargetEntityId = target.Id;
                    settings.TargetPosition = target.RegionLocation.Position;
                }

                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            // Keyword procs
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != triggerType)
                    continue;

                bool requiredKeywordState = kvp.Key.Enum == PropertyEnum.ProcKeyword;   // true for ProcKeyword, false for ProcNotKeyword
                if (CheckKeywordProc(kvp, out Power procPower, powerProto.KeywordsMask, requiredKeywordState, procChanceMultiplier) == false)
                    continue;

                // Check for recursion (this will also null check procPower)
                if (CheckOnHitRecursion(procPower, powerResults.PowerPrototype) == false)
                    continue;

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                settings.PowerResults = powerResults;

                if (target != null && target.IsInWorld)
                {
                    settings.TargetEntityId = target.Id;
                    settings.TargetPosition = target.RegionLocation.Position;
                }

                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnHitConditions();
        }

        public void TryActivateOnBlockProcs(PowerResults powerResults)  // 4
        {
            // TODO
        }

        public void TryActivateOnCollideProcs(ProcTriggerType triggerType, WorldEntity other, Vector3 position)
        {
            // TODO
            //Logger.Debug($"TryActivateOnCollideProcs(): {triggerType} with [{other}] at [{position}]");
        }

        public void TryActivateOnConditionEndProcs(Condition condition) // 8
        {
            if (IsInWorld == false)
                return;

            using PropertyCollection procProperties = GetProcProperties(condition.Properties);

            // Run common proc logic
            TryActivateProcsCommon(ProcTriggerType.OnConditionEnd, procProperties);

            // Check keyword procs
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != ProcTriggerType.OnConditionEnd)
                    continue;

                bool requiredKeywordState = kvp.Key.Enum == PropertyEnum.ProcKeyword;   // true for ProcKeyword, false for ProcNotKeyword
                if (CheckKeywordProc(kvp, out Power procPower, condition, requiredKeywordState) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnConditionEndProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }
        }

        public void TryActivateOnConditionStackCountProcs(Condition condition)  // 9
        {
            // TODO
        }

        // See 17 below for OnGotDamagedByCrit

        public void TryActivateOnDeathProcs(PowerResults powerResults)  // 12
        {
            // TODO Rewrite this

            if (this is not Agent) return;
            Power power = null;

            // Get OnDeath ProcPower
            foreach (var kvp in PowerCollection)
            {
                var proto = kvp.Value.PowerPrototype;
                if (proto.Activation != PowerActivationType.Passive) continue;

                string protoName = kvp.Key.GetNameFormatted();
                if (protoName.Contains("OnDeath"))
                {
                    power = kvp.Value.Power;
                    break;
                }
            }

            if (power == null) return;

            // Get OnDead power
            var conditions = power.Prototype.AppliesConditions;
            if (conditions.Count != 1) return;
            var conditionProto = conditions[0].Prototype as ConditionPrototype;

            // Get summon power
            SummonPowerPrototype summonPower = null;
            foreach (var kvp in conditionProto.Properties.IteratePropertyRange(PropertyEnum.Proc))
            {
                Property.FromParam(kvp.Key, 0, out int procEnum);
                if ((ProcTriggerType)procEnum != ProcTriggerType.OnDeath) continue;
                Property.FromParam(kvp.Key, 1, out PrototypeId summonPowerRef);
                summonPower = GameDatabase.GetPrototype<SummonPowerPrototype>(summonPowerRef);
                if (summonPower != null) break;
            }

            if (summonPower != null) EntityHelper.OnDeathSummonFromPowerPrototype(this, summonPower);
        }

        public void TryActivateOnDodgeProcs(PowerResults powerResults)  // 13
        {
            // TODO
        }

        public void TryActivateOnEnduranceProcs(ManaType manaType)  // 14-15
        {
            // TODO
        }

        public void TryActivateOnGotAttackedProcs(PowerResults powerResults)    // 16
        {
            // TODO
        }

        public void TryActivateOnGotDamagedProcs(PowerResults powerResults) // 11, 17-27
        {
            // TODO
        }

        public void TryActivateOnHealthProcs(PrototypeId procPowerProtoRef = PrototypeId.Invalid)  // 28-31
        {
            // TODO
        }

        public void TryActivateOnInCombatProcs()    // 32
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnInCombat, procProperties);
        }

        public virtual void TryActivateOnKillProcs(ProcTriggerType triggerType, PowerResults powerResults)    // 35-39
        {
            if (IsInWorld == false)
                return;

            if (TryForwardOnKillProcsToOwner(triggerType, powerResults))
                return;

            // TODO
        }

        public void TryActivateOnKnockdownEndProcs()    // 40
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnKnockdownEnd, procProperties);
        }

        public void TryActivateOnLifespanExpiredProcs() // 41
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnLifespanExpired, procProperties);
        }

        public void TryActivateOnLootPickupProcs(WorldEntity item)  // 42
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnLootPickup, procProperties, item);
        }

        public void TryActivateOnMissileAbsorbedProcs() // 43
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnMissileAbsorbed, procProperties);
        }

        public void TryActivateOnNegStatusAppliedProcs()  // 46
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnNegStatusApplied, procProperties);
        }

        public void TryActivateOnOutOfCombatProcs() // 48
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnOutCombat, procProperties);
        }

        public void TryActivateOnOverlapBeginProcs(WorldEntity other, Vector3 position, Vector3 otherPosition)  // 49
        {
            // TODO
            //Logger.Debug($"TryActivateOnOverlapBeginProcs(): With [{other}] at [{position}]");
        }

        public void TryActivateOnOverlapBeginProcs(PropertyId propertyId)
        {
            // override for checking overlaps that are already happening when this proc is assigned
        }

        public void TryActivateOnPetHitProcs(PowerResults powerResults, WorldEntity summon) // 51
        {
            // TODO
        }

        public void TryActivateOnPowerUseProcs(ProcTriggerType triggerType, Power power, ref PowerActivationSettings settings)  // 58-62
        {
            // TODO
        }

        public void TryActivateOnRunestonePickupProcs()
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnRunestonePickup, procProperties);
        }

        public void TryActivateOnSecondaryResourceValueChangeProcs(float newValue)  // 64
        {
            if (IsInWorld == false)
                return;

            if (newValue == 0f)
            {
                using PropertyCollection procProperties = GetProcProperties(Properties);
                TryActivateProcsCommon(ProcTriggerType.OnSecondaryResourceEmpty, procProperties);
            }
        }

        public void TryActivateOnSecondaryResourcePipsChangeProcs(int newPips, int oldPips) // 65-68
        {
            if (IsInWorld == false)
                return;

            using PropertyCollection procProperties = GetProcProperties(Properties);

            if (newPips > oldPips)
                TryActivateProcsCommon(ProcTriggerType.OnSecondaryResourcePipGain, procProperties);
            else
                TryActivateProcsCommon(ProcTriggerType.OnSecondaryResourcePipLoss, procProperties);

            if (newPips == Properties[PropertyEnum.SecondaryResourceMaxPips])
                TryActivateProcsCommon(ProcTriggerType.OnSecondaryResourcePipMax, procProperties);
            else if (newPips == 0)
                TryActivateProcsCommon(ProcTriggerType.OnSecondaryResourcePipZero, procProperties);
        }

        public void TryActivateOnSkillshotReflectProcs()   // 69
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnSkillshotReflect, procProperties);
        }

        public void TryActivateOnMissileHitProcs(Power power, WorldEntity target)   // 72
        {
            // TODO
        }

        private void TryActivateProcsCommon(ProcTriggerType triggerType, PropertyCollection properties, WorldEntity target = null, float procChanceMultiplier = 1f)
        {
            if (IsInWorld == false)
                return;

            if (target != null && target.CanTriggerOtherProcs(triggerType) == false)
                return;

            foreach (var kvp in properties.IteratePropertyRange(PropertyEnum.Proc, (int)triggerType))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateProcsCommon(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;
                procPower.Properties.CopyProperty(properties, PropertyEnum.CharacterLevel);

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(triggerType);
        }

        #endregion

        #region Proc Forwarding

        public bool TryForwardOnHitProcsToOwner(ProcTriggerType triggerType, PowerResults powerResults)
        {
            WorldEntityPrototype worldEntityProto = WorldEntityPrototype;
            if (worldEntityProto == null) return Logger.WarnReturn(false, "ForwardOnHitProcsToOwner(): worldEntityProto == null");

            if (worldEntityProto.ForwardOnHitProcsToOwner == false)
                return false;

            WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(PowerUserOverrideId);
            if (owner == null || owner.IsInWorld == false)
                return false;

            owner.TryActivateOnHitProcs(triggerType, powerResults);
            return true;
        }

        public bool TryForwardOnKillProcsToOwner(ProcTriggerType triggerType, PowerResults powerResults)
        {
            WorldEntityPrototype worldEntityProto = WorldEntityPrototype;
            if (worldEntityProto == null) return Logger.WarnReturn(false, "TryForwardOnKillProcsToOwner(): worldEntityProto == null");

            if (worldEntityProto.ForwardOnHitProcsToOwner == false)
                return false;

            WorldEntity owner = Game.EntityManager.GetEntity<WorldEntity>(PowerUserOverrideId);
            if (owner == null || owner.IsInWorld == false)
                return false;

            owner.TryActivateOnKillProcs(triggerType, powerResults);
            return true;
        }

        #endregion

        #region Helpers

        public bool CanTriggerOtherProcs(ProcTriggerType triggerType)
        {
            return Properties[PropertyEnum.DontTriggerOtherProcs, (int)triggerType] == false;
        }

        private bool CheckProc(in KeyValuePair<PropertyId, PropertyValue> procProperty, out Power procPower,
            int param = 0, float procChanceMultiplier = 1f, Power triggeringPower = null)
        {
            procPower = null;

            // Check param threshold
            Property.FromParam(procProperty.Key, 0, out int triggerTypeValue);
            Property.FromParam(procProperty.Key, 2, out int paramThreshold);

            bool passedThreshold;
            switch ((ProcTriggerType)triggerTypeValue)
            {
                case ProcTriggerType.OnAnyHitTargetHealthBelowPct:
                case ProcTriggerType.OnEnduranceBelow:
                case ProcTriggerType.OnGotDamagedHealthBelowPct:
                case ProcTriggerType.OnHealthBelow:
                case ProcTriggerType.OnHealthBelowToggle:
                    passedThreshold = param <= paramThreshold;
                    break;

                case ProcTriggerType.OnConditionStackCount:
                    passedThreshold = param == paramThreshold;
                    break;

                default:
                    passedThreshold = param >= paramThreshold;                    
                    break;
            }

            if (passedThreshold == false)
                return false;

            if (CheckProcChance(procProperty, procChanceMultiplier) == false)
                return false;

            procPower = GetProcPower(procProperty);
            if (procPower == null)
                return false;

            return procPower.CanTrigger() == PowerUseResult.Success;
        }

        private bool CheckKeywordProc<T>(in KeyValuePair<PropertyId, PropertyValue> procProperty, out Power procPower,
            T keywordedObject, bool requiredKeywordState, float procChanceMultiplier = 1f) where T: IKeyworded
        {
            procPower = null;

            Property.FromParam(procProperty.Key, 2, out PrototypeId keywordProtoRef);
            KeywordPrototype keywordProto = keywordProtoRef.As<KeywordPrototype>();
            if (keywordProto == null) return Logger.WarnReturn(false, "CheckKeywordProc(): keywordProto == null");

            if (keywordedObject.HasKeyword(keywordProto) != requiredKeywordState)
                return false;

            if (CheckProcChance(procProperty, procChanceMultiplier) == false)
                return false;

            procPower = GetProcPower(procProperty);
            if (procPower == null)
                return false;

            return procPower.CanTrigger() == PowerUseResult.Success;
        }

        private bool CheckProcChance(in KeyValuePair<PropertyId, PropertyValue> procProperty, float procChanceMultiplier)
        {
            Property.FromParam(procProperty.Key, 1, out PrototypeId procPowerProtoRef);

            // See if we have a proc chance override for this power
            float procChance = Properties[PropertyEnum.ProcChanceOverride, procPowerProtoRef];
            
            // If not, calculate proc chance taking into account the multiplier we have
            if (procChance <= 0f)
            {
                procChance = Properties[procProperty.Key];

                if (Segment.EpsilonTest(procChanceMultiplier, 1f) == false)
                {
                    PowerPrototype powerProto = procPowerProtoRef.As<PowerPrototype>();
                    if (powerProto == null) return Logger.WarnReturn(false, "CheckProcChance(): powerProto == null");

                    switch (powerProto.ProcChanceMultiplierBehavior)
                    {
                        case ProcChanceMultiplierBehaviorType.AllowProcChanceMultiplier:
                            procChance *= procChanceMultiplier;
                            break;

                        case ProcChanceMultiplierBehaviorType.IgnoreProcChanceMultiplier:
                            // ignore without warning
                            break;

                        case ProcChanceMultiplierBehaviorType.IgnoreProcChanceMultiplierUnlessZero:
                            if (Segment.IsNearZero(procChanceMultiplier))
                                return false;
                            break;

                        default:
                            Logger.Warn($"CheckProcChance(): Unhandled ProcChanceMultiplierBehaviorType {powerProto.ProcChanceMultiplierBehavior} in [{powerProto}]");
                            break;
                    }
                }
            }

            // Check if we have a counter that guarantees procs
            if (Properties.HasProperty(PropertyEnum.ProcAlwaysSucceedCount))
            {
                if (ConditionCollection != null)
                {
                    // Decrement the counter on all conditions that grant us guaranteed procs
                    foreach (Condition condition in ConditionCollection)
                    {
                        if (condition.Properties.HasProperty(PropertyEnum.ProcAlwaysSucceedCount))
                            condition.Properties.AdjustProperty(-1, PropertyEnum.ProcAlwaysSucceedCount);
                    }
                }

                return true;
            }

            return Game.Random.NextFloat() < procChance;
        }

        private bool CheckOnHitRecursion(Power procPower, PowerPrototype triggeringPowerProto)
        {
            if (triggeringPowerProto == null) return Logger.WarnReturn(false, "CheckOnHitRecursion(): triggeringPowerProto == null");

            PowerPrototype procPowerProto = procPower?.Prototype;
            if (procPowerProto == null) return Logger.WarnReturn(false, "CheckOnHitRecursion(): procPowerProto == null");

            // Do not allow self-trigger
            if (triggeringPowerProto == procPowerProto)
                return false;

            PowerCollection powerCollection = procPower.Owner?.PowerCollection;
            if (powerCollection == null) return Logger.WarnReturn(false, "CheckOnHitRecursion(): powerCollection == null");

            // Triggering power may not necessarily be on the owner of the proc (e.g. missile activated procs)
            Power triggeringPower = powerCollection.GetPower(triggeringPowerProto.DataRef);
            if (triggeringPower == null)
                return true;

            // Check for infinite loops
            bool success = true;
            HashSet<PrototypeId> triggeringPowers = HashSetPool<PrototypeId>.Instance.Get(); 

            PrototypeId parentTriggeringPowerRef = triggeringPower.Properties[PropertyEnum.TriggeringPowerRef, triggeringPowerProto.DataRef];
            while (parentTriggeringPowerRef != PrototypeId.Invalid)
            {
                PowerPrototype parentTriggeringPowerProto = parentTriggeringPowerRef.As<PowerPrototype>();
                if (parentTriggeringPowerProto.HasRescheduleActivationEventWithInvalidPowerRef)
                    break;

                if (triggeringPowers.Contains(parentTriggeringPowerRef) || parentTriggeringPowerRef == procPowerProto.DataRef)
                {
                    success = false;
                    break;
                }

                triggeringPowers.Add(parentTriggeringPowerRef);

                Power parentTriggeringPower = powerCollection.GetPower(parentTriggeringPowerRef);
                parentTriggeringPowerRef = parentTriggeringPower != null
                    ? parentTriggeringPower.Properties[PropertyEnum.TriggeringPowerRef, parentTriggeringPowerRef]
                    : PrototypeId.Invalid;
            }

            if (success == false)
                Logger.Warn($"CheckOnHitRecursion(): Recursion check failed for procPower=[{procPower}], triggeringPower=[{triggeringPowerProto}]");

            HashSetPool<PrototypeId>.Instance.Return(triggeringPowers);
            return success;
        }

        private Power GetProcPower(in KeyValuePair<PropertyId, PropertyValue> procProperty)
        {
            if (IsInWorld == false) return Logger.WarnReturn<Power>(null, "GetProcPower(): IsInWorld == false");

            Property.FromParam(procProperty.Key, 1, out PrototypeId procPowerProtoRef);

            WorldEntity caster = this;

            // Check if we have a caster override for this
            ulong procCasterOverrideId = Properties[PropertyEnum.ProcCasterOverride, procPowerProtoRef];
            if (procCasterOverrideId != InvalidId)
            {
                caster = Game.EntityManager.GetEntity<WorldEntity>(procCasterOverrideId);
                if (caster == null || caster.IsInWorld == false)
                    return null;
            }

            return caster.GetPower(procPowerProtoRef);
        }

        private static PropertyCollection GetProcProperties(PropertyCollection source)
        {
            PropertyCollection destination = ObjectPoolManager.Instance.Get<PropertyCollection>();

            foreach (PropertyEnum propertyEnum in Property.ProcPropertyTypesAll)
                destination.CopyPropertyRange(source, propertyEnum);

            // Needed for the common handler
            destination.CopyProperty(source, PropertyEnum.CharacterLevel);

            return destination;
        }

        #endregion
    }
}
