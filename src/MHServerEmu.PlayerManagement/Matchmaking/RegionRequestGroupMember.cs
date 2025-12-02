using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.PlayerManagement.Matchmaking
{
    /// <summary>
    /// Represents a state of a <see cref="RegionRequestGroupMember"/>.
    /// </summary>
    public interface IRegionRequestGroupMemberState
    {
        public RegionRequestQueueUpdateVar StatusVar { get; }

        public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState);
    }

    public class RegionRequestGroupMember
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public RegionRequestGroup Group { get; }
        public PlayerHandle Player { get; }
        public IRegionRequestGroupMemberState State { get; private set; }
        public RegionRequestQueueUpdateVar Status { get; set; }

        public RegionRequestGroupMember(RegionRequestGroup group, PlayerHandle player)
        {
            Group = group;
            Player = player;

            // We don't want to send status updates during initialization, so set state directly without calling SetStateInternal().
            State = InitialState.Instance;
            Status = RegionRequestQueueUpdateVar.eRRQ_Invalid;
        }

        public bool SetState(IRegionRequestGroupMemberState newState)
        {
            IRegionRequestGroupMemberState oldState = State;

            if (newState == oldState)
                return false;

            return oldState.SetState(this, newState);
        }

        private bool SetStateInternal(IRegionRequestGroupMemberState newState)
        {
            IRegionRequestGroupMemberState oldState = State;

            // This should have already been validated by now
            if (newState == oldState)
                return Logger.WarnReturn(false, "SetStateInternal(): newState == oldState");

            State = newState;
            Group.UpdatePlayerStatus(Player, newState.StatusVar);

            /* TODO: Set and cancel expiration timers for RemovedGracePeriod, GroupInvitePendingState, and MatchInvitePendingState
            switch (newState)
            {
                case RemovedGracePeriod:
                    break;

                case GroupInvitePendingState:
                    break;

                case MatchInvitePendingState:
                    break;
            }
            */

            return true;
        }

        private bool OnInvalidStateTransition(IRegionRequestGroupMemberState newState)
        {
            Logger.Warn($"SetState(): Attempted invalid state transition {State} -> {newState} for player [{Player}]");
            return false;
        }

        #region State Implementations

        // NOTE: IRegionRequestGroupMemberState implementations need to be nested in RegionRequestGroupMember to be able to access its private members.

        public class InitialState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_Invalid; }

            public static InitialState Instance { get; } = new();

            private InitialState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
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

        public class RemovedGracePeriodState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_RemovedGracePeriod; }

            public static RemovedGracePeriodState Instance { get; } = new();

            private RemovedGracePeriodState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case MatchInviteAcceptedState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public class GroupInvitePendingState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_GroupInvitePending; }

            public static GroupInvitePendingState Instance { get; } = new();

            private GroupInvitePendingState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    case GroupInviteAcceptedState:
                        return member.SetStateInternal(newState);

                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public class GroupInviteAcceptedState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_GroupInviteAccepted; }

            public static GroupInviteAcceptedState Instance { get; } = new();

            private GroupInviteAcceptedState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
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

        public class WaitingInQueueState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_WaitingInQueue; }

            public static WaitingInQueueState Instance { get; } = new();

            private WaitingInQueueState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
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

        public class WaitingInWaitlistState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_WaitingInWaitlist; }

            public static WaitingInWaitlistState Instance { get; } = new();

            private WaitingInWaitlistState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
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

        // TODO: Figure out if we need this and how we can recover from this state.
        public class WaitingInWaitlistLockedState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_WaitingInWaitlistLocked; }

            public static WaitingInWaitlistLockedState Instance { get; } = new();

            private WaitingInWaitlistLockedState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
            {
                switch (newState)
                {
                    default:
                        return member.OnInvalidStateTransition(newState);
                }
            }
        }

        public class MatchInvitePendingState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_MatchInvitePending; }

            public static MatchInvitePendingState Instance { get; } = new();

            private MatchInvitePendingState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
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
        }

        public class MatchInviteAcceptedState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_MatchInviteAccepted; }

            public static MatchInviteAcceptedState Instance { get; } = new();

            private MatchInviteAcceptedState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
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

        public class InMatchState : IRegionRequestGroupMemberState
        {
            public RegionRequestQueueUpdateVar StatusVar { get => RegionRequestQueueUpdateVar.eRRQ_InMatch; }

            public static InMatchState Instance { get; } = new();

            private InMatchState() { }

            public bool SetState(RegionRequestGroupMember member, IRegionRequestGroupMemberState newState)
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

        #endregion
    }
}
