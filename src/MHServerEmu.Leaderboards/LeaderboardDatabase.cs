using System.Diagnostics;
using System.Text.Json;
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
using MHServerEmu.Leaderboards.Scheduling;

namespace MHServerEmu.Leaderboards
{   
    /// <summary>
    /// A singleton that manages runtime leaderboard data.
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
            string databasePath = Path.Combine(LeaderboardsDirectory, config.DatabaseFile);
            bool noTables = false;
            DBManager.Initialize(databasePath, ref noTables);

            // Initialize leaderboards from prototypes if there is no data in the database
            string schedulePath = Path.Combine(LeaderboardsDirectory, config.ScheduleFile);
            if (noTables)
                GenerateTables(schedulePath);

            // Load and cache player names (remove/disable this if the number of accounts gets out of hand)
            if (SQLiteDBManager.Instance.TryGetPlayerNames(_playerNames))
                Logger.Info($"Loaded and cached {_playerNames.Count} player names");
            else
                Logger.Warn($"Failed get player names from SQLiteDBManager");

            // load ActiveLeaderboards
            List<DBLeaderboard> activeLeaderboards = new();
            List<DBLeaderboardInstance> refreshInstances = new();
            LoadSchedule(schedulePath, activeLeaderboards, refreshInstances);
            LoadLeaderboards();

            // send initial leaderboard state to games
            SendLeaderboardsToGames();

            Logger.Info($"Initialized {_leaderboards.Count} leaderboards in {stopwatch.ElapsedMilliseconds} ms");
            return true;
        }

        /// <summary>
        /// Loads leaderboard schedule from JSON.
        /// </summary>
        private bool LoadSchedule(string schedulePath, List<DBLeaderboard> activeLeaderboards, List<DBLeaderboardInstance> refreshInstances)
        {
            string scheduleJson = File.ReadAllText(schedulePath);

            try
            {
                LeaderboardSchedule[] leaderboards = JsonSerializer.Deserialize<LeaderboardSchedule[]>(scheduleJson, LeaderboardSchedule.JsonSerializerOptions);
                DBLeaderboard[] oldDbLeaderboards = DBManager.GetLeaderboards();

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
                return Logger.WarnReturn(false, $"LoadSchedule(): Schedule {schedulePath} deserialization failed - {e.Message}");
            }

            Logger.Info($"Loaded leaderboard schedule from {Path.GetFileName(schedulePath)}");

            DBManager.UpdateLeaderboards(activeLeaderboards);
            DBManager.UpdateOrInsertInstances(refreshInstances);

            return activeLeaderboards.Count > 0;
        }

        /// <summary>
        /// Reloads the schedule from JSON and updates active leaderboards.
        /// </summary>
        public void ReloadSchedule()
        {
            lock (_leaderboardLock)
            {
                var config = ConfigManager.Instance.GetConfig<LeaderboardsConfig>();
                string schedulePath = Path.Combine(LeaderboardsDirectory, config.ScheduleFile);
                
                List<DBLeaderboard> activeLeaderboards = new();
                List<DBLeaderboardInstance> refreshInstances = new();
                if (LoadSchedule(schedulePath, activeLeaderboards, refreshInstances))
                    ReloadActiveLeaderboards(activeLeaderboards, refreshInstances);
            }
        }

        /// <summary>
        /// Initializes <see cref="LeaderboardScheduler"/> and refreshes <see cref="DBLeaderboardInstance">DBLeaderboardInstances</see>
        /// for the provided list of active <see cref="DBLeaderboard">DBLeaderboards</see>.
        /// </summary>
        private void ReloadActiveLeaderboards(List<DBLeaderboard> activeLeaderboards, List<DBLeaderboardInstance> refreshInstances)
        {            
            foreach (DBLeaderboard activeLeaderboard in activeLeaderboards)
            {
                Leaderboard leaderboard = GetLeaderboard((PrototypeGuid)activeLeaderboard.LeaderboardId);
                if (leaderboard == null)
                    continue;                

                leaderboard.Scheduler.Initialize(activeLeaderboard);

                foreach (DBLeaderboardInstance refreshInstance in refreshInstances.Where(inst => inst.LeaderboardId == activeLeaderboard.LeaderboardId))
                    leaderboard.RefreshInstance(refreshInstance);
            }
        }

        /// <summary>
        /// Loads <see cref="Leaderboard"/> data from the database.
        /// </summary>
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

        /// <summary>
        /// Sends the state of all leaderboards to the game instance service.
        /// </summary>
        private void SendLeaderboardsToGames()
        {
            List<GameServiceProtocol.LeaderboardStateChange> instances = new();

            foreach (var leaderboard in _leaderboards.Values)
                leaderboard.GetInstanceInfos(instances);

            foreach (var leaderboard in _metaLeaderboards.Values)
                leaderboard.GetInstanceInfos(instances);

            GameServiceProtocol.LeaderboardStateChangeList message = new(instances);
            ServerManager.Instance.SendMessageToService(ServerType.GameInstanceServer, message);
        }

        /// <summary>
        /// Initializes database data.
        /// </summary>
        private void GenerateTables(string schedulePath)
        {
            List<DBLeaderboard> dbLeaderboards = new();
            List<DBLeaderboardInstance> dbInstances = new();
            List<LeaderboardSchedule> schedule = new();

            DateTime currentYear = new(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long startEvent = Clock.DateTimeToTimestamp(currentYear);
            long endEvent = Clock.DateTimeToTimestamp(currentYear.AddYears(1));

            foreach (PrototypeId dataRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<LeaderboardPrototype>(PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                LeaderboardPrototype proto = GameDatabase.GetPrototype<LeaderboardPrototype>(dataRef);
                if (proto == null || proto.DesignState != DesignWorkflowState.Live || proto.Public == false)
                    continue;

                PrototypeGuid leaderboardId = GameDatabase.GetPrototypeGuid(dataRef);
                ulong instanceId = Leaderboard.GenerateInitialInstanceId(leaderboardId);

                bool isActive = proto.ResetFrequency == LeaderboardResetFrequency.NeverReset;
                if (leaderboardId == (PrototypeGuid)16486420054343424221)   // Anniversary2016
                    isActive = false;

                DBLeaderboard dbLeaderboard = new()
                {
                    LeaderboardId = (long)leaderboardId,
                    PrototypeName = dataRef.GetNameFormatted(),
                    ActiveInstanceId = (long)instanceId,
                    IsActive = isActive,
                    Frequency = (int)LeaderboardResetFrequency.Weekly,
                    Interval = 1,
                    StartEvent = startEvent,
                    EndEvent = endEvent
                };

                LeaderboardSchedule dbSchedule = new LeaderboardSchedule(dbLeaderboard);
                dbSchedule.Scheduler.InitFromProto(proto);

                dbLeaderboards.Add(dbLeaderboard);
                schedule.Add(dbSchedule);

                dbInstances.Add(new DBLeaderboardInstance
                {
                    InstanceId = (long)instanceId,
                    LeaderboardId = (long)leaderboardId,
                    State = isActive ? LeaderboardState.eLBS_Created : LeaderboardState.eLBS_Rewarded,
                    ActivationDate = 0,
                    Visible = isActive
                });

                if (proto.IsMetaLeaderboard)
                {
                    List<DBMetaEntry> dbMetaEntries = new();
                    foreach (MetaLeaderboardEntryPrototype meta in proto.MetaLeaderboardEntries)
                    {
                        PrototypeGuid subLeaderboardId = GameDatabase.GetPrototypeGuid(meta.Leaderboard);
                        ulong subInstanceId = Leaderboard.GenerateInitialInstanceId(subLeaderboardId);
                        dbMetaEntries.Add(new DBMetaEntry
                        {
                            LeaderboardId = (long)leaderboardId,
                            InstanceId = (long)instanceId,
                            SubLeaderboardId = (long)subLeaderboardId,
                            SubInstanceId = (long)subInstanceId
                        });
                    }
                    DBManager.InsertMetaEntries(dbMetaEntries);
                }
            }

            string scheduleJson = JsonSerializer.Serialize(schedule, LeaderboardSchedule.JsonSerializerOptions);
            File.WriteAllText(schedulePath, scheduleJson);

            DBManager.InsertLeaderboards(dbLeaderboards);
            DBManager.UpdateOrInsertInstances(dbInstances);
        }

        /// <summary>
        /// Returns a <see cref="string"/> containing the name of the specified player participant. 
        /// </summary>
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

        /// <summary>
        /// Builds a <see cref="LeaderboardReport"/> for the provided <see cref="NetMessageLeaderboardRequest"/>.
        /// </summary>
        public LeaderboardReport GetLeaderboardReport(NetMessageLeaderboardRequest request)
        {
            PrototypeGuid leaderboardId = 0;
            ulong instanceId = 0;

            LeaderboardReport.Builder report = LeaderboardReport.CreateBuilder()
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

        /// <summary>
        /// Retrieves the <see cref="LeaderboardTableData"/> for the specified leaderboard instance.
        /// </summary>
        private bool GetLeaderboardTableData(PrototypeGuid leaderboardId, ulong instanceId, out LeaderboardTableData tableData)
        {
            tableData = null;
            
            Leaderboard leaderboard = GetLeaderboard(leaderboardId);
            if (leaderboard == null)
                return false;

            LeaderboardInstance instance = leaderboard.GetInstance(instanceId);
            if (instance == null)
                return false;

            tableData = instance.GetTableData();
            return true;
        }

        /// <summary>
        /// Builds <see cref="LeaderboardScoreData"/> for a participant in the specified leaderboard instance.
        /// </summary>
        private bool GetLeaderboardScoreData(PrototypeGuid leaderboardId, ulong instanceId, ulong participantId, ulong avatarId, 
            out LeaderboardScoreData scoreData)
        {
            scoreData = null;

            Leaderboard leaderboard = GetLeaderboard(leaderboardId);
            if (leaderboard == null)
                return false;

            LeaderboardType type = leaderboard.Prototype.Type;

            LeaderboardInstance instance = leaderboard.GetInstance(instanceId);
            if (instance == null)
                return false;

            LeaderboardEntry entry;
            if (type == LeaderboardType.MetaLeaderboard)
            {
                PrototypeGuid subLeaderboardId = instance.GetSubLeaderboardId(participantId);
                entry = instance.GetEntry((ulong)subLeaderboardId, avatarId);
            }
            else
            {
                entry = instance.GetEntry(participantId, avatarId);
            }

            if (entry == null)
                return false;

            LeaderboardScoreData.Builder scoreDataBuilder = LeaderboardScoreData.CreateBuilder()
                .SetLeaderboardId((ulong)leaderboardId);

            if (instanceId != 0)
                scoreDataBuilder.SetInstanceId(instanceId);

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

        /// <summary>
        /// Returns the <see cref="Leaderboard"/> with the specified <see cref="PrototypeGuid"/>.
        /// </summary>
        public Leaderboard GetLeaderboard(PrototypeGuid leaderboardId)
        {
            lock (_leaderboardLock)
            {
                if (_leaderboards.TryGetValue(leaderboardId, out Leaderboard leaderboard))
                    return leaderboard;

                if (_metaLeaderboards.TryGetValue(leaderboardId, out Leaderboard metaLeaderboard))
                    return metaLeaderboard;

                return null;
            }
        }

        /// <summary>
        /// Enqueues a <see cref="GameServiceProtocol.LeaderboardScoreUpdateBatch"/> to be processed during the next update.
        /// </summary>
        public void EnqueueLeaderboardScoreUpdate(in GameServiceProtocol.LeaderboardScoreUpdateBatch leaderboardScoreUpdateBatch)
        {
            // We could probably potentially use a SpinLock here, but I'm not sure if it's worth it
            lock (_scoreUpdateLock)
                _pendingScoreUpdateQueue.Enqueue(leaderboardScoreUpdateBatch);
        }

        /// <summary>
        /// Processes queued <see cref="GameServiceProtocol.LeaderboardScoreUpdateBatch"/> instances.
        /// </summary>
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

        /// <summary>
        /// Adds all <see cref="Leaderboard">Leaderboards</see> to the provided <see cref="List{T}"/>.
        /// </summary>
        public void GetLeaderboards(List<Leaderboard> leaderboards)
        {
            lock (_leaderboardLock)
            {
                leaderboards.AddRange(_leaderboards.Values);
                leaderboards.AddRange(_metaLeaderboards.Values);
            }
        }

        /// <summary>
        /// Updates the state of all <see cref="Leaderboard">Leaderboards</see>.
        /// </summary>
        public void UpdateState()
        {
            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            GetLeaderboards(leaderboards);

            DateTime updateTime = Clock.UtcNowPrecise;
            foreach (Leaderboard leaderboard in leaderboards)
                leaderboard.UpdateState(updateTime);

            ListPool<Leaderboard>.Instance.Return(leaderboards);
        }

        /// <summary>
        /// Saves all <see cref="LeaderboardEntry"/> instances for all active leaderboards to the database.
        /// </summary>
        public void Save()
        {
            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            GetLeaderboards(leaderboards);

            foreach (var leaderboard in leaderboards)                
                leaderboard.ActiveInstance?.SaveEntries(true);

            ListPool<Leaderboard>.Instance.Return(leaderboards);
        }

        /// <summary>
        /// Searches for the <see cref="LeaderboardInstance"/> with the specified instance id.
        /// </summary>
        public LeaderboardInstance FindInstance(ulong instanceId)
        {
            List<Leaderboard> leaderboards = ListPool<Leaderboard>.Instance.Get();
            GetLeaderboards(leaderboards);

            try
            {
                foreach (Leaderboard leaderboard in leaderboards)
                    foreach (LeaderboardInstance instance in leaderboard.Instances)
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
