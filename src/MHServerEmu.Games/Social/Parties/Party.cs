using MHServerEmu.Core.Logging;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Social.Parties
{
    public class Party
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, PartyMemberInfo> _members = new();

        public ulong PartyId { get; }
        public Gazillion.GroupType Type { get; private set; }
        public ulong LeaderId { get; private set; }
        public PrototypeId DifficultyTierProtoRef { get; private set; }
        // string groupPSNSessionId

        public int NumMembers { get => _members.Count; }

        public Party(ulong partyId)
        {
            PartyId = partyId;
        }

        public override string ToString()
        {
            return $"id=0x{PartyId:X}";
        }

        public Dictionary<ulong, PartyMemberInfo>.Enumerator GetEnumerator()
        {
            return _members.GetEnumerator();
        }

        public void SetFromMessage(Gazillion.PartyInfo protobuf)
        {
            LeaderId = protobuf.LeaderDbId;
            Type = protobuf.Type;
            DifficultyTierProtoRef = (PrototypeId)protobuf.DifficultyTierProtoId;
            // groupPSNSessionId

            for (int i = 0; i < protobuf.MembersCount; i++)
                AddMember(protobuf.MembersList[i]);
        }

        public bool AddMember(Gazillion.PartyMemberInfo protobuf)
        {
            ulong playerDbId = protobuf.PlayerDbId;
            if (playerDbId == 0) return Logger.WarnReturn(false, "AddMember(): playerDbId == 0");

            if (GetMemberInfo(playerDbId) != null)
                return UpdateMember(protobuf);

            PartyMemberInfo memberInfo = new();
            memberInfo.SetFromMsg(protobuf);

            Logger.Info($"Adding new party member. newMember={memberInfo}, party={this}");

            _members[playerDbId] = memberInfo;

            UpdateBoostCounts();

            return true;
        }

        public bool RemoveMember(ulong memberId, Gazillion.GroupLeaveReason reason)
        {
            if (_members.TryGetValue(memberId, out PartyMemberInfo memberInfo) == false)
            {
                // This matches the client, but it looks like a bug. Shouldn't boost counts be updating when we actually do a removal?
                UpdateBoostCounts();
                return false;
            }

            Logger.Info($"Removing party member. memberId=0x{memberId:X}, reason={reason}, party={this}");

            _members.Remove(memberId);

            return true;
        }

        public bool UpdateMember(Gazillion.PartyMemberInfo protobuf)
        {
            ulong playerDbId = protobuf.PlayerDbId;

            if (_members.TryGetValue(playerDbId, out PartyMemberInfo memberInfo) == false)
                return false;

            memberInfo.SetFromMsg(protobuf);

            UpdateBoostCounts();

            return true;
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

        private void UpdateBoostCounts()
        {
            // TODO
        }
    }
}
