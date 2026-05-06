using Dapper;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;
using System.Data.SQLite;
using System.Diagnostics;

namespace MHServerEmu.DatabaseAccess.SQLite
{
    public enum SQLiteImportResult
    {
        Success,
        GenericError,
        SQLiteDisabled,
        FileNotFound,
        AccountError,
        GuildError,
    }

    public class SQLiteImporter
    {
        private const int MachineIdOffset = 48;
        private const ulong MachineIdMaxSize = (1 << 12) - 1;   // 0x0FFF
        private const ulong MachineIdMask = MachineIdMaxSize << MachineIdOffset;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly string _fileName;
        private readonly string _filePath;
        private readonly string _connectionString;

        private readonly string _emailSuffix;
        private readonly string _nameSuffix;
        private readonly ulong _machineIdBits;

        public SQLiteImporter(string fileName, string emailSuffix = "server2.com", string nameSuffix = "#2", ulong machineId = 1)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentException.ThrowIfNullOrWhiteSpace(emailSuffix);
            if (nameSuffix.StartsWith('@')) throw new ArgumentException(null, nameof(emailSuffix));
            ArgumentException.ThrowIfNullOrWhiteSpace(nameSuffix);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(machineId, MachineIdMaxSize);

            _fileName = Path.GetFileName(fileName);
            _filePath = Path.Combine(FileHelper.DataDirectory, fileName);
            _connectionString = $"Data Source={_filePath};Synchronous=NORMAL;foreign_keys=OFF;";

            _machineIdBits = (machineId & MachineIdMaxSize) << MachineIdOffset;
            _emailSuffix = emailSuffix;
            _nameSuffix = nameSuffix;
        }

        public override string ToString()
        {
            return _fileName;
        }

        public SQLiteImportResult Import()
        {
            if (IDBManager.Instance != SQLiteDBManager.Instance)
                return SQLiteImportResult.SQLiteDisabled;

            Stopwatch stopwatch = Stopwatch.StartNew();

            // TODO: Back up original DB file

            Logger.Info($"Beginning import from {_fileName}...");

            SQLiteImportResult result;
            string errorMessage = string.Empty;

            try
            {
                result = DoImport();
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                result = SQLiteImportResult.GenericError;
            }

            if (result != SQLiteImportResult.Success)
            {
                Logger.Error($"Error importing from: {_fileName}: result=[{result}], errorMessage=[{errorMessage}]");
                // TODO: Restore DB backup
            }

            stopwatch.Stop();
            Logger.Info($"Import finished: result=[{result}], elapsedTime=[{TimeSpan.FromSeconds((long)stopwatch.Elapsed.TotalSeconds)}]");
            return result;
        }

        private SQLiteImportResult DoImport()
        {
            if (File.Exists(_filePath) == false)
                return SQLiteImportResult.FileNotFound;

            if (ImportAccounts() == false)
                return SQLiteImportResult.AccountError;

            if (ImportGuilds() == false)
                return SQLiteImportResult.GuildError;

            return SQLiteImportResult.Success;
        }

        private bool ImportAccounts()
        {
            SQLiteDBManager targetDB = SQLiteDBManager.Instance;

            List<DBAccount> accountsToImport = new();
            GetAccounts(accountsToImport);

            int count = accountsToImport.Count;
            Logger.Info($"Found {count} account entries");

            // Iterate in reverse order and remove processed accounts to allow GC to free memory.
            // The work could be potentially split across threads (X read/update threads + 1 write thread),
            // but we're not going to do this often, so it's not worth the complexity cost for now.
            for (int i = count - 1; i >= 0; i--)
            {
                DBAccount account = accountsToImport[i];
                accountsToImport.RemoveAt(i);

                LoadPlayerData(account);

                UpdateAccountForImport(account);

                if (targetDB.InsertAccount(account) == false)
                    return false;

                if (account.Player != null)
                {
                    if (targetDB.SavePlayerData(account) == false)
                        return false;
                }
            }

            return true;
        }

        private void UpdateAccountForImport(DBAccount account)
        {
            account.Id = UpdateGuidForImport(account.Id);
            account.PlayerName += _nameSuffix;

            // We expect all emails in the imported database to use the same domain name (e.g. user@server2.com),
            // but there may be edge cases, such as auto created test accounts that use @test.com.
            if (account.Email.EndsWith(_emailSuffix) == false)
                account.Email = string.Join('@', account.Email.Split('@')[0], _emailSuffix);

            if (account.Player == null)
                return;

            account.Player.DbGuid = UpdateGuidForImport(account.Player.DbGuid);
            account.Player.Flags |= (long)DBPlayerFlags.Imported;

            UpdateDBEntityCollectionForImport(account.Avatars);
            UpdateDBEntityCollectionForImport(account.TeamUps);
            UpdateDBEntityCollectionForImport(account.Items);
            UpdateDBEntityCollectionForImport(account.ControlledEntities);
        }

        private void UpdateDBEntityCollectionForImport(DBEntityCollection collection)
        {
            List<DBEntity> entities = new(collection.Count);
            foreach (DBEntity entity in collection)
            {
                entity.DbGuid = UpdateGuidForImport(entity.DbGuid);
                entity.ContainerDbGuid = UpdateGuidForImport(entity.ContainerDbGuid);
                entities.Add(entity);
            }

            collection.Clear();
            collection.AddRange(entities);
        }

        private long UpdateGuidForImport(long guid)
        {
            if (guid == 0)
                return guid;

            ulong bits = (ulong)guid;
            bits &= ~MachineIdMask;
            bits |= _machineIdBits;
            return (long)bits;
        }

        private bool ImportGuilds()
        {
            // TODO
            return true;
        }

        private void GetAccounts(List<DBAccount> accounts)
        {
            using SQLiteConnection connection = GetConnection();

            IEnumerable<DBAccount> accountsQuery = connection.Query<DBAccount>("SELECT * FROM Account");
            accounts.AddRange(accountsQuery);
        }

        private void LoadPlayerData(DBAccount account)
        {
            using SQLiteConnection connection = GetConnection();

            account.Player = connection.QueryFirstOrDefault<DBPlayer>("SELECT * FROM Player WHERE DbGuid = @DbGuid", new { DbGuid = account.Id });

            // Do not initialize anything for account that have never logged in.
            if (account.Player == null)
                return;

            // Load inventory entities
            SQLiteEntityTable avatarTable = SQLiteEntityTable.GetTable(DBEntityCategory.Avatar);
            SQLiteEntityTable teamUpTable = SQLiteEntityTable.GetTable(DBEntityCategory.TeamUp);
            SQLiteEntityTable itemTable = SQLiteEntityTable.GetTable(DBEntityCategory.Item);
            SQLiteEntityTable controlledEntityTable = SQLiteEntityTable.GetTable(DBEntityCategory.ControlledEntity);

            avatarTable.LoadEntities(connection, account.Id, account.Avatars);
            teamUpTable.LoadEntities(connection, account.Id, account.TeamUps);
            itemTable.LoadEntities(connection, account.Id, account.Items);

            foreach (DBEntity avatar in account.Avatars)
            {
                itemTable.LoadEntities(connection, avatar.DbGuid, account.Items);
                controlledEntityTable.LoadEntities(connection, avatar.DbGuid, account.ControlledEntities);
            }

            foreach (DBEntity teamUp in account.TeamUps)
            {
                itemTable.LoadEntities(connection, teamUp.DbGuid, account.Items);
            }
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
