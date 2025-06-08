namespace MHServerEmu.Games.Entities
{
    public enum CraftingResult  // CPlayer::DisplayCraftingFailureMessage()
    {
        Success,
        CraftingFailed,
        RecipeNotInRecipeLibrary,
        LiveTuning,
        InsufficientIngredients,
        InsufficientCredits,
        InsufficientLegendaryMarks,
        Result7,
        Result8,   // InsufficientIngredients
        Result9,   // InsufficientIngredients
        IngredientLevelRestricted,
        Result11,  // InsufficientIngredients
        Result12,  // InsufficientIngredients
        Result13,  // InsufficientIngredients
        Result14,  // InsufficientIngredients
        Result15,  // InsufficientIngredients
        Result16,  // InsufficientIngredients
        Result17,  // InsufficientIngredients
        LootRollFailed,
    }

    public partial class Player
    {
        public CraftingResult Craft(ulong recipeItemId, List<ulong> ingredientIds, bool isRecraft)
        {
            return Logger.DebugReturn(CraftingResult.CraftingFailed, $"Craft(): recipeItemId={recipeItemId}, ingredientIds=[{string.Join(' ', ingredientIds)}], isRecraft={isRecraft}");
        }
    }
}
