using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Social.Guilds;

namespace MHServerEmu.Games.Dialog
{
    public class GuildInviteOption : InteractionOption
    {
        public GuildInviteOption()
        {
            Priority = 50;
            MethodEnum = InteractionMethod.GuildInvite;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags))
            {
                Player interactingPlayer = interactor.GetOwnerOfType<Player>();
                if (interactingPlayer == null)
                {
                    Logger.Warn($"GuildInviteOption only works on avatars with a player, but could not find one on {interactor.PrototypeName}!");
                    return false;
                }
                isAvailable = GuildMember.CanInvite(interactingPlayer.GuildMembership);
            }
            return isAvailable;
        }
    }
}
