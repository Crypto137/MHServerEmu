using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class UseAffixPower : IAIState, ISingleton<UseAffixPower>
    {
        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            throw new NotImplementedException();
        }

        public void Start(IStateContext context)
        {
            throw new NotImplementedException();
        }

        public StaticBehaviorReturnType Update(IStateContext context)
        {
            throw new NotImplementedException();
        }

        public bool Validate(IStateContext context)
        {
            throw new NotImplementedException();
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
