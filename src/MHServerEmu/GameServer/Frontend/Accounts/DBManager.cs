using System.Data.SQLite;
using Dapper;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Frontend.Accounts.DBModels;

namespace MHServerEmu.GameServer.Frontend.Accounts
{
    public static class DBManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string ConnectionString;

        static DBManager()
        {
            ConnectionString = $"Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Account.db")}";
        }

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

        public static void SaveAccount(DBAccount account)
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
                            VALUES (@AccountId), @RawRegion, @RawAvatar)", account.Player, transaction);

                        connection.Execute(@"INSERT INTO Avatar (AccountId, RawPrototype, RawCostume)
                            VALUES (@AccountId, @RawPrototype, @RawCostume)", account.Avatars, transaction);

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorException(e, "SaveAccount failed");
                        transaction.Rollback();
                    }
                }
            }
        }

        public static void CreateAndSaveTestAccounts()
        {
            List<DBAccount> accountList = new()
            {
                new("test1@test.com", "TestPlayer1", "123"),
                new("test2@test.com", "TestPlayer2", "123"),
                new("test3@test.com", "TestPlayer3", "123"),
                new("test4@test.com", "TestPlayer4", "123"),
                new("test5@test.com", "TestPlayer5", "123")
            };

            accountList.ForEach(account => SaveAccount(account));
        }

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
