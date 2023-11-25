namespace MHServerEmu.Games.GameData.Prototypes
{
    public class CraftingInputPrototype : Prototype
    {
        public ulong SlotDisplayInfo { get; set; }
        public bool OnlyDroppableForThisAvatar { get; set; }
        public bool OnlyNotDroppableForThisAvatar { get; set; }
        public bool OnlyEquippableAtThisAvatarLevel { get; set; }
        public bool MatchFirstInput { get; set; }
    }

    public class AutoPopulatedInputPrototype : CraftingInputPrototype
    {
        public ulong Ingredient { get; set; }
        public int Quantity { get; set; }
    }

    public class RestrictionSetInputPrototype : CraftingInputPrototype
    {
        public DropRestrictionPrototype[] Restrictions { get; set; }
    }

    public class AllowedItemListInputPrototype : CraftingInputPrototype
    {
        public ulong[] AllowedItems { get; set; }
    }

    public class CraftingCostPrototype : Prototype
    {
        public EvalPrototype CostEvalCredits { get; set; }
        public bool DependsOnInput1 { get; set; }
        public bool DependsOnInput2 { get; set; }
        public bool DependsOnInput3 { get; set; }
        public bool DependsOnInput4 { get; set; }
        public bool DependsOnInput5 { get; set; }
        public EvalPrototype CostEvalLegendaryMarks { get; set; }
        public EvalPrototype CostEvalCurrencies { get; set; }
    }

    public class CraftingIngredientPrototype : ItemPrototype
    {
    }

    public class CostumeCorePrototype : CraftingIngredientPrototype
    {
    }

    public class CraftingRecipePrototype : ItemPrototype
    {
        public CraftingInputPrototype[] RecipeInputs { get; set; }
        public LootTablePrototype RecipeOutput { get; set; }
        public ulong RecipeDescription { get; set; }
        public ulong RecipeIconPath { get; set; }
        public int SortOrder { get; set; }
        public ulong RecipeTooltip { get; set; }
        public CraftingCostPrototype CraftingCost { get; set; }
        public int UnlockAtCrafterRank { get; set; }
        public EvalPrototype OnRecipeComplete { get; set; }
        public ulong RecipeCategory { get; set; }
        public ulong RecipeIconPathHiRes { get; set; }
    }
}
