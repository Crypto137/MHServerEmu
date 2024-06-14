using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;

namespace MHServerEmu.Games.Dialog
{
    public class AttackOption : InteractionOption
    {
        public AttackOption()
        {
            Priority = 0;
            MethodEnum = InteractionMethod.Attack;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags))
            {                
                var interactorAvatar = interactor as Avatar;
                isAvailable = CanBeAttacked(localInteractee, interactorAvatar);
            }
            return isAvailable;
        }

        private bool CanBeAttacked(WorldEntity interactee, Avatar interactor)
        {
             return interactee != null && interactor != null
                && interactee.Id != interactor.Id
                && interactee.IsInWorld && interactor.IsInWorld
                && interactee.IsDead == false && interactor.IsDead == false 
                && interactee.IsTargetable(interactor) 
                && interactor.IsValidTargetForCurrentPower(interactee);
        }
    }
}
