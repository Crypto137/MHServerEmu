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
        // Handlers are ordered by ProcTriggerType enum

        public void TryActivateOnHitProcs(ProcTriggerType triggerType, PowerResults powerResults)   // 1-3, 10, 52-56, 71
        {

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

            // TODO: Proper implementation

            // HACK: Activate cooldown for Moon Knight's signature
            if (condition.CreatorPowerPrototypeRef == (PrototypeId)924314278184884866)
            {
                PowerIndexProperties indexProps = new(0, CharacterLevel, CombatLevel);
                AssignPower((PrototypeId)10152747549179582463, indexProps);
                PowerActivationSettings settings = new(Id, default, RegionLocation.Position);
                ActivatePower((PrototypeId)10152747549179582463, ref settings);
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

        public void TryActivateOnGotAttackedProcs(PowerResults powerResults)    // 16
        {
            // TODO
        }

        public void TryActivateOnGotDamagedProcs(PowerResults powerResults) // 11, 17-27
        {
            // TODO
        }

        public void TryActivateOnKillProcs(ProcTriggerType triggerType, PowerResults powerResults)    // 35-39
        {
            // TODO
        }

        public void TryActivateOnPetHitProcs(PowerResults powerResults, WorldEntity summon) // 51
        {

        }

        public void TryActivateOnMissileHitProcs(Power power, WorldEntity target)   // 72
        {
            // TODO
        }
    }
}
