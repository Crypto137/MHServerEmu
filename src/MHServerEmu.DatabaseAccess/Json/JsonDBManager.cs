﻿using System.Text.Json;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.DatabaseAccess.Json
{
    public class JsonDBManager : IDBManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private string _accountFilePath;
        private DBAccount _account;
        private JsonSerializerOptions _jsonOptions;

        private int _maxBackupNumber;
        private TimeSpan _backupInterval;
        private TimeSpan _lastBackupTime;

        public static JsonDBManager Instance { get; } = new();

        public bool ValidateAccounts { get => false; }

        private JsonDBManager() { }

        public bool Initialize()
        {
            var config = ConfigManager.Instance.GetConfig<JsonDBManagerConfig>();
            _accountFilePath = Path.Combine(FileHelper.DataDirectory, config.FileName);

            _jsonOptions = new();
            _jsonOptions.Converters.Add(new DBEntityCollectionJsonConverter());

            if (File.Exists(_accountFilePath))
            {
                Logger.Info($"Found existing account file {FileHelper.GetRelativePath(_accountFilePath)}");

                try
                {
                    _account = FileHelper.DeserializeJson<DBAccount>(_accountFilePath, _jsonOptions);
                }
                catch
                {
                    Logger.Warn($"Initialize(): Failed to load existing account data, resetting");
                }
            }

            if (_account == null)
            {
                // Initialize a new default account from config
                _account = new(config.PlayerName);
                _account.Player = new(_account.Id);

                Logger.Info($"Initialized default account {_account}");
            }
            else
            {
                _account.PlayerName = config.PlayerName;
                Logger.Info($"Loaded default account {_account}");
            }

            _maxBackupNumber = config.MaxBackupNumber;
            _backupInterval = TimeSpan.FromMinutes(config.BackupIntervalMinutes);
            _lastBackupTime = Clock.GameTime;

            return _account != null;
        }

        public bool TryQueryAccountByEmail(string email, out DBAccount account)
        {
            account = _account;
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
            if (account != _account)
                return Logger.WarnReturn(false, "UpdateAccountData(): Attempting to update non-default account when bypass auth is enabled");

            Logger.Info($"Updated account file {FileHelper.GetRelativePath(_accountFilePath)}");
            FileHelper.SerializeJson(_accountFilePath, _account, _jsonOptions);

            TryCreateBackup();

            return true;
        }

        public void CreateTestAccounts(int numAccounts)
        {
            Logger.Warn("CreateTestAccounts(): Operation not supported");
        }

        private void TryCreateBackup()
        {
            TimeSpan now = Clock.GameTime;

            if ((now - _lastBackupTime) < _backupInterval)
                return;

            if (FileHelper.CreateFileBackup(_accountFilePath, _maxBackupNumber))
                Logger.Info("Created account file backup");

            _lastBackupTime = now;
        }

    }
}