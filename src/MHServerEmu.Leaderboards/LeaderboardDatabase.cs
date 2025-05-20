using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models.Leaderboards;
using MHServerEmu.DatabaseAccess.SQLite;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Leaderboards
{   
    /// <summary>
    /// A singleton that contains leaderboard infomation.
    /// </summary>
    public class LeaderboardDatabase
    {
        private const ulong UpdateTimeIntervalMS = 30 * 1000;   // 30 seconds

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string LeaderboardsDirectory = Path.Combine(FileHelper.DataDirectory, "Leaderboards");

        private readonly object _leaderboardLock = new();
        private readonly object _scoreUpdateLock = new();

        private readonly Dictionary<PrototypeGuid, Leaderboard> _leaderboards = new();
        private readonly Dictionary<PrototypeGuid, Leaderboard> _metaLeaderboards = new();
        private readonly Dictionary<ulong, string> _playerNames = new();

        private Queue<GameServiceProtocol.LeaderboardScoreUpdateBatch> _pendingScoreUpdateQueue = new();
        private Queue<GameServiceProtocol.LeaderboardScoreUpdateBatch> _scoreUpdateQueue = new();

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

            // Create the leaderboards data directory if needed
            if (Directory.Exists(LeaderboardsDirectory) == false)
                Directory.CreateDirectory(LeaderboardsDirectory);

            // Initialize leaderboard database
            string configPath = Path.Combine(LeaderboardsDirectory, config.FileName);
            bool noTables = false;
            DBManager.Initialize(configPath, ref noTables);

            // Add leaderboards from prototypes
            string jsonConfigPath = Path.Combine(LeaderboardsDirectory, config.JsonConfig);
            if (noTables) GenerateTables(jsonConfigPath);

            // Load and cache player names (remove/disable this if the number of accounts gets out of hand)
            if (SQLiteDBManager.Instance.TryGetPlayerNames(_playerNames))
                Logger.Info($"Loaded and cached {_playerNames.Count} player names");
            else
                Logger.Warn($"Failed get player names from SQLiteDBManager");

            // load ActiveLeaderboards
            List<DBLeaderboard> activeLeaderboards = new();
            List<DBLeaderboardInstance> refreshInstances = new();
            LoadJsonConfig(jsonConfigPath, activeLeaderboards, refreshInstances);
            LoadLeaderboards();

            // send initial leaderboard state to games
            SendLeaderboardsToGames();

            Logger.Info($"Initialized {_leaderboards.Count} leaderboards in {stopwatch.ElapsedMilliseconds} ms");
            return true;
        }

        private bool LoadJsonConfig(string jsonConfigPath, List<DBLeaderboard> activeLeaderboards, List<DBLeaderboardInstance> refreshInstances)
        {
            string leaderboardsJson = File.ReadAllText(jsonConfigPath);

            try
            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() }
                };

                var leaderboards = JsonSerializer.Deserialize<IEnumerable<LeaderboardSchedule>>(leaderboardsJson, options);
                var oldDbLeaderboards = DBManager.GetLeaderboards();

                foreach (LeaderboardSchedule leaderboard in leaderboards)
                {
                    // Skip old
                    var oldLeaderboard = oldDbLeaderboards.FirstOrDefault(lb => lb.LeaderboardId == leaderboard.LeaderboardId);
                    if (oldLeaderboard == null || leaderboard.Compare(oldLeaderboard)) continue;
                    
                    // Add changed leaderboards
                    var activeLeaderboard = leaderboard.ToDBLeaderboard();
                    activeLeaderboard.ActiveInstanceId = oldLeaderboard.ActiveInstanceId;
                    activeLeaderboards.Add(activeLeaderboard);

                    if (oldLeaderboard.IsActive == false)
                    {
                        if (leaderboard.Scheduler.IsActive)
                        {
                            // Add new instance
                            var activationDate = leaderboard.Scheduler.CalcNextUtcActivationDate();

                            refreshInstances.Add(new DBLeaderboardInstance
                            {
                                InstanceId = oldLeaderboard.ActiveInstanceId + 1,
                                LeaderboardId = leaderboard.LeaderboardId,
                                State = LeaderboardState.eLBS_Created,
                                ActivationDate = Clock.DateTimeToTimestamp(activationDate),
                                Visible = true
                            });
                        }
                        else
                        {
                            // Deactivate active instances

                            var instances = DBManager.GetInstances(leaderboard.LeaderboardId, 0);
                            foreach (var instance in instances)
                                refreshInstances.Add(new DBLeaderboardInstance
                                {
                                    InstanceId = instance.InstanceId,
                                    LeaderboardId = instance.LeaderboardId,
                                    State = LeaderboardState.eLBS_Rewarded,
                                    ActivationDate = instance.ActivationDate,
                                    Visible = false
                                });
                        }
                    }
                    else
                    {
                        // old Instance inactive
                        var instances = DBManager.GetInstances(leaderboard.LeaderboardId, 0);
                        foreach (var instance in instances)
                        {
                            if (leaderboard.Scheduler.IsActive)
                            {
                                // Update instance
                                long activationDate = instance.ActivationDate;

                                if (leaderboard.Scheduler.StartEvent != oldLeaderboard.GetStartDateTime())
                                {
                                    // Find next activation time
                                    var nextEvent = leaderboard.Scheduler.CalcNextUtcActivationDate();
                                    activationDate = Clock.DateTimeToTimestamp(nextEvent);
                                }

                                refreshInstances.Add(new DBLeaderboardInstance
                                {
                                    InstanceId = instance.InstanceId,
                                    LeaderboardId = instance.LeaderboardId,
                                    State = instance.State,
                                    ActivationDate = activationDate,
                                    Visible = true
                                });
                            }
                            else
                            {
                                // Deactivate active instance

                                refreshInstances.Add(new DBLeaderboardInstance
                                {
                                    InstanceId = instance.InstanceId,
                                    LeaderboardId = instance.LeaderboardId,
                                    State = LeaderboardState.eLBS_Rewarded,
                                    ActivationDate = instance.ActivationDate,
                                    Visible = false
                                });
                            }
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                return Logger.WarnReturn(false, $"Initialize(): LeaderboardsJson {jsonConfigPath} deserialization failed - {e.Message}");
            }

            DBManager.UpdateLeaderboards(activeLeaderboards);
            DBManager.SetInstances(refreshInstances);

            return activeLeaderboards.Count > 0;
        }

        public void ReloadJsonConfig()
        {
            lock (_leaderboardLock)
            {
                var config = ConfigManager.Instance.GetConfig<LeaderboardsConfig>();
                string jsonConfigPath = Path.Combine(LeaderboardsDirectory, config.JsonConfig);
                
                List<DBLeaderboard> activeLeaderboards = new();
                List<DBLeaderboardInstance> refreshInstances = new();
                if (LoadJsonConfig(jsonConfigPath, activeLeaderboards, refreshInstances))
                    ReloadActiveLeaderboards(activeLeaderboards, refreshInstances);
            }
        }

        private void ReloadActiveLeaderboards(List<DBLeaderboard> activeLeaderboards, List<DBLeaderboardInstance> refreshInstances)
        {            
            foreach (var activeLeaderboard in activeLeaderboards)
            {
                var leaderboard = GetLeaderboard((PrototypeGuid)activeLeaderboard.LeaderboardId);
                if (leaderboard == null) continue;                

                leaderboard.Scheduler.Initialize(activeLeaderboard);

                var leaderboarInstances = refreshInstances.Where(inst => inst.LeaderboardId == activeLeaderboard.LeaderboardId);
                foreach (var refreshInstance in leaderboarInstances)
                    leaderboard.RefreshInstance(refreshInstance);
            }
        }

        private void LoadLeaderboards()
        {
            foreach (var dbLeaderboard in DBManager.GetLeaderboards())
            {
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

        private void SendLeaderboardsToGames()
        {
            List<GameServiceProtocol.LeaderboardStateChange> instances = new();

            foreach (var leaderboard in _leaderboards.Values)
                leaderboard.GetInstancesInfo(instances);

            foreach (var leaderboard in _metaLeaderboards.Values)
                leaderboard.GetInstancesInfo(instances);

            GameServiceProtocol.LeaderboardStateChangeList message = new(instances);
            ServerManager.Instance.SendMessageToService(ServerType.GameInstanceServer, message);
        }

        private void GenerateTables(string jsonConfigPath)
        {
            List<DBLeaderboard> dbLeaderboards = new();
            List<DBLeaderboardInstance> dbInstances = new();
            List<LeaderboardSchedule> jsonLeaderboards = new();

            var currentYear = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var startEvent = Clock.DateTimeToTimestamp(currentYear);
            var endEvent = Clock.DateTimeToTimestamp(currentYear.AddYears(1));

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
                    Frequency = (int)LeaderboardResetFrequency.Weekly,
                    Interval = 1,
                    StartEvent = startEvent,
                    EndEvent = endEvent
                };

                var dbSchedule = new LeaderboardSchedule(dbLeaderboard);
                dbSchedule.Scheduler.InitFromProto(proto);

                dbLeaderboards.Add(dbLeaderboard);
                jsonLeaderboards.Add(dbSchedule);

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

            var options = new JsonSerializerOptions 
            { 
                Converters = { new JsonStringEnumConverter() },
                WriteIndented = true 
            };

            string json = JsonSerializer.Serialize<IEnumerable<LeaderboardSchedule>>(jsonLeaderboards, options);
            File.WriteAllText(jsonConfigPath, json);

            DBManager.SetLeaderboards(dbLeaderboards);
            DBManager.SetInstances(dbInstances);
        }

        public string GetPlayerNameById(ulong participantId)
        {
            lock (_leaderboardLock)
            {
                // Check name cache
                if (_playerNames.TryGetValue(participantId, out string playerName))
                    return playerName;

                // Query the database if not cached
                return SQLiteDBManager.Instance.UpdatePlayerName(_playerNames, participantId);
            }
        }

        public LeaderboardReport GetLeaderboardReport(NetMessageLeaderboardRequest request)
        {
            PrototypeGuid leaderboardId = 0;
            ulong instanceId = 0;

            var report = LeaderboardReport.CreateBuilder()
                .SetNextUpdateTimeIntervalMS(UpdateTimeIntervalMS);

            lock (_leaderboardLock)
            {
                if (request.HasPlayerScoreQuery)
                {
                    LeaderboardPlayerScoreQuery query = request.PlayerScoreQuery;
                    leaderboardId = (PrototypeGuid)query.LeaderboardId;
                    instanceId = query.InstanceId;
                    ulong playerId = query.PlayerId;
                    ulong avatarId = query.HasAvatarId ? query.AvatarId : 0;

                    if (GetLeaderboardScoreData(leaderboardId, instanceId, playerId, avatarId, out LeaderboardScoreData scoreData))
                        report.SetScoreData(scoreData);
                }

                if (request.HasGuildScoreQuery) // Not used
                {
                    LeaderboardGuildScoreQuery query = request.GuildScoreQuery;
                    leaderboardId = (PrototypeGuid)query.LeaderboardId;
                    instanceId = query.InstanceId;
                    ulong guildId = query.GuildId;

                    if (GetLeaderboardScoreData(leaderboardId, instanceId, guildId, 0, out LeaderboardScoreData scoreData))
                        report.SetScoreData(scoreData);
                }

                if (request.HasMetaScoreQuery) // Tournament: Civil War
                {
                    LeaderboardMetaScoreQuery query = request.MetaScoreQuery;
                    leaderboardId = (PrototypeGuid)query.LeaderboardId;
                    instanceId = query.InstanceId;
                    ulong playerId = query.PlayerId;

                    if (GetLeaderboardScoreData(leaderboardId, instanceId, playerId, 0, out LeaderboardScoreData scoreData))
                        report.SetScoreData(scoreData);
                }

                if (request.HasDataQuery)
                {
                    LeaderboardDataQuery query = request.DataQuery;
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

        private bool GetLeaderboardScoreData(PrototypeGuid leaderboardId, ulong instanceId, ulong participantId, ulong avatarId, 
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
                PrototypeGuid metaLeaderboardId = instance.GetMetaLeaderboardId(participantId);
                entry = instance.GetEntry((ulong)metaLeaderboardId, avatarId);
            }
            else
            {
                entry = instance.GetEntry(participantId, avatarId);
            }

            if (entry == null) return false;

            var scoreDataBuilder = LeaderboardScoreData.CreateBuilder()
                .SetLeaderboardId((ulong)leaderboardId);

            if (instanceId != 0) scoreDataBuilder.SetInstanceId(instanceId);

            if (type == LeaderboardType.Player) 
            {
                scoreDataBuilder.SetAvatarId(avatarId);
                scoreDataBuilder.SetPlayerId(participantId);
            }

            if (type == LeaderboardType.Guild)
                scoreDataBuilder.SetGuildId(participantId);

            scoreDataBuilder.SetScore(entry.Score);
            scoreDataBuilder.SetPercentileBucket((uint)instance.GetPercentileBucket(entry));

            scoreData = scoreDataBuilder.Build();

            return true;
        }

        public Leaderboard GetLeaderboard(PrototypeGuid guid)
        {
            lock (_leaderboardLock)
            {
                if (_leaderboards.TryGetValue(guid, out var leaderboard))
                    return leaderboard;

                if (_metaLeaderboards.TryGetValue(guid, out var metaLeaderboard))
                    return metaLeaderboard;

                return null;
            }
        }

        public void EnqueueLeaderboardScoreUpdate(in GameServiceProtocol.LeaderboardScoreUpdateBatch leaderboardScoreUpdateBatch)
        {
            // We could probably potentially use a SpinLock here, but I'm not sure if it's worth it
            lock (_scoreUpdateLock)
                _pendingScoreUpdateQueue.Enqueue(leaderboardScoreUpdateBatch);
        }

        public void ProcessLeaderboardScoreUpdateQueue()
        {
            lock (_scoreUpdateLock)
                (_pendingScoreUpdateQueue, _scoreUpdateQueue) = (_scoreUpdateQueue, _pendingScoreUpdateQueue);

            while (_scoreUpdateQueue.Count > 0)
            {
                GameServiceProtocol.LeaderboardScoreUpdateBatch batch = _scoreUpdateQueue.Dequeue();
                for (int i = 0; i < batch.Count; i++)
                {
                    ref GameServiceProtocol.LeaderboardScoreUpdate update = ref batch[i];
                    Leaderboard leaderboard = GetLeaderboard((PrototypeGuid)update.LeaderboardId);
                    leaderboard?.OnScoreUpdate(ref update);
                }

                batch.Destroy();
            }
        }

        public void GetLeaderboards(List<Leaderboard> leaderboards)
        {
            lock (_leaderboardLock)
            {
                leaderboards.AddRange(_leaderboards.Values);
                leaderboards.AddRange(_metaLeaderboards.Values);
            }
        }

        public void UpdateState()
        {
            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            GetLeaderboards(leaderboards);

            var updateTime = Clock.UtcNowPrecise;
            foreach (var leaderboard in leaderboards)
                leaderboard.UpdateState(updateTime);

            ListPool<Leaderboard>.Instance.Return(leaderboards);
        }

        public void Save()
        {
            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            GetLeaderboards(leaderboards);

            foreach (var leaderboard in leaderboards)                
                leaderboard.ActiveInstance?.SaveEntries(true);

            ListPool<Leaderboard>.Instance.Return(leaderboards);
        }

        public LeaderboardInstance FindInstance(ulong instanceId)
        {
            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            GetLeaderboards(leaderboards);

            try
            {
                foreach (var leaderboard in leaderboards)
                    foreach (var instance in leaderboard.Instances)
                        if (instance.InstanceId == instanceId)
                            return instance;

                return null;
            }
            finally
            {
                ListPool<Leaderboard>.Instance.Return(leaderboards);
            }
        }
    }
}
