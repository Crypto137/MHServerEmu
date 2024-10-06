using System.Text;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot.Specs;

namespace MHServerEmu.Games.Loot
{
    /// <summary>
    /// A collection of data accumulated from various <see cref="Loot.LootResult"/> instances.
    /// </summary>
    public class LootResultSummary : IPoolable, IDisposable
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public LootType Types { get; private set; }
        public bool HasAnyResult { get => Types != LootType.None; }

        public List<ItemSpec> ItemSpecs { get; } = new();
        public List<AgentSpec> AgentSpecs { get; } = new();
        public List<int> Credits { get; } = new();
        public int Experience { get; private set; }
        public int PowerPoints { get; private set; }
        public int HealthBonus { get; private set; }
        public int EnduranceBonus { get; private set; }
        public int RealMoney { get; private set; }
        public List<LootNodePrototype> CallbackNodes { get; } = new();
        public List<PrototypeId> VanityTitles { get; } = new();
        public List<VendorXPSummary> VendorXP { get; } = new();
        public List<CurrencySpec> Currencies { get; } = new();

        public void Add(in LootResult lootResult)
        {
            switch (lootResult.Type)
            {
                case LootType.Item:
                    ItemSpecs.Add(lootResult.ItemSpec);
                    break;

                default:
                    Logger.Warn($"Add(): Unimplemented LootType {lootResult.Type}");
                    break;
            }
        }

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
                foreach (VendorXPSummary vendorXP in VendorXP)
                    builder.AddVendorxp(vendorXP.ToProtobuf());
            }

            if (Types.HasFlag(LootType.Currency))
            {
                foreach (CurrencySpec currency in Currencies)
                    builder.AddCurrencies(currency.ToProtobuf());
            }

            if (Types.HasFlag(LootType.PowerPoints))
                builder.SetPowerPoints((uint)PowerPoints);

            return builder.Build();
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            if (Types.HasFlag(LootType.Item))
                sb.AppendLine($"Item {ItemSpecs[0].ItemProtoRef.GetNameFormatted()} [{ItemSpecs.Count}]");

            return sb.ToString();
        }

        public void ResetForPool()
        {
            Types = default;

            ItemSpecs.Clear();
            AgentSpecs.Clear();
            Credits.Clear();
            Experience = default;
            PowerPoints = default;
            HealthBonus = default;
            EnduranceBonus = default;
            RealMoney = default;
            CallbackNodes.Clear();
            VanityTitles.Clear();
            VendorXP.Clear();
            Currencies.Clear();
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }
    }
}