namespace MHServerEmu.Games.GameData.Prototypes
{
    public class CraftingInputPrototype : Prototype
    {
        public PrototypeId SlotDisplayInfo { get; protected set; }
        public bool OnlyDroppableForThisAvatar { get; protected set; }
        public bool OnlyNotDroppableForThisAvatar { get; protected set; }
        public bool OnlyEquippableAtThisAvatarLevel { get; protected set; }
        public bool MatchFirstInput { get; protected set; }
    }

    public class AutoPopulatedInputPrototype : CraftingInputPrototype
    {
        public PrototypeId Ingredient { get; protected set; }
        public int Quantity { get; protected set; }
    }

    public class RestrictionSetInputPrototype : CraftingInputPrototype
    {
        public DropRestrictionPrototype[] Restrictions { get; protected set; }
    }

    public class AllowedItemListInputPrototype : CraftingInputPrototype
    {
        public PrototypeId[] AllowedItems { get; protected set; }
    }

    public class CraftingCostPrototype : Prototype
    {
        public EvalPrototype CostEvalCredits { get; protected set; }
        public bool DependsOnInput1 { get; protected set; }
        public bool DependsOnInput2 { get; protected set; }
        public bool DependsOnInput3 { get; protected set; }
        public bool DependsOnInput4 { get; protected set; }
        public bool DependsOnInput5 { get; protected set; }
        public EvalPrototype CostEvalLegendaryMarks { get; protected set; }
        public EvalPrototype CostEvalCurrencies { get; protected set; }
    }

    public class CraftingIngredientPrototype : ItemPrototype
    {
    }

    public class CostumeCorePrototype : CraftingIngredientPrototype
    {
    }

    public class CraftingRecipePrototype : ItemPrototype
    {
        public CraftingInputPrototype[] RecipeInputs { get; protected set; }
        public LootTablePrototype RecipeOutput { get; protected set; }
        public LocaleStringId RecipeDescription { get; protected set; }
        public AssetId RecipeIconPath { get; protected set; }
        public int SortOrder { get; protected set; }
        public LocaleStringId RecipeTooltip { get; protected set; }
        public CraftingCostPrototype CraftingCost { get; protected set; }
        public int UnlockAtCrafterRank { get; protected set; }
        public EvalPrototype OnRecipeComplete { get; protected set; }
        public PrototypeId RecipeCategory { get; protected set; }
        public AssetId RecipeIconPathHiRes { get; protected set; }
    }
}
