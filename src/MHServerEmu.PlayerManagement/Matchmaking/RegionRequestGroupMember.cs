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

            State = InitialState.Instance;
            Status = RegionRequestQueueUpdateVar.eRRQ_Invalid;
        }

        public void SetState(IRegionRequestGroupMemberState newState)
        {
            IRegionRequestGroupMemberState oldState = State;

            if (newState == oldState)
                return;

            oldState.SetState(this, newState);
        }

        private void SetStateInternal(IRegionRequestGroupMemberState newState)
        {
            State = newState;
            Group.UpdatePlayerStatus(Player, newState.StatusVar);

            /* TODO: Set invite expiration timer for GroupInvitePendingState and MatchInvitePendingState
            switch (newState)
            {
                case GroupInvitePendingState:
                    break;

                case MatchInvitePendingState:
                    break;
            }
            */
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
                    //case WaitingInWaitlistLockedState:
                    case MatchInvitePendingState:
                        member.SetStateInternal(newState);
                        return true;

                    default:
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
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
                        member.SetStateInternal(newState);
                        return true;

                    default:
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
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
                        member.SetStateInternal(newState);
                        return true;

                    default:
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
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
                        member.SetStateInternal(newState);
                        return true;

                    case WaitingInWaitlistState:
                    case MatchInvitePendingState:
                        if (member.Group.IsBypass == false)
                            return Logger.WarnReturn(false, $"SetState(): Attempted to skip to state {newState} in a non-bypass group for player [{member.Player}]");

                        member.SetStateInternal(newState);
                        return true;
                    
                    default:
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
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
                        member.SetStateInternal(newState);
                        return true;

                    default:
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
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
                    //case WaitingInWaitlistLockedState:
                    case MatchInvitePendingState:
                        member.SetStateInternal(newState);
                        return true;

                    default:
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
                }
            }
        }

        /* TODO: Figure out if we need this and what states this can transition to.
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
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
                }
            }
        }
        */

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
                    //case WaitingInWaitlistLockedState:
                    case MatchInviteAcceptedState:
                        member.SetStateInternal(newState);
                        return true;

                    default:
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
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
                        member.SetStateInternal(newState);
                        return true;

                    default:
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
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
                        member.SetStateInternal(newState);
                        return true;

                    default:
                        return Logger.WarnReturn(false, $"SetState(): Attempted invalid state transition {member.State} -> {newState} for player [{member.Player}]");
                }
            }
        }

        #endregion
    }
}
