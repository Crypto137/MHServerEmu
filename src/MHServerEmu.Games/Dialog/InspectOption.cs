using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Dialog
{
    public class InspectOption : InteractionOption
    {
        public InspectOption()
        {
            MethodEnum = InteractionMethod.Inspect;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags))
            {
                Player interactingPlayer = interactor.GetOwnerOfType<Player>();
                if (interactingPlayer == null)
                {
                    Logger.Warn($"InspectOption only works on avatars with a player, but could not find one on {interactor.PrototypeName}!");
                    return false;
                }
                isAvailable = interacteeDesc.PlayerName != interactingPlayer.GetName(); 
            }
            return isAvailable;
        }
    }
}
