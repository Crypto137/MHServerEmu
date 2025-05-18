using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.Models.Leaderboards;
using MHServerEmu.DatabaseAccess.SQLite;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardRewardManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, List<DBRewardEntry>> _pendingRewards = new();

        private Queue<GameServiceProtocol.LeaderboardRewardRequest> _requestQueue = new();
        private Queue<GameServiceProtocol.LeaderboardRewardRequest> _processRequestQueue = new();
        private Queue<GameServiceProtocol.LeaderboardRewardConfirmation> _confirmationQueue = new();
        private Queue<GameServiceProtocol.LeaderboardRewardConfirmation> _processConfirmationQueue = new();

        private readonly object _queueLock = new();

        public LeaderboardRewardManager()
        {
        }

        public void OnLeaderboardRewardRequest(in GameServiceProtocol.LeaderboardRewardRequest request)
        {
            lock (_queueLock)
                _requestQueue.Enqueue(request);
        }

        public void OnLeaderboardRewardConfirmation(in GameServiceProtocol.LeaderboardRewardConfirmation confirmation)
        {
            lock (_queueLock)
                _confirmationQueue.Enqueue(confirmation);
        }

        public void ProcessMessages()
        {
            // Swap queues
            lock (_queueLock)
            {
                (_requestQueue, _processRequestQueue) = (_processRequestQueue, _requestQueue);
                (_confirmationQueue, _processConfirmationQueue) = (_processConfirmationQueue, _confirmationQueue);
            }

            // Process confirmations first
            while (_processConfirmationQueue.Count > 0)
            {
                GameServiceProtocol.LeaderboardRewardConfirmation confirmation = _processConfirmationQueue.Dequeue();
                FinalizeReward((long)confirmation.LeaderboardId, (long)confirmation.InstanceId, confirmation.GameId);
            }

            // Now initiate new requests
            while (_processRequestQueue.Count > 0)
            {
                GameServiceProtocol.LeaderboardRewardRequest request = _processRequestQueue.Dequeue();
                QueryRewards(request.GameId);
            }
        }

        private bool QueryRewards(ulong gameId)
        {
            Logger.Debug($"QueryRewards(): gameId=0x{gameId:X}");

            if (_pendingRewards.ContainsKey(gameId))
                return Logger.WarnReturn(false, $"QueryRewards(): Player 0x{gameId:X} already has pending rewards");

            // Query the database and exit early if there are no rewards to give
            List<DBRewardEntry> dbRewards = SQLiteLeaderboardDBManager.Instance.GetRewards(gameId);
            if (dbRewards.Count == 0)
            {
                Logger.Debug($"QueryRewards(): No rewards for 0x{gameId:X}");
                return true;
            }

            // Keep track of all pending rewards
            _pendingRewards.Add(gameId, dbRewards);

            // Send reward information to game
            GameServiceProtocol.LeaderboardRewardEntry[]  rewardEntries = new GameServiceProtocol.LeaderboardRewardEntry[dbRewards.Count];
            for (int i = 0; i < dbRewards.Count; i++)
            {
                DBRewardEntry dbReward = dbRewards[i];
                Logger.Debug($"QueryRewards(): Found reward for 0x{gameId}: leaderboardId={dbReward.LeaderboardId}, instanceId={dbReward.InstanceId}");
                rewardEntries[i] = new((ulong)dbReward.LeaderboardId, (ulong)dbReward.InstanceId, (ulong)dbReward.GameId, (ulong)dbReward.RewardId, dbReward.Rank);
            }

            GameServiceProtocol.LeaderboardRewardRequestResponse requestResponse = new(gameId, rewardEntries);
            ServerManager.Instance.SendMessageToService(ServerType.GameInstanceServer, requestResponse);

            return true;
        }

        private bool FinalizeReward(long leaderboardId, long instanceId, ulong gameId)
        {
            Logger.Debug($"FinalizeReward(): leaderboardId={leaderboardId}, instanceId={instanceId}, gameId=0x{gameId:X}");

            if (_pendingRewards.TryGetValue(gameId, out List<DBRewardEntry> rewards) == false)
                return Logger.WarnReturn(false, $"FinalizeReward(): Received confirmation for player 0x{gameId:X}, who does not have pending rewards");

            // Find the specified pending reward
            DBRewardEntry reward = null;
            for (int i = 0; i < rewards.Count; i++)
            {
                DBRewardEntry itReward = rewards[i];
                if (itReward.LeaderboardId == leaderboardId && itReward.InstanceId == instanceId)
                {
                    reward = itReward;
                    rewards.RemoveAt(i);
                    break;
                }
            }

            if (reward == null)
                return Logger.WarnReturn(false, $"FinalizeReward(): Failed to find reward for leaderboardId={leaderboardId}, instanceId={instanceId}, gameId=0x{gameId:X}");

            // Update reward in the database
            reward.Rewarded();
            SQLiteLeaderboardDBManager.Instance.SetRewarded(reward);

            // Finish this batch of rewards if we have received confirmations for everything
            if (rewards.Count == 0)
            {
                Logger.Debug($"FinalizeReward(): Received confirmation for all pending rewards for 0x{gameId:X}");
                _pendingRewards.Remove(gameId);
            }

            return true;
        }
    }
}
