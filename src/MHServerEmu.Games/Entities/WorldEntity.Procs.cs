using MHServerEmu.Games.Entities.PowerCollections;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Powers;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities
{
    public partial class WorldEntity
    {
        // Handlers are ordered by ProcTriggerType enum

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

        public void TryActivateOnKillProcs(ProcTriggerType triggerType, PowerResults powerResults)    // 35-39
        {
            // TODO
        }

        public void TryActivateOnMissileHitProcs(Power power, WorldEntity target)   // 72
        {
            // TODO
        }
    }
}
