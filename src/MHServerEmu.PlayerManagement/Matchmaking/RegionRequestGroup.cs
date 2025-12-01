using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Social;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    public class RegionRequestGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static ulong _currentGroupId = 0;

        private readonly Dictionary<ulong, RegionRequestGroupMember> _members = new();

        public ulong Id { get; }
        public RegionRequestQueue Queue { get; }
        public PrototypeId DifficultyTierRef { get; }
        public PrototypeId MetaStateRef { get; }
        public bool IsBypass { get; }

        private RegionRequestGroup(ulong id, RegionRequestQueue queue, PrototypeId difficultyTierRef, PrototypeId metaStateRef, bool isBypass)
        {
            Id = id;
            Queue = queue;

            DifficultyTierRef = difficultyTierRef;
            MetaStateRef = metaStateRef;
            IsBypass = isBypass;
        }

        public static RegionRequestGroup Create(RegionRequestQueue queue, PrototypeId difficultyTierRef, PrototypeId metaStateRef,
            PlayerHandle player, MasterParty party, bool isBypass)
        {
            if (queue == null) return Logger.WarnReturn<RegionRequestGroup>(null, "Create(): queue == null");
            if (player == null) return Logger.WarnReturn<RegionRequestGroup>(null, "Create(): player == null");

            ulong groupId = ++_currentGroupId;

            HashSet<PlayerHandle> players = HashSetPool<PlayerHandle>.Instance.Get();
            if (party != null)
                party.GetMembers(players);
            else
                players.Add(player);

            RegionRequestGroup group = new(groupId, queue, difficultyTierRef, metaStateRef, isBypass);
            group.AddPlayers(players);

            return group;
        }

        public void AddPlayers(HashSet<PlayerHandle> players)
        {
            if (players == null)
                return;

            foreach (PlayerHandle player in players)
                AddPlayerInternal(player);
        }

        public void RemovePlayers(HashSet<PlayerHandle> players)
        {
            if (players == null)
                return;

            foreach (PlayerHandle player in players)
                RemovePlayerInternal(player);
        }

        public void RemovePlayer(PlayerHandle player)
        {
            HashSet<PlayerHandle> players = HashSetPool<PlayerHandle>.Instance.Get();
            players.Add(player);
            RemovePlayers(players);
            HashSetPool<PlayerHandle>.Instance.Return(players);
        }

        public void UpdatePlayerStatus(PlayerHandle updatePlayer, RegionRequestQueueUpdateVar status)
        {
            // Update player may no longer be a member of this group, but we still need to send an update to them (e.g. after removing)
            SendStatusUpdate(updatePlayer, updatePlayer, status);

            foreach (RegionRequestGroupMember member in _members.Values)
            {
                PlayerHandle recipientPlayer = member.Player;

                if (recipientPlayer == updatePlayer)
                    member.Status = status;
                else
                    SendStatusUpdate(recipientPlayer, updatePlayer, status);
            }
        }

        private bool AddPlayerInternal(PlayerHandle player)
        {
            ulong playerDbId = player.PlayerDbId;

            if (_members.ContainsKey(playerDbId))
                return false;

            RegionRequestGroupMember member = new(this, player);
            _members.Add(playerDbId, member);

            player.RegionRequestGroup = this;

            member.Status = RegionRequestQueueUpdateVar.eRRQ_WaitingInQueue;    // REMOVEME

            SyncStatus(player);

            return true;
        }

        private bool RemovePlayerInternal(PlayerHandle player)
        {
            ulong playerDbId = player.PlayerDbId;

            if (_members.Remove(playerDbId) == false)
                Logger.Warn($"RemovePlayerInternal(): Player [{player}] is not a member of region request group {Id}");

            player.RegionRequestGroup = null;

            UpdatePlayerStatus(player, RegionRequestQueueUpdateVar.eRRQ_RemovedFromGroup);

            return true;
        }

        private void SyncStatus(PlayerHandle recipientPlayer)
        {
            foreach (RegionRequestGroupMember member in _members.Values)
                SendStatusUpdate(recipientPlayer, member.Player, member.Status);
        }

        private void SendStatusUpdate(PlayerHandle recipientPlayer, PlayerHandle updatePlayer, RegionRequestQueueUpdateVar status)
        {
            if (recipientPlayer == null)
                return;

            if (recipientPlayer.State != PlayerHandleState.InGame)
                return;

            if (updatePlayer == null)
                return;

            ulong gameId = recipientPlayer.CurrentGame.Id;
            ulong playerDbId = recipientPlayer.PlayerDbId;
            ulong regionProtoId = (ulong)Queue.PrototypeDataRef;
            ulong difficultyTierProtoId = (ulong)DifficultyTierRef;
            int playersInQueue = 0;
            ulong regionRequestGroupId = Id;

            ServiceMessage.MatchQueueUpdate message = new(gameId, playerDbId, regionProtoId, difficultyTierProtoId,
                playersInQueue, regionRequestGroupId, new());

            ulong updatePlayerGuid = updatePlayer.PlayerDbId;
            string updatePlayerName = null;

            if (recipientPlayer.CurrentParty == null || recipientPlayer.CurrentParty.HasMember(updatePlayer) == false)
                updatePlayerName = updatePlayer.PlayerName;

            ServiceMessage.MatchQueueUpdateData updatePlayerData = new(updatePlayerGuid, status, updatePlayerName);
            message.Data.Add(updatePlayerData);

            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }
    }
}
