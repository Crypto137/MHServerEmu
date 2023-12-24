namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ProductPrototype : Prototype
    {
        public bool ForSale { get; private set; }
    }

    public class CurrencyPrototype : Prototype
    {
        public ulong CostString { get; private set; }
        public ulong DisplayName { get; private set; }
        public ulong Icon { get; private set; }
        public ulong Tooltip { get; private set; }
        public ulong IconSmall { get; private set; }
        public int MaxAmount { get; private set; }
        public ulong IconHiRes { get; private set; }
        public ulong LootBonusFlatCurve { get; private set; }
        public ulong LootBonusPctCurve { get; private set; }
    }

    public class ItemCostComponentPrototype : Prototype
    {
    }

    public class ItemCostCreditsPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; private set; }
        public ulong Currency { get; private set; }
    }

    public class ItemCostLegendaryMarksPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; private set; }
        public EvalPrototype NumberExt { get; private set; }
    }

    public class ItemCostRunestonesPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; private set; }
        public EvalPrototype NumberExt { get; private set; }
    }

    public class ItemCostItemStackPrototype : ItemCostComponentPrototype
    {
        public ulong CurrencyItem { get; private set; }
        public EvalPrototype Number { get; private set; }
    }

    public class ItemCostCurrencyPrototype : ItemCostComponentPrototype
    {
        public ulong Currency { get; private set; }
        public EvalPrototype Amount { get; private set; }
    }

    public class ItemCostPrototype : Prototype
    {
        public ItemCostComponentPrototype[] Components { get; private set; }
        public EvalPrototype Credits { get; private set; }
        public EvalPrototype Runestones { get; private set; }
    }
}
