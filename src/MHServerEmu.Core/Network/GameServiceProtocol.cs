using System.Buffers;
using Gazillion;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.Network
{
    /// <summary>
    /// Marker interface for <see cref="IGameService"/> messages.
    /// </summary>
    public interface IGameServiceMessage
    {
    }

    public static class GameServiceProtocol
    {
        // NOTE: Although we are currently using readonly structs here, unfortunately it seems
        // using pattern matching to switch on the message type causes boxing. Need to figure
        // out a more performant way to send messages without overcomplicating everything
        // (e.g. using the visitor pattern here would probably work, but it may be too cumbersome).

        public readonly struct AddClient(IFrontendClient client) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
        }

        public readonly struct RemoveClient(IFrontendClient client) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
        }

        public readonly struct RouteMessageBuffer(IFrontendClient client, MessageBuffer messageBuffer) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
            public readonly MessageBuffer MessageBuffer = messageBuffer;
        }

        public readonly struct RouteMessage(IFrontendClient client, Type protocol, MailboxMessage message) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
            public readonly Type Protocol = protocol;
            public readonly MailboxMessage Message = message;
        }

        #region Grouping Manager

        public readonly struct GroupingManagerChat(IFrontendClient client, NetMessageChat chat, int prestigeLevel, List<ulong> playerFilter) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
            public readonly NetMessageChat Chat = chat;
            public readonly int PrestigeLevel = prestigeLevel;
            public readonly List<ulong> PlayerFilter = playerFilter;
        }

        public readonly struct GroupingManagerTell(IFrontendClient client, NetMessageTell tell) : IGameServiceMessage
        {
            public readonly IFrontendClient Client = client;
            public readonly NetMessageTell Tell = tell;
        }

        #endregion

        #region Leaderboards

        /// <summary>
        /// [Game -> LeaderboardService] Communicates a change of state of a specific leaderboard rule.
        /// </summary>
        public readonly struct LeaderboardScoreUpdate(ulong leaderboardId, ulong participantId, ulong avatarId, ulong ruleId, ulong count) : IGameServiceMessage
        {
            public readonly ulong LeaderboardId = leaderboardId;
            public readonly ulong ParticipantId = participantId;
            public readonly ulong AvatarId = avatarId;
            public readonly ulong RuleId = ruleId;
            public readonly ulong Count = count;
        }

        /// <summary>
        /// [Game -> LeaderboardService] Container for a batch of <see cref="LeaderboardScoreUpdate"/> instances.
        /// </summary>
        public readonly struct LeaderboardScoreUpdateBatch(int count) : IGameServiceMessage
        {
            private static readonly ArrayPool<LeaderboardScoreUpdate> Pool = ArrayPool<LeaderboardScoreUpdate>.Create();

            // Use arrays instead of lists to access data by reference instead of copying.
            // ArrayPool can return arrays larger than requested, so we also need to specify count.
            private readonly LeaderboardScoreUpdate[] _updates = Pool.Rent(count);

            public readonly int Count = count;

            public ref LeaderboardScoreUpdate this[int i] { get => ref _updates[i]; }

            /// <summary>
            /// Releases resources used by this <see cref="LeaderboardScoreUpdateBatch"/>. Call this when this instance is no longer needed.
            /// </summary>
            public void Destroy()
            {
                Pool.Return(_updates);
            }
        }

        /// <summary>
        /// [LeaderboardService -> Game] Communicates a change of state of a specific leaderboard.
        /// </summary>
        public readonly struct LeaderboardStateChange(ulong leaderboardId, ulong instanceId, LeaderboardState state, 
            DateTime activationTime, DateTime expirationTime, bool visible) : IGameServiceMessage
        {
            public readonly ulong LeaderboardId = leaderboardId;
            public readonly ulong InstanceId = instanceId;
            public readonly LeaderboardState State = state;
            public readonly DateTime ActivationTime = activationTime;
            public readonly DateTime ExpirationTime = expirationTime;
            public readonly bool Visible = visible;

            public NetMessageLeaderboardStateChange ToProtobuf()
            {
                return NetMessageLeaderboardStateChange.CreateBuilder()
                    .SetLeaderboardId(LeaderboardId)
                    .SetInstanceId(InstanceId)
                    .SetNewState(State)
                    .SetActivationTimestamp(Clock.DateTimeToTimestamp(ActivationTime))
                    .SetExpirationTimestamp(Clock.DateTimeToTimestamp(ExpirationTime))
                    .SetVisible(Visible)
                    .Build();
            }
        }

        /// <summary>
        /// [LeaderboardService -> Game] Container for a batch of <see cref="LeaderboardStateChange"/> instances.
        /// </summary>
        public readonly struct LeaderboardStateChangeList(List<LeaderboardStateChange> list) : IGameServiceMessage
        {
            // This is currently used only during server initialization, so it's okay not to pool this.
            public readonly IReadOnlyList<LeaderboardStateChange> List = list;

            public List<LeaderboardStateChange>.Enumerator GetEnumerator()
            {
                return ((List<LeaderboardStateChange>)List).GetEnumerator();
            }
        }

        /// <summary>
        /// [Game -> LeaderboardService] Requests for a list of <see cref="LeaderboardRewardEntry"/> instances for the specified participant.
        /// </summary>
        public readonly struct LeaderboardRewardRequest(ulong participantId) : IGameServiceMessage
        {
            public readonly ulong ParticipantId = participantId;
        }

        /// <summary>
        /// [LeaderboardService -> Game] Communicates a reward for the specified participant.
        /// </summary>
        public readonly struct LeaderboardRewardEntry(ulong leaderboardId, ulong instanceId, ulong participantId, ulong rewardId, int rank) : IGameServiceMessage
        {
            public readonly ulong LeaderboardId = leaderboardId;
            public readonly ulong InstanceId = instanceId;
            public readonly ulong ParticipantId = participantId;
            public readonly ulong RewardId = rewardId;
            public readonly int Rank = rank;
        }

        /// <summary>
        /// [LeaderboardService -> Game] Container for a batch of <see cref="LeaderboardRewardEntry"/> instances.
        /// </summary>
        public readonly struct LeaderboardRewardRequestResponse(ulong participantId, LeaderboardRewardEntry[] entries) : IGameServiceMessage
        {
            // This probably doesn't happen frequently enough to pool
            public readonly ulong ParticipantId = participantId;
            public readonly LeaderboardRewardEntry[] Entries = entries;
        }

        /// <summary>
        /// [Game -> LeaderboardService] Communicates that a reward has been distributed to the specified participant.
        /// </summary>
        public readonly struct LeaderboardRewardConfirmation(ulong leaderboardId, ulong instanceId, ulong participantId) : IGameServiceMessage
        {
            public readonly ulong LeaderboardId = leaderboardId;
            public readonly ulong InstanceId = instanceId;
            public readonly ulong ParticipantId = participantId;
        }

        #endregion
    }
}
