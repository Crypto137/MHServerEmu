using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Leaderboards;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// Allows <see cref="IGameService"/> implementations to send <see cref="IGameServiceMessage"/> instances to a <see cref="Game"/>. 
    /// </summary>
    public class ServiceMailbox
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // IGameServiceMessage are boxed anyway when doing pattern matching, so it should probably be fine.
        // If we encounter performance issues here, replace this with a specialized data structure.
        private Queue<IGameServiceMessage> _pendingQueue = new();
        private Queue<IGameServiceMessage> _processQueue = new();

        private SpinLock _lock = new(false);

        public Game Game { get; }

        public ServiceMailbox(Game game)
        {
            Game = game;
        }

        /// <summary>
        /// Called from other threads to post an <see cref="IGameServiceMessage"/>
        /// </summary>
        public void PostMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            // Explicitly box beforehand to minimize time spent in spinlock
            IGameServiceMessage boxedMessage = message;

            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                _pendingQueue.Enqueue(boxedMessage);
            }
            finally
            {
                if (lockTaken)
                    _lock.Exit(false);
            }
        }

        public void ProcessMessages()
        {
            // Swap queues so that we can continue queueing messages while we process
            bool lockTaken = false;
            try
            {
                _lock.Enter(ref lockTaken);
                (_pendingQueue, _processQueue) = (_processQueue, _pendingQueue);
            }
            finally
            {
                if (lockTaken)
                    _lock.Exit(false);
            }

            while (_processQueue.Count > 0)
                HandleServiceMessage(_processQueue.Dequeue());

        }

        private void HandleServiceMessage(IGameServiceMessage message)
        {
            switch (message)
            {
                case GameServiceProtocol.LeaderboardStateChange leaderboardStateChange:
                    OnLeaderboardStateChange(leaderboardStateChange);
                    break;

                case GameServiceProtocol.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse:
                    OnLeaderboardRewardRequestResponse(leaderboardRewardRequestResponse);
                    break;
            }
        }

        #region Leaderboard Messages

        private void OnLeaderboardStateChange(in GameServiceProtocol.LeaderboardStateChange leaderboardStateChange)
        {
            LeaderboardState state = leaderboardStateChange.State;
            bool rewarded = state == LeaderboardState.eLBS_Rewarded;
            bool sendClient = state == LeaderboardState.eLBS_Created
                || state == LeaderboardState.eLBS_Active
                || state == LeaderboardState.eLBS_Expired
                || state == LeaderboardState.eLBS_Rewarded;

            NetMessageLeaderboardStateChange message = null;
            if (sendClient)
                message = leaderboardStateChange.ToProtobuf();

            foreach (var player in new PlayerIterator(Game))
            {
                player.LeaderboardManager.OnUpdateEventContext();

                if (rewarded)
                    player.LeaderboardManager.RequestRewards();

                if (sendClient)
                {
                    //Logger.Debug($"OnLeaderboardStateChange(): Sending [{leaderboardStateChange.InstanceId}][{state}] to {player.GetName()}");
                    player.SendMessage(message);
                }
            }
        }

        private bool OnLeaderboardRewardRequestResponse(in GameServiceProtocol.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse)
        {
            ulong playerId = leaderboardRewardRequestResponse.ParticipantId;
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(leaderboardRewardRequestResponse.ParticipantId);
            if (player == null)
                return Logger.WarnReturn(false, $"OnLeaderboardRewardRequestResponse(): Player 0x{playerId:X} not found in game [{Game}]");

            player.LeaderboardManager.AddPendingRewards(leaderboardRewardRequestResponse.Entries);
            return true;
        }

        #endregion
    }
}
