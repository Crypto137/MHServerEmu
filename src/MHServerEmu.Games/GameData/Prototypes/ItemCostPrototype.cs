using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;

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
        public EvalPrototype Number { get; protected set; }
        public PrototypeId Currency { get; protected set; }
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
            // TODO
            return 0;
        }
    }
}
