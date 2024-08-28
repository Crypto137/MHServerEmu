using System.Text.Json;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess.Json
{
    public class JsonDBManager : IDBManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string DefaultAccountFilePath = Path.Combine(FileHelper.DataDirectory, "DefaultPlayer.json");

        private DBAccount _defaultAccount;
        private JsonSerializerOptions _jsonOptions;

        public static JsonDBManager Instance { get; } = new();

        public bool ValidateAccounts { get => false; }

        private JsonDBManager() { }

        public bool Initialize()
        {
            _jsonOptions = new();
            _jsonOptions.Converters.Add(new DBEntityCollectionJsonConverter());

            if (File.Exists(DefaultAccountFilePath))
            {
                try
                {
                    _defaultAccount = FileHelper.DeserializeJson<DBAccount>(DefaultAccountFilePath, _jsonOptions);
                }
                catch
                {
                    Logger.Warn($"Initialize(): Incompatible default player data, resetting");
                }
            }

            var config = ConfigManager.Instance.GetConfig<DefaultPlayerDataConfig>();

            if (_defaultAccount == null)
            {
                // Initialize a new default account from config
                _defaultAccount = new(config.PlayerName);
                _defaultAccount.Player = new(_defaultAccount.Id);

                Logger.Info($"Initialized default account {_defaultAccount}");
            }
            else
            {
                _defaultAccount.PlayerName = config.PlayerName;
                Logger.Info($"Loaded default account {_defaultAccount}");
            }

            return _defaultAccount != null;
        }

        public bool TryQueryAccountByEmail(string email, out DBAccount account)
        {
            account = _defaultAccount;
            return true;
        }

        public bool QueryIsPlayerNameTaken(string playerName)
        {
            return Logger.WarnReturn(true, "QueryIsPlayerNameTaken(): Operation not supported");
        }

        public bool InsertAccount(DBAccount account)
        {
            return Logger.WarnReturn(false, "InsertAccount(): Operation not supported");
        }

        public bool UpdateAccount(DBAccount account)
        {
            return Logger.WarnReturn(false, "UpdateAccount(): Operation not supported");
        }

        public bool UpdateAccountData(DBAccount account)
        {
            if (account != _defaultAccount)
                return Logger.WarnReturn(false, "UpdateAccountData(): Attempting to update non-default account when bypass auth is enabled");

            FileHelper.SerializeJson(DefaultAccountFilePath, _defaultAccount, _jsonOptions);

            return true;
        }

        public void CreateTestAccounts(int numAccounts)
        {
            Logger.Warn("CreateTestAccounts(): Operation not supported");
        }
    }
}
