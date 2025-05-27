using Dapper;
using System.Data.SQLite;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models.Leaderboards;

namespace MHServerEmu.DatabaseAccess.SQLite
{
    public class SQLiteLeaderboardDBManager
    {
        private const int CurrentSchemaVersion = 1;         // Increment this when making changes to the database schema

        private static readonly Logger Logger = LogManager.CreateLogger();
        public static SQLiteLeaderboardDBManager Instance { get; } = new();

        private string _dbFilePath;
        private string _connectionString;

        private SQLiteLeaderboardDBManager() { }

        public bool Initialize(string configPath, ref bool noTables)
        {
            _dbFilePath = configPath; 
            _connectionString = $"Data Source={_dbFilePath}";

            if (File.Exists(_dbFilePath) == false)
            {
                // Create a new database file if it does not exist
                if (InitializeDatabaseFile() == false)
                    return false;

                noTables = true;
            }

            return true;
        }

        /// <summary>
        /// Initializes a new empty database file using the current schema.
        /// </summary>
        private bool InitializeDatabaseFile()
        {
            string initializationScript = SQLiteScripts.GetLeaderboardsScript();
            if (initializationScript == string.Empty)
                return Logger.ErrorReturn(false, "InitializeDatabaseFile(): Failed to get database initialization script");

            SQLiteConnection.CreateFile(_dbFilePath);
            using SQLiteConnection connection = GetConnection();
            connection.Execute(initializationScript);

            Logger.Info($"Initialized a new database file at {Path.GetRelativePath(FileHelper.ServerRoot, _dbFilePath)} using schema version {CurrentSchemaVersion}");            

            return true;
        }

        /// <summary>
        /// Creates and opens a new <see cref="SQLiteConnection"/>.
        /// </summary>
        private SQLiteConnection GetConnection()
        {
            SQLiteConnection connection = new(_connectionString);
            connection.Open();
            return connection;
        }

        public void InsertLeaderboards(List<DBLeaderboard> dbLeaderboards)
        {
            if (dbLeaderboards.Count == 0) return;
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            connection.Execute(@"
                INSERT INTO Leaderboards 
                (LeaderboardId, PrototypeName, ActiveInstanceId, IsEnabled, StartTime, MaxResetCount)
                VALUES 
                (@LeaderboardId, @PrototypeName, @ActiveInstanceId, @IsEnabled, @StartTime, @MaxResetCount)",
                dbLeaderboards, transaction);

            transaction.Commit();
        }

        public void UpdateLeaderboards(List<DBLeaderboard> dbLeaderboards)
        {
            if (dbLeaderboards.Count == 0) return;
            using SQLiteConnection connection = GetConnection();
            connection.Execute(@"
                UPDATE Leaderboards 
                SET 
                    ActiveInstanceId = @ActiveInstanceId,
                    IsEnabled = @IsEnabled,
                    StartTime = @StartTime,
                    MaxResetCount = @MaxResetCount
                WHERE LeaderboardId = @LeaderboardId",
                dbLeaderboards);
        }

        public DBLeaderboard[] GetLeaderboards()
        {
            using SQLiteConnection connection = GetConnection();
            return connection.Query<DBLeaderboard>("SELECT * FROM Leaderboards").ToArray();
        }

        public bool UpdateActiveInstanceState(long leaderboardId, long activeInstanceId, int state)
        {
            using SQLiteConnection connection = GetConnection();

            int rows = connection.Execute(@"
                UPDATE Leaderboards SET ActiveInstanceId = @ActiveInstanceId 
                WHERE LeaderboardId = @LeaderboardId",
                new { LeaderboardId = leaderboardId, ActiveInstanceId = activeInstanceId });

            rows += DoUpdateInstanceState(connection, activeInstanceId, state);

            return rows == 2;
        }

        public List<DBLeaderboardInstance> GetInstances(long leaderboardId, int maxArchivedInstances)
        {
            using SQLiteConnection connection = GetConnection();

            // Get active instances
            List<DBLeaderboardInstance> instanceList = new(
                connection.Query<DBLeaderboardInstance>(@"
                    SELECT * FROM Instances 
                    WHERE LeaderboardId = @LeaderboardId AND State <= 1 
                    ORDER BY InstanceId DESC",
                    new { LeaderboardId = leaderboardId }));

            // Update visibility of archived instances
            UpdateArchivedInstanceVisibility(connection, leaderboardId, maxArchivedInstances);

            // Get visible archived instances
            IEnumerable<DBLeaderboardInstance> archivedInstances = connection.Query<DBLeaderboardInstance>(@"
                SELECT * FROM Instances 
                WHERE LeaderboardId = @LeaderboardId AND State > 1 AND Visible = 1
                ORDER BY InstanceId DESC
                LIMIT @MaxArchivedInstances",
                new { LeaderboardId = leaderboardId, MaxArchivedInstances = maxArchivedInstances });

            instanceList.AddRange(archivedInstances);

            return instanceList;
        }

        public DBLeaderboardInstance GetInstance(long leaderboardId, long instanceId)
        {
            using SQLiteConnection connection = GetConnection();
            return connection.QueryFirstOrDefault<DBLeaderboardInstance>(@"
                SELECT * FROM Instances
                WHERE LeaderboardId = @LeaderboardId AND InstanceId = @InstanceId",
                new { LeaderboardId = leaderboardId, InstanceId = instanceId });
        }

        /// <summary>
        /// Updates archived leaderboard visiblity and retrieves 
        /// </summary>
        private void UpdateArchivedInstanceVisibility(SQLiteConnection connection, long leaderboardId, int maxArchivedInstances)
        {
            // Make archived instances that had no participants invisible
            connection.Execute(@"
                UPDATE Instances 
                SET Visible = 0 
                WHERE LeaderboardId = @LeaderboardId AND State > 1 AND Visible = 1
                  AND NOT EXISTS (
                      SELECT 1 FROM Entries 
                      WHERE Entries.InstanceId = Instances.InstanceId
                  )",
                new { LeaderboardId = leaderboardId });

            // Get the most recent archived instances
            List<long> excludedInstanceIds = connection.Query<long>(@"
                SELECT InstanceId 
                FROM Instances 
                WHERE LeaderboardId = @LeaderboardId AND State > 1 AND Visible = 1
                ORDER BY InstanceId DESC
                LIMIT @MaxArchivedInstances",
                new { LeaderboardId = leaderboardId, MaxArchivedInstances = maxArchivedInstances }).ToList();

            if (excludedInstanceIds.Count == 0)
                excludedInstanceIds.Add(0);

            // Make non-recent archived instances that have no rewards invisible
            connection.Execute(@"
                UPDATE Instances 
                SET Visible = 0 
                WHERE LeaderboardId = @LeaderboardId AND State > 1 AND Visible = 1
                  AND InstanceId NOT IN @ExcludedInstanceIds
                  AND NOT EXISTS (
                      SELECT 1 FROM Rewards 
                      WHERE Rewards.LeaderboardId = Instances.LeaderboardId 
                        AND Rewards.InstanceId = Instances.InstanceId 
                        AND Rewards.RewardedDate IS NOT NULL
                  )",
                new { LeaderboardId = leaderboardId, ExcludedInstanceIds = excludedInstanceIds });
        }

        public void UpdateOrInsertInstances(List<DBLeaderboardInstance> dbInstances)
        {
            if (dbInstances.Count == 0)
                return;

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            const string updateCommand = @"
                UPDATE Instances
                SET State = @State, ActivationDate = @ActivationDate, Visible = @Visible
                WHERE InstanceId = @InstanceId";

            const string insertCommand = @"
                INSERT INTO Instances (InstanceId, LeaderboardId, State, ActivationDate, Visible) 
                VALUES (@InstanceId, @LeaderboardId, @State, @ActivationDate, @Visible)";

            foreach (var instance in dbInstances)
                if (connection.Execute(updateCommand, instance, transaction) == 0)
                    connection.Execute(insertCommand, instance, transaction);

            transaction.Commit();
        }

        public void InsertInstance(DBLeaderboardInstance dbInstance)
        {
            using SQLiteConnection connection = GetConnection();

            connection.Execute(@"
                INSERT INTO Instances (InstanceId, LeaderboardId, State, ActivationDate, Visible) 
                VALUES (@InstanceId, @LeaderboardId, @State, @ActivationDate, @Visible);", dbInstance);
        }

        public void UpdateInstanceState(long instanceId, int state)
        {
            using SQLiteConnection connection = GetConnection();
            DoUpdateInstanceState(connection, instanceId, state);
        }

        private int DoUpdateInstanceState(SQLiteConnection connection, long instanceId, int state)
        {
            return connection.Execute(@"
                UPDATE Instances SET State = @State 
                WHERE InstanceId = @InstanceId",
                new { InstanceId = instanceId, State = state });
        }

        public void UpdateInstanceActivationDate(DBLeaderboardInstance dbInstance)
        {
            using SQLiteConnection connection = GetConnection();
            connection.Execute(@"
                UPDATE Instances SET ActivationDate = @ActivationDate 
                WHERE InstanceId = @InstanceId", dbInstance);
        }

        public List<DBLeaderboardEntry> GetEntries(long instanceId, bool ascending)
        {
            using SQLiteConnection connection = GetConnection();

            string order = ascending ? "ASC" : "DESC";

            return connection.Query<DBLeaderboardEntry>(@"
                SELECT * FROM Entries WHERE InstanceId = @InstanceId 
                ORDER BY HighScore " + order, 
                new { InstanceId = instanceId }).ToList();
        }

        public void UpdateOrInsertEntries(List<DBLeaderboardEntry> dbEntries)
        {
            if (dbEntries.Count == 0)
                return;

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            const string updateCommand = @"
                UPDATE Entries
                SET Score = @Score, HighScore = @HighScore, RuleStates = @RuleStates
                WHERE InstanceId = @InstanceId AND ParticipantId = @ParticipantId";

            const string insertCommand = @"
                INSERT INTO Entries (InstanceId, ParticipantId, Score, HighScore, RuleStates)
                VALUES (@InstanceId, @ParticipantId, @Score, @HighScore, @RuleStates)";

            foreach (var entry in dbEntries)
                if (connection.Execute(updateCommand, entry, transaction) == 0)
                    connection.Execute(insertCommand, entry, transaction);

            transaction.Commit();
        }

        public long GetSubInstanceId(long leaderboardId, long instanceId, long subLeaderboardId)
        {
            using var connection = GetConnection();
            return connection.QuerySingleOrDefault<long>(@"
                SELECT SubInstanceId FROM MetaEntries
                WHERE LeaderboardId = @LeaderboardId AND InstanceId = @InstanceId 
                AND SubLeaderboardId = @SubLeaderboardId",
                new { LeaderboardId = leaderboardId, InstanceId = instanceId, SubLeaderboardId = subLeaderboardId });
        }

        public void InsertMetaEntries(List<DBMetaEntry> instances)
        {
            if (instances.Count == 0)
                return;

            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            const string insertCommand = @"
                INSERT INTO MetaEntries (LeaderboardId, InstanceId, SubLeaderboardId, SubInstanceId)
                VALUES (@LeaderboardId, @InstanceId, @SubLeaderboardId, @SubInstanceId)";

            connection.Execute(insertCommand, instances, transaction);
            transaction.Commit();
        }

        public List<DBMetaEntry> GetMetaEntries(long leaderboardId, long instanceId)
        {
            using var connection = GetConnection();
            return connection.Query<DBMetaEntry>(@"
                SELECT * FROM MetaEntries
                WHERE LeaderboardId = @LeaderboardId AND InstanceId = @InstanceId", 
                new { LeaderboardId = leaderboardId, InstanceId = instanceId }).ToList();
        }

        public void InsertRewards(List<DBRewardEntry> dbRewards)
        {
            if (dbRewards.Count == 0) return;
            using var connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            connection.Execute(@"
                INSERT INTO Rewards (LeaderboardId, InstanceId, ParticipantId, RewardId, Rank, CreationDate)
                VALUES (@LeaderboardId, @InstanceId, @ParticipantId, @RewardId, @Rank, @CreationDate)", dbRewards, transaction);

            transaction.Commit();
        }

        public List<DBRewardEntry> GetRewards(long participantId)
        {
            using SQLiteConnection connection = GetConnection();

            return connection.Query<DBRewardEntry>(@"
                SELECT * FROM Rewards WHERE ParticipantId = @ParticipantId AND RewardedDate IS NULL",
                new { ParticipantId = (long)participantId }).ToList();
        }

        public void UpdateReward(DBRewardEntry reward)
        {
            using SQLiteConnection connection = GetConnection();
            using var transaction = connection.BeginTransaction();

            connection.Execute(@"
                UPDATE Rewards SET RewardedDate = @RewardedDate 
                WHERE LeaderboardId = @LeaderboardId AND InstanceId = @InstanceId AND ParticipantId = @ParticipantId", reward, transaction);

            transaction.Commit();
        }
    }
}
