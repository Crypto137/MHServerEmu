using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.DatabaseAccess.SQLite;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Leaderboards;
using System.Diagnostics;
using System.Text.Json;

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
        private static readonly string LeaderboardsDirectory = Path.Combine(FileHelper.DataDirectory, "Leaderboards");
        private Dictionary<PrototypeGuid, Leaderboard> _leaderboards = new();
        private Dictionary<PrototypeGuid, Leaderboard> _metaLeaderboards = new();
        private Dictionary<ulong, string> _playerNames = new();
        public SQLiteLeaderboardDBManager DBManager { get; private set; }
        public int LeaderboardCount { get => _leaderboards.Count; }
        public static LeaderboardDatabase Instance { get; } = new();
        private LeaderboardDatabase() { }

        /// <summary>
        /// Initializes the <see cref="LeaderboardDatabase"/> instance.
        /// </summary>
        public bool Initialize(SQLiteLeaderboardDBManager instance)
        {
            DBManager = instance;

            var stopwatch = Stopwatch.StartNew();

            var config = ConfigManager.Instance.GetConfig<LeaderboardsConfig>();

            // Initialize leaderboard database
            string configPath = Path.Combine(LeaderboardsDirectory, config.FileName);
            bool noTables = false;
            DBManager.Initialize(configPath, ref noTables);

            // Add leaderboards from prototypes
            string jsonConfigPath = Path.Combine(LeaderboardsDirectory, config.JsonConfig);
            if (noTables) GenerateTables(jsonConfigPath);

            // load PlayerNames
            if (SQLiteDBManager.Instance.TryGetPlayerNames(_playerNames) == false)
                Logger.Warn($"Failed get player names from SQLiteDBManager");

            // load ActiveLeaderboards
            List<DBLeaderboard> activeLeaderboards = new();
            LoadJsonConfig(jsonConfigPath, activeLeaderboards);
            LoadLeaderboards();

            // send to LeaderboardGameDatabase
            SendLeaderboardsToGameDatabase();

            Logger.Info($"Initialized {_leaderboards.Count} leaderboards in {stopwatch.ElapsedMilliseconds} ms");
            return true;
        }

        private bool LoadJsonConfig(string jsonConfigPath, List<DBLeaderboard> activeLeaderboards)
        {
            List<DBLeaderboardInstance> dbInstances = new();
            string leaderboardsJson = File.ReadAllText(jsonConfigPath);

            try
            {
                JsonSerializerOptions options = new();
                var leaderboards = JsonSerializer.Deserialize<IEnumerable<LeaderboardSchedule>>(leaderboardsJson, options);
                var oldDbLeaderboards = DBManager.GetLeaderboards();
                foreach (LeaderboardSchedule leaderboard in leaderboards)
                {
                    // Skip old
                    var oldLeaderboard = oldDbLeaderboards.First(lb => lb.LeaderboardId == leaderboard.LeaderboardId);
                    if (oldLeaderboard == null) continue;
                    if (oldLeaderboard.IsActive == leaderboard.IsActive 
                        && oldLeaderboard.Schedule == leaderboard.Schedule) continue;
                    
                    var activeLeaderboard = leaderboard.ToDBLeaderboard();
                    activeLeaderboard.ActiveInstanceId = oldLeaderboard.ActiveInstanceId;
                    activeLeaderboards.Add(activeLeaderboard);

                    if (leaderboard.IsActive)
                        dbInstances.Add(new DBLeaderboardInstance
                        {
                            InstanceId = oldLeaderboard.ActiveInstanceId + 1,
                            LeaderboardId = leaderboard.LeaderboardId,
                            State = LeaderboardState.eLBS_Created,
                            ActivationDate = 0,
                            Visible = leaderboard.IsActive
                        });
                }
            }
            catch (Exception e)
            {
                return Logger.WarnReturn(false, $"Initialize(): LeaderboardsJson {jsonConfigPath} deserialization failed - {e.Message}");
            }

            DBManager.UpdateLeaderboards(activeLeaderboards);
            DBManager.SetInstances(dbInstances);

            return activeLeaderboards.Count > 0;
        }

        public void ReloadJsonConfig()
        {
            lock (_lock)
            {
                var config = ConfigManager.Instance.GetConfig<LeaderboardsConfig>();
                string jsonConfigPath = Path.Combine(LeaderboardsDirectory, config.JsonConfig);
                
                List<DBLeaderboard> activeLeaderboards = new();
                if (LoadJsonConfig(jsonConfigPath, activeLeaderboards))
                    ReloadActiveLeaderboards(activeLeaderboards);
            }
        }

        private void ReloadActiveLeaderboards(List<DBLeaderboard> activeLeaderboards)
        {            
            foreach (var activeLeaderboard in activeLeaderboards)
            {
                var leaderboard = GetLeaderboard((PrototypeGuid)activeLeaderboard.LeaderboardId);
                if (leaderboard == null) continue;
                
                if (activeLeaderboard.IsActive)
                {
                    leaderboard.SetSchedule(activeLeaderboard);

                    // Add new instances
                    var dbNewInstance = new DBLeaderboardInstance
                    {
                        InstanceId = activeLeaderboard.ActiveInstanceId + 1,
                        LeaderboardId = activeLeaderboard.LeaderboardId,
                        State = LeaderboardState.eLBS_Created,
                        ActivationDate = 0,
                        Visible = true
                    };

                    if (leaderboard.Prototype.IsMetaLeaderboard)
                    {
                        var metaInstance = leaderboard.GetInstance((ulong)activeLeaderboard.ActiveInstanceId);
                        // add new MetaInstances
                        metaInstance?.AddMetaInstances(dbNewInstance.InstanceId);
                    }

                    leaderboard.AddInstance(dbNewInstance, false);
                    leaderboard.OnStateChange((ulong)dbNewInstance.InstanceId, LeaderboardState.eLBS_Created);
                }
                else
                {
                    var activeInstance = leaderboard.ActiveInstance;
                    if (activeInstance == null) continue;

                    // disable active instance
                    activeInstance.Visible = false;
                    activeInstance.State = LeaderboardState.eLBS_Rewarded;
                    activeInstance.UpdateDBState(LeaderboardState.eLBS_Rewarded);
                    leaderboard.OnStateChange(leaderboard.ActiveInstance.InstanceId, LeaderboardState.eLBS_Rewarded);
                }
            }
        }

        private void LoadLeaderboards()
        {
            foreach (var dbLeaderboard in DBManager.GetLeaderboards())
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
                if (proto.IsMetaLeaderboard)
                    _metaLeaderboards.Add(leaderboardId, leaderboard);
                else
                    _leaderboards.Add(leaderboardId, leaderboard);
            }
        }

        private void SendLeaderboardsToGameDatabase()
        {
            List<LeaderboardInstanceInfo> instances = new();

            foreach (var leaderboard in _leaderboards.Values)
                leaderboard.GetInstancesInfo(instances);

            foreach (var leaderboard in _metaLeaderboards.Values)
                leaderboard.GetInstancesInfo(instances);

            LeaderboardGameDatabase.Instance.UpdateLeaderboards(instances);
        }

        private void GenerateTables(string jsonConfigPath)
        {
            List<DBLeaderboard> dbLeaderboards = new();
            List<DBLeaderboardInstance> dbInstances = new();
            List<LeaderboardSchedule> jsonLeaderboards = new();

            foreach (var dataRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<LeaderboardPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                var proto = GameDatabase.GetPrototype<LeaderboardPrototype>(dataRef);
                if (proto == null || proto.DesignState != DesignWorkflowState.Live || proto.Public == false) continue;

                var leaderboardId = GameDatabase.GetPrototypeGuid(dataRef);
                var instanceId = (long)Leaderboard.GenInstanceId(leaderboardId);

                bool isActive = proto.ResetFrequency == LeaderboardResetFrequency.NeverReset;
                if (leaderboardId == (PrototypeGuid)16486420054343424221) isActive = false; // Anniversary2016

                var dbLeaderboard = new DBLeaderboard
                {
                    LeaderboardId = (long)leaderboardId,
                    PrototypeName = dataRef.GetNameFormatted(),
                    ActiveInstanceId = instanceId,
                    IsActive = isActive,
                    Schedule = "* * * * *"
                };

                dbLeaderboards.Add(dbLeaderboard);
                jsonLeaderboards.Add(new LeaderboardSchedule(dbLeaderboard));

                dbInstances.Add(new DBLeaderboardInstance
                {
                    InstanceId = instanceId,
                    LeaderboardId = (long)leaderboardId,
                    State = isActive ? LeaderboardState.eLBS_Created : LeaderboardState.eLBS_Rewarded,
                    ActivationDate = 0,
                    Visible = isActive
                });

                if (proto.IsMetaLeaderboard)
                {
                    List<DBMetaInstance> dbMetaInstances = new();
                    foreach (var meta in proto.MetaLeaderboardEntries)
                    {
                        var metaLeaderboardId = GameDatabase.GetPrototypeGuid(meta.Leaderboard);
                        var metaInstanceId = (long)Leaderboard.GenInstanceId(metaLeaderboardId);
                        dbMetaInstances.Add(new DBMetaInstance
                        {
                            LeaderboardId = (long)leaderboardId,
                            InstanceId = instanceId,
                            MetaLeaderboardId = (long)metaLeaderboardId,
                            MetaInstanceId = metaInstanceId
                        });
                    }
                    DBManager.SetMetaInstances(dbMetaInstances);
                }
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize<IEnumerable<LeaderboardSchedule>>(jsonLeaderboards, options);
            File.WriteAllText(jsonConfigPath, json);

            DBManager.SetLeaderboards(dbLeaderboards);
            DBManager.SetInstances(dbInstances);
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

                int maxInstances = info.Prototype.MaxArchivedInstances;

                foreach (var instance in info.Instances)
                {
                    instances.Add(instance);
                    if (--maxInstances < 0) break;
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

            lock (_lock)
            {
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
                var metaLeaderboardId = instance.GetMetaLeaderboardId(guid);
                entry = instance.GetEntry(metaLeaderboardId, avatarId);
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

                if (_metaLeaderboards.TryGetValue(guid, out var metaLeaderboard))
                    return metaLeaderboard;

                return null;
            }
        }

        public void ScoreUpdateForLeaderboards(Queue<LeaderboardQueue> updateQueue)
        {
            while (updateQueue.TryDequeue(out var queue)) 
            {
                var leaderboard = GetLeaderboard(queue.LeaderboardId);
                leaderboard?.OnScoreUpdate(queue);
            }
        }

        public void UpdateState()
        {
            List<Leaderboard> leaderboards = new();
            var updateTime = Clock.UtcNowPrecise;

            lock (_lock)
            {
                leaderboards.AddRange(_leaderboards.Values);
                leaderboards.AddRange(_metaLeaderboards.Values);
            }

            foreach (var leaderboard in leaderboards)
                leaderboard.UpdateState(updateTime);
        }

        public void Save()
        {
            List<Leaderboard> leaderboards = new();

            lock (_lock)
            {
                leaderboards.AddRange(_leaderboards.Values);
                leaderboards.AddRange(_metaLeaderboards.Values);
            }

            foreach (var leaderboard in leaderboards)                
                leaderboard.ActiveInstance?.SaveEntries(true);
        }
    }
}
