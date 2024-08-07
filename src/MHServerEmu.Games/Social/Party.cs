using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.Social
{
    public class Party
    {
        public int NumMembers { get; internal set; }

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
    }
}
