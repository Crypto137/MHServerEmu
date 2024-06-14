using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.Dialog
{
    public class EntitySelectorActionOption : InteractionOption
    {
        public EntitySelectorActionOption()
        {
            Priority = 10;
            MethodEnum = InteractionMethod.Converse;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (localInteractee != null
                && localInteractee.Properties[PropertyEnum.EntSelActHasInteractOption] 
                && localInteractee.Properties[PropertyEnum.EntSelActInteractOptDisabled] == false )
            {
                var interactorAlliance = interactor.Alliance;
                if (interactorAlliance != null)
                {
                    var interacteeAlliance = localInteractee.Alliance;
                    if (interacteeAlliance == null || interacteeAlliance.IsHostileTo(interactorAlliance) == false)
                        isAvailable = true;
                }
                else
                    isAvailable = true;
            }
            return isAvailable;
        }

    }
}
