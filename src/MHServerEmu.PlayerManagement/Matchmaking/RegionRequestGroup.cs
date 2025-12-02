using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Social;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Represents the state of a <see cref="RegionRequestGroup"/>.
    /// </summary>
    public abstract class RegionRequestGroupState
    {
        public override string ToString()
        {
            return GetType().Name;
        }

        public abstract void Update(RegionRequestGroup group);
        public abstract int AddPlayers(RegionRequestGroup group);
        public abstract bool IsReady(RegionRequestGroup group);

        public virtual void OnEntered(RegionRequestGroup group) { }
        public virtual void OnExited(RegionRequestGroup group) { }
    }

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

        public Action<PlayerHandle> GroupInviteExpiredCallback { get; }
        public Action<PlayerHandle> MatchInviteExpiredCallback { get; }
        public Action<PlayerHandle> RemovedGracePeriodExpiredCallback { get; }

        public RegionRequestGroupState State { get; private set; }

        private RegionRequestGroup(ulong id, RegionRequestQueue queue, PrototypeId difficultyTierRef, PrototypeId metaStateRef, bool isBypass)
        {
            Id = id;
            Queue = queue;

            DifficultyTierRef = difficultyTierRef;
            MetaStateRef = metaStateRef;
            IsBypass = isBypass;

            GroupInviteExpiredCallback = OnGroupInviteExpired;
            MatchInviteExpiredCallback = OnMatchInviteExpired;
            RemovedGracePeriodExpiredCallback = OnRemovedGracePeriodExpired;

            State = InitializationState.Instance;
            State.OnEntered(this);
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

            HashSetPool<PlayerHandle>.Instance.Return(players);
            return group;
        }

        public bool SetState(RegionRequestGroupState newState)
        {
            RegionRequestGroupState oldState = State;

            if (newState == null)
                return Logger.WarnReturn(false, "SetState(): newState == null");

            if (newState == oldState)
                return false;

            // TODO: state change event
            oldState.OnExited(this);
            State = newState;
            newState.OnEntered(this);

            return true;
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

            member.SetState(RegionRequestGroupMember.WaitingInQueueState.Instance);

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

        #region Event Callbacks

        private void OnGroupInviteExpired(PlayerHandle player)
        {
            if (_members.ContainsKey(player.PlayerDbId) == false)
                return;

            UpdatePlayerStatus(player, RegionRequestQueueUpdateVar.eRRQ_GroupInviteExpired);
            RemovePlayer(player);

            Logger.Info($"Group invite expired for player [{player}]");
        }

        private void OnMatchInviteExpired(PlayerHandle player)
        {
            if (_members.ContainsKey(player.PlayerDbId) == false)
                return;

            UpdatePlayerStatus(player, RegionRequestQueueUpdateVar.eRRQ_MatchInviteExpired);
            RemovePlayer(player);

            Logger.Info($"Match invite expired for player [{player}]");
        }

        private void OnRemovedGracePeriodExpired(PlayerHandle player)
        {
            if (_members.ContainsKey(player.PlayerDbId) == false)
                return;

            UpdatePlayerStatus(player, RegionRequestQueueUpdateVar.eRRQ_RemovedGracePeriodExpired);
            RemovePlayer(player);

            Logger.Info($"Remove grace period expired for player [{player}]");
        }

        #endregion

        #region State Implementations

        // NOTE: RegionRequestGroupState implementations need to be nested in RegionRequestGroup to be able to access its private members.

        public sealed class InitializationState : RegionRequestGroupState
        {
            public static InitializationState Instance { get; } = new();

            private InitializationState() { }

            public override void Update(RegionRequestGroup group)
            {
            }

            public override int AddPlayers(RegionRequestGroup group)
            {
                return 0;
            }

            public override bool IsReady(RegionRequestGroup group)
            {
                return false;
            }

            public override void OnEntered(RegionRequestGroup group)
            {
                Logger.Debug($"OnEntered(): {this}");
            }
        }

        public sealed class WaitingInQueueState : RegionRequestGroupState
        {
            public static WaitingInQueueState Instance { get; } = new();

            private WaitingInQueueState() { }

            public override void Update(RegionRequestGroup group)
            {
            }

            public override int AddPlayers(RegionRequestGroup group)
            {
                return 0;
            }

            public override bool IsReady(RegionRequestGroup group)
            {
                return false;
            }
        }

        public sealed class MatchFoundState : RegionRequestGroupState
        {
            public static MatchFoundState Instance { get; } = new();

            private MatchFoundState() { }

            public override void Update(RegionRequestGroup group)
            {
            }

            public override int AddPlayers(RegionRequestGroup group)
            {
                return 0;
            }

            public override bool IsReady(RegionRequestGroup group)
            {
                return false;
            }
        }

        public sealed class BypassQueueState : RegionRequestGroupState
        {
            public static BypassQueueState Instance { get; } = new();

            private BypassQueueState() { }

            public override void Update(RegionRequestGroup group)
            {
            }

            public override int AddPlayers(RegionRequestGroup group)
            {
                return 0;
            }

            public override bool IsReady(RegionRequestGroup group)
            {
                return false;
            }
        }

        public sealed class InMatchState : RegionRequestGroupState
        {
            public static InMatchState Instance { get; } = new();

            private InMatchState() { }

            public override void Update(RegionRequestGroup group)
            {
            }

            public override int AddPlayers(RegionRequestGroup group)
            {
                return 0;
            }

            public override bool IsReady(RegionRequestGroup group)
            {
                return false;
            }
        }

        public sealed class ShutdownState : RegionRequestGroupState
        {
            public static ShutdownState Instance { get; } = new();

            private ShutdownState() { }

            public override void Update(RegionRequestGroup group)
            {
            }

            public override int AddPlayers(RegionRequestGroup group)
            {
                return 0;
            }

            public override bool IsReady(RegionRequestGroup group)
            {
                return false;
            }
        }

        #endregion
    }
}
