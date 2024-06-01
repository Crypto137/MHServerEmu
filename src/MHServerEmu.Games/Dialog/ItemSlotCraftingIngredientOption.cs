namespace MHServerEmu.Games.Dialog
{
    public class ItemSlotCraftingIngredientOption : InteractionOption
    {
        public ItemSlotCraftingIngredientOption()
        {
            Priority = 8;
            MethodEnum = InteractionMethod.SlotCraftingIngredient;
        }
    }
}
