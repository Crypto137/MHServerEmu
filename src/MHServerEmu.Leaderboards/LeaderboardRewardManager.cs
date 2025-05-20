using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models.Leaderboards;
using MHServerEmu.DatabaseAccess.SQLite;

namespace MHServerEmu.Leaderboards
{
    public class LeaderboardRewardManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);    // If we don't get all confirmations in 5 minutes, something must have gone very wrong

        private readonly Dictionary<ulong, RewardQueryResult> _pendingRewards = new();

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

        public void Update()
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
                FinalizeReward((long)confirmation.LeaderboardId, (long)confirmation.InstanceId, confirmation.ParticipantId);
            }

            // Now initiate new requests
            while (_processRequestQueue.Count > 0)
            {
                GameServiceProtocol.LeaderboardRewardRequest request = _processRequestQueue.Dequeue();
                QueryRewards(request.ParticipantId);
            }

            // Check for timeouts
            TimeSpan now = Clock.UnixTime;
            foreach (var kvp in _pendingRewards)
            {
                if ((now - kvp.Value.Timestamp) >= Timeout)
                {
                    Logger.Error($"Update(): Reward timeout for participant 0x{kvp.Key:X}");
                    _pendingRewards.Remove(kvp.Key);
                }
            }
        }

        private bool QueryRewards(ulong participantId)
        {
            Logger.Debug($"QueryRewards(): participantId=0x{participantId:X}");

            if (_pendingRewards.ContainsKey(participantId))
                return Logger.WarnReturn(false, $"QueryRewards(): Participant 0x{participantId:X} already has pending rewards");

            // Query the database and exit early if there are no rewards to give
            List<DBRewardEntry> dbRewards = SQLiteLeaderboardDBManager.Instance.GetRewards(participantId);
            if (dbRewards.Count == 0)
            {
                Logger.Debug($"QueryRewards(): No rewards for 0x{participantId:X}");
                return true;
            }

            // Keep track of all pending rewards
            _pendingRewards.Add(participantId, new(dbRewards));

            // Send reward information to game
            GameServiceProtocol.LeaderboardRewardEntry[]  rewardEntries = new GameServiceProtocol.LeaderboardRewardEntry[dbRewards.Count];
            for (int i = 0; i < dbRewards.Count; i++)
            {
                DBRewardEntry dbReward = dbRewards[i];
                Logger.Debug($"QueryRewards(): Found reward for participant 0x{participantId}: leaderboardId={dbReward.LeaderboardId}, instanceId={dbReward.InstanceId}");
                rewardEntries[i] = new((ulong)dbReward.LeaderboardId, (ulong)dbReward.InstanceId, (ulong)dbReward.ParticipantId, (ulong)dbReward.RewardId, dbReward.Rank);
            }

            GameServiceProtocol.LeaderboardRewardRequestResponse requestResponse = new(participantId, rewardEntries);
            ServerManager.Instance.SendMessageToService(ServerType.GameInstanceServer, requestResponse);

            return true;
        }

        private bool FinalizeReward(long leaderboardId, long instanceId, ulong participantId)
        {
            Logger.Debug($"FinalizeReward(): leaderboardId={leaderboardId}, instanceId={instanceId}, participantId=0x{participantId:X}");

            if (_pendingRewards.TryGetValue(participantId, out RewardQueryResult rewardQuery) == false)
                return Logger.WarnReturn(false, $"FinalizeReward(): Received confirmation for participant 0x{participantId:X}, who does not have pending rewards");

            List<DBRewardEntry> rewards = rewardQuery.Rewards;

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
                return Logger.WarnReturn(false, $"FinalizeReward(): Failed to find reward for leaderboardId={leaderboardId}, instanceId={instanceId}, participant=0x{participantId:X}");

            // Update reward in the database
            reward.Rewarded();
            SQLiteLeaderboardDBManager.Instance.SetRewarded(reward);

            // Finish this batch of rewards if we have received confirmations for everything
            if (rewards.Count == 0)
            {
                Logger.Debug($"FinalizeReward(): Received confirmation for all pending rewards for participant 0x{participantId:X}");
                _pendingRewards.Remove(participantId);
            }

            return true;
        }

        private readonly struct RewardQueryResult(List<DBRewardEntry> rewards)
        {
            public readonly List<DBRewardEntry> Rewards = rewards;
            public readonly TimeSpan Timestamp = Clock.UnixTime;
        }
    }
}
