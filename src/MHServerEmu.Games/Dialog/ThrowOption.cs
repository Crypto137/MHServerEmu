using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Powers;

namespace MHServerEmu.Games.Dialog
{
    public class ThrowOption : InteractionOption
    {
        public ThrowOption()
        {
            Priority = -10;
            MethodEnum = InteractionMethod.Throw;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            var interactee = interacteeDesc.GetEntity<WorldEntity>(interactor.Game);
            if (interactee == null) return false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags))
            {
                isAvailable = interactee != null
                    && interactee.IsDead == false
                    && interactee.IsThrowableBy(interactor);

                if (isAvailable && interactor.GetThrowablePower() != null)
                {
                    Power activePower = interactor.ActivePower;
                    if (activePower == null || activePower.GetPowerCategory() != PowerCategoryType.ThrowableCancelPower)
                        isAvailable = false;
                }
            }
            if (isAvailable && interactionFlags.HasFlag(InteractionFlags.StopMove)) isAvailable = true;
            return isAvailable;
        }
    }
}
