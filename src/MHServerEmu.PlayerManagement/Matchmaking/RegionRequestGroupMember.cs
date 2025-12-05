using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Represents the state of a <see cref="RegionRequestGroupMember"/>.
    /// </summary>
    public abstract class RegionRequestGroupMemberState
    {
        public virtual RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_Invalid; }

        public override string ToString()
        {
            return GetType().Name;
        }

        public abstract bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState);

        public virtual void OnEntered(RegionRequestGroupMember member) { }
        public virtual void OnExited(RegionRequestGroupMember member) { }
    }

    public class RegionRequestGroupMember
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public RegionRequestGroup Group { get; }
        public PlayerHandle Player { get; }
        public RegionRequestGroupMemberState State { get; private set; }
        public RegionRequestQueueUpdateVar Status { get; set; }

        public bool IsWaitingInWaitlist { get => State == WaitingInWaitlistState.Instance || State == WaitingInWaitlistLockedState.Instance; }

        public RegionRequestGroupMember(RegionRequestGroup group, PlayerHandle player)
        {
            Group = group;
            Player = player;

            // We don't want to send status updates during initialization, so set state directly without calling SetStateInternal().
            State = InitialState.Instance;
            Status = RegionRequestQueueUpdateVar.eRRQ_Invalid;
        }

        public bool SetState(RegionRequestGroupMemberState newState)
        {
            RegionRequestGroupMemberState oldState = State;

            if (newState == null)
                return Logger.WarnReturn(false, "SetState(): newState == null");

            if (newState == oldState)
                return false;

            return oldState.SetState(this, newState);
        }

        private bool SetStateInternal(RegionRequestGroupMemberState newState)
        {
            RegionRequestGroupMemberState oldState = State;

            // This should have already been validated by now
            if (newState == oldState)
                return Logger.WarnReturn(false, "SetStateInternal(): newState == oldState");

            oldState.OnExited(this);
            State = newState;
            newState.OnEntered(this);

            Group.UpdatePlayerStatus(Player, newState.StatusVar);

            return true;
        }

        private bool OnInvalidStateTransition(RegionRequestGroupMemberState newState)
        {
            Logger.Warn($"SetState(): Attempted invalid state transition {State} -> {newState} for player [{Player}]");
            return false;
        }

        #region State Implementations

        // NOTE: RegionRequestGroupMemberState implementations need to be nested in RegionRequestGroupMember to be able to access its private members.

        public sealed class InitialState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_Invalid; }

            public static InitialState Instance { get; } = new();

            private InitialState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case GroupInvitePendingState:
                    case GroupInviteAcceptedState:
                    case WaitingInQueueState:
                    case WaitingInWaitlistState:
                    case WaitingInWaitlistLockedState:
                    case MatchInvitePendingState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public sealed class GroupInvitePendingState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_GroupInvitePending; }

            public static GroupInvitePendingState Instance { get; } = new();

            private GroupInvitePendingState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case GroupInviteAcceptedState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }

            public override void OnEntered(RegionRequestGroupMember member)
            {
                var eventScheduler = PlayerManagerService.Instance.EventScheduler.MatchmakingGroupInviteExpired;

                eventScheduler.ScheduleEvent(member.Player.PlayerDbId, TimeSpan.FromSeconds(30),
                    member.Group.GroupInviteExpiredCallback, member.Player);
            }

            public override void OnExited(RegionRequestGroupMember member)
            {
                var eventScheduler = PlayerManagerService.Instance.EventScheduler.MatchmakingGroupInviteExpired;

                eventScheduler.CancelEvent(member.Player.PlayerDbId);
            }
        }

        public sealed class GroupInviteAcceptedState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_GroupInviteAccepted; }

            public static GroupInviteAcceptedState Instance { get; } = new();

            private GroupInviteAcceptedState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case WaitingInQueueState:
                        return member.SetStateInternal(newState);

                    case WaitingInWaitlistState:
                    case MatchInvitePendingState:
                        if (member.Group.IsBypass == false)
                            return Logger.WarnReturn(false, $"SetState(): Attempted to skip to state {newState} in a non-bypass group for player [{member.Player}]");

                        return member.SetStateInternal(newState);
                    
                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public sealed class WaitingInQueueState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_WaitingInQueue; }

            public static WaitingInQueueState Instance { get; } = new();

            private WaitingInQueueState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case MatchInvitePendingState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public sealed class WaitingInWaitlistState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_WaitingInWaitlist; }

            public static WaitingInWaitlistState Instance { get; } = new();

            private WaitingInWaitlistState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case WaitingInQueueState:
                    case WaitingInWaitlistLockedState:
                    case MatchInvitePendingState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public sealed class WaitingInWaitlistLockedState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_WaitingInWaitlistLocked; }

            public static WaitingInWaitlistLockedState Instance { get; } = new();

            private WaitingInWaitlistLockedState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case WaitingInWaitlistState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public sealed class MatchInvitePendingState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_MatchInvitePending; }

            public static MatchInvitePendingState Instance { get; } = new();

            private MatchInvitePendingState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case WaitingInQueueState:
                    case WaitingInWaitlistLockedState:
                    case MatchInviteAcceptedState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }

            public override void OnEntered(RegionRequestGroupMember member)
            {
                var eventScheduler = PlayerManagerService.Instance.EventScheduler.MatchmakingMatchInviteExpired;

                eventScheduler.ScheduleEvent(member.Player.PlayerDbId, TimeSpan.FromSeconds(30),
                    member.Group.MatchInviteExpiredCallback, member.Player);
            }

            public override void OnExited(RegionRequestGroupMember member)
            {
                var eventScheduler = PlayerManagerService.Instance.EventScheduler.MatchmakingMatchInviteExpired;

                eventScheduler.CancelEvent(member.Player.PlayerDbId);
            }
        }

        public sealed class MatchInviteAcceptedState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_MatchInviteAccepted; }

            public static MatchInviteAcceptedState Instance { get; } = new();

            private MatchInviteAcceptedState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case WaitingInQueueState:
                    case InMatchState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public sealed class InMatchState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_InMatch; }

            public static InMatchState Instance { get; } = new();

            private InMatchState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case RemovedGracePeriodState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public sealed class RemovedGracePeriodState : RegionRequestGroupMemberState
        {
            public override RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_RemovedGracePeriod; }

            public static RemovedGracePeriodState Instance { get; } = new();

            private RemovedGracePeriodState() { }

            public override bool SetState(RegionRequestGroupMember member, RegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case MatchInviteAcceptedState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }

            public override void OnEntered(RegionRequestGroupMember member)
            {
                var eventScheduler = PlayerManagerService.Instance.EventScheduler.MatchmakingRemovedGracePeriodExpired;

                eventScheduler.ScheduleEvent(member.Player.PlayerDbId, TimeSpan.FromMinutes(1),
                    member.Group.RemovedGracePeriodExpiredCallback, member.Player);
            }

            public override void OnExited(RegionRequestGroupMember member)
            {
                var eventScheduler = PlayerManagerService.Instance.EventScheduler.MatchmakingRemovedGracePeriodExpired;

                eventScheduler.CancelEvent(member.Player.PlayerDbId);
            }
        }

        #endregion
    }
}
