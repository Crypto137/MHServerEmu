namespace MHServerEmu.Games.GameData.Prototypes
{
    public class ProductPrototype : Prototype
    {
        public bool ForSale { get; protected set; }
    }

    public class CurrencyPrototype : Prototype
    {
        public ulong CostString { get; protected set; }
        public ulong DisplayName { get; protected set; }
        public ulong Icon { get; protected set; }
        public ulong Tooltip { get; protected set; }
        public ulong IconSmall { get; protected set; }
        public int MaxAmount { get; protected set; }
        public ulong IconHiRes { get; protected set; }
        public ulong LootBonusFlatCurve { get; protected set; }
        public ulong LootBonusPctCurve { get; protected set; }
    }

    public class ItemCostComponentPrototype : Prototype
    {
    }

    public class ItemCostCreditsPrototype : ItemCostComponentPrototype
    {
        public EvalPrototype Number { get; protected set; }
        public ulong Currency { get; protected set; }
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
        public ulong CurrencyItem { get; protected set; }
        public EvalPrototype Number { get; protected set; }
    }

    public class ItemCostCurrencyPrototype : ItemCostComponentPrototype
    {
        public ulong Currency { get; protected set; }
        public EvalPrototype Amount { get; protected set; }
    }

    public class ItemCostPrototype : Prototype
    {
        public ItemCostComponentPrototype[] Components { get; protected set; }
        public EvalPrototype Credits { get; protected set; }
        public EvalPrototype Runestones { get; protected set; }
    }
}
