using System.Text;
using Gazillion;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Loot
{
    public class LootResultSummary
    {
        public LootTypes Types { get; set; }
        public List<AgentSpec> AgentSpecs { get; private set; } = new();
        public List<int> Credits { get; private set; } = new();
        public uint EnduranceBonus { get; private set; }
        public uint Experience { get; private set; }
        public uint HealthBonus { get; private set; }
        public List<ItemSpec> ItemSpecs { get; set; } = new();
        public uint RealMoney { get; private set; }
        public bool LootResult { get => Types != LootTypes.None; }
        public List<LootNodePrototype> CallbackNodes { get; private set; } = new();
        public List<PrototypeId> VanityTitles { get; private set; } = new();
        public List<VendorXPSummary> Vendors { get; private set; } = new();
        public List<CurrencySpec> Currencies { get; private set; } = new();
        public uint PowerPoints { get; private set; }

        public NetStructLootResultSummary ToProtobuf()
        {
            var message = NetStructLootResultSummary.CreateBuilder();

            if (Types.HasFlag(LootTypes.Agent))
                foreach (var agentSpec in AgentSpecs)
                    message.AddAgents(agentSpec.ToProtobuf());

            if (Types.HasFlag(LootTypes.Credits))
                foreach (var creditsAmount in Credits)
                    message.AddCredits(creditsAmount);

            if (Types.HasFlag(LootTypes.EnduranceBonus)) message.SetEnduranceBonus(EnduranceBonus);
            if (Types.HasFlag(LootTypes.Experience)) message.SetEnduranceBonus(Experience);
            if (Types.HasFlag(LootTypes.HealthBonus)) message.SetEnduranceBonus(HealthBonus);

            if (Types.HasFlag(LootTypes.Item))
                foreach (var itemSpec in ItemSpecs)
                    message.AddItems(itemSpec.ToStackProtobuf());

            if (Types.HasFlag(LootTypes.RealMoney)) message.SetEnduranceBonus(RealMoney);

            if (Types.HasFlag(LootTypes.CallbackNode))
                foreach (var callbackNode in CallbackNodes)
                    message.AddCallbackNodes((ulong)callbackNode.DataRef);

            if (Types.HasFlag(LootTypes.VanityTitle))
                foreach (var protoRef in VanityTitles)
                    message.AddProtorefs((ulong)protoRef);

            if (Types.HasFlag(LootTypes.VendorXP))
                foreach (var vendor in Vendors)
                    message.AddVendorxp(vendor.ToProtobuf());

            if (Types.HasFlag(LootTypes.Currency))
                foreach (var currency in Currencies)
                    message.AddCurrencies(currency.ToProtobuf());

            if (Types.HasFlag(LootTypes.PowerPoints)) message.SetPowerPoints(PowerPoints);

            return message.Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            if (Types.HasFlag(LootTypes.Item))
                sb.AppendLine($"Item {ItemSpecs[0].ItemProtoRef.GetNameFormatted()} [{ItemSpecs.Count}]");

            return sb.ToString();
        }
    }

    public struct VendorXPSummary
    {
        public ulong VendorProtoRef;
        public uint XpAmount;

        public NetStructVendorXPSummary ToProtobuf()
        {
            return NetStructVendorXPSummary.CreateBuilder()
                .SetVendorProtoRef(VendorProtoRef)
                .SetXpAmount(XpAmount)
                .Build();
        }
    }

    public struct AgentSpec
    {
        public uint AgentLevel;
        public uint CreditsAmount;
        public PrototypeId AgentProtoRef;

        public NetStructAgentSpec ToProtobuf()
        {
            return NetStructAgentSpec.CreateBuilder()
                .SetAgentLevel(AgentLevel)
                .SetCreditsAmount(CreditsAmount)
                .SetAgentProtoRef((ulong)AgentProtoRef)
                .Build();
        }
    }
}