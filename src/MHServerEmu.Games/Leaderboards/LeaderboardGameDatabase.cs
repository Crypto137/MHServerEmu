using Gazillion;
using MHServerEmu.Core.Helpers;
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

        private static readonly string LeaderboardsDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "Leaderboards");
        private Dictionary<PrototypeGuid, LeaderboardInfo> _leaderboardInfoMap = new();
        private Queue<LeaderboardQueue> _updateQueue = new(); 
        private readonly object updateLock = new object();
        public static LeaderboardGameDatabase Instance { get; } = new();
        public int LeaderboardCount { get; set; }

        private LeaderboardGameDatabase() { }

        /// <summary>
        /// Initializes the <see cref="LeaderboardGameDatabase"/> instance.
        /// </summary>
        public bool Initialize()
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Check leaderboards
            string configPath = Path.Combine(LeaderboardsDirectory, "Leaderboard.db");
            if (File.Exists(configPath) == false)
            {
                // TODO create new leaderboard.db
            }

            Logger.Info($"Initialized {_leaderboardInfoMap.Count} leaderboards in {stopwatch.ElapsedMilliseconds} ms");
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
