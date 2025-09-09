using Gazillion;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    /// <summary>
    /// The authoritative representation of a party on the server (as apposed to local parties in game instances).
    /// </summary>
    public class Party
    {
        // This class has the same name as game-side local representations of parties.
        // This is not ideal, but I'm not sure what else to call it without making it more confusing.

        private readonly List<PlayerHandle> _members = new();
        private readonly HashSet<PlayerHandle> _pendingMembers = new();

        public ulong Id { get; }
        public PlayerHandle Leader { get; private set; }

        public bool IsFull { get; }

        public Party(ulong id, PlayerHandle creator)
        {
            Id = id;

            AddMember(creator);
            SetLeader(creator);
        }

        public override string ToString()
        {
            return $"id={Id}";
        }

        public void GetMembers(HashSet<PlayerHandle> members)
        {
            foreach (PlayerHandle member in _members)
                members.Add(member);
        }

        public void AddMember(PlayerHandle player)
        {
            player.PendingParty = null;
            player.CurrentParty = this;
        }

        public void RemoveMember(PlayerHandle player, GroupLeaveReason reason)
        {
            player.CurrentParty = null;
        }

        public void UpdateMember(PlayerHandle player)
        {

        }

        public void SetLeader(PlayerHandle player)
        {
            Leader = player;
        }

        public bool HasInvitation(PlayerHandle player)
        {
            return _pendingMembers.Contains(player);
        }

        public void AddInvitation(PlayerHandle player)
        {
            _pendingMembers.Add(player);
            player.PendingParty = this;
        }

        public void CancelInvitation(PlayerHandle player)
        {
            _pendingMembers.Remove(player);
            player.PendingParty = null;
        }
    }
}
