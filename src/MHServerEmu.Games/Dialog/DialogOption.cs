using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Dialog
{
    public class DialogOption : InteractionOption
    {
        public DialogOption()
        {
            Priority = 1000;
            MethodEnum = InteractionMethod.Converse;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            return base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && localInteractee != null && localInteractee.IsDead == false;
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

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            return base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags)
                && localInteractee != null
                && localInteractee.Properties[PropertyEnum.HealerNPC]
                && interactor.IsDead == false;
        }
    }
}