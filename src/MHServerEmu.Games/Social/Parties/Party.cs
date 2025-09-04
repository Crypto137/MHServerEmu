using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Social.Parties
{
    public class Party
    {
        private readonly Dictionary<ulong, PartyMemberInfo> _members = new();

        public ulong LeaderId { get; private set; }
        public int NumMembers { get => _members.Count; }

        public Party()
        {
        }

        public Dictionary<ulong, PartyMemberInfo>.Enumerator GetEnumerator()
        {
            return _members.GetEnumerator();
        }

        public PartyMemberInfo GetMemberInfo(ulong memberId)
        {
            if (_members.TryGetValue(memberId, out PartyMemberInfo memberInfo) == false)
                return null;

            return memberInfo;
        }

        public bool IsMember(ulong databaseUniqueId)
        {
            return _members.ContainsKey(databaseUniqueId);
        }

        public bool IsMember(string playerName)
        {
            return GetMemberIdByName(playerName) != 0;
        }

        public bool IsMember(Player player)
        {
            if (player == null)
                return false;

            return _members.ContainsKey(player.DatabaseUniqueId);
        }

        public bool IsLeader(Player interactingPlayer)
        {
            return IsMember(interactingPlayer) && interactingPlayer.DatabaseUniqueId == LeaderId;
        }

        public ulong GetMemberIdByName(string playerName)
        {
            foreach (PartyMemberInfo member in _members.Values)
            {
                if (member.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    return member.PlayerDbId;
            }

            return 0;
        }

        public CommunityMember GetCommunityMemberForDbGuid(Player player, ulong memberId)
        {
            PartyMemberInfo memberInfo = GetMemberInfo(memberId);
            if (memberInfo == null)
                return null;

            Community community = player.Community;
            CommunityCircle partyCircle = community.GetCircle(CircleId.__Party);
            CommunityMember member = community.GetMember(memberId);

            if (member == null || partyCircle == null || member.IsInCircle(partyCircle) == false)
                return null;

            return member;
        }

        public CommunityMember GetCommunityMemberForLeader(Player player)
        {
            return GetCommunityMemberForDbGuid(player, LeaderId);
        }
    }
}
