using MHServerEmu.Core.Collisions;
using MHServerEmu.Core.Helpers;
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
            KeywordsMask keywordsMask = powerResults.KeywordsMask != null ? powerResults.KeywordsMask : powerProto.KeywordsMask;

            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != triggerType)
                    continue;

                if (CheckKeywordProc(kvp, out Power procPower, keywordsMask, procChanceMultiplier) == false)
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

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(triggerType);
        }

        public void TryActivateOnBlockProcs(PowerResults powerResults)  // 4
        {
            TryActivateOnBlockOrDodgeProcHelper(ProcTriggerType.OnBlock, powerResults);
        }

        public void TryActivateOnCollideProcs(ProcTriggerType triggerType, WorldEntity target, Vector3 collisionPosition)
        {
            if (IsInWorld == false)
                return;

            // null target indicates terrain collision
            if (target != null && target.CanTriggerOtherProcs(triggerType) == false)
                return;

            ulong targetId = target != null ? target.Id : 0;
            Vector3 targetPosition = target != null ? target.RegionLocation.Position : collisionPosition;

            using PropertyCollection procProperties = GetProcProperties(Properties);
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)triggerType))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnCollideProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(targetId, targetPosition, collisionPosition);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(triggerType);
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

                if (CheckKeywordProc(kvp, out Power procPower, condition) == false)
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
            if (IsInWorld == false)
                return;

            int param = ConditionCollection.GetNumberOfStacks(condition);
            if (param == 0)
                return;

            using PropertyCollection procProperties = GetProcProperties(condition.Properties);
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)ProcTriggerType.OnConditionStackCount))
            {
                if (CheckProc(kvp, out Power procPower, param) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnConditionStackCountProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }
        }

        // See 17 below for OnGotDamagedByCrit

        public void TryActivateOnDeathProcs(PowerResults powerResults)  // 12
        {
            WorldEntity killer = null;

            if (powerResults != null)
            {
                killer = Game.EntityManager.GetEntity<WorldEntity>(powerResults.UltimateOwnerId);
                if (killer != null && killer.CanTriggerOtherProcs(ProcTriggerType.OnDeath) == false)
                    return;
            }

            using PropertyCollection procProperties = GetProcProperties(Properties);
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)ProcTriggerType.OnDeath))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnDeathProcs(): procPower == null");
                    continue;
                }

                if (procPower.NeedsTarget() && procPower.IsValidTarget(killer) == false)
                    continue;

                WorldEntity procPowerOwner = procPower.Owner;

                // Record PowerUserOverrideId to restore it later
                ulong powerUserOverrideIdBefore = PowerUserOverrideId;

                ulong targetId = InvalidId;
                Vector3 targetPosition = Vector3.Zero;

                if (killer != null)
                {
                    // If this is a transient power user (e.g. a destructible object that explodes on death),
                    // attribute the proc activation to the killer (e.g. an avatar who exploded something).
                    if (Properties[PropertyEnum.IsTransientPowerUser])
                        Properties[PropertyEnum.PowerUserOverrideID] = killer.Id;

                    targetId = killer.Id;
                    targetPosition = killer.RegionLocation.Position;
                }
                else if (powerResults != null)
                {
                    targetPosition = powerResults.PowerOwnerPosition;
                }

                // Update spawn spec
                if (SpawnSpec != null && procPower.Prototype.PostContactDelayMS > 0)
                    SpawnSpec.PostContactDelayMS = TimeSpan.FromMilliseconds(procPower.Prototype.PostContactDelayMS);

                // Activate
                PowerActivationSettings settings = new(targetId, targetPosition, procPowerOwner.RegionLocation.Position);
                settings.PowerResults = powerResults;

                procPowerOwner.ActivateProcPower(procPower, ref settings, this, true);

                // Restore the power user override id from before this proc was activated
                Properties[PropertyEnum.PowerUserOverrideID] = powerUserOverrideIdBefore;
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(ProcTriggerType.OnDeath);
        }

        public void TryActivateOnDodgeProcs(PowerResults powerResults)  // 13
        {
            TryActivateOnBlockOrDodgeProcHelper(ProcTriggerType.OnDodge, powerResults);
        }

        public void TryActivateOnEnduranceProcs(ManaType manaType)  // 14-15
        {
            if (IsInWorld == false)
                return;

            if (TestStatus(EntityStatus.ExitingWorld))
                return;

            using PropertyCollection procProperties = GetProcProperties(Properties);
            foreach (var kvp in procProperties)
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                ProcTriggerType triggerType = (ProcTriggerType)triggerTypeValue;
                if (triggerType != ProcTriggerType.OnEnduranceAbove && triggerType != ProcTriggerType.OnEnduranceBelow)
                    continue;

                // Check mana type
                Property.FromParam(kvp.Key, 3, out int manaTypeValue);
                if ((ManaType)manaTypeValue != manaType)
                    continue;

                float enduranceMax = Properties[PropertyEnum.EnduranceMax, manaType];
                if (enduranceMax <= 0f)
                    continue;

                int param = (int)(Properties[PropertyEnum.Endurance, manaType] / enduranceMax * 100f);

                if (CheckProc(kvp, out Power procPower, param) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnEnduranceProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }
        }

        public void TryActivateOnGotAttackedProcs(PowerResults powerResults)    // 16
        {
            if (powerResults == null)
                return;

            if (IsInWorld == false)
                return;

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(powerResults.UltimateOwnerId);

            using PropertyCollection procProperties = GetProcProperties(Properties);
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)ProcTriggerType.OnGotAttacked))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnGotAttackedProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                ulong targetId;
                Vector3 targetPosition;

                if (target != null && target.IsInWorld)
                {
                    targetId = target.Id;
                    targetPosition = target.RegionLocation.Position;
                }
                else
                {
                    targetId = InvalidId;
                    targetPosition = powerResults.PowerOwnerPosition;
                }

                PowerActivationSettings settings = new(targetId, targetPosition, procPowerOwner.RegionLocation.Position);
                settings.PowerResults = powerResults;

                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }
        }

        public void TryActivateOnGotDamagedProcs(ProcTriggerType triggerType, PowerResults powerResults, float healthDelta) // 17-27
        {
            if (powerResults == null)
                return;

            if (IsInWorld == false)
                return;

            // Check if the attacker is valid for procs of this trigger type
            WorldEntity attacker = Game.EntityManager.GetEntity<WorldEntity>(powerResults.UltimateOwnerId);
            if (attacker != null && attacker.CanTriggerOtherProcs(triggerType) == false)
                return;

            // If this is a health threshold proc, make sure this entity has health
            long healthMax = Properties[PropertyEnum.HealthMax];
            if (healthMax == 0 && (triggerType == ProcTriggerType.OnGotDamagedForPctHealth || triggerType == ProcTriggerType.OnGotDamagedHealthBelowPct))
                return;

            // Get proc chance multiplier
            PowerPrototype powerProto = powerResults.PowerPrototype;
            float procChanceMultiplier = powerProto.OnHitProcChanceMultiplier;

            // Calculate param
            // NOTE: For OnGotDamagedHealthBelowPct we can use the health property because these procs are triggered after the health change occurs   
            int param = triggerType switch
            {
                ProcTriggerType.OnGotDamagedForPctHealth    => (int)(-healthDelta / healthMax * 100f),
                ProcTriggerType.OnGotDamagedHealthBelowPct  => (int)(MathHelper.Ratio(Properties[PropertyEnum.Health], healthMax) * 100f),
                _                                           => (int)-healthDelta,
            };

            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)triggerType))
            {
                if (CheckProc(kvp, out Power procPower, param, procChanceMultiplier) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnGotDamagedProcs(): procPower == null");
                    continue;
                }

                TryActivateOnGotDamagedProcHelper(procPower, powerResults);
            }

            // Keyworded procs
            KeywordsMask keywordsMask = powerResults.KeywordsMask != null ? powerResults.KeywordsMask : powerProto.KeywordsMask;

            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != triggerType)
                    continue;

                if (CheckKeywordProc(kvp, out Power procPower, keywordsMask, procChanceMultiplier) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnGotDamagedProcs(): procPower == null");
                    continue;
                }

                TryActivateOnGotDamagedProcHelper(procPower, powerResults);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(triggerType);
        }

        private void TryActivateOnGotDamagedProcHelper(Power procPower, PowerResults powerResults)
        {
            WorldEntity procPowerOwner = procPower.Owner;
            Vector3 userPosition = procPowerOwner.RegionLocation.Position;

            ulong targetId;
            Vector3 targetPosition;

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(powerResults.UltimateOwnerId);
            if (target != null && target.IsInWorld)
            {
                targetId = target.Id;
                targetPosition = target.RegionLocation.Position;
            }
            else
            {
                targetId = InvalidId;
                targetPosition = userPosition;
            }

            PowerActivationSettings settings = new(targetId, targetPosition, userPosition);
            settings.PowerResults = powerResults;

            procPowerOwner.ActivateProcPower(procPower, ref settings, this);
        }

        public bool TryActivateOnHealthProcs(PrototypeId procPowerProtoRef = PrototypeId.Invalid)  // 28-31
        {
            if (IsInWorld == false)
                return false;

            if (TestStatus(EntityStatus.ExitingWorld))
                return false;

            long healthMax = Properties[PropertyEnum.HealthMax];
            if (healthMax <= 0)
                return Logger.WarnReturn(false, "TryActivateOnHealthProcs(): healthMax <= 0");

            int param = (int)(MathHelper.Ratio(Properties[PropertyEnum.Health], healthMax) * 100f);

            using PropertyCollection procProperties = GetProcProperties(Properties);
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                switch ((ProcTriggerType)triggerTypeValue)
                {
                    case ProcTriggerType.OnHealthAbove:
                    case ProcTriggerType.OnHealthBelow:
                        if (procPowerProtoRef != PrototypeId.Invalid)
                        {
                            Property.FromParam(kvp.Key, 1, out PrototypeId paramProcPowerProtoRef);
                            if (paramProcPowerProtoRef != procPowerProtoRef)
                                continue;
                        }

                        if (CheckProc(kvp, out Power procPower, param) == false)
                            continue;

                        if (procPower == null)
                        {
                            Logger.Warn("TryActivateOnHealthProcs(): procPower == null");
                            continue;
                        }

                        WorldEntity procPowerOwner = procPower.Owner;

                        PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                        procPowerOwner.ActivateProcPower(procPower, ref settings, this);

                        break;

                    case ProcTriggerType.OnHealthAboveToggle:
                    case ProcTriggerType.OnHealthBelowToggle:
                        if (procPowerProtoRef != PrototypeId.Invalid)
                        {
                            Property.FromParam(kvp.Key, 1, out PrototypeId paramProcPowerProtoRef);
                            if (paramProcPowerProtoRef != procPowerProtoRef)
                                continue;
                        }

                        // false return here is valid and indicates that the power needs to be toggled off
                        bool toggle = CheckProc(kvp, out procPower, param);

                        if (procPower == null)
                        {
                            procPower = GetProcPower(kvp);
                            if (procPower == null)
                                continue;
                        }

                        if (procPower.IsToggled() == false)
                        {
                            Logger.Warn($"TryActivateOnHealthProcs(): Proc power [{procPower}] is not a toggled power");
                            continue;
                        }

                        procPowerOwner = procPower.Owner;

                        settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);

                        if (toggle)
                        {
                            if (procPower.IsToggledOn() == false)
                                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
                        }
                        else
                        {
                            if (procPower.IsToggledOn())
                                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
                        }

                        break;
                }
            }

            return true;
        }

        public void TryActivateOnInCombatProcs()    // 32
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnInCombat, procProperties);
        }

        public void TryActivateOnInteractedWithProcs(ProcTriggerType triggerType, WorldEntity interactor)   // 33-34
        {
            if (IsInWorld == false)
                return;

            if (interactor != null && interactor.CanTriggerOtherProcs(triggerType) == false)
                return;

            using PropertyCollection procProperties = GetProcProperties(Properties);
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)triggerType))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnInteractedWithProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                ulong targetId;
                Vector3 targetPosition;

                if (interactor != null)
                {
                    targetId = interactor.Id;
                    targetPosition = interactor.RegionLocation.Position;
                }
                else
                {
                    targetId = InvalidId;
                    targetPosition = Vector3.Zero;
                }

                PowerActivationSettings settings = new(targetId, targetPosition, procPowerOwner.RegionLocation.Position);

                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(triggerType);
        }

        public virtual void TryActivateOnKillProcs(ProcTriggerType triggerType, PowerResults powerResults)    // 35-39
        {
            if (IsInWorld == false)
                return;

            if (TryForwardOnKillProcsToOwner(triggerType, powerResults))
                return;

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(powerResults.TargetId);
            if (target != null && target.CanTriggerOtherProcs(triggerType) == false)
                return;

            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)triggerType))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnKillProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                ulong targetId;
                Vector3 targetPosition;

                if (target != null && target.IsInWorld)
                {
                    targetId = target.Id;
                    targetPosition = target.RegionLocation.Position;
                }
                else
                {
                    targetId = InvalidId;
                    targetPosition = Vector3.Zero;
                }

                PowerActivationSettings settings = new(targetId, targetPosition, procPowerOwner.RegionLocation.Position);
                settings.PowerResults = powerResults;

                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            // Keyworded procs
            KeywordGlobalsPrototype keywordGlobals = GameDatabase.KeywordGlobalsPrototype;
            DataDirectory dataDirectory = DataDirectory.Instance;

            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != triggerType)
                    continue;

                Property.FromParam(kvp.Key, 2, out PrototypeId procPowerProtoRef);

                // Keyworded OnKill procs check different keyword sources based on the type of proc of power
                Power procPower;

                if (dataDirectory.PrototypeIsChildOfBlueprint(procPowerProtoRef, (BlueprintId)keywordGlobals.EntityKeywordPrototype))
                {
                    if (target == null)
                        continue;

                    if (CheckKeywordProc(kvp, out procPower, target) == false)
                        continue;
                }
                else
                {
                    PowerPrototype powerProto = powerResults.PowerPrototype;
                    if (powerProto == null)
                        continue;

                    KeywordsMask keywordsMask = powerResults.KeywordsMask != null ? powerResults.KeywordsMask : powerProto.KeywordsMask;
                    if (CheckKeywordProc(kvp, out procPower, powerProto.KeywordsMask) == false)
                        continue;
                }

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnKillProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                ulong targetId;
                Vector3 targetPosition;

                if (target != null && target.IsInWorld)
                {
                    targetId = target.Id;
                    targetPosition = target.RegionLocation.Position;
                }
                else
                {
                    targetId = InvalidId;
                    targetPosition = Vector3.Zero;
                }

                PowerActivationSettings settings = new(targetId, targetPosition, procPowerOwner.RegionLocation.Position);
                settings.PowerResults = powerResults;

                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(triggerType);
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

        public void TryActivateOnMovementStartedProcs() // 44
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)ProcTriggerType.OnMovementStarted))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnMovementStartedProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(ProcTriggerType.OnMovementStarted);
        }

        public void TryActivateOnMovementStoppedProcs() // 45
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);

            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)ProcTriggerType.OnMovementStopped))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnMovementStoppedProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(ProcTriggerType.OnMovementStopped);
        }

        public void TryActivateOnNegStatusAppliedProcs()  // 46
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnNegStatusApplied, procProperties);
        }

        public void TryActivateOnOrbPickupProcs(Agent orb)  // 47
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyworded procs
            TryActivateProcsCommon(ProcTriggerType.OnOrbPickup, procProperties);

            // Keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != ProcTriggerType.OnOrbPickup)
                    continue;

                if (CheckKeywordProc(kvp, out Power procPower, orb) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnOrbPickupProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }
        }

        public void TryActivateOnOutCombatProcs() // 48
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);
            TryActivateProcsCommon(ProcTriggerType.OnOutCombat, procProperties);
        }

        public void TryActivateOnOverlapBeginProcs(WorldEntity target, Vector3 overlapPosition)  // 49
        {
            if (IsInWorld == false)
                return;

            using PropertyCollection procProperties = GetProcProperties(Properties);
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)ProcTriggerType.OnOverlapBegin))
                TryActivateOnOverlapBeginProcHelper(kvp, target, overlapPosition);

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(ProcTriggerType.OnOverlapBegin);
        }

        public void TryActivateOnOverlapBeginProcs(PropertyId propertyId)
        {
            // Check overlaps that are already happening when this proc is assigned
            if (propertyId.Enum != PropertyEnum.Proc)
                return;

            List<ulong> overlappingEntities = ListPool<ulong>.Instance.Get();
            if (Physics.GetOverlappingEntities(overlappingEntities))
            {
                KeyValuePair<PropertyId, PropertyValue> procProperty = new(propertyId, Properties[propertyId]);
                Vector3 overlapPosition = RegionLocation.Position;
                EntityManager entityManager = Game.EntityManager;

                foreach (ulong entityId in overlappingEntities)
                {
                    WorldEntity target = entityManager.GetEntity<WorldEntity>(entityId);
                    if (target == null)
                        continue;

                    TryActivateOnOverlapBeginProcHelper(procProperty, target, overlapPosition);
                }
            }

            ListPool<ulong>.Instance.Return(overlappingEntities);
        }

        private void TryActivateOnOverlapBeginProcHelper(in KeyValuePair<PropertyId, PropertyValue> procProperty, WorldEntity target, Vector3 overlapPosition)
        {
            if (IsInWorld == false)
                return;

            if (CheckProc(procProperty, out Power procPower) == false)
                return;

            if (procPower == null)
                return;

            if (procPower.IsValidTarget(target) == false)
                return;

            WorldEntity procPowerOwner = procPower.Owner;

            PowerActivationSettings settings = new(target.Id, target.RegionLocation.Position, overlapPosition);
            procPowerOwner.ActivateProcPower(procPower, ref settings, this);
        }
        
        public void TryActivateOnPetDeathProcs(WorldEntity pet)  // 50
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyworded procs
            TryActivateProcsCommon(ProcTriggerType.OnPetDeath, procProperties);

            // Keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != ProcTriggerType.OnPetDeath)
                    continue;

                if (CheckKeywordProc(kvp, out Power procPower, pet) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnPetDeathProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }
        }

        public void TryActivateOnPetHitProcs(PowerResults powerResults, WorldEntity pet) // 51
        {
            if (powerResults == null)
                return;

            WorldEntity target = Game.EntityManager.GetEntity<WorldEntity>(powerResults.TargetId);
            if (target != null && target.CanTriggerOtherProcs(ProcTriggerType.OnPetHit) == false)
                return;

            // Get proc chance multiplier for this power
            PowerPrototype powerProto = powerResults.PowerPrototype;
            float procChanceMultiplier = powerProto.OnHitProcChanceMultiplier;

            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesAll))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != ProcTriggerType.OnPetHit)
                    continue;

                PropertyEnum propertyEnum = kvp.Key.Enum;
                if (propertyEnum == PropertyEnum.ProcKeyword || propertyEnum == PropertyEnum.ProcNotKeyword)
                {
                    // For keyword properties here we check both the pet and its conditions
                    bool requiredKeywordState = propertyEnum == PropertyEnum.ProcKeyword;   // true for ProcKeyword, false for ProcNotKeyword
                    Property.FromParam(kvp.Key, 2, out PrototypeId keywordProtoRef);
                    KeywordPrototype keywordProto = keywordProtoRef.As<KeywordPrototype>();

                    if ((pet.HasKeyword(keywordProto) || pet.HasConditionWithKeyword(keywordProto)) != requiredKeywordState)
                        continue;
                }

                if (CheckProcChance(kvp, procChanceMultiplier) == false)
                    continue;

                Power procPower = GetProcPower(kvp);
                if (procPower == null)
                    continue;

                // NOTE: We do not check CanTrigger() for pet attacks

                WorldEntity procPowerOwner = procPower.Owner;

                if (CheckOnHitRecursion(procPower, powerProto) == false)
                    continue;

                ulong targetId;
                Vector3 targetPosition;

                if (target != null && target.IsInWorld)
                {
                    targetId = target.Id;
                    targetPosition = target.RegionLocation.Position;
                }
                else
                {
                    targetId = InvalidId;
                    targetPosition = Vector3.Zero;
                }

                PowerActivationSettings settings = new(targetId, targetPosition, procPowerOwner.RegionLocation.Position);
                settings.PowerResults = powerResults;

                Logger.Debug($"OnPetHit(): {powerProto} for [{procPowerOwner}] (pet=[{pet}])");
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(ProcTriggerType.OnPetHit);
        }

        public void TryActivateOnPowerUseProcs(ProcTriggerType triggerType, Power onPowerUsePower, ref PowerActivationSettings onPowerUseSettings)  // 58-62
        {
            if (IsInWorld == false)
                return;

            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)triggerType))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnPowerUseProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = onPowerUseSettings;
                settings.UserPosition = procPowerOwner.RegionLocation.Position;

                // Do not allow procs to trigger more on power use procs
                if (triggerType == ProcTriggerType.OnPowerUseProcEffect)
                    settings.Flags |= PowerActivationSettingsFlags.NoOnPowerUseProcs;

                procPower.Properties[PropertyEnum.OnPowerUsePowerRef] = onPowerUsePower.PrototypeDataRef;

                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            // Keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != triggerType)
                    continue;

                if (CheckKeywordProc(kvp, out Power procPower, onPowerUsePower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnPowerUseProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = onPowerUseSettings;
                settings.UserPosition = procPowerOwner.RegionLocation.Position;

                // Do not allow procs to trigger more on power use procs
                if (triggerType == ProcTriggerType.OnPowerUseProcEffect)
                    settings.Flags |= PowerActivationSettingsFlags.NoOnPowerUseProcs;

                procPower.Properties[PropertyEnum.OnPowerUsePowerRef] = onPowerUsePower.PrototypeDataRef;

                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(triggerType);
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

        public void TryActivateOnSummonPetProcs(WorldEntity pet)   // 70
        {
            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyworded procs
            TryActivateProcsCommon(ProcTriggerType.OnSummonPet, procProperties);

            // Keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != ProcTriggerType.OnSummonPet)
                    continue;

                if (CheckKeywordProc(kvp, out Power procPower, pet) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnSummonPetProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(InvalidId, Vector3.Zero, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }
        }

        public void TryActivateOnMissileHitProcs(Power power, WorldEntity target)   // 72
        {
            float procChanceMultiplier = power.Prototype.OnHitProcChanceMultiplier;

            using PropertyCollection procProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();

            // Non-keyworded procs
            TryActivateProcsCommon(ProcTriggerType.OnMissileHit, procProperties, null, procChanceMultiplier);

            // Keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != ProcTriggerType.OnMissileHit)
                    continue;

                if (CheckKeywordProc(kvp, out Power procPower, power, procChanceMultiplier) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnMissileHitProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(target.Id, target.RegionLocation.Position, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }
        }

        public virtual void TryActivateOnHotspotNegatedProcs(WorldEntity other) // 73
        {
            // TODO: Check if this works properly after we implement hotspot powers
            if (IsInWorld == false)
                return;

            if (other.CanTriggerOtherProcs(ProcTriggerType.OnHotspotNegated) == false)
                return;

            using PropertyCollection procProperties = GetProcProperties(Properties);
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)ProcTriggerType.OnHotspotNegated))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnHotspotNegatedProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(other.Id, other.RegionLocation.Position, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(ProcTriggerType.OnHotspotNegated);
        }

        public void TryActivateOnControlledEntityReleasedProcs(WorldEntity controller)  // 74
        {
            // TODO: Check if this works properly after we implement controlled entities
            if (IsInWorld == false)
                return;

            if (controller.CanTriggerOtherProcs(ProcTriggerType.OnControlledEntityReleased) == false)
                return;

            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)ProcTriggerType.OnControlledEntityReleased))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnControlledEntityReleasedProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;
                procPower.Properties.CopyProperty(procProperties, PropertyEnum.CharacterLevel);

                PowerActivationSettings settings = new(controller.Id, controller.RegionLocation.Position, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            // Keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != ProcTriggerType.OnControlledEntityReleased)
                    continue;

                if (CheckKeywordProc(kvp, out Power procPower, this) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnControlledEntityReleasedProcs(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                PowerActivationSettings settings = new(controller.Id, controller.RegionLocation.Position, procPowerOwner.RegionLocation.Position);
                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            ConditionCollection?.RemoveCancelOnProcTriggerConditions(ProcTriggerType.OnControlledEntityReleased);
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

        private void TryActivateOnBlockOrDodgeProcHelper(ProcTriggerType triggerType, PowerResults powerResults)
        {
            if (powerResults == null)
                return;

            if (IsInWorld == false)
                return;

            WorldEntity attacker = Game.EntityManager.GetEntity<WorldEntity>(powerResults.UltimateOwnerId);
            if (attacker != null && attacker.CanTriggerOtherProcs(triggerType) == false)
                return;

            using PropertyCollection procProperties = GetProcProperties(Properties);

            // Non-keyworded procs
            foreach (var kvp in procProperties.IteratePropertyRange(PropertyEnum.Proc, (int)triggerType))
            {
                if (CheckProc(kvp, out Power procPower) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnBlockOrDodgeProcHelper(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                ulong targetId;
                Vector3 targetPosition;

                if (attacker != null && attacker.IsInWorld)
                {
                    targetId = attacker.Id;
                    targetPosition = attacker.RegionLocation.Position;
                }
                else
                {
                    targetId = InvalidId;
                    targetPosition = powerResults.PowerOwnerPosition;
                }

                PowerActivationSettings settings = new(targetId, targetPosition, procPowerOwner.RegionLocation.Position);
                settings.PowerResults = powerResults;

                procPowerOwner.ActivateProcPower(procPower, ref settings, this);
            }

            // Keyworded procs
            PowerPrototype powerProto = powerResults.PowerPrototype;
            KeywordsMask keywordsMask = powerResults.KeywordsMask != null ? powerResults.KeywordsMask : powerProto.KeywordsMask;

            foreach (var kvp in procProperties.IteratePropertyRange(Property.ProcPropertyTypesKeyword))
            {
                Property.FromParam(kvp.Key, 0, out int triggerTypeValue);
                if ((ProcTriggerType)triggerTypeValue != triggerType)
                    continue;

                if (CheckKeywordProc(kvp, out Power procPower, keywordsMask) == false)
                    continue;

                if (procPower == null)
                {
                    Logger.Warn("TryActivateOnBlockOrDodgeProcHelper(): procPower == null");
                    continue;
                }

                WorldEntity procPowerOwner = procPower.Owner;

                ulong targetId;
                Vector3 targetPosition;

                if (attacker != null && attacker.IsInWorld)
                {
                    targetId = attacker.Id;
                    targetPosition = attacker.RegionLocation.Position;
                }
                else
                {
                    targetId = InvalidId;
                    targetPosition = powerResults.PowerOwnerPosition;
                }

                PowerActivationSettings settings = new(targetId, targetPosition, procPowerOwner.RegionLocation.Position);
                settings.PowerResults = powerResults;

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

        private bool CheckProc(in KeyValuePair<PropertyId, PropertyValue> procProperty, out Power procPower, int param = 0, float procChanceMultiplier = 1f)
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
            T keywordedObject, float procChanceMultiplier = 1f) where T: IKeyworded
        {
            procPower = null;

            PropertyEnum propertyEnum = procProperty.Key.Enum;
            if (propertyEnum != PropertyEnum.ProcKeyword && propertyEnum != PropertyEnum.ProcNotKeyword)
                return Logger.WarnReturn(false, $"CheckKeywordProc(): Attempted to check non-keyword proc property {procProperty.Key} for [{this}]");

            bool requiredKeywordState = propertyEnum == PropertyEnum.ProcKeyword;  // true for ProcKeyword, false for ProcNotKeyword

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
