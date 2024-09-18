using Gazillion;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Entities.Items
{
    public class CurrencySpec       // This class looks pretty struct-y
    {
        private PrototypeId _agentOrItemProtoRef;
        private PrototypeId _currencyRef;
        private int _amount;

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
            return string.Format("{0}={1}, {2}={3}, {4}={5}",
                nameof(_agentOrItemProtoRef), GameDatabase.GetPrototypeName(_agentOrItemProtoRef),
                nameof(_currencyRef), GameDatabase.GetFormattedPrototypeName(_currencyRef),
                nameof(_amount), _amount);
        }

        public void ApplyCurrency(PropertyCollection properties)
        {
            properties[PropertyEnum.ItemCurrency, _currencyRef] = _amount;
        }
    }
}
