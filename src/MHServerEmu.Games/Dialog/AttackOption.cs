namespace MHServerEmu.Games.Dialog
{
    public class AttackOption : InteractionOption
    {
        public AttackOption()
        {
            Priority = 0;
            MethodEnum = InteractionMethod.Attack;
        }
    }
}
