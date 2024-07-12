using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData.LiveTuning;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Properties.Evals;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ProductPrototype : Prototype
    {
        public bool ForSale { get; protected set; }
    }

    public class CurrencyPrototype : Prototype
    {
        public LocaleStringId CostString { get; protected set; }
        public LocaleStringId DisplayName { get; protected set; }
        public AssetId Icon { get; protected set; }
        public LocaleStringId Tooltip { get; protected set; }
        public AssetId IconSmall { get; protected set; }
        public int MaxAmount { get; protected set; }
        public AssetId IconHiRes { get; protected set; }
        public CurveId LootBonusFlatCurve { get; protected set; }
        public CurveId LootBonusPctCurve { get; protected set; }
    }

    public class ItemCostComponentPrototype : Prototype
    {
    }

    public class ItemCostCreditsPrototype : ItemCostComponentPrototype
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public EvalPrototype Number { get; protected set; }
        public PrototypeId Currency { get; protected set; }

        public int GetNoStackSellPrice(Player player, ItemSpec itemSpec, Item item)
        {
            int price = GetNoStackBasePrice(player, itemSpec, item);
            if (price <= 0) return price;

            float floatPrice = price;
            int itemLevel = item != null ? item.Properties[PropertyEnum.ItemLevel] : itemSpec.ItemLevel;

            GlobalsPrototype globalsProto = GameDatabase.GlobalsPrototype;
            if (globalsProto?.ItemPriceMultiplierSellToVendor == null)
                return Logger.WarnReturn(price, "GetNoStackSellPrice(): globalsProto?.ItemPriceMultiplierSellToVendor == null");

            EvalContextData contextData = new();
            contextData.SetVar_Int(EvalContext.Var1, itemLevel);
            float globalItemSellPriceMultiplier = Eval.RunFloat(globalsProto.ItemPriceMultiplierSellToVendor, contextData);

            if (globalItemSellPriceMultiplier > 0f)
            {
                floatPrice *= globalItemSellPriceMultiplier;
            }
            else
            {
                Logger.Warn("GetNoStackSellPrice(): globalItemSellPriceMultiplier < 0f");
            }

            floatPrice *= LiveTuningManager.GetLiveGlobalTuningVar(Gazillion.GlobalTuningVar.eGTV_VendorSellPrice);

            return (int)floatPrice;
        }

        private int GetNoStackBasePrice(Player player, ItemSpec itemSpec, Item item)
        {
            RarityPrototype rarityProto = GameDatabase.GetPrototype<RarityPrototype>(itemSpec.RarityProtoRef);
            int rarityTier = rarityProto != null ? rarityProto.Tier : 0;
            int numAffixes = itemSpec.AffixSpecs.Count();
            int itemLevel = item != null ? item.Properties[PropertyEnum.ItemLevel] : itemSpec.ItemLevel;

            EvalContextData contextData = new();
            contextData.SetReadOnlyVar_PropertyCollectionPtr(EvalContext.Default, player.Properties);
            contextData.SetVar_Int(EvalContext.Var1, rarityTier);
            contextData.SetVar_Int(EvalContext.Var2, numAffixes);
            contextData.SetVar_Int(EvalContext.Var3, itemLevel);

            return Eval.RunInt(Number, contextData);
        }
    }

    public class ItemCostLegendaryMarksPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; protected set; }
        public EvalPrototype NumberExt { get; protected set; }
    }

    public class ItemCostRunestonesPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; protected set; }
        public EvalPrototype NumberExt { get; protected set; }
    }

    public class ItemCostItemStackPrototype : ItemCostComponentPrototype
    {
        public PrototypeId CurrencyItem { get; protected set; }
        public EvalPrototype Number { get; protected set; }
    }

    public class ItemCostCurrencyPrototype : ItemCostComponentPrototype
    {
        public PrototypeId Currency { get; protected set; }
        public EvalPrototype Amount { get; protected set; }
    }

    public class ItemCostPrototype : Prototype
    {
        public ItemCostComponentPrototype[] Components { get; protected set; }
        public EvalPrototype Credits { get; protected set; }
        public EvalPrototype Runestones { get; protected set; }

        public int GetSellPriceInCredits(Player player, Item item)
        {
            return GetNoStackSellPriceInCredits(player, item.ItemSpec, item) * item.CurrentStackSize;
        }

        public int GetNoStackSellPriceInCredits(Player player, ItemSpec itemSpec, Item item)
        {
            int price = 0;

            if (Components.HasValue())
            {
                foreach (ItemCostComponentPrototype costComponentProto in Components)
                {
                    if (costComponentProto is not ItemCostCreditsPrototype creditsCostComponentProto)
                        continue;

                    price += creditsCostComponentProto.GetNoStackSellPrice(player, itemSpec, item);
                }
            }

            return price;
        }
    }
}
