namespace MHServerEmu.Games.Dialog
{
    public class ItemMoveToTradeInventoryOption : InteractionOption
    {
        public ItemMoveToTradeInventoryOption()
        {
            Priority = 9;
            MethodEnum = InteractionMethod.MoveToTradeInventory;
        }
    }
}
