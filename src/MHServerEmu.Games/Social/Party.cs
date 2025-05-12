using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Social
{
    public class Party
    {
        public int NumMembers { get; internal set; }

        internal IEnumerable<Player> GetMembers()
        {
            throw new NotImplementedException();
        }

        internal bool IsLeader(Player interactingPlayer)
        {
            throw new NotImplementedException();
        }

        internal bool IsMember(string playerName)
        {
            throw new NotImplementedException();
        }

        internal bool IsMember(ulong databaseUniqueId)
        {
            throw new NotImplementedException();
        }

        public CommunityMember GetCommunityMemberForLeader(Player player)
        {
            // TODO
            return null;
        }
    }
}
