using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    /// <summary>
    /// Holds community data for all players. This is like the mega-community of all communities on the server.
    /// </summary>
    public class CommunityRegistry
    {
        // NOTE: Member entries continue to exist for as long as the server is up to be available for lookups. This is by design and not a leak.
        private readonly Dictionary<ulong, CommunityMemberEntry> _members = new();
        private readonly HashSet<CommunityMemberEntry> _membersToBroadcast = new();

        private readonly PlayerManagerService _playerManager;

        public CommunityRegistry(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        public void Update()
        {
            if (_membersToBroadcast.Count == 0)
                return;

            ServiceMessage.CommunityBroadcastBatch broadcastBatch;

            if (_membersToBroadcast.Count == 1)
            {
                // Optimization for the common case when a batch contains only a single member. Do not allocate a list for this.
                var enumerator = _membersToBroadcast.GetEnumerator();
                enumerator.MoveNext();
                CommunityMemberBroadcast broadcast = enumerator.Current.GetBroadcast();
                broadcastBatch = new(broadcast);
            }
            else
            {
                List<CommunityMemberBroadcast> broadcastList = new(_membersToBroadcast.Count);

                foreach (CommunityMemberEntry member in _membersToBroadcast)
                {
                    CommunityMemberBroadcast broadcast = member.GetBroadcast();
                    broadcastList.Add(broadcast);
                }

                broadcastBatch = new(broadcastList);
            }

            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, broadcastBatch);

            _membersToBroadcast.Clear();
        }

        public void RefreshPlayerStatus(PlayerHandle player)
        {
            bool sendBroadcast = false;

            CommunityMemberEntry member = GetMemberEntry(player.PlayerDbId);
            if (member == null)
            {
                member = AddMemberEntry(player.PlayerDbId, player.PlayerName);
                member.SetIsOnline(player.IsConnected);
                member.SetLastLogoutTime(player.LastLogoutTime);
                sendBroadcast = true;
            }
            else
            {
                sendBroadcast |= member.SetPrestigeLevel(0);
                sendBroadcast |= member.SetIsOnline(player.IsConnected);
                sendBroadcast |= member.SetLastLogoutTime(player.LastLogoutTime);
            }

            if (sendBroadcast)
                SendBroadcastOnNextUpdate(member);
        }

        public void OnPlayerNameChanged(ulong playerDbId, string playerName)
        {
            CommunityMemberEntry member = GetMemberEntry(playerDbId);
            if (member == null)
                return;

            if (member.SetCurrentPlayerName(playerName))
                SendBroadcastOnNextUpdate(member);
        }

        public void ReceiveMemberBroadcast(CommunityMemberBroadcast broadcast)
        {
            ulong playerDbId = broadcast.MemberPlayerDbId;

            // We should be receiving broadcasts only from online players
            PlayerHandle player = _playerManager.ClientManager.GetPlayer(playerDbId);
            if (!Verify.IsNotNull(player, $"No player found for dbid 0x{playerDbId:X}"))
                return;

            // Member entry should be created when a player logs in
            CommunityMemberEntry member = GetMemberEntry(playerDbId);
            if (!Verify.IsNotNull(member)) return;

            bool sendBroadcast = false;

            if (broadcast.HasCurrentRegionRefId)
                sendBroadcast |= member.SetCurrentRegionRefId(broadcast.CurrentRegionRefId);

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            if (broadcast.HasCurrentDifficultyRefId)
                sendBroadcast |= member.SetCurrentDifficultyRefId(broadcast.CurrentDifficultyRefId);

            if (broadcast.SlotsCount > 0)
            {
                // We don't care about the second slot on PC.
                CommunityMemberAvatarSlot avatarSlot = broadcast.SlotsList[0];

                if (avatarSlot.HasAvatarRefId)
                    sendBroadcast |= member.SetAvatarRefId(avatarSlot.AvatarRefId);

                if (avatarSlot.HasCostumeRefId)
                    sendBroadcast |= member.SetCostumeRefId(avatarSlot.CostumeRefId);

                if (avatarSlot.HasLevel)
                    sendBroadcast |= member.SetLevel(avatarSlot.Level);

                if (avatarSlot.HasPrestigeLevel)
                    sendBroadcast |= member.SetPrestigeLevel(avatarSlot.PrestigeLevel);
            }
#else
            if (broadcast.HasCurrentAvatarRefId)
                sendBroadcast |= member.SetAvatarRefId(broadcast.CurrentAvatarRefId);

            if (broadcast.HasCurrentCostumeRefId)
                sendBroadcast |= member.SetCostumeRefId(broadcast.CurrentCostumeRefId);

            if (broadcast.HasCurrentCharacterLevel)
                sendBroadcast |= member.SetLevel((uint)broadcast.CurrentCharacterLevel);

            if (broadcast.HasCurrentPrestigeLevel)
                sendBroadcast |= member.SetPrestigeLevel((uint)broadcast.CurrentPrestigeLevel);
#endif

            sendBroadcast |= member.SetIsOnline(player.IsConnected);
            sendBroadcast |= member.SetLastLogoutTime(player.LastLogoutTime);

            if (sendBroadcast)
                SendBroadcastOnNextUpdate(member);
        }

        public void RequestMemberBroadcast(ulong gameId, ulong playerDbId, List<ulong> members)
        {
            CommunityMemberEntry requester = GetMemberEntry(playerDbId);
            if (requester == null)
                return;

            ServiceMessage.CommunityBroadcastBatch broadcastBatch;

            if (members.Count == 1)
            {
                // Optimization for the common case when a batch contains only a single member. Do not allocate a list for this.
                CommunityMemberBroadcast broadcast = QueryMemberBroadcast(members[0]);
                broadcastBatch = new(broadcast, gameId, playerDbId);
            }
            else
            {
                List<CommunityMemberBroadcast> broadcastList = new(members.Count);

                foreach (ulong queryPlayerDbId in members)
                {
                    CommunityMemberBroadcast broadcast = QueryMemberBroadcast(queryPlayerDbId);
                    broadcastList.Add(broadcast);
                }

                broadcastBatch = new(broadcastList, gameId, playerDbId);
            }

            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, broadcastBatch);
        }

        private CommunityMemberBroadcast QueryMemberBroadcast(ulong playerDbId)
        {
            CommunityMemberEntry queryMember = GetMemberEntry(playerDbId);
            if (queryMember == null)
            {
                if (PlayerNameCache.Instance.TryGetPlayerName(playerDbId, out string playerName) == false)
                    playerName = "Unknown";

                queryMember = AddMemberEntry(playerDbId, playerName);
                queryMember.SetIsOnline(false);

                if (_playerManager.GuildManager.GetGuildForPlayer(playerDbId) != null)
                {
                    if (IDBManager.Instance.TryGetLastLogoutTime(playerDbId, out long lastLogoutTime))
                        queryMember.SetLastLogoutTime(TimeSpan.FromMilliseconds(lastLogoutTime));
                }
            }

            return queryMember.GetBroadcast();
        }

        private CommunityMemberEntry AddMemberEntry(ulong playerDbId, string playerName)
        {
            if (_members.TryGetValue(playerDbId, out CommunityMemberEntry member) == false)
            {
                member = new(playerDbId, playerName);
                _members.Add(playerDbId, member);
            }

            return member;
        }

        private CommunityMemberEntry GetMemberEntry(ulong playerDbId)
        {
            if (_members.TryGetValue(playerDbId, out CommunityMemberEntry member) == false)
                return null;

            return member;
        }

        private void SendBroadcastOnNextUpdate(CommunityMemberEntry member)
        {
            _membersToBroadcast.Add(member);
        }
    }
}
