using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Behavior.StaticAI
{
    public class UseAffixPower : IAIState
    {
        public static UseAffixPower Instance { get; } = new();
        private UseAffixPower() { }

        public void End(AIController ownerController, StaticBehaviorReturnType state)
        {
            throw new NotImplementedException();
        }

        public void Start(in IStateContext context)
        {
            throw new NotImplementedException();
        }

        public StaticBehaviorReturnType Update(in IStateContext context)
        {
            throw new NotImplementedException();
        }

        public bool Validate(in IStateContext context)
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
