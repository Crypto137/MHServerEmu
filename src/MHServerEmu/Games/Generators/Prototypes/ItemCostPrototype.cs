using MHServerEmu.Games.GameData.Prototypes;


namespace MHServerEmu.Games.Generators.Prototypes
{
    public class ProductPrototype : Prototype
    {
        public bool ForSale;
        public ProductPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ProductPrototype), proto); }
    }

    public class CurrencyPrototype : Prototype
    {
        public ulong CostString;
        public ulong DisplayName;
        public ulong Icon;
        public ulong Tooltip;
        public ulong IconSmall;
        public int MaxAmount;
        public ulong IconHiRes;
        public ulong LootBonusFlatCurve;
        public ulong LootBonusPctCurve;
        public CurrencyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CurrencyPrototype), proto); }
    }

    public class ItemCostComponentPrototype : Prototype
    {
        public ItemCostComponentPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemCostComponentPrototype), proto); }
    }

    public class ItemCostCreditsPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number;
        public ulong Currency;
        public ItemCostCreditsPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemCostCreditsPrototype), proto); }
    }

    public class ItemCostLegendaryMarksPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number;
        public EvalPrototype NumberExt;
        public ItemCostLegendaryMarksPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemCostLegendaryMarksPrototype), proto); }
    }

    public class ItemCostRunestonesPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number;
        public EvalPrototype NumberExt;
        public ItemCostRunestonesPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemCostRunestonesPrototype), proto); }
    }

    public class ItemCostItemStackPrototype : ItemCostComponentPrototype
    {
        public ulong CurrencyItem;
        public EvalPrototype Number;
        public ItemCostItemStackPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemCostItemStackPrototype), proto); }
    }

    public class ItemCostCurrencyPrototype : ItemCostComponentPrototype
    {
        public ulong Currency;
        public EvalPrototype Amount;
        public ItemCostCurrencyPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemCostCurrencyPrototype), proto); }
    }

    public class ItemCostPrototype : Prototype
    {
        public ItemCostComponentPrototype[] Components;
        public EvalPrototype Credits;
        public EvalPrototype Runestones;
        public ItemCostPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(ItemCostPrototype), proto); }
    }
}
