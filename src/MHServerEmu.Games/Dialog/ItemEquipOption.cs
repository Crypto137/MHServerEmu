namespace MHServerEmu.Games.Dialog
{
    public class ItemEquipOption : InteractionOption
    {
        public ItemEquipOption()
        {
            Priority = 12;
            MethodEnum = InteractionMethod.Equip;
        }
    }
}
