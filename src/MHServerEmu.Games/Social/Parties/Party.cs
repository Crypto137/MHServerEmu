using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Powers.Conditions;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Social.Parties
{
    public class Party
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, PartyMemberInfo> _members = new();
        private readonly Dictionary<PrototypeId, int> _boostCounts = new();

        private ulong _lastAIAggroNotificationEntityId = Entity.InvalidId;

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

            Player player = Game.Current.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            player?.OnAddedToParty(this);

            OnPartySizeChanged();

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

            Player player = Game.Current.EntityManager.GetEntityByDbGuid<Player>(memberId);
            player?.OnRemovedFromParty(this, reason);

            OnPartySizeChanged();

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

        public string GetMemberName(ulong memberId)
        {
            if (_members.TryGetValue(memberId, out PartyMemberInfo memberInfo) == false)
                return string.Empty;

            return memberInfo.PlayerName;
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

        public bool SendAIAggroNotification(PrototypeId bannerMessageRef, Agent aiAgent, Player aggroPlayer)
        {
            if (bannerMessageRef == PrototypeId.Invalid) return Logger.WarnReturn(false, "SendAIAggroNotification(): bannerMessageRef == PrototypeId.Invalid");
            if (aiAgent == null) return Logger.WarnReturn(false, "SendAIAggroNotification(): aiAgent == null");
            if (aggroPlayer == null) return Logger.WarnReturn(false, "SendAIAggroNotification(): aggroPlayer == null");

            Region region = aggroPlayer.GetRegion();
            if (region == null) return Logger.WarnReturn(false, "SendAIAggroNotification(): region == null");

            Avatar aggroAvatar = aggroPlayer.CurrentAvatar;
            if (aggroAvatar == null) return Logger.WarnReturn(false, "SendAIAggroNotification(): aggroAvatar == null");
            if (aggroAvatar.IsInWorld == false) return Logger.WarnReturn(false, "SendAIAggroNotification(): aggroAvatar.IsInWorld == false");

            UINotificationGlobalsPrototype notificationGlobalsProto = GameDatabase.UIGlobalsPrototype.UINotificationGlobals.As<UINotificationGlobalsPrototype>();
            if (notificationGlobalsProto == null) return Logger.WarnReturn(false, "SendAIAggroNotification(): notificationGlobalsProto == null");

            if (aiAgent.Id == _lastAIAggroNotificationEntityId)
                return true;

            _lastAIAggroNotificationEntityId = aiAgent.Id;

            ulong regionId = region.Id;
            Vector3 aggroPosition = aggroAvatar.RegionLocation.Position;
            float notificationRangeSq = MathHelper.Square(notificationGlobalsProto.NotificationPartyAIAggroRange);

            EntityManager entityManager = aggroPlayer.Game.EntityManager;
            foreach (PartyMemberInfo member in _members.Values)
            {
                Player itPlayer = entityManager.GetEntityByDbGuid<Player>(member.PlayerDbId);
                if (itPlayer == null)
                    continue;

                if (itPlayer.DatabaseUniqueId == aggroPlayer.DatabaseUniqueId)
                    continue;

                Region itRegion = itPlayer.GetRegion();
                if (itRegion == null || itRegion.Id != regionId)
                    continue;

                Avatar itAvatar = itPlayer.CurrentAvatar;
                if (itAvatar == null || itAvatar.IsInWorld == false)
                    continue;

                Vector3 itPosition = itAvatar.RegionLocation.Position;
                float distanceSq = Vector3.DistanceSquared2D(aggroPosition, itPosition);
                if (distanceSq <= notificationRangeSq)
                    continue;

                itPlayer.SendAIAggroNotification(bannerMessageRef, aiAgent, aggroPlayer, false);
            }

            return true;
        }

        private void UpdateBoostCounts()
        {
            _boostCounts.Clear();

            foreach (PartyMemberInfo member in _members.Values)
            {
                foreach (PrototypeId boostProtoRef in member.Boosts)
                {
                    _boostCounts.TryGetValue(boostProtoRef, out int value);
                    _boostCounts[boostProtoRef] = ++value;
                }
            }

            // Update party boost conditions on members in this game instance.
            EntityManager entityManager = Game.Current.EntityManager;
            foreach (var kvp in _boostCounts)
            {
                foreach (PartyMemberInfo member in _members.Values)
                {
                    Player player = entityManager.GetEntityByDbGuid<Player>(member.PlayerDbId);
                    if (player == null)
                        continue;

                    Avatar avatar = player.CurrentAvatar;
                    if (avatar == null || avatar.IsInWorld == false)
                        continue;

                    Condition condition = avatar.ConditionCollection.GetConditionByRef(kvp.Key);
                    if (condition == null)
                        continue;

                    condition.Properties[PropertyEnum.PartyBoostCount] = kvp.Value;
                    condition.RunEvalPartyBoost();
                }
            }
        }

        private void OnPartySizeChanged()
        {
            EntityManager entityManager = Game.Current.EntityManager;

            foreach (var kvp in _members)
            {
                Player player = entityManager.GetEntityByDbGuid<Player>(kvp.Key);
                player?.OnPartySizeChanged(this);
            }
        }
    }
}
