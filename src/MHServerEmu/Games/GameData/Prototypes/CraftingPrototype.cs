namespace MHServerEmu.Games.GameData.Prototypes
{
    public class CraftingInputPrototype : Prototype
    {
        public ulong SlotDisplayInfo { get; private set; }
        public bool OnlyDroppableForThisAvatar { get; private set; }
        public bool OnlyNotDroppableForThisAvatar { get; private set; }
        public bool OnlyEquippableAtThisAvatarLevel { get; private set; }
        public bool MatchFirstInput { get; private set; }
    }

    public class AutoPopulatedInputPrototype : CraftingInputPrototype
    {
        public ulong Ingredient { get; private set; }
        public int Quantity { get; private set; }
    }

    public class RestrictionSetInputPrototype : CraftingInputPrototype
    {
        public DropRestrictionPrototype[] Restrictions { get; private set; }
    }

    public class AllowedItemListInputPrototype : CraftingInputPrototype
    {
        public ulong[] AllowedItems { get; private set; }
    }

    public class CraftingCostPrototype : Prototype
    {
        public EvalPrototype CostEvalCredits { get; private set; }
        public bool DependsOnInput1 { get; private set; }
        public bool DependsOnInput2 { get; private set; }
        public bool DependsOnInput3 { get; private set; }
        public bool DependsOnInput4 { get; private set; }
        public bool DependsOnInput5 { get; private set; }
        public EvalPrototype CostEvalLegendaryMarks { get; private set; }
        public EvalPrototype CostEvalCurrencies { get; private set; }
    }

    public class CraftingIngredientPrototype : ItemPrototype
    {
    }

    public class CostumeCorePrototype : CraftingIngredientPrototype
    {
    }

    public class CraftingRecipePrototype : ItemPrototype
    {
        public CraftingInputPrototype[] RecipeInputs { get; private set; }
        public LootTablePrototype RecipeOutput { get; private set; }
        public ulong RecipeDescription { get; private set; }
        public ulong RecipeIconPath { get; private set; }
        public int SortOrder { get; private set; }
        public ulong RecipeTooltip { get; private set; }
        public CraftingCostPrototype CraftingCost { get; private set; }
        public int UnlockAtCrafterRank { get; private set; }
        public EvalPrototype OnRecipeComplete { get; private set; }
        public ulong RecipeCategory { get; private set; }
        public ulong RecipeIconPathHiRes { get; private set; }
    }
}
