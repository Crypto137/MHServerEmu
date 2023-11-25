namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ProductPrototype : Prototype
    {
        public bool ForSale { get; set; }
    }

    public class CurrencyPrototype : Prototype
    {
        public ulong CostString { get; set; }
        public ulong DisplayName { get; set; }
        public ulong Icon { get; set; }
        public ulong Tooltip { get; set; }
        public ulong IconSmall { get; set; }
        public int MaxAmount { get; set; }
        public ulong IconHiRes { get; set; }
        public ulong LootBonusFlatCurve { get; set; }
        public ulong LootBonusPctCurve { get; set; }
    }

    public class ItemCostComponentPrototype : Prototype
    {
    }

    public class ItemCostCreditsPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; set; }
        public ulong Currency { get; set; }
    }

    public class ItemCostLegendaryMarksPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; set; }
        public EvalPrototype NumberExt { get; set; }
    }

    public class ItemCostRunestonesPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; set; }
        public EvalPrototype NumberExt { get; set; }
    }

    public class ItemCostItemStackPrototype : ItemCostComponentPrototype
    {
        public ulong CurrencyItem { get; set; }
        public EvalPrototype Number { get; set; }
    }

    public class ItemCostCurrencyPrototype : ItemCostComponentPrototype
    {
        public ulong Currency { get; set; }
        public EvalPrototype Amount { get; set; }
    }

    public class ItemCostPrototype : Prototype
    {
        public ItemCostComponentPrototype[] Components { get; set; }
        public EvalPrototype Credits { get; set; }
        public EvalPrototype Runestones { get; set; }
    }
}
