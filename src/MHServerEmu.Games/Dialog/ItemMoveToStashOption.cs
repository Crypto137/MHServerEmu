namespace MHServerEmu.Games.Dialog
{
    public class ItemMoveToStashOption : InteractionOption
    {
        public ItemMoveToStashOption()
        {
            Priority = 8;
            MethodEnum = InteractionMethod.MoveToStash;
        }
    }
}
