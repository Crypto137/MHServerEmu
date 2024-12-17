using Dapper;
using System.Data.SQLite;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;

namespace MHServerEmu.DatabaseAccess.SQLite
{
    public class SQLiteLDBManager
    {
        private const int CurrentSchemaVersion = 1;         // Increment this when making changes to the database schema

        private static readonly Logger Logger = LogManager.CreateLogger();
        public static SQLiteLDBManager Instance { get; } = new();

        private string _dbFilePath;
        private string _connectionString;

        private SQLiteLDBManager() { }

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
    }
}
