namespace MHServerEmu.Games.Dialog
{
    public class ItemUseOption : InteractionOption
    {
        public ItemUseOption()
        {
            Priority = 11;
            MethodEnum = InteractionMethod.Use;
        }
    }
}
