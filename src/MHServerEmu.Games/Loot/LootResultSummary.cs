using System.Text;
using Gazillion;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot.Specs;

namespace MHServerEmu.Games.Loot
{
    public class LootResultSummary
    {
        public LootType Types { get; set; }
        public List<AgentSpec> AgentSpecs { get; private set; } = new();
        public List<int> Credits { get; private set; } = new();
        public int EnduranceBonus { get; private set; }
        public int Experience { get; private set; }
        public int HealthBonus { get; private set; }
        public List<ItemSpec> ItemSpecs { get; set; } = new();
        public int RealMoney { get; private set; }

        public bool LootResult { get => Types != LootType.None; }

        public List<LootNodePrototype> CallbackNodes { get; private set; } = new();
        public List<PrototypeId> VanityTitles { get; private set; } = new();
        public List<VendorXPSummary> Vendors { get; private set; } = new();
        public List<CurrencySpec> Currencies { get; private set; } = new();
        public uint PowerPoints { get; private set; }

        public NetStructLootResultSummary ToProtobuf()
        {
            var builder = NetStructLootResultSummary.CreateBuilder();

            if (Types.HasFlag(LootType.Agent))
            {
                foreach (AgentSpec agentSpec in AgentSpecs)
                    builder.AddAgents(agentSpec.ToProtobuf());
            }

            if (Types.HasFlag(LootType.Credits))
            {
                foreach (int creditsAmount in Credits)
                    builder.AddCredits(creditsAmount);
            }

            if (Types.HasFlag(LootType.EnduranceBonus))
                builder.SetEnduranceBonus((uint)EnduranceBonus);

            if (Types.HasFlag(LootType.Experience))
                builder.SetEnduranceBonus((uint)Experience);

            if (Types.HasFlag(LootType.HealthBonus))
                builder.SetEnduranceBonus((uint)HealthBonus);

            if (Types.HasFlag(LootType.Item))
            {
                foreach (ItemSpec itemSpec in ItemSpecs)
                    builder.AddItems(itemSpec.ToStackProtobuf());
            }

            if (Types.HasFlag(LootType.RealMoney))
                builder.SetEnduranceBonus((uint)RealMoney);

            if (Types.HasFlag(LootType.CallbackNode))
            {
                foreach (LootNodePrototype callbackNode in CallbackNodes)
                    builder.AddCallbackNodes((ulong)callbackNode.DataRef);
            }

            if (Types.HasFlag(LootType.VanityTitle))
            {
                foreach (PrototypeId protoRef in VanityTitles)
                    builder.AddProtorefs((ulong)protoRef);
            }

            if (Types.HasFlag(LootType.VendorXP))
            {
                foreach (VendorXPSummary vendorXP in Vendors)
                    builder.AddVendorxp(vendorXP.ToProtobuf());
            }

            if (Types.HasFlag(LootType.Currency))
            {
                foreach (CurrencySpec currency in Currencies)
                    builder.AddCurrencies(currency.ToProtobuf());
            }

            if (Types.HasFlag(LootType.PowerPoints))
                builder.SetPowerPoints(PowerPoints);

            return builder.Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            if (Types.HasFlag(LootType.Item))
                sb.AppendLine($"Item {ItemSpecs[0].ItemProtoRef.GetNameFormatted()} [{ItemSpecs.Count}]");

            return sb.ToString();
        }
    }
}