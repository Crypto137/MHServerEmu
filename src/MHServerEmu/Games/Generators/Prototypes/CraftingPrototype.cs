using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Generators.Prototypes
{
    public class CraftingInputPrototype : Prototype
    {
        public ulong SlotDisplayInfo;
        public bool OnlyDroppableForThisAvatar;
        public bool OnlyNotDroppableForThisAvatar;
        public bool OnlyEquippableAtThisAvatarLevel;
        public bool MatchFirstInput;
        public CraftingInputPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CraftingInputPrototype), proto); }
    }

    public class AutoPopulatedInputPrototype : CraftingInputPrototype
    {
        public ulong Ingredient;
        public int Quantity;
        public AutoPopulatedInputPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AutoPopulatedInputPrototype), proto); }
    }

    public class RestrictionSetInputPrototype : CraftingInputPrototype
    {
        public DropRestrictionPrototype[] Restrictions;
        public RestrictionSetInputPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(RestrictionSetInputPrototype), proto); }
    }

    public class AllowedItemListInputPrototype : CraftingInputPrototype
    {
        public ulong[] AllowedItems;
        public AllowedItemListInputPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(AllowedItemListInputPrototype), proto); }
    }

    public class CraftingCostPrototype : Prototype
    {
        public EvalPrototype CostEvalCredits;
        public bool DependsOnInput1;
        public bool DependsOnInput2;
        public bool DependsOnInput3;
        public bool DependsOnInput4;
        public bool DependsOnInput5;
        public EvalPrototype CostEvalLegendaryMarks;
        public EvalPrototype CostEvalCurrencies;
        public CraftingCostPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CraftingCostPrototype), proto); }
    }

    public class CraftingIngredientPrototype : ItemPrototype
    {
        public CraftingIngredientPrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CraftingIngredientPrototype), proto); }
    }

    public class CostumeCorePrototype : CraftingIngredientPrototype
    {
        public CostumeCorePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CostumeCorePrototype), proto); }
    }

    public class CraftingRecipePrototype : ItemPrototype
    {
        public CraftingInputPrototype[] RecipeInputs;
        public LootTablePrototype RecipeOutput;
        public ulong RecipeDescription;
        public ulong RecipeIconPath;
        public int SortOrder;
        public ulong RecipeTooltip;
        public CraftingCostPrototype CraftingCost;
        public int UnlockAtCrafterRank;
        public EvalPrototype OnRecipeComplete;
        public ulong RecipeCategory;
        public ulong RecipeIconPathHiRes;
        public CraftingRecipePrototype(Prototype proto) : base(proto) { FillPrototype(typeof(CraftingRecipePrototype), proto); }
    }
}
