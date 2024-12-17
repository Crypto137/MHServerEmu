using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using System.Diagnostics;

namespace MHServerEmu.Games.Leaderboards
{   
    /// <summary>
    /// A singleton that contains leaderboard infomation.
    /// </summary>
    public class LeaderboardGameDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private Dictionary<PrototypeGuid, LeaderboardInfo> _leaderboardInfoMap = new();
        private Queue<LeaderboardQueue> _updateQueue = new(); 
        private readonly object updateLock = new object();
        public static LeaderboardGameDatabase Instance { get; } = new();

        private LeaderboardGameDatabase() { }

        /// <summary>
        /// Initializes the <see cref="LeaderboardGameDatabase"/> instance.
        /// </summary>
        public bool Initialize()
        {
            var stopwatch = Stopwatch.StartNew();

            int count = 0;

            // Load leaderboard prototypes
            foreach (var dataRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<LeaderboardPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var proto = GameDatabase.GetPrototype<LeaderboardPrototype>(dataRef);
                if (proto == null) Logger.Warn($"Prototype {dataRef} == null");
                count++;
            }

            Logger.Info($"Initialized {count} leaderboards in {stopwatch.ElapsedMilliseconds} ms");

            return true;
        }

        public IEnumerable<LeaderboardPrototype> GetActiveLeaderboardPrototypes()
        {
            foreach (var leaderboard in _leaderboardInfoMap.Values)
                foreach (var instance in leaderboard.Instances)
                    if (instance.State == LeaderboardState.eLBS_Active)
                        yield return leaderboard.Prototype;
        }

        public void UpdateLeaderboards()
        {
           // TODO
        }

        public void AddUpdateQueue(LeaderboardQueue queue)
        {
            lock (updateLock)
            {
                _updateQueue.Enqueue(queue);
            }
        }

        public Queue<LeaderboardQueue> GetUpdateQueue()
        {
            lock (updateLock)
            {
                Queue<LeaderboardQueue> queue = new(_updateQueue);
                _updateQueue.Clear();
                return queue;
            }
        }
    }
}
