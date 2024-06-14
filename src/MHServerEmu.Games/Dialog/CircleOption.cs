using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Dialog
{
    public class CircleOption : InteractionOption
    {
        public CircleId CircleId { get; protected set; }
        public bool ContainMember { get; protected set; }

        public CircleOption()
        {
            Priority = 50;
            CircleId = CircleId.__None;
            ContainMember = true;
        }

        public override bool IsCurrentlyAvailable(EntityDesc interacteeDesc, WorldEntity localInteractee, WorldEntity interactor, InteractionFlags interactionFlags)
        {
            bool isAvailable = false;
            if (interacteeDesc.EntityId != interactor.Id
                && base.IsCurrentlyAvailable(interacteeDesc, localInteractee, interactor, interactionFlags))
            {
                Player player = interactor.GetOwnerOfType<Player>();
                if (player == null) return false;

                string playerName = interacteeDesc.PlayerName;
                Community community = player.Community;
                CommunityMember member = community.GetMemberByName(playerName);
                CommunityCircle circle = community.GetCircle(CircleId);
                if (circle == null) return false;

                if (ContainMember)
                    isAvailable = member == null || (member.IsInCircle(circle) == false && circle.CanContainPlayer(playerName, member.DbId));               
                else
                    isAvailable = member != null && member.IsInCircle(circle);
            }
            return isAvailable;
        }
    }

    public class FriendOption : CircleOption
    {
        public FriendOption()
        {
            MethodEnum = InteractionMethod.Friend;
            CircleId = CircleId.__Friends;
        }
    }

    public class IgnoreOption : CircleOption
    {
        public IgnoreOption()
        {
            MethodEnum = InteractionMethod.Ignore;
            CircleId = CircleId.__Ignore;
        }
    }

    public class UnfriendOption : CircleOption
    {
        public UnfriendOption()
        {
            MethodEnum = InteractionMethod.Unfriend;
            CircleId = CircleId.__Friends;
            ContainMember = false;
        }
    }

    public class UnignoreOption : CircleOption
    {
        public UnignoreOption()
        {
            MethodEnum = InteractionMethod.Unignore;
            CircleId = CircleId.__Ignore;
            ContainMember = false;
        }
    }

}