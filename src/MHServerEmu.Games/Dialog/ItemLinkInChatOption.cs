namespace MHServerEmu.Games.Dialog
{
    public class ItemLinkInChatOption : InteractionOption
    {
        public ItemLinkInChatOption()
        {
            Priority = 10;
            MethodEnum = InteractionMethod.LinkItemInChat;
        }
    }
}
