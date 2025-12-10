using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Regions;
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

        public virtual void OnEntered(RegionRequestGroup group) { }
        public virtual void OnExited(RegionRequestGroup group) { }
        public virtual void Update(RegionRequestGroup group, bool memberCountChanged) { }
        public virtual int AddPlayers(RegionRequestGroup group, HashSet<PlayerHandle> players) { return 0; }
        public virtual bool IsReady(RegionRequestGroup group) { return false; }
    }

    public class RegionRequestGroup
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static ulong _currentGroupId = 0;

        private readonly Dictionary<ulong, RegionRequestGroupMember> _members = new();

        public ulong Id { get; }
        public RegionRequestQueue Queue { get; }
        public RegionRequestQueueParams QueueParams { get; }

        public PrototypeId DifficultyTierRef { get => QueueParams.DifficultyTierRef; }
        public PrototypeId MetaStateRef { get => QueueParams.MetaStateRef; }
        public bool IsBypass { get => QueueParams.IsBypass; }

        public Action<RegionRequestGroupState> GroupStateChangeCallback { get; }
        public Action<bool> GroupStateUpdateCallback { get; }
        public Action<PlayerHandle> GroupInviteExpiredCallback { get; }
        public Action<PlayerHandle> MatchInviteExpiredCallback { get; }
        public Action<PlayerHandle> RemovedGracePeriodExpiredCallback { get; }

        public int Count { get => _members.Count; }
        public bool IsReady { get => State.IsReady(this); }
        public int PlayerLimit { get => Queue.Prototype.QueueGroupLimit; }
        public bool IsFull { get => GetCountNotInWaitlist() >= PlayerLimit; }

        public RegionRequestGroupState State { get; private set; }
        public List<RegionRequestGroup> Bucket { get; set; }
        public Match Match { get; private set; }

        private RegionRequestGroup(ulong id, RegionRequestQueue queue, in RegionRequestQueueParams queueParams)
        {
            Id = id;
            Queue = queue;
            QueueParams = queueParams;

            GroupStateChangeCallback = OnGroupStateChange;
            GroupStateUpdateCallback = OnGroupStateUpdate;
            GroupInviteExpiredCallback = OnGroupInviteExpired;
            MatchInviteExpiredCallback = OnMatchInviteExpired;
            RemovedGracePeriodExpiredCallback = OnRemovedGracePeriodExpired;

            State = InitializationState.Instance;
            State.OnEntered(this);
        }

        public static RegionRequestGroup Create(RegionRequestQueue queue, in RegionRequestQueueParams queueParams,
            PlayerHandle player, MasterParty party)
        {
            if (queue == null) return Logger.WarnReturn<RegionRequestGroup>(null, "Create(): queue == null");
            if (player == null) return Logger.WarnReturn<RegionRequestGroup>(null, "Create(): player == null");

            ulong groupId = ++_currentGroupId;

            HashSet<PlayerHandle> players = HashSetPool<PlayerHandle>.Instance.Get();
            if (party != null)
                party.GetMembers(players);
            else
                players.Add(player);

            RegionRequestGroup group = new(groupId, queue, queueParams);
            group.AddPlayers(players);

            HashSetPool<PlayerHandle>.Instance.Return(players);
            return group;
        }

        public override string ToString()
        {
            return $"id={Id}";
        }

        public Dictionary<ulong, RegionRequestGroupMember>.ValueCollection.Enumerator GetEnumerator()
        {
            return _members.Values.GetEnumerator();
        }

        public bool SetState(RegionRequestGroupState newState)
        {
            RegionRequestGroupState oldState = State;

            if (newState == null)
                return Logger.WarnReturn(false, "SetState(): newState == null");

            if (newState == oldState)
                return false;

            PlayerManagerService.Instance.EventScheduler.MatchmakingGroupStateChange.ScheduleEvent(Id, TimeSpan.Zero, GroupStateChangeCallback, newState);
            return true;
        }

        public void AddPlayers(HashSet<PlayerHandle> players)
        {
            if (players == null)
                return;

            State.AddPlayers(this, players);
        }

        public void AddPlayer(PlayerHandle player)
        {
            if (player == null)
                return;

            HashSet<PlayerHandle> players = HashSetPool<PlayerHandle>.Instance.Get();
            players.Add(player);
            AddPlayers(players);
            HashSetPool<PlayerHandle>.Instance.Return(players);
        }

        public void RemovePlayers(HashSet<PlayerHandle> players)
        {
            if (players == null || players.Count == 0)
                return;

            foreach (PlayerHandle player in players)
            {
                if (_members.Remove(player.PlayerDbId) == false)
                    Logger.Warn($"RemovePlayers(): Player [{player}] is not a member of region request group {Id}");

                player.RegionRequestGroup = null;

                UpdatePlayerStatus(player, RegionRequestQueueUpdateVar.eRRQ_RemovedFromGroup);
            }

            State.Update(this, true);
        }

        public void RemovePlayer(PlayerHandle player)
        {
            if (player == null)
                return;

            HashSet<PlayerHandle> players = HashSetPool<PlayerHandle>.Instance.Get();
            players.Add(player);
            RemovePlayers(players);
            HashSetPool<PlayerHandle>.Instance.Return(players);
        }

        public void OnPlayerBeginTransfer(PlayerHandle player, RegionHandle newRegion)
        {
            if (_members.TryGetValue(player.PlayerDbId, out RegionRequestGroupMember member) == false)
                return;

            if (member.State == RegionRequestGroupMember.InMatchState.Instance && newRegion != Match?.Region)
                RemovePlayer(player);   // TODO: grace period for leaving
        }

        public void OnPlayerFinishTransfer(PlayerHandle player)
        {
            if (_members.TryGetValue(player.PlayerDbId, out RegionRequestGroupMember member) == false)
                return;

            // Sync status updates that may have gotten lost in transfer.
            SendStatusUpdate(player, player, member.Status);
        }

        public void UpdatePlayerStatus(PlayerHandle updatePlayer, RegionRequestQueueUpdateVar status)
        {
            // Update player may no longer be a member of this group, but we still need to send an update to them (e.g. after removing)
            SendStatusUpdate(updatePlayer, updatePlayer, status);

            foreach (RegionRequestGroupMember member in this)
            {
                PlayerHandle recipientPlayer = member.Player;

                if (recipientPlayer == updatePlayer)
                    member.Status = status;
                else
                    SendStatusUpdate(recipientPlayer, updatePlayer, status);
            }
        }

        public bool HasMember(PlayerHandle player)
        {
            if (player == null) return Logger.WarnReturn(false, "HasMember(): player == null");

            return _members.ContainsKey(player.PlayerDbId);
        }

        public int GetCountNotInWaitlist()
        {
            int numMembers = 0;

            foreach (RegionRequestGroupMember member in this)
            {
                if (member.IsWaitingInWaitlist)
                    continue;

                numMembers++;
            }

            return numMembers;
        }

        public bool SetMatch(Match match)
        {
            if (State != WaitingInQueueState.Instance && State != BypassQueueState.Instance)
                return Logger.WarnReturn(false, $"SetMatch(): Invalid state {State} for group {this}");

            if (Bucket == null && IsBypass == false)
                return Logger.WarnReturn(false, $"SetMatch(): No bucket when assigning a match to a non-bypass group {this}");

            if (Match != null)
                return Logger.WarnReturn(false, $"SetMatch(): Group {this} already has match {Match} assigned to it");

            Match = match;

            if (Bucket != null)
            {
                Bucket.Remove(this);
                Bucket = null;
            }

            State.Update(this, false);

            return true;
        }

        public void ClearMatch()
        {
            Match = null;
        }

        public void OnMatchRegionAccessChange(RegionHandle region)
        {
            RegionHandle currentMatchRegion = Match?.Region;
            if (currentMatchRegion == null || currentMatchRegion != region)
            {
                Logger.Warn("OnMatchRegionAccessChange(): currentMatchRegion == null || currentMatchRegion != region");
                return;
            }

            MatchTeam? team = Match.GetTeamForGroup(this);
            if (team == null)
            {
                Logger.Warn("OnMatchRegionAccessChange(): team == null");
                return;
            }

            HashSet<PlayerHandle> playersToRemove = HashSetPool<PlayerHandle>.Instance.Get();

            foreach (RegionRequestGroupMember member in this)
            {
                switch (region.PlayerAccess)
                {
                    case RegionPlayerAccessVar.eRPA_Open:
                    case RegionPlayerAccessVar.eRPA_InviteOnly:
                        // Invite waitlisted players to fill up free spots in the team.
                        if (member.State == RegionRequestGroupMember.WaitingInWaitlistLockedState.Instance)
                            member.SetState(RegionRequestGroupMember.WaitingInWaitlistState.Instance);

                        if (member.State == RegionRequestGroupMember.WaitingInWaitlistState.Instance &&
                            team.Value.IsFull() == false &&
                            region.IsAccessible(member.Player, true))
                        {
                            member.SetState(RegionRequestGroupMember.MatchInvitePendingState.Instance);
                        }

                        break;

                    case RegionPlayerAccessVar.eRPA_Locked:
                        if (member.State == RegionRequestGroupMember.WaitingInWaitlistState.Instance ||
                            member.State == RegionRequestGroupMember.MatchInvitePendingState.Instance)
                        {
                            member.SetState(RegionRequestGroupMember.WaitingInWaitlistLockedState.Instance);
                        }
                        break;

                    case RegionPlayerAccessVar.eRPA_Closed:
                        playersToRemove.Add(member.Player);
                        break;
                }
            }

            RemovePlayers(playersToRemove);

            HashSetPool<PlayerHandle>.Instance.Return(playersToRemove);
        }

        public bool ReceiveMatchInviteResponse(PlayerHandle player, bool response)
        {
            if (player == null) return Logger.WarnReturn(false, "ReceiveMatchInviteResponse(): player == null");
            if (player.RegionRequestGroup != this) return Logger.WarnReturn(false, "ReceiveMatchInviteResponse(): player.RegionRequestGroup != this");
            if (HasMember(player) == false) return Logger.WarnReturn(false, "ReceiveMatchInviteResponse(): HasMember(player) == false");
            if (Match == null) return Logger.WarnReturn(false, "ReceiveMatchInviteResponse(): Match == null");

            if (response)
            {
                Logger.Trace($"Player [{player}] accepted match invite for group [{this}]");

                // HasMember() should guarantee that we have this player as a member.
                _members[player.PlayerDbId].SetState(RegionRequestGroupMember.MatchInviteAcceptedState.Instance);
                State.Update(this, false);
            }
            else
            {
                Logger.Trace($"Player [{player}] declined match invite for group [{this}]");

                UpdatePlayerStatus(player, RegionRequestQueueUpdateVar.eRRQ_MatchInviteDeclined);
                RemovePlayer(player);
            }

            return true;
        }

        private int AddPlayersInternal(HashSet<PlayerHandle> players, RegionRequestGroupMemberState memberState)
        {
            int numAdded = 0;

            foreach (PlayerHandle player in players)
            {
                if (AddPlayerInternal(player, memberState))
                    numAdded++;
            }

            return numAdded;
        }

        private bool AddPlayerInternal(PlayerHandle player, RegionRequestGroupMemberState memberState)
        {
            if (player.RegionRequestGroup != null && player.RegionRequestGroup.Match != null)
                return false;

            if (HasMember(player))
                return true;

            player.RegionRequestGroup?.RemovePlayer(player);

            if (player.RegionRequestGroup != null)
                return false;

            // If we are adding mid-match, put the player in a waitlist if there are no available spots.
            if (Match != null)
            {
                // TODO: Check for eRTF_PlayerBlacklistedFromRegion

                MatchTeam? team = Match.GetTeamForGroup(this);

                if (Match.Region?.PlayerAccess == RegionPlayerAccessVar.eRPA_Locked)
                    memberState = RegionRequestGroupMember.WaitingInWaitlistLockedState.Instance;
                else if (team?.IsFull() == true)
                    memberState = RegionRequestGroupMember.WaitingInWaitlistState.Instance;
            }

            RegionRequestGroupMember member = new(this, player);
            _members.Add(player.PlayerDbId, member);
            player.RegionRequestGroup = this;

            member.SetState(memberState);
            SyncStatus(player);

            return true;
        }

        private void UpdateContainers(bool memberCountChanged)
        {
            if (Bucket != null)
                Queue.UpdateGroupBucket(this);

            Match?.OnGroupUpdate(this, memberCountChanged);
        }

        private void RemoveFromContainers()
        {
            if (Bucket != null)
            {
                Bucket.Remove(this);
                Bucket = null;
            }

            Match?.OnGroupUpdate(this, true);
        }

        private void ScheduleUpdate()
        {
            RegionHandle matchRegion = Match?.Region;
            if (matchRegion == null)
                return;

            MatchTeam? team = Match.GetTeamForGroup(this);
            if (team?.IsFull() != false)    // null team or full
                return;

            bool hasMembersInWaitlist = false;
            foreach (RegionRequestGroupMember member in this)
            {
                if (member.IsWaitingInWaitlist)
                {
                    hasMembersInWaitlist = true;
                    break;
                }
            }

            if (hasMembersInWaitlist == false)
                return;

            if (matchRegion.IsAccessible(null, true))
                return;

            // Update players waitlisted for the currently inaccessible match region every 5 seconds.
            var eventScheduler = PlayerManagerService.Instance.EventScheduler.MatchmakingGroupStateUpdate;
            eventScheduler.ScheduleEvent(Id, TimeSpan.FromSeconds(5), GroupStateUpdateCallback, true);
        }

        private void MovePlayersToMatch()
        {
            MatchTeam? team = Match?.GetTeamForGroup(this);
            if (team == null)
            {
                Logger.Warn("MovePlayersToMatch(): team == null");
                SetState(ShutdownState.Instance);
                return;
            }

            HashSet<PlayerHandle> playersToRemove = HashSetPool<PlayerHandle>.Instance.Get();

            foreach (RegionRequestGroupMember member in this)
            {
                if (member.State != RegionRequestGroupMember.MatchInviteAcceptedState.Instance)
                    continue;

                member.SetState(RegionRequestGroupMember.InMatchState.Instance);
                bool success = member.Player.BeginRegionTransferToMatch(Match.Region, team.Value.Index);

                if (success == false)
                    playersToRemove.Add(member.Player);
            }

            RemovePlayers(playersToRemove);

            HashSetPool<PlayerHandle>.Instance.Return(playersToRemove);
        }

        private void SyncStatus(PlayerHandle recipientPlayer)
        {
            foreach (RegionRequestGroupMember member in this)
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

        private void OnGroupStateChange(RegionRequestGroupState newState)
        {
            if (newState == null)
                return;

            if (newState == State)
                return;

            State.OnExited(this);
            State = newState;
            State.OnEntered(this);

            //Logger.Debug($"OnGroupStateChange(): {this} - {newState}");
        }

        private void OnGroupStateUpdate(bool memberCountChanged)
        {
            State.Update(this, memberCountChanged);
        }

        private void OnGroupInviteExpired(PlayerHandle player)
        {
            if (HasMember(player) == false)
                return;

            UpdatePlayerStatus(player, RegionRequestQueueUpdateVar.eRRQ_GroupInviteExpired);
            RemovePlayer(player);

            Logger.Info($"Group invite expired for player [{player}]");
        }

        private void OnMatchInviteExpired(PlayerHandle player)
        {
            if (HasMember(player) == false)
                return;

            UpdatePlayerStatus(player, RegionRequestQueueUpdateVar.eRRQ_MatchInviteExpired);
            RemovePlayer(player);

            Logger.Info($"Match invite expired for player [{player}]");
        }

        private void OnRemovedGracePeriodExpired(PlayerHandle player)
        {
            if (HasMember(player) == false)
                return;

            UpdatePlayerStatus(player, RegionRequestQueueUpdateVar.eRRQ_RemovedGracePeriodExpired);
            RemovePlayer(player);

            Logger.Info($"Remove grace period expired for player [{player}]");
        }

        #endregion

        #region State Implementations

        // NOTE: RegionRequestGroupState implementations need to be nested in RegionRequestGroup to be able to access its private members.

        // NOTE: WaitingInQueue, MatchFound, and InMatch are used only in regular matchmaking groups.
        // Bypass groups use BypassQueueState instead of these three.

        public sealed class InitializationState : RegionRequestGroupState
        {
            public static InitializationState Instance { get; } = new();

            private InitializationState() { }

            public override void OnEntered(RegionRequestGroup group)
            {
                if (group.Bucket != null)
                {
                    Logger.Warn($"OnEntered(): Group {group} has a bucket assigned when entering InitializationState");
                    return;
                }

                if (group.Match != null)
                {
                    Logger.Warn($"OnEntered(): Group {group} has a match assigned when entering InitializationState");
                    return;
                }

                int numPlayers = 0;
                int maxPlayers = group.Queue.Prototype.QueueGroupLimit;

                foreach (RegionRequestGroupMember member in group)
                {
                    if (member.State == RegionRequestGroupMember.WaitingInWaitlistState.Instance)
                        continue;

                    member.SetState(RegionRequestGroupMember.GroupInviteAcceptedState.Instance);
                    numPlayers++;
                }

                foreach (RegionRequestGroupMember member in group)
                {
                    if (numPlayers >= maxPlayers)
                        break;

                    if (member.State != RegionRequestGroupMember.WaitingInWaitlistState.Instance)
                        continue;

                    member.SetState(RegionRequestGroupMember.GroupInviteAcceptedState.Instance);
                    numPlayers++;
                }
            }

            public override void Update(RegionRequestGroup group, bool memberCountChanged)
            {
                if (group.Bucket != null)
                {
                    Logger.Warn($"Update(): Group {group} has a bucket assigned to it in the InitializationState");
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                if (group.Match != null)
                {
                    Logger.Warn($"Update(): Group {group} has a match assigned to it in the InitializationState");
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                if (group.Count == 0)
                {
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                bool allMembersReady = true;

                foreach (RegionRequestGroupMember member in group)
                {
                    if (member.State == RegionRequestGroupMember.GroupInvitePendingState.Instance)
                    {
                        allMembersReady = false;
                        break;
                    }
                }

                if (allMembersReady == false)
                    return;

                if (group.IsBypass)
                    group.SetState(BypassQueueState.Instance);
                else
                    group.SetState(WaitingInQueueState.Instance);
            }

            public override int AddPlayers(RegionRequestGroup group, HashSet<PlayerHandle> players)
            {
                int numAdded = group.AddPlayersInternal(players, RegionRequestGroupMember.GroupInviteAcceptedState.Instance);

                Update(group, numAdded != 0);

                return numAdded;
            }
        }

        public sealed class WaitingInQueueState : RegionRequestGroupState
        {
            public static WaitingInQueueState Instance { get; } = new();

            private WaitingInQueueState() { }

            public override void OnEntered(RegionRequestGroup group)
            {
                if (group.IsBypass)
                {
                    Logger.Warn($"OnEntered(): Bypass group {group} entered WaitingInQueueState");
                    return;
                }

                int waitingInQueueCount = 0;
                
                // Put active group members into the queue.
                foreach (RegionRequestGroupMember member in group)
                {
                    if (member.State == RegionRequestGroupMember.WaitingInWaitlistState.Instance)
                        continue;

                    member.SetState(RegionRequestGroupMember.WaitingInQueueState.Instance);
                    waitingInQueueCount++;
                }

                // Fill out remaining slots with waitlisted members.
                foreach (RegionRequestGroupMember member in group)
                {
                    if (waitingInQueueCount >= group.PlayerLimit)
                        break;

                    if (member.State != RegionRequestGroupMember.WaitingInWaitlistState.Instance)
                        continue;

                    member.SetState(RegionRequestGroupMember.WaitingInQueueState.Instance);
                    waitingInQueueCount++;
                }

                group.Queue.UpdateGroupBucket(group);
            }

            public override void OnExited(RegionRequestGroup group)
            {
                if (group.Bucket != null)
                {
                    group.Bucket.Remove(group);
                    group.Bucket = null;
                }
            }

            public override void Update(RegionRequestGroup group, bool memberCountChanged)
            {
                // We need to have either a bucket or an assigned match when we are looking for match to avoid getting stuck.
                if (group.Bucket == null && group.Match == null)
                {
                    Logger.Warn($"Update(): No bucket or match for group {group} in WaitingInQueueState");
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                if (group.Count == 0)
                {
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                if (memberCountChanged)
                    group.UpdateContainers(memberCountChanged);

                if (group.Match != null)
                    group.SetState(MatchFoundState.Instance);
            }

            public override int AddPlayers(RegionRequestGroup group, HashSet<PlayerHandle> players)
            {
                int numAdded = group.AddPlayersInternal(players, RegionRequestGroupMember.WaitingInQueueState.Instance);

                Update(group, numAdded != 0);

                return numAdded;
            }
        }

        public sealed class MatchFoundState : RegionRequestGroupState
        {
            public static MatchFoundState Instance { get; } = new();

            private MatchFoundState() { }

            public override void OnEntered(RegionRequestGroup group)
            {
                if (group.Match == null)
                {
                    Logger.Warn($"OnEntered(): No match assigned to group {group} when entering MatchFoundState");
                    return;
                }

                // Invite everybody
                foreach (RegionRequestGroupMember member in group)
                {
                    if (member.State == RegionRequestGroupMember.WaitingInWaitlistState.Instance)
                        continue;

                    member.SetState(RegionRequestGroupMember.MatchInvitePendingState.Instance);
                }
            }

            public override void Update(RegionRequestGroup group, bool memberCountChanged)
            {
                if (group.Match == null)
                {
                    Logger.Warn($"OnEntered(): No match assigned to group {group} in MatchFoundState");
                    return;
                }

                if (group.Count == 0)
                {
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                group.UpdateContainers(memberCountChanged);
            }

            public override int AddPlayers(RegionRequestGroup group, HashSet<PlayerHandle> players)
            {
                int numAdded = group.AddPlayersInternal(players, RegionRequestGroupMember.MatchInvitePendingState.Instance);

                Update(group, numAdded != 0);

                return numAdded;
            }

            public override bool IsReady(RegionRequestGroup group)
            {
                foreach (RegionRequestGroupMember member in group)
                {
                    switch (member.State)
                    {
                        case RegionRequestGroupMember.MatchInvitePendingState:
                            return false;

                        case RegionRequestGroupMember.WaitingInWaitlistState:
                        case RegionRequestGroupMember.MatchInviteAcceptedState:
                            break;

                        default:
                            Logger.Warn($"IsReady(): Invalid member state {member.State} while the group is in the MatchFoundState");
                            break;
                    }
                }

                return true;
            }
        }

        public sealed class InMatchState : RegionRequestGroupState
        {
            public static InMatchState Instance { get; } = new();

            private InMatchState() { }

            public override void OnEntered(RegionRequestGroup group)
            {
                Update(group, false);
            }

            public override void Update(RegionRequestGroup group, bool memberCountChanged)
            {
                if (group.Count == 0)
                {
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                if (group.Match == null)
                {
                    Logger.Warn($"Update(): No match assigned to group {group} in InMatchState");
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                if (group.Match.Region != null)
                {
                    group.MovePlayersToMatch();

                    if (memberCountChanged)
                        group.UpdateContainers(memberCountChanged);
                }

                group.ScheduleUpdate();
            }

            public override int AddPlayers(RegionRequestGroup group, HashSet<PlayerHandle> players)
            {
                int numAdded = group.AddPlayersInternal(players, RegionRequestGroupMember.MatchInvitePendingState.Instance);

                Update(group, numAdded != 0);

                return numAdded;
            }

            public override bool IsReady(RegionRequestGroup group)
            {
                return true;
            }
        }

        public sealed class BypassQueueState : RegionRequestGroupState
        {
            public static BypassQueueState Instance { get; } = new();

            private BypassQueueState() { }

            public override void OnEntered(RegionRequestGroup group)
            {
                if (group.Bucket != null)
                {
                    Logger.Warn($"OnEntered(): Group {group} has a bucket assigned when entering BypassQueueState");
                    return;
                }

                if (group.Match != null)
                {
                    Logger.Warn($"OnEntered(): Group {group} has a match assigned when entering BypassQueueState");
                    return;
                }

                if (group.IsBypass == false)
                {
                    Logger.Warn($"OnEntered(): Non-bypass group {group} entered BypassQueueState");
                    return;
                }

                // Adding a bypass group to the queue should assign a match to it immediately.
                group.Queue.AddBypassGroup(group);
            }

            public override void Update(RegionRequestGroup group, bool memberCountChanged)
            {
                if (group.Count == 0)
                {
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                if (memberCountChanged)
                    group.UpdateContainers(memberCountChanged);

                if (group.Match == null)
                {
                    Logger.Warn($"Update(): No match assigned to group {group} in BypassQueueState");
                    group.SetState(ShutdownState.Instance);
                    return;
                }

                if (group.Match.Region == null && IsReady(group))
                    group.Match.CreateRegion();

                if (group.Match.Region != null)
                    group.MovePlayersToMatch();

                group.ScheduleUpdate();
            }

            public override int AddPlayers(RegionRequestGroup group, HashSet<PlayerHandle> players)
            {
                int numAdded = group.AddPlayersInternal(players, RegionRequestGroupMember.MatchInvitePendingState.Instance);
                
                if (numAdded != 0)
                    Update(group, true);

                return numAdded;
            }

            public override bool IsReady(RegionRequestGroup group)
            {
                bool isReady = true;

                MatchTeam? team = group.Match.GetTeamForGroup(group);
                if (team == null)
                {
                    Logger.Warn("IsReady(): team == null");
                    group.SetState(ShutdownState.Instance);
                    return false;
                }

                int limit = team.Value.PlayerLimit;
                int numInvited = 0;

                foreach (RegionRequestGroupMember member in group)
                {
                    if (member.State != RegionRequestGroupMember.MatchInvitePendingState.Instance &&
                        member.State != RegionRequestGroupMember.MatchInviteAcceptedState.Instance)
                    {
                        continue;
                    }

                    numInvited++;
                }

                int numAvailable = limit - numInvited;
                if (numAvailable < 0)
                {
                    Logger.Warn("IsReady(): numAvailable < 0");
                    group.SetState(ShutdownState.Instance);
                    return false;
                }

                // Send as many match invites as we have available slots, put everyone else in the waitlist.
                // Wait for all pending match invites to be accepted.
                foreach (RegionRequestGroupMember member in group)
                {
                    switch (member.State)
                    {
                        case RegionRequestGroupMember.GroupInviteAcceptedState:
                            if (numAvailable > 0)
                            {
                                member.SetState(RegionRequestGroupMember.MatchInvitePendingState.Instance);
                                numAvailable--;
                                isReady = false;
                            }
                            else
                            {
                                member.SetState(RegionRequestGroupMember.WaitingInWaitlistState.Instance);
                            }

                            break;

                        case RegionRequestGroupMember.WaitingInWaitlistState:
                            if (numAvailable > 0)
                            {
                                member.SetState(RegionRequestGroupMember.MatchInvitePendingState.Instance);
                                numAvailable--;
                                isReady = false;
                            }

                            break;

                        case RegionRequestGroupMember.MatchInvitePendingState:
                            isReady = false;
                            break;

                        case RegionRequestGroupMember.MatchInviteAcceptedState:
                            // This is the state we want everybody to be in.
                            break;

                        default:
                            Logger.Warn($"IsReady(): Invalid member state {member.State} for member {member} in group {group}");
                            break;
                    }
                }

                return isReady;
            }
        }

        public sealed class ShutdownState : RegionRequestGroupState
        {
            public static ShutdownState Instance { get; } = new();

            private ShutdownState() { }

            public override void OnEntered(RegionRequestGroup group)
            {
                HashSet<PlayerHandle> players = HashSetPool<PlayerHandle>.Instance.Get();

                foreach (RegionRequestGroupMember member in group)
                    players.Add(member.Player);

                group.RemovePlayers(players);
                group.RemoveFromContainers();

                HashSetPool<PlayerHandle>.Instance.Return(players);
            }
        }

        #endregion
    }
}
