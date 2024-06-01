using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Dialog
{
    public class DialogOption : InteractionOption
    {
        public DialogOption()
        {
            Priority = 1000;
            MethodEnum = InteractionMethod.Converse;
        }
    }

    public class HealOption : DialogOption
    {
        public HealOption()
        {
            Priority = 50;
            MethodEnum = InteractionMethod.Heal;
            IndicatorType = HUDEntityOverheadIcon.Healer;
        }
    }
}