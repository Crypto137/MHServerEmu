using System.Data.SQLite;
using Dapper;
using MHServerEmu.Common.Helpers;
using MHServerEmu.Common.Logging;
using MHServerEmu.PlayerManagement.Accounts.DBModels;

namespace MHServerEmu.PlayerManagement.Accounts
{
    public static class DBManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string ConnectionString;

        public static bool IsInitialized { get; }

        static DBManager()
        {
            string dbPath = Path.Combine(FileHelper.DataDirectory, "Account.db");
            if (File.Exists(dbPath) == false)
            {
                Logger.Fatal($"{dbPath} not found");
                return;
            }

            ConnectionString = $"Data Source={dbPath}";
            IsInitialized = true;
        }

        #region Queries

        /// <summary>
        /// Queries an account from the database by its email.
        /// </summary>
        /// <param name="email">Email to query.</param>
        /// <param name="account">Account or null.</param>
        /// <returns>IsSuccess</returns>
        public static bool TryQueryAccountByEmail(string email, out DBAccount account)
        {
            using (SQLiteConnection connection = new(ConnectionString))
            {
                var accounts = connection.Query<DBAccount>("SELECT * FROM Account WHERE Email = @Email", new { Email = email });

                if (accounts.Any())
                {
                    account = accounts.First();
                    LoadAccountData(connection, account);
                    return true;
                }
                else
                {
                    account = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Queries if the specified player name is already taken.
        /// </summary>
        /// <param name="playerName">Name to check.</param>
        /// <returns>IsTaken</returns>
        public static bool QueryIsPlayerNameTaken(string playerName)
        {
            using (SQLiteConnection connection = new(ConnectionString))
            {
                // This check is case insensitive (COLLATE NOCASE)
                var results = connection.Query<string>("SELECT PlayerName FROM Account WHERE PlayerName = @PlayerName COLLATE NOCASE", new { PlayerName = playerName });
                return results.Any();
            }
        }

        #endregion

        #region Executes

        /// <summary>
        /// Inserts a new account with all of its data into the database.
        /// </summary>
        /// <param name="account">Account to insert.</param>
        /// <returns>IsSuccess</returns>
        public static bool InsertAccount(DBAccount account)
        {
            using (SQLiteConnection connection = new(ConnectionString))
            {
                connection.Open();

                // Use a transaction to make sure all data is saved
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        connection.Execute(@"INSERT INTO Account (Id, Email, PlayerName, PasswordHash, Salt, UserLevel, IsBanned, IsArchived, IsPasswordExpired)
                            VALUES (@Id, @Email, @PlayerName, @PasswordHash, @Salt, @UserLevel, @IsBanned, @IsArchived, @IsPasswordExpired)", account, transaction);

                        connection.Execute(@"INSERT INTO Player (AccountId, RawRegion, RawAvatar)
                            VALUES (@AccountId, @RawRegion, @RawAvatar)", account.Player, transaction);

                        connection.Execute(@"INSERT INTO Avatar (AccountId, RawPrototype, RawCostume)
                            VALUES (@AccountId, @RawPrototype, @RawCostume)", account.Avatars, transaction);

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
        }

        /// <summary>
        /// Updates the Account table in the database with the provided account.
        /// </summary>
        /// <param name="account">Account to update.</param>
        /// <returns>IsSuccess</returns>
        public static bool UpdateAccount(DBAccount account)
        {
            using (SQLiteConnection connection = new(ConnectionString))
            {
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
        }

        /// <summary>
        /// Updates the Player and Avatar tables in the database with the data from the provided account.
        /// </summary>
        /// <param name="account"></param>
        /// <returns>IsSuccess</returns>
        public static bool UpdateAccountData(DBAccount account)
        {
            using (SQLiteConnection connection = new(ConnectionString))
            {
                connection.Open();

                // Use a transaction to make sure all data is saved
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        connection.Execute(@"UPDATE Player SET RawRegion=@RawRegion, RawAvatar=@RawAvatar WHERE AccountId=@AccountId", account.Player, transaction);
                        connection.Execute(@"UPDATE Avatar SET RawCostume=@RawCostume WHERE AccountId=@AccountId AND RawPrototype=@RawPrototype", account.Avatars, transaction);

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
        }

        /// <summary>
        /// Creates and inserts test accounts into the database for testing.
        /// </summary>
        public static void CreateTestAccounts()
        {
            List<DBAccount> accountList = new()
            {
                new("test1@test.com", "TestPlayer1", "123"),
                new("test2@test.com", "TestPlayer2", "123"),
                new("test3@test.com", "TestPlayer3", "123"),
                new("test4@test.com", "TestPlayer4", "123"),
                new("test5@test.com", "TestPlayer5", "123")
            };

            accountList.ForEach(account => InsertAccount(account));
        }

        #endregion

        /// <summary>
        /// Loads account data for the specified account and maps relations.
        /// </summary>
        /// <param name="connection">Database connection for querying.</param>
        /// <param name="account">Account to get data for.</param>
        private static void LoadAccountData(SQLiteConnection connection, DBAccount account)
        {
            var @params = new { AccountId = account.Id };

            var players = connection.Query<DBPlayer>("SELECT * FROM Player WHERE AccountId = @AccountId", @params);
            account.Player = players.First();

            var avatars = connection.Query<DBAvatar>("SELECT * FROM Avatar WHERE AccountId = @AccountId", @params);
            account.Avatars = avatars.ToArray();
        }
    }
}
