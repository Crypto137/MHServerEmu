namespace MHServerEmu.Games.Dialog
{
    public class ItemMoveToGeneralInventoryOption : InteractionOption
    {
        public ItemMoveToGeneralInventoryOption()
        {
            Priority = 10;
            MethodEnum = InteractionMethod.MoveToGeneralInventory;
        }
    }
}
