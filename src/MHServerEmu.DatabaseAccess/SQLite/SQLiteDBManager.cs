using Dapper;
using System.Data.SQLite;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess.SQLite
{
    /// <summary>
    /// Provides functionality for storing <see cref="DBAccount"/> instances in a SQLite database using the <see cref="IDBManager"/> interface.
    /// </summary>
    public class SQLiteDBManager : IDBManager
    {
        private const int CurrentSchemaVersion = 3;         // Increment this when making changes to the database schema
        private const int NumTestAccounts = 5;              // Number of test accounts to create for new database files
        private const int NumPlayerDataWriteAttempts = 3;   // Number of write attempts to do when saving player data

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly object _writeLock = new();

        private string _dbFilePath;
        private string _connectionString;

        private int _maxBackupNumber;
        private TimeSpan _backupInterval;
        private TimeSpan _lastBackupTime;

        public static SQLiteDBManager Instance { get; } = new();

        private SQLiteDBManager() { }

        public bool Initialize()
        {
            var config = ConfigManager.Instance.GetConfig<SQLiteDBManagerConfig>();

            _dbFilePath = Path.Combine(FileHelper.DataDirectory, config.FileName);
            _connectionString = $"Data Source={_dbFilePath}";

            if (File.Exists(_dbFilePath) == false)
            {
                // Create a new database file if it does not exist
                if (InitializeDatabaseFile() == false)
                    return false;
            }
            else
            {
                // Migrate existing database if needed
                if (MigrateDatabaseFileToCurrentSchema() == false)
                    return false;
            }

            _maxBackupNumber = config.MaxBackupNumber;
            _backupInterval = TimeSpan.FromMinutes(config.BackupIntervalMinutes);
            _lastBackupTime = Clock.GameTime;
            
            Logger.Info($"Using database file {FileHelper.GetRelativePath(_dbFilePath)}");
            return true;
        }

        public bool TryQueryAccountByEmail(string email, out DBAccount account)
        {
            using SQLiteConnection connection = GetConnection();
            var accounts = connection.Query<DBAccount>("SELECT * FROM Account WHERE Email = @Email", new { Email = email });

            // Associated player data is loaded separately
            account = accounts.FirstOrDefault();
            return account != null;
        }

        public bool TryGetPlayerName(ulong id, out string playerName)
        {
            using SQLiteConnection connection = GetConnection();
            
            playerName = connection.QueryFirstOrDefault<string>("SELECT PlayerName FROM Account WHERE Id = @Id", new { Id = (long)id });

            return string.IsNullOrWhiteSpace(playerName) == false;
        }

        public bool GetPlayerNames(Dictionary<ulong, string> playerNames)
        {
            using SQLiteConnection connection = GetConnection();
            
            var accounts = connection.Query<DBAccount>("SELECT Id, PlayerName FROM Account");

            foreach (DBAccount account in accounts)
                playerNames[(ulong)account.Id] = account.PlayerName;

            return playerNames.Count > 0;
        }

        public bool QueryIsPlayerNameTaken(string playerName)
        {
            using SQLiteConnection connection = GetConnection();

            // This check is case insensitive (COLLATE NOCASE)
            var results = connection.Query<string>("SELECT PlayerName FROM Account WHERE PlayerName = @PlayerName COLLATE NOCASE", new { PlayerName = playerName });
            return results.Any();
        }

        public bool InsertAccount(DBAccount account)
        {
            lock (_writeLock)
            {
                using SQLiteConnection connection = GetConnection();

                try
                {
                    connection.Execute(@"INSERT INTO Account (Id, Email, PlayerName, PasswordHash, Salt, UserLevel, Flags)
                        VALUES (@Id, @Email, @PlayerName, @PasswordHash, @Salt, @UserLevel, @Flags)", account);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.ErrorException(e, nameof(InsertAccount));
                    return false;
                }
            }
        }

        public bool UpdateAccount(DBAccount account)
        {
            lock (_writeLock)
            {
                using SQLiteConnection connection = GetConnection();

                try
                {
                    connection.Execute(@"UPDATE Account SET Email=@Email, PlayerName=@PlayerName, PasswordHash=@PasswordHash, Salt=@Salt,
                        UserLevel=@UserLevel, Flags=@Flags WHERE Id=@Id", account);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.ErrorException(e, nameof(UpdateAccount));
                    return false;
                }
            }
        }

        public bool LoadPlayerData(DBAccount account)
        {
            // Clear existing data
            account.Player = null;
            account.ClearEntities();

            // Load fresh data
            using SQLiteConnection connection = GetConnection();

            var @params = new { DbGuid = account.Id };

            var players = connection.Query<DBPlayer>("SELECT * FROM Player WHERE DbGuid = @DbGuid", @params);
            account.Player = players.FirstOrDefault();

            if (account.Player == null)
            {
                account.Player = new(account.Id);
                Logger.Info($"Initialized player data for account 0x{account.Id:X}");
            }

            // Load inventory entities
            account.Avatars.AddRange(LoadEntitiesFromTable(connection, "Avatar", account.Id));
            account.TeamUps.AddRange(LoadEntitiesFromTable(connection, "TeamUp", account.Id));
            account.Items.AddRange(LoadEntitiesFromTable(connection, "Item", account.Id));

            foreach (DBEntity avatar in account.Avatars)
            {
                account.Items.AddRange(LoadEntitiesFromTable(connection, "Item", avatar.DbGuid));
                account.ControlledEntities.AddRange(LoadEntitiesFromTable(connection, "ControlledEntity", avatar.DbGuid));
            }

            foreach (DBEntity teamUp in account.TeamUps)
            {
                account.Items.AddRange(LoadEntitiesFromTable(connection, "Item", teamUp.DbGuid));
            }

            return true;
        }

        public bool SavePlayerData(DBAccount account)
        {
            for (int i = 0; i < NumPlayerDataWriteAttempts; i++)
            {
                if (DoSavePlayerData(account))
                    return Logger.InfoReturn(true, $"Successfully written player data for account [{account}]");

                // Maybe we should add a delay here
            }

            return Logger.WarnReturn(false, $"SavePlayerData(): Failed to write player data for account [{account}]");
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

        /// <summary>
        /// Initializes a new empty database file using the current schema.
        /// </summary>
        private bool InitializeDatabaseFile()
        {
            string initializationScript = SQLiteScripts.GetInitializationScript();
            if (initializationScript == string.Empty)
                return Logger.ErrorReturn(false, "InitializeDatabaseFile(): Failed to get database initialization script");

            SQLiteConnection.CreateFile(_dbFilePath);
            using SQLiteConnection connection = GetConnection();
            connection.Execute(initializationScript);

            Logger.Info($"Initialized a new database file at {Path.GetRelativePath(FileHelper.ServerRoot, _dbFilePath)} using schema version {CurrentSchemaVersion}");

            CreateTestAccounts(NumTestAccounts);

            return true;
        }

        /// <summary>
        /// Creates the specified number of test accounts.
        /// </summary>
        private void CreateTestAccounts(int numAccounts)
        {
            for (int i = 0; i < numAccounts; i++)
            {
                string email = $"test{i + 1}@test.com";
                string playerName = $"Player{i + 1}";
                string password = "123";

                DBAccount account = new(email, playerName, password);
                InsertAccount(account);
                Logger.Info($"Created test account {account}");
            }
        }

        /// <summary>
        /// Migrates an existing database file to the current schema if needed.
        /// </summary>
        private bool MigrateDatabaseFileToCurrentSchema()
        {
            using SQLiteConnection connection = GetConnection();

            int schemaVersion = GetSchemaVersion(connection);
            if (schemaVersion > CurrentSchemaVersion)
                return Logger.ErrorReturn(false, $"Initialize(): Existing database file uses unsupported schema version {schemaVersion} (current = {CurrentSchemaVersion})");

            Logger.Info($"Found existing database file with schema version {schemaVersion} (current = {CurrentSchemaVersion})");

            if (schemaVersion == CurrentSchemaVersion)
                return true;

            // Create a backup to fall back to if something goes wrong
            string backupDbPath = $"{_dbFilePath}.v{schemaVersion}";
            File.Copy(_dbFilePath, backupDbPath);

            bool success = true;

            while (schemaVersion < CurrentSchemaVersion)
            {
                Logger.Info($"Migrating version {schemaVersion} => {schemaVersion + 1}...");

                string migrationScript = SQLiteScripts.GetMigrationScript(schemaVersion);
                if (migrationScript == string.Empty)
                {
                    Logger.Error($"MigrateDatabaseFileToCurrentSchema(): Failed to get database migration script for version {schemaVersion}");
                    success = false;
                    break;
                }

                connection.Execute(migrationScript);
                SetSchemaVersion(connection, ++schemaVersion);
            }

            success &= GetSchemaVersion(connection) == CurrentSchemaVersion;

            if (success == false)
            {
                // Restore backup
                File.Delete(_dbFilePath);
                File.Move(backupDbPath, _dbFilePath);
                return Logger.ErrorReturn(false, "MigrateDatabaseFileToCurrentSchema(): Migration failed, backup restored");
            }
            else
            {
                // Clean up backup
                File.Delete(backupDbPath);
            }

            Logger.Info($"Successfully migrated to schema version {CurrentSchemaVersion}");
            return true;
        }

        private bool DoSavePlayerData(DBAccount account)
        {
            // Lock to prevent corruption if we are doing a backup (TODO: Make this better)
            lock (_writeLock)
            {
                using SQLiteConnection connection = GetConnection();

                // Use a transaction to make sure all data is saved
                using SQLiteTransaction transaction = connection.BeginTransaction();

                try
                {
                    // Update player entity
                    if (account.Player != null)
                    {
                        connection.Execute(@$"INSERT OR IGNORE INTO Player (DbGuid) VALUES (@DbGuid)", account.Player, transaction);
                        connection.Execute(@$"UPDATE Player SET ArchiveData=@ArchiveData, StartTarget=@StartTarget,
                                            StartTargetRegionOverride=@StartTargetRegionOverride, AOIVolume=@AOIVolume,
                                            GazillioniteBalance=@GazillioniteBalance WHERE DbGuid = @DbGuid",
                                            account.Player, transaction);
                    }
                    else
                    {
                        Logger.Warn($"DoSavePlayerData(): Attempted to save null player entity data for account {account}");
                    }

                    // Update inventory entities
                    UpdateEntityTable(connection, transaction, "Avatar", account.Id, account.Avatars);
                    UpdateEntityTable(connection, transaction, "TeamUp", account.Id, account.TeamUps);
                    UpdateEntityTable(connection, transaction, "Item", account.Id, account.Items);

                    foreach (DBEntity avatar in account.Avatars)
                    {
                        UpdateEntityTable(connection, transaction, "Item", avatar.DbGuid, account.Items);
                        UpdateEntityTable(connection, transaction, "ControlledEntity", avatar.DbGuid, account.ControlledEntities);
                    }

                    foreach (DBEntity teamUp in account.TeamUps)
                    {
                        UpdateEntityTable(connection, transaction, "Item", teamUp.DbGuid, account.Items);
                    }

                    transaction.Commit();

                    TryCreateBackup();

                    return true;
                }
                catch (Exception e)
                {
                    Logger.Warn($"DoSavePlayerData(): SQLite error for account [{account}]: {e.Message}");
                    transaction.Rollback();
                    return false;
                }
            }
        }

        /// <summary>
        /// Creates a backup of the database file if enough time has passed since the last one.
        /// </summary>
        private void TryCreateBackup()
        {
            // TODO: Use SQLite backup functionality for this
            TimeSpan now = Clock.GameTime;

            if ((now - _lastBackupTime) < _backupInterval)
                return;

            if (FileHelper.CreateFileBackup(_dbFilePath, _maxBackupNumber))
                Logger.Info("Created database file backup");

            _lastBackupTime = now;
        }

        /// <summary>
        /// Returns the user_version value of the current database file.
        /// </summary>
        private static int GetSchemaVersion(SQLiteConnection connection)
        {
            var queryResult = connection.Query<int>("PRAGMA user_version");
            if (queryResult.Any())
                return queryResult.First();

            return Logger.WarnReturn(-1, "GetSchemaVersion(): Failed to query user_version from the DB");
        }

        /// <summary>
        /// Sets the user_version value of the current database file.
        /// </summary>
        private static void SetSchemaVersion(SQLiteConnection connection, int version)
        {
            connection.Execute($"PRAGMA user_version = {version}");
        }

        /// <summary>
        /// Loads <see cref="DBEntity"/> instances belonging to the specified container from the specified table.
        /// </summary>
        private static IEnumerable<DBEntity> LoadEntitiesFromTable(SQLiteConnection connection, string tableName, long containerDbGuid)
        {
            var @params = new { ContainerDbGuid = containerDbGuid };
            return connection.Query<DBEntity>($"SELECT * FROM {tableName} WHERE ContainerDbGuid = @ContainerDbGuid", @params);
        }

        /// <summary>
        /// Updates <see cref="DBEntity"/> instances belonging to the specified container in the specified table using the provided <see cref="DBEntityCollection"/>.
        /// </summary>
        private static void UpdateEntityTable(SQLiteConnection connection, SQLiteTransaction transaction, string tableName,
            long containerDbGuid, DBEntityCollection dbEntityCollection)
        {
            var @params = new { ContainerDbGuid = containerDbGuid };

            // Delete items that no longer belong to this account
            var storedEntities = connection.Query<long>($"SELECT DbGuid FROM {tableName} WHERE ContainerDbGuid = @ContainerDbGuid", @params);
            var entitiesToDelete = storedEntities.Except(dbEntityCollection.Guids);
            connection.Execute($"DELETE FROM {tableName} WHERE DbGuid IN ({string.Join(',', entitiesToDelete)})");

            // Insert and update
            IReadOnlyList<DBEntity> entries = dbEntityCollection.GetEntriesForContainer(containerDbGuid);

            connection.Execute(@$"INSERT OR IGNORE INTO {tableName} (DbGuid) VALUES (@DbGuid)", entries, transaction);
            connection.Execute(@$"UPDATE {tableName} SET ContainerDbGuid=@ContainerDbGuid, InventoryProtoGuid=@InventoryProtoGuid,
                                Slot=@Slot, EntityProtoGuid=@EntityProtoGuid, ArchiveData=@ArchiveData WHERE DbGuid=@DbGuid",
                                entries, transaction);
        }
    }
}
