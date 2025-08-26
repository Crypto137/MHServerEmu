using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Social
{
    /// <summary>
    /// Holds community data for all players. This is like the mega-community of all communities on the server.
    /// </summary>
    public class CommunityRegistry
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

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
            Logger.Debug($"RefreshPlayerStatus(): {player} (connected={player.IsConnected})");

            CommunityMemberEntry member = GetOrCreateMemberEntry(player.PlayerDbId);

            bool sendBroadcast = false;

            sendBroadcast |= member.SetIsOnline(player.IsConnected ? 1 : 0);

            if (sendBroadcast)
                SendBroadcastOnNextUpdate(member);
        }

        public bool ReceiveMemberBroadcast(CommunityMemberBroadcast broadcast)
        {
            ulong playerDbId = broadcast.MemberPlayerDbId;

            // We should be receiving broadcasts only from online players
            if (_playerManager.ClientManager.TryGetPlayerHandle(playerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"ReceiveMemberBroadcast(): No player found for dbid 0x{playerDbId:X}");

            Logger.Debug($"ReceiveMemberBroadcast(): {broadcast}");

            return true;
        }

        public void UpdateSubscription(CommunitySubscriptionOpType operation, ulong subscriberPlayerDbId, ulong targetPlayerDbId)
        {
            // TODO
            Logger.Debug($"UpdateSubscription(): operation={operation}, subscriber=0x{subscriberPlayerDbId:X}, target=0x{targetPlayerDbId:X}");
        }

        public void RequestMemberBroadcast(ulong gameId, ulong playerDbId, List<ulong> members)
        {
            // TODO
            Logger.Debug($"RequestStatus(): gameId=0x{gameId:X}, playerDbId=0x{playerDbId:X}, members={members.Count}");
        }

        private CommunityMemberEntry GetOrCreateMemberEntry(ulong playerDbId)
        {
            if (_members.TryGetValue(playerDbId, out CommunityMemberEntry member) == false)
            {
                member = new(playerDbId);
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
