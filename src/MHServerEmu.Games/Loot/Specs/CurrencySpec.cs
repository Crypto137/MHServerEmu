using Gazillion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Loot.Specs
{
    public readonly struct CurrencySpec
    {
        private readonly PrototypeId _agentOrItemProtoRef;
        private readonly PrototypeId _currencyRef;
        private readonly int _amount;

        public bool IsAgent { get => _agentOrItemProtoRef != PrototypeId.Invalid && GameDatabase.DataDirectory.PrototypeIsA<AgentPrototype>(_agentOrItemProtoRef); }
        public bool IsItem { get => _agentOrItemProtoRef != PrototypeId.Invalid && GameDatabase.DataDirectory.PrototypeIsA<ItemPrototype>(_agentOrItemProtoRef); }

        public CurrencySpec(PrototypeId agentOrItemProtoRef, PrototypeId currencyRef, int amount)
        {
            _agentOrItemProtoRef = agentOrItemProtoRef;
            _currencyRef = currencyRef;
            _amount = amount;
        }

        public CurrencySpec(NetStructCurrencySpec protobuf)
        {
            _agentOrItemProtoRef = (PrototypeId)protobuf.AgentOrItemProtoRef;
            _currencyRef = (PrototypeId)protobuf.CurrencyRef;
            _amount = (int)protobuf.Amount;
        }

        public NetStructCurrencySpec ToProtobuf()
        {
            return NetStructCurrencySpec.CreateBuilder()
                .SetAgentOrItemProtoRef((ulong)_agentOrItemProtoRef)
                .SetCurrencyRef((ulong)_currencyRef)
                .SetAmount((uint)_amount)
                .Build();
        }

        public override string ToString()
        {
            return $"agentOrItemProtoRef={_agentOrItemProtoRef.GetName()}, currencyRef={_currencyRef.GetNameFormatted()}, amount={_amount}";
        }

        public void ApplyCurrency(PropertyCollection properties)
        {
            properties[PropertyEnum.ItemCurrency, _currencyRef] = _amount;
        }
    }
}
