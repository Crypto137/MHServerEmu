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
            if (context is not UseAffixPowerContext) return;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return;
            BehaviorBlackboard blackboard = ownerController.Blackboard;

            PrototypeId randomAffixRef = blackboard.PropertyCollection[PropertyEnum.AIAffixPowerToActivate];
            if (randomAffixRef == PrototypeId.Invalid) return;

            var usePowerContext = new UsePowerContext()
            {
                OwnerController = ownerController,
                Power = randomAffixRef
            };

            UsePower.Instance.Start(usePowerContext);
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            var failResult = StaticBehaviorReturnType.Failed;
            if (context is not UseAffixPowerContext) return failResult;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return failResult;
            BehaviorBlackboard blackboard = ownerController.Blackboard;

            PrototypeId randomAffixRef = blackboard.PropertyCollection[PropertyEnum.AIAffixPowerToActivate];
            if (randomAffixRef == PrototypeId.Invalid) return failResult;

            var usePowerContext = new UsePowerContext()
            {
                OwnerController = ownerController,
                Power = randomAffixRef
            };

            return UsePower.Instance.Update(usePowerContext);
        }

        public bool Validate(in IStateContext context)
        {
            if (context is not UseAffixPowerContext) return false;
            AIController ownerController = context.OwnerController;
            if (ownerController == null) return false;
            Agent agent = ownerController.Owner;
            if (agent == null) return false;
            Game game = agent.Game;
            if (game == null) return false;

            Picker<PrototypeId> randomAffixPower = new(game.Random);

            foreach (var kvp in agent.Properties.IteratePropertyRange(PropertyEnum.EnemyBoost))
            {
                if (kvp.Value == false) continue;

                Property.FromParam(kvp.Key, 0, out PrototypeId enemyBoostRef);
                if (enemyBoostRef == PrototypeId.Invalid) return false;

                EnemyBoostPrototype enemyBoostProto = GameDatabase.GetPrototype<EnemyBoostPrototype>(enemyBoostRef);
                if (enemyBoostProto == null) return false;

                if (enemyBoostProto.ActivePower != PrototypeId.Invalid)
                    randomAffixPower.Add(enemyBoostProto.ActivePower);
            }

            while (randomAffixPower.Empty() == false)
            {
                randomAffixPower.PickRemove(out PrototypeId randomAffixRef);
                if (randomAffixRef == PrototypeId.Invalid) return false;

                UsePowerContext usePowerContext = new()
                {
                    OwnerController = ownerController,
                    Power = randomAffixRef
                };

                if (UsePower.Instance.Validate(usePowerContext))
                {
                    BehaviorBlackboard blackboard = ownerController.Blackboard;
                    blackboard.PropertyCollection[PropertyEnum.AIAffixPowerToActivate] = randomAffixRef;

                    if ((long)game.GetCurrentTime().TotalMilliseconds >= blackboard.PropertyCollection[PropertyEnum.AIProceduralPowerSpecificCDTime, randomAffixRef])
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
