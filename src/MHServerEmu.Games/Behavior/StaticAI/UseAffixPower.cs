using MHServerEmu.Core.Collections;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class UseAffixPower : IAIState
    {
        public static UseAffixPower Instance { get; } = new();
        private UseAffixPower() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            UsePower.Instance.End(ownerController, state);
        }

        public void Start(in IStateContext context)
        {
            if (context == null) return;
            AIController ownerController = context.OwnerController;

            if (ownerController == null) return;
            BehaviorBlackboard blackboard = ownerController.Blackboard;

            PrototypeId powerId = blackboard.PropertyCollection[PropertyEnum.AIAffixPowerToActivate];
            if (powerId == PrototypeId.Invalid) return;

            UsePowerContext usePowerContext = new UsePowerContext
            {
                OwnerController = ownerController,
                Power = powerId
            };

            UsePower.Instance.Start(usePowerContext);
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;
            if (context == null) return failResult;

            AIController ownerController = context.OwnerController;
            if (ownerController == null) return failResult;

            BehaviorBlackboard blackboard = ownerController.Blackboard;

            PrototypeId powerId = blackboard.PropertyCollection[PropertyEnum.AIAffixPowerToActivate];
            if (powerId == PrototypeId.Invalid) return failResult;

            UsePowerContext usePowerContext = new UsePowerContext
            {
                OwnerController = ownerController,
                Power = powerId
            };

            return UsePower.Instance.Update(usePowerContext);
        }

        public bool Validate(in IStateContext context)
        {
            if (context == null) return false;

            AIController ownerController = context.OwnerController;
            if (ownerController == null) return false;

            Agent agent = ownerController.Owner;
            if (agent == null) return false;

            Game game = agent.Game;
            if (game == null) return false;

            Picker<PrototypeId> randomAffixPower = new(game.Random);

            foreach (var kvp in agent.Properties.IteratePropertyRange(PropertyEnum.EnemyBoost))
            {
                if (kvp.Value == false)
                    continue;

                Property.FromParam(kvp.Key, 0, out PrototypeId enemyBoost);
                if (enemyBoost == PrototypeId.Invalid) return false;

                EnemyBoostPrototype prototype = GameDatabase.GetPrototype<EnemyBoostPrototype>(enemyBoost);
                if (prototype == null) return false;

                if (prototype.ActivePower != PrototypeId.Invalid)
                    randomAffixPower.Add(prototype.ActivePower);
            }

            while (randomAffixPower.Empty() == false)
            {
                randomAffixPower.PickRemove(out PrototypeId randomAffix);

                if (randomAffix == PrototypeId.Invalid) return false;

                UsePowerContext usePowerContext = new()
                {
                    OwnerController = ownerController,
                    Power = randomAffix
                };

                if (UsePower.Instance.Validate(usePowerContext))
                {
                    BehaviorBlackboard blackboard = ownerController.Blackboard;
                    blackboard.PropertyCollection[PropertyEnum.AIAffixPowerToActivate] = randomAffix;

                    if ((long)game.GetCurrentTime().TotalMilliseconds >= blackboard.PropertyCollection[PropertyEnum.AIProceduralPowerSpecificCDTime, randomAffix])
                        return true;
                }
            }

            return false;
        }
    }

    public struct UseAffixPowerContext : IStateContext
    {
        public AIController OwnerController { get; set; }
        public UseAffixPowerContext(AIController ownerController, UseAffixPowerContextPrototype proto)
        {
            OwnerController = ownerController;
        }
    }

}
