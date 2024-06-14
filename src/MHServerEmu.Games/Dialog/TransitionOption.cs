namespace MHServerEmu.Games.Dialog
{
    public class TransitionOption : InteractionOption
    {
        public TransitionOption()
        {
            Priority = 10;
            MethodEnum = InteractionMethod.Use;
        }
    }
}
