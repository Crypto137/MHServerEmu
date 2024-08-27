using Dapper;
using System.Data.SQLite;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess
{
    public class SQLiteDBManager : IDBManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private string _connectionString;

        public static SQLiteDBManager Instance { get; } = new();

        private SQLiteDBManager() { }

        public bool Initialize()
        {
            string dbPath = Path.Combine(FileHelper.DataDirectory, "Account.db");
            if (File.Exists(dbPath) == false) return Logger.FatalReturn(false, $"Initialize(): {dbPath} not found");

            _connectionString = $"Data Source={dbPath}";
            Logger.Info("Established database connection");
            return true;
        }

        public bool TryQueryAccountByEmail(string email, out DBAccount account)
        {
            using SQLiteConnection connection = new(_connectionString);
            var accounts = connection.Query<DBAccount>("SELECT * FROM Account WHERE Email = @Email", new { Email = email });

            if (accounts.Any() == false)
            {
                account = null;
                return false;
            }

            account = accounts.First();
            LoadAccountData(connection, account);
            return true;
        }

        public bool QueryIsPlayerNameTaken(string playerName)
        {
            using SQLiteConnection connection = new(_connectionString);
            // This check is case insensitive (COLLATE NOCASE)
            var results = connection.Query<string>("SELECT PlayerName FROM Account WHERE PlayerName = @PlayerName COLLATE NOCASE", new { PlayerName = playerName });
            return results.Any();
        }

        public bool InsertAccount(DBAccount account)
        {
            using SQLiteConnection connection = new(_connectionString);
            connection.Open();

            // Use a transaction to make sure all data is saved
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    connection.Execute(@"INSERT INTO Account (Id, Email, PlayerName, PasswordHash, Salt, UserLevel, IsBanned, IsArchived, IsPasswordExpired)
                            VALUES (@Id, @Email, @PlayerName, @PasswordHash, @Salt, @UserLevel, @IsBanned, @IsArchived, @IsPasswordExpired)", account, transaction);

                    connection.Execute(@"INSERT INTO Player (AccountId, RawRegion, RawAvatar, RawWaypoint, AOIVolume)
                            VALUES (@AccountId, @RawRegion, @RawAvatar, @RawWaypoint, @AOIVolume)", account.Player, transaction);

                    transaction.Commit();
                    return true;
                }
                catch (Exception e)
                {
                    Logger.ErrorException(e, nameof(InsertAccount));
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public bool UpdateAccount(DBAccount account)
        {
            using SQLiteConnection connection = new(_connectionString);

            try
            {
                connection.Execute(@"UPDATE Account SET Email=@Email, PlayerName=@PlayerName, PasswordHash=@PasswordHash, Salt=@Salt, UserLevel=@UserLevel,
                        IsBanned=@IsBanned, IsArchived=@IsArchived, IsPasswordExpired=@IsPasswordExpired WHERE Id=@Id", account);
                return true;
            }
            catch (Exception e)
            {
                Logger.ErrorException(e, nameof(UpdateAccount));
                return false;
            }
        }

        public bool UpdateAccountData(DBAccount account)
        {
            using SQLiteConnection connection = new(_connectionString);
            connection.Open();

            // Use a transaction to make sure all data is saved
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // Update saved player entity
                    connection.Execute(@"UPDATE Player SET RawRegion=@RawRegion, RawAvatar=@RawAvatar, RawWaypoint=@RawWaypoint, AOIVolume=@AOIVolume WHERE AccountId=@AccountId", account.Player, transaction);

                    // Update inventory entities
                    UpdateEntityTable(connection, transaction, "Avatar", (long)account.Id, account.Avatars);
                    UpdateEntityTable(connection, transaction, "Item", (long)account.Id, account.Items);

                    transaction.Commit();
                    return true;
                }
                catch (Exception e)
                {
                    Logger.ErrorException(e, nameof(UpdateAccountData));
                    transaction.Rollback();
                    return false;
                }
            }
        }

        public void CreateTestAccounts(int numAccounts)
        {
            for (int i = 0; i < numAccounts; i++)
            {
                string email = $"test{i + 1}@test.com";
                string playerName = $"TestPlayer{i + 1}";
                string password = "123";

                DBAccount account = new(email, playerName, password);
                InsertAccount(account);
                Logger.Info($"Created test account {email}");
            }
        }

        /// <summary>
        /// Loads account data for the specified <see cref="DBAccount"/> and maps relations.
        /// </summary>
        private void LoadAccountData(SQLiteConnection connection, DBAccount account)
        {
            var @params = new { AccountId = account.Id };

            var players = connection.Query<DBPlayer>("SELECT * FROM Player WHERE AccountId = @AccountId", @params);
            account.Player = players.First();

            var avatars = connection.Query<DBEntity>("SELECT * FROM Avatar WHERE ContainerDbGuid = @AccountId", @params);
            account.Avatars.AddRange(avatars);

            var items = connection.Query<DBEntity>("SELECT * FROM Item WHERE ContainerDbGuid = @AccountId", @params);
            account.Items.AddRange(items);
        }

        private static void UpdateEntityTable(SQLiteConnection connection, SQLiteTransaction transaction, string tableName,
            long containerDbGuid, DBEntityCollection dbEntityCollection)
        {
            var @params = new { ContainerDbGuid = containerDbGuid };

            // Delete items that no longer belong to this account
            var storedEntities = connection.Query<long>($"SELECT DbGuid FROM {tableName} WHERE ContainerDbGuid = @ContainerDbGuid", @params);
            var entitiesToDelete = storedEntities.Except(dbEntityCollection.Guids);
            connection.Execute($"DELETE FROM {tableName} WHERE DbGuid IN ({string.Join(',', entitiesToDelete)})");

            // Insert and update
            IEnumerable<DBEntity> entries = dbEntityCollection.GetEntriesForContainer(containerDbGuid);

            connection.Execute(@$"INSERT OR IGNORE INTO {tableName} (DbGuid) VALUES (@DbGuid)", entries, transaction);
            connection.Execute(@$"UPDATE {tableName} SET ContainerDbGuid=@ContainerDbGuid, InventoryProtoGuid=@InventoryProtoGuid,
                                Slot=@Slot, EntityProtoGuid=@EntityProtoGuid, ArchiveData=@ArchiveData WHERE DbGuid=@DbGuid",
                                entries, transaction);
        }
    }
}
