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
        //public List<long> CouponCodes { get; private set; } = new();  // seems to be unused
        public List<LootNodePrototype> CallbackNodes { get; } = new();
        public List<LootMutationPrototype> LootMutations { get; } = new();
        public List<PrototypeId> VanityTitles { get; } = new();
        public List<VendorXPSummary> VendorXP { get; } = new();
        public List<CurrencySpec> Currencies { get; } = new();

        public List<ItemSpec> VaporizedItemSpecs { get; } = new();
        public List<int> VaporizedCredits { get; } = new();

        public int NumDrops { get => ItemSpecs.Count + AgentSpecs.Count + Credits.Count + Currencies.Count; }

        public bool IsInPool { get; set; }

        public LootResultSummary() { }

        public void Add(in LootResult lootResult)
        {
            switch (lootResult.Type)
            {
                case LootType.Item:
                    if (lootResult.IsVaporized)
                        VaporizedItemSpecs.Add(lootResult.ItemSpec);
                    else
                        ItemSpecs.Add(lootResult.ItemSpec);

                    Types |= LootType.Item;
                    break;

                case LootType.Agent:
                    AgentSpecs.Add(lootResult.AgentSpec);
                    Types |= LootType.Agent;
                    break;

                case LootType.Credits:
                    if (lootResult.IsVaporized)
                        VaporizedCredits.Add(lootResult.Amount);
                    else
                        Credits.Add(lootResult.Amount);

                    Types |= LootType.Credits;
                    break;

                case LootType.Experience:
                    Experience += lootResult.Amount;
                    Types |= LootType.Experience;
                    break;

                case LootType.PowerPoints:
                    Logger.Debug($"Add(): powerPoints=[{lootResult.Amount}]");
                    PowerPoints += lootResult.Amount;
                    Types |= LootType.PowerPoints;
                    break;

                case LootType.HealthBonus:
                    Logger.Debug($"Add(): healthBonus=[{lootResult.Amount}]");
                    HealthBonus += lootResult.Amount;
                    Types |= LootType.HealthBonus;
                    break;

                case LootType.EnduranceBonus:
                    Logger.Debug($"Add(): enduranceBonus=[{lootResult.Amount}]");
                    EnduranceBonus += lootResult.Amount;
                    Types |= LootType.EnduranceBonus;
                    break;

                case LootType.RealMoney:
                    //Logger.Debug($"Add(): realMoney=[{lootResult.Amount}]");
                    RealMoney += lootResult.RealMoneyProto.NumMin;
                    Types |= LootType.RealMoney;
                    break;

                case LootType.CallbackNode:
                    CallbackNodes.Add(lootResult.CallbackNodeProto);
                    Types |= LootType.CallbackNode;
                    break;

                case LootType.LootMutation:
                    LootMutations.Add(lootResult.LootMutationProto);
                    Types |= LootType.LootMutation;
                    break;

                case LootType.VanityTitle:
                    VanityTitles.Add(lootResult.VanityTitleProtoRef);
                    Types |= LootType.VanityTitle;
                    break;

                case LootType.VendorXP:
                    VendorXP.Add(lootResult.VendorXPSummary);
                    Types |= LootType.VendorXP;
                    break;

                case LootType.Currency:
                    Currencies.Add(lootResult.CurrencySpec);
                    Types |= LootType.Currency;
                    break;

                default:
                    Logger.Warn($"Add(): Unimplemented LootType {lootResult.Type}");
                    break;
            }
        }

        public void CombineCurrencyStacks(PrototypeId itemOrAgentProtoRef, PrototypeId currencyRef)
        {
            if (Currencies.Count == 0)
                return;

            int totalAmount = 0;

            // Iterate from the end to be able to remove currency specs as we calculate the total amount
            for (int i = Currencies.Count - 1; i >= 0; i--)
            {
                CurrencySpec currencySpec = Currencies[i];

                if (currencySpec.AgentOrItemProtoRef != itemOrAgentProtoRef || currencySpec.CurrencyRef != currencyRef)
                    continue;

                totalAmount += currencySpec.Amount;
                Currencies.RemoveAt(i);
            }

            if (totalAmount == 0)
                return;

            Currencies.Add(new(itemOrAgentProtoRef, currencyRef, totalAmount));
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
                builder.SetExperience((uint)Experience);

            if (Types.HasFlag(LootType.HealthBonus))
                builder.SetHealthBonus((uint)HealthBonus);

            if (Types.HasFlag(LootType.Item))
            {
                foreach (ItemSpec itemSpec in ItemSpecs)
                    builder.AddItems(itemSpec.ToStackProtobuf());
            }

            if (Types.HasFlag(LootType.RealMoney))
                builder.SetRealMoney((uint)RealMoney);

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
            return $"Types=[{Types}]";
        }

        public string ToStringVerbose()
        {
            StringBuilder sb = new();

            if (Types.HasFlag(LootType.Item))
            {
                sb.AppendLine("Item:");

                foreach (ItemSpec itemSpec in ItemSpecs)
                    sb.AppendLine($"\titemProtoRef={itemSpec.ItemProtoRef.GetName()}, rarity={GameDatabase.GetFormattedPrototypeName(itemSpec.RarityProtoRef)}");
            }

            if (Types.HasFlag(LootType.Agent))
            {
                sb.AppendLine("Agent:");

                foreach (AgentSpec agentSpec in AgentSpecs)
                    sb.AppendLine($"\t{agentSpec}");
            }

            if (Types.HasFlag(LootType.Credits))
            {
                sb.AppendLine("Credits:");

                sb.Append($"\t{Credits[0]}");

                for (int i = 1; i < Credits.Count; i++)
                    sb.Append($"+{Credits[i]}");

                sb.AppendLine();
            }

            if (Types.HasFlag(LootType.Experience))
            {
                sb.AppendLine("Experience:");
                // TODO
            }

            if (Types.HasFlag(LootType.PowerPoints))
            {
                sb.AppendLine("PowerPoints:");
                // TODO
            }

            if (Types.HasFlag(LootType.HealthBonus))
            {
                sb.AppendLine("HealthBonus:");
                // TODO
            }

            if (Types.HasFlag(LootType.EnduranceBonus))
            {
                sb.AppendLine("EnduranceBonus:");
                // TODO
            }

            if (Types.HasFlag(LootType.RealMoney))
            {
                sb.AppendLine("RealMoney:");
                // TODO
            }

            if (Types.HasFlag(LootType.CallbackNode))
            {
                sb.AppendLine("CallbackNode:");
                // TODO
            }

            if (Types.HasFlag(LootType.LootMutation))
            {
                sb.AppendLine("LootMutation:");
                // TODO
            }

            if (Types.HasFlag(LootType.VanityTitle))
            {
                sb.AppendLine("VanityTitle:");
                // TODO
            }

            if (Types.HasFlag(LootType.VendorXP))
            {
                sb.AppendLine("VendorXP:");
                // TODO
            }

            if (Types.HasFlag(LootType.Currency))
            {
                sb.AppendLine("Currency:");

                foreach (CurrencySpec currencySpec in Currencies)
                    sb.AppendLine($"\t{currencySpec}");
            }

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
            LootMutations.Clear();
            VanityTitles.Clear();
            VendorXP.Clear();
            Currencies.Clear();

            VaporizedItemSpecs.Clear();
            VaporizedCredits.Clear();
        }

        public void Dispose()
        {
            ObjectPoolManager.Instance.Return(this);
        }
    }
}