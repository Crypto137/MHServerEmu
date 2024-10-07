using Gazillion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Loot.Specs
{
    public readonly struct CurrencySpec
    {
        public PrototypeId AgentOrItemProtoRef { get; }
        public PrototypeId CurrencyRef { get; }
        public int Amount { get; }

        public bool IsAgent { get => AgentOrItemProtoRef != PrototypeId.Invalid && GameDatabase.DataDirectory.PrototypeIsA<AgentPrototype>(AgentOrItemProtoRef); }
        public bool IsItem { get => AgentOrItemProtoRef != PrototypeId.Invalid && GameDatabase.DataDirectory.PrototypeIsA<ItemPrototype>(AgentOrItemProtoRef); }

        public CurrencySpec(PrototypeId agentOrItemProtoRef, PrototypeId currencyRef, int amount)
        {
            AgentOrItemProtoRef = agentOrItemProtoRef;
            CurrencyRef = currencyRef;
            Amount = amount;
        }

        public NetStructCurrencySpec ToProtobuf()
        {
            return NetStructCurrencySpec.CreateBuilder()
                .SetAgentOrItemProtoRef((ulong)AgentOrItemProtoRef)
                .SetCurrencyRef((ulong)CurrencyRef)
                .SetAmount((uint)Amount)
                .Build();
        }

        public override string ToString()
        {
            return $"agentOrItemProtoRef={AgentOrItemProtoRef.GetName()}, currencyRef={CurrencyRef.GetNameFormatted()}, amount={Amount}";
        }

        public void ApplyCurrency(PropertyCollection properties)
        {
            properties[PropertyEnum.ItemCurrency, CurrencyRef] = Amount;
        }
    }
}
