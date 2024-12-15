using MHServerEmu.Games.GameData.Prototypes;
using System.Collections.Concurrent;

namespace MHServerEmu.Games.Leaderboards
{
    public class Leaderboard
    {
        public ulong LeaderboardId { get; } // PrototypeGuid
        public LeaderboardPrototype Prototype { get; }
        public List<LeaderboardInstance> Instances { get; }
        public LeaderboardInstance ActiveInstance { get; protected set; }
        public bool IsActive { get; set; }
        public bool NeedsUpdate { get => _updateCounter > 0; }

        private int _updateCounter;
        private ConcurrentQueue<LeaderboardQueue> _updateQueue;

        public Leaderboard(LeaderboardPrototype proto)
        {
            Prototype = proto;

            _updateCounter = 0;
            _updateQueue = new();
        }

        public LeaderboardInstance GetInstance(ulong instanceId)
        {
            return Instances.Find(instance => instance.InstanceId == instanceId);
        }

        public void OnUpdate()
        {
            while (_updateQueue.TryDequeue(out LeaderboardQueue queue))
            {
                ActiveInstance?.OnUpdate(queue);
                Interlocked.Decrement(ref _updateCounter);
            }
        }

        public void AddToQueue(in LeaderboardQueue queue)
        {
            if (IsActive)
            {
                _updateQueue.Enqueue(queue);
                Interlocked.Increment(ref _updateCounter);
            }
        }
    }

    public struct LeaderboardQueue
    {
        public ulong GameId;
        public ulong AvatarId;
        public long RuleId;
        public int Count;

        public LeaderboardQueue(in LeaderboardGuidKey key, int count)
        {
            GameId = key.PlayerGuid;
            AvatarId = (ulong)key.AvatarGuid;
            RuleId = key.RuleGuid;
            Count = count;
        }
    }
}
