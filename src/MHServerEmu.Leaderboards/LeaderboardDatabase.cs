using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.SQLite;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Leaderboards;
using System.Diagnostics;

namespace MHServerEmu.Leaderboards
{   
    /// <summary>
    /// A singleton that contains leaderboard infomation.
    /// </summary>
    public class LeaderboardDatabase
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private const ulong UpdateTimeIntervalMS = 30 * 1000;   // 30 seconds

        private readonly object _lock = new object();
        private Dictionary<PrototypeGuid, Leaderboard> _leaderboards = new();
        private Dictionary<PrototypeGuid, Leaderboard> _metaLeaderboards = new();
        private Dictionary<ulong, string> _playerNames = new();
        public SQLiteLDBManager DBManager { get; private set; }
        public int LeaderboardCount { get; set; }
        public static LeaderboardDatabase Instance { get; } = new();
        private LeaderboardDatabase() { }

        /// <summary>
        /// Initializes the <see cref="LeaderboardDatabase"/> instance.
        /// </summary>
        public bool Initialize(SQLiteLDBManager instance)
        {
            DBManager = instance;

            var stopwatch = Stopwatch.StartNew();

            var config = ConfigManager.Instance.GetConfig<LeaderboardsConfig>();

            // Initialize leaderboard database
            string configPath = Path.Combine(FileHelper.DataDirectory, config.FileName);
            DBManager.Initialize(configPath);

            // load PlayerNames
            if (SQLiteDBManager.Instance.TryGetPlayerNames(_playerNames) == false)
                Logger.Warn($"Failed get player names from SQLiteDBManager");

            // load ActiveLeaderboards
            foreach (var dbLeaderboard in DBManager.GetLeaderboardList())
            {
                if (dbLeaderboard.IsActive == false) continue;

                PrototypeGuid leaderboardId = (PrototypeGuid)dbLeaderboard.LeaderboardId;
                PrototypeId dataRef = GameDatabase.GetDataRefByPrototypeGuid(leaderboardId);
                if (dataRef == PrototypeId.Invalid)
                {
                    Logger.Warn($"Failed GetDataRefByPrototypeGuid LeaderboardId = {leaderboardId}");
                    continue;
                }
                var proto = GameDatabase.GetPrototype<LeaderboardPrototype>(dataRef);
                if (proto == null) 
                {
                    Logger.Warn($"Failed GetPrototype dataRef = {dataRef}");
                    continue;
                }

                var leaderboard = new Leaderboard(proto, dbLeaderboard);
                if (proto.Type == LeaderboardType.MetaLeaderboard)
                    _metaLeaderboards.Add(leaderboardId, leaderboard);
                else
                    _leaderboards.Add(leaderboardId, leaderboard);
            }

            Logger.Info($"Initialized {_leaderboards.Count} leaderboards in {stopwatch.ElapsedMilliseconds} ms");
            return true;
        }

        public string GetPlayerNameById(PrototypeGuid id)
        {
            lock (_lock)
            {
                if (_playerNames.TryGetValue((ulong)id, out var playerName)) return playerName;
                return SQLiteDBManager.Instance.UpdatePlayerName(_playerNames, (ulong)id);
            }
        }

        public bool GetLeaderboardInstances(PrototypeGuid guid, out List<LeaderboardInstance> instances)
        {
            lock (_lock)
            {
                instances = new();

                if (_leaderboards.TryGetValue(guid, out var info) == false) return false;
                if (info.Prototype == null) return false;

                int maxarchived = info.Prototype.MaxArchivedInstances;

                foreach (var instance in info.Instances)
                {
                    instances.Add(instance);
                    if (maxarchived-- == 0) break;
                }

                return true;
            }
        }

        public LeaderboardReport GetLeaderboardReport(NetMessageLeaderboardRequest request)
        {
            PrototypeGuid leaderboardId = 0;
            ulong instanceId = 0;

            var report = LeaderboardReport.CreateBuilder()
                .SetNextUpdateTimeIntervalMS(UpdateTimeIntervalMS);

            if (request.HasPlayerScoreQuery)
            {
                var query = request.PlayerScoreQuery;
                leaderboardId = (PrototypeGuid)query.LeaderboardId;
                instanceId = query.InstanceId;
                var playerId = (PrototypeGuid)query.PlayerId;
                ulong avatarId = query.HasAvatarId ? query.AvatarId : 0;

                if (GetLeaderboardScoreData(leaderboardId, instanceId, playerId, avatarId, out LeaderboardScoreData scoreData))
                    report.SetScoreData(scoreData);
            }

            if (request.HasGuildScoreQuery) // Not used
            {
                var query = request.GuildScoreQuery;
                leaderboardId = (PrototypeGuid)query.LeaderboardId;
                instanceId = query.InstanceId;
                var guid = (PrototypeGuid)query.GuildId;

                if (GetLeaderboardScoreData(leaderboardId, instanceId, guid, 0, out LeaderboardScoreData scoreData))
                    report.SetScoreData(scoreData);
            }

            if (request.HasMetaScoreQuery) // Tournament: Civil War
            {
                var query = request.MetaScoreQuery;
                leaderboardId = (PrototypeGuid)query.LeaderboardId;
                instanceId = query.InstanceId;
                var playerId = (PrototypeGuid)query.PlayerId;

                if (GetLeaderboardScoreData(leaderboardId, instanceId, playerId, 0, out LeaderboardScoreData scoreData))
                    report.SetScoreData(scoreData);
            }

            if (request.HasDataQuery)
            {
                var query = request.DataQuery;
                leaderboardId = (PrototypeGuid)query.LeaderboardId;
                instanceId = query.InstanceId;

                if (GetLeaderboardTableData(leaderboardId, instanceId, out LeaderboardTableData tableData))
                    report.SetTableData(tableData);
            }

            report.SetLeaderboardId((ulong)leaderboardId).SetInstanceId(instanceId);

            return report.Build();
        }

        private bool GetLeaderboardTableData(PrototypeGuid leaderboardId, ulong instanceId, out LeaderboardTableData tableData)
        {
            tableData = null;
            var leaderboard = GetLeaderboard(leaderboardId);
            if (leaderboard == null) return false;

            var instance = leaderboard.GetInstance(instanceId);
            if (instance == null) return false;

            tableData = instance.GetTableData();
            return true;
        }

        private bool GetLeaderboardScoreData(PrototypeGuid leaderboardId, ulong instanceId, PrototypeGuid guid, ulong avatarId, 
            out LeaderboardScoreData scoreData)
        {
            scoreData = null;
            var leaderboard = GetLeaderboard(leaderboardId);
            if (leaderboard == null) return false;

            var type = leaderboard.Prototype.Type;

            var instance = leaderboard.GetInstance(instanceId);
            if (instance == null) return false;

            LeaderboardEntry entry;
            if (type == LeaderboardType.MetaLeaderboard)
            {
                var leaderboardEntryId = instance.GetLeaderboardEntryId(guid);
                entry = instance.GetEntry(leaderboardEntryId, avatarId);
            }
            else
            {
                entry = instance.GetEntry(guid, avatarId);
            }

            if (entry == null) return false;

            var scoreDataBuilder = LeaderboardScoreData.CreateBuilder()
                .SetLeaderboardId((ulong)leaderboardId);

            if (instanceId != 0) scoreDataBuilder.SetInstanceId(instanceId);

            if (type == LeaderboardType.Player) 
            {
                scoreDataBuilder.SetAvatarId(avatarId);
                scoreDataBuilder.SetPlayerId((ulong)guid);
            }

            if (type == LeaderboardType.Guild)
                scoreDataBuilder.SetGuildId((ulong)guid);

            scoreDataBuilder.SetScore(entry.Score);
            scoreDataBuilder.SetPercentileBucket((uint)instance.GetPercentileBucket(entry));

            scoreData = scoreDataBuilder.Build();

            return true;
        }

        public Leaderboard GetLeaderboard(PrototypeGuid guid)
        {
            lock (_lock)
            {
                if (_leaderboards.TryGetValue(guid, out var leaderboard))
                    return leaderboard;
                return null;
            }
        }

        public void UpdateLeaderboards(Queue<LeaderboardQueue> updateQueue)
        {
            while (updateQueue.TryDequeue(out var queue)) 
            {
                var leaderboard = GetLeaderboard(queue.LeaderboardId);
                leaderboard?.OnUpdate(queue);
            }
        }
    }
}
