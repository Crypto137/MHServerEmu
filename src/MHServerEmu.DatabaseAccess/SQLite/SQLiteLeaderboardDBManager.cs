using Dapper;
using System.Data.SQLite;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;

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

        public bool Initialize(string configPath)
        {
            _dbFilePath = configPath; 
            _connectionString = $"Data Source={_dbFilePath}";

            if (File.Exists(_dbFilePath) == false)
            {
                // Create a new database file if it does not exist
                if (InitializeDatabaseFile() == false)
                    return false;
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

            CreateLeaderboards();

            return true;
        }

        private void CreateLeaderboards()
        {
            // TODO Add leaderboards
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

        public DBLeaderboard[] GetLeaderboardList()
        {
            using SQLiteConnection connection = GetConnection();
            return connection.Query<DBLeaderboard>("SELECT * FROM Leaderboards").ToArray();
        }

        public List<DBLeaderboardInstance> GetInstanceList(long leaderboardId, int maxArchivedInstances)
        {
            using SQLiteConnection connection = GetConnection();

            List<DBLeaderboardInstance> instanceList = new(
                connection.Query<DBLeaderboardInstance>(@"
                    SELECT * FROM Instances 
                    WHERE LeaderboardId = @LeaderboardId AND State <= 1 
                    ORDER BY InstanceId DESC",
                    new { LeaderboardId = leaderboardId }));

            if (maxArchivedInstances > 0)
            {
                instanceList.AddRange(
                    connection.Query<DBLeaderboardInstance>(@"
                        SELECT * FROM Instances 
                        WHERE LeaderboardId = @LeaderboardId AND State > 1 
                        ORDER BY InstanceId DESC LIMIT @MaxArchivedInstances",
                        new { LeaderboardId = leaderboardId, MaxArchivedInstances = maxArchivedInstances}));
            }

            return instanceList;
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

        public void SetEntries(List<DBLeaderboardEntry> dbEntries)
        {
            if (dbEntries.Count == 0) return;
            using var connection = GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            var updateCommand = new SQLiteCommand(connection)
            {
                CommandText = @"
                UPDATE Entries
                SET Score = @Score, HighScore = @HighScore, RuleStates = @RuleStates
                WHERE InstanceId = @InstanceId AND GameId = @GameId"
            };

            var insertCommand = new SQLiteCommand(connection)
            {
                CommandText = @"
                INSERT INTO Entries (InstanceId, GameId, Score, HighScore, RuleStates)
                VALUES (@InstanceId, @GameId, @Score, @HighScore, @RuleStates)"
            };

            foreach (var entry in dbEntries)
            {
                entry.SetParameters(updateCommand);
                if (updateCommand.ExecuteNonQuery() == 0)
                {
                    entry.SetParameters(insertCommand);
                    insertCommand.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }

        public void SetMetaInstances(long leaderboardId, long instanceId, List<DBMetaInstance> instances)
        {
            using var connection = GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();
            using var command = new SQLiteCommand(connection)
            {
                CommandText = @"
                INSERT INTO MetaInstances (LeaderboardId, InstanceId, MetaLeaderboardId, MetaInstanceId)
                VALUES (@LeaderboardId, @InstanceId, @MetaLeaderboardId, @MetaInstanceId)"
            };

            foreach (var instance in instances)
            {
                command.Parameters.Clear();
                command.Parameters.AddWithValue("@LeaderboardId", leaderboardId);
                command.Parameters.AddWithValue("@InstanceId", instanceId);
                command.Parameters.AddWithValue("@MetaLeaderboardId", instance.MetaLeaderboardId);
                command.Parameters.AddWithValue("@MetaInstanceId", instance.MetaInstanceId);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public List<DBMetaInstance> GetMetaInstances(long leaderboardId, long instanceId)
        {
            var instances = new List<DBMetaInstance>();

            using var connection = GetConnection();
            connection.Open();

            using var command = new SQLiteCommand(connection)
            {
                CommandText = @"
                SELECT MetaLeaderboardId, MetaInstanceId
                FROM MetaInstances
                WHERE LeaderboardId = @LeaderboardId AND InstanceId = @InstanceId"
            };

            command.Parameters.AddWithValue("@LeaderboardId", leaderboardId);
            command.Parameters.AddWithValue("@InstanceId", instanceId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var instance = new DBMetaInstance
                {
                    MetaLeaderboardId = reader.GetInt64(0),
                    MetaInstanceId = reader.GetInt64(1)
                };

                instances.Add(instance);
            }

            return instances;
        }
    }
}
