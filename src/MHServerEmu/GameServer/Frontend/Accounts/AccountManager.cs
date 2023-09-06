using System.Text.Json;
using Gazillion;
using MHServerEmu.Auth;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;

namespace MHServerEmu.GameServer.Frontend.Accounts
{
    public static class AccountManager
    {
        private const int MinimumPasswordLength = 3;
        private const int MaximumPasswordLength = 64;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string SavedDataDirectory = $"{Directory.GetCurrentDirectory()}\\SavedData";
        private static readonly string AccountFilePath = $"{SavedDataDirectory}\\Accounts.json";
        private static readonly string PlayerDataFilePath = $"{SavedDataDirectory}\\PlayerData.json";

        private static object _accountLock = new();

        private static List<Account> _accountList = new();
        private static Dictionary<ulong, Account> _idAccountDict = new();
        private static Dictionary<string, Account> _emailAccountDict = new();
        private static Dictionary<ulong, PlayerData> _playerDataDict = new();

        public static Account DefaultAccount = new(0, "default@account.mh", "123");
        public static PlayerData DefaultPlayerData = new(0, ConfigManager.PlayerData.PlayerName,
            ConfigManager.PlayerData.StartingRegion, ConfigManager.PlayerData.StartingAvatar, ConfigManager.PlayerData.CostumeOverride);

        public static bool IsInitialized { get; private set; }

        static AccountManager()
        {
            LoadAccounts();
            IsInitialized = true;
        }

        public static Account GetAccountByEmail(string email, string password, out AuthErrorCode? errorCode)
        {
            errorCode = null;

            if (_emailAccountDict.ContainsKey(email) == false)
            {
                errorCode = AuthErrorCode.IncorrectUsernameOrPassword1;
                return null;
            }

            Account account = _emailAccountDict[email];

            if (Cryptography.VerifyPassword(password, account.PasswordHash, account.Salt))
            {
                if (account.IsBanned)
                {
                    errorCode = AuthErrorCode.AccountBanned;
                    return null;
                }
                else if (account.IsArchived)
                {
                    errorCode = AuthErrorCode.AccountArchived;
                    return null;
                }
                else if (account.IsPasswordExpired)
                {
                    errorCode = AuthErrorCode.PasswordExpired;
                    return null;
                }
                else
                {
                    return account;
                }
            }
            else
            {
                errorCode = AuthErrorCode.IncorrectUsernameOrPassword1;
                return null;
            }
        }

        public static Account GetAccountByLoginDataPB(LoginDataPB loginDataPB, out AuthErrorCode? errorCode)
        {
            string email = loginDataPB.EmailAddress.ToLower();
            return GetAccountByEmail(email, loginDataPB.Password, out errorCode);
        }

        public static string CreateAccount(string email, string password)
        {
            if (_emailAccountDict.ContainsKey(email) == false)
            {
                if (password.Length >= MinimumPasswordLength && password.Length <= MaximumPasswordLength)
                {
                    lock (_accountLock)
                    {
                        // Create a new account
                        Account account = new(HashHelper.GenerateUniqueRandomId(_idAccountDict), email, password);
                        _accountList.Add(account);
                        _idAccountDict.Add(account.Id, account);
                        _emailAccountDict.Add(account.Email, account);
                        
                        // Create new player data for this account
                        _playerDataDict.Add(account.Id, new(account.Id));
                    }

                    SaveAccounts();
                    SavePlayerData();

                    return $"Created a new account: {email}.";
                }
                else
                {
                    return $"Failed to create account: password must between {MinimumPasswordLength} and {MaximumPasswordLength} characters long";
                }
            }
            else
            {
                return $"Failed to create account: email {email} is already used by another account";
            }
        }

        public static string ChangeAccountPassword(string email, string newPassword)
        {
            if (_emailAccountDict.ContainsKey(email))
            {
                if (newPassword.Length >= MinimumPasswordLength && newPassword.Length <= MaximumPasswordLength)
                {
                    Account account = _emailAccountDict[email];
                    account.PasswordHash = Cryptography.HashPassword(newPassword, out byte[] salt);
                    account.Salt = salt;
                    account.IsPasswordExpired = false;
                    SaveAccounts();
                    return $"Successfully changed password for account {email}.";
                }
                else
                {
                    return $"Failed to change password: password must between {MinimumPasswordLength} and {MaximumPasswordLength} characters long.";
                }
            }
            else
            {
                return $"Failed to change password: account {email} not found.";
            }
        }

        public static string BanAccount(string email)
        {
            if (_emailAccountDict.ContainsKey(email))
            {
                if (_emailAccountDict[email].IsBanned == false)
                {
                    _emailAccountDict[email].IsBanned = true;
                    SaveAccounts();
                    return $"Successfully banned account {email}.";
                }
                else
                {
                    return $"Account {email} is already banned.";
                }
            }
            else
            {
                return $"Cannot ban {email}: account not found.";
            }
        }

        public static string UnbanAccount(string email)
        {
            if (_emailAccountDict.ContainsKey(email))
            {
                if (_emailAccountDict[email].IsBanned)
                {
                    _emailAccountDict[email].IsBanned = false;
                    SaveAccounts();
                    return $"Successfully unbanned account {email}.";
                }
                else
                {
                    return $"Account {email} is not banned.";
                }
            }
            else
            {
                return $"Cannot unban {email}: account not found.";
            }
        }

        public static PlayerData GetPlayerData(ulong accountId)
        {
            if (accountId == 0)
                return DefaultPlayerData;
            else if (_playerDataDict.TryGetValue(accountId, out PlayerData playerData))
                return playerData;
            else
            {
                Logger.Warn($"PlayerData for accountId not found, creating new PlayerData");
                lock (_accountLock) _playerDataDict.Add(accountId, new(accountId));
                SavePlayerData();
                return _playerDataDict[accountId];
            }
        }

        private static void LoadAccounts()
        {
            if (_accountList.Count == 0)
            {
                if (Directory.Exists(SavedDataDirectory) == false) Directory.CreateDirectory(SavedDataDirectory);

                if (File.Exists(AccountFilePath))
                {
                    lock (_accountLock)
                    {
                        _accountList = JsonSerializer.Deserialize<List<Account>>(File.ReadAllText(AccountFilePath));

                        if (File.Exists(PlayerDataFilePath))
                            _playerDataDict = JsonSerializer.Deserialize<Dictionary<ulong, PlayerData>>(File.ReadAllText(PlayerDataFilePath));

                        foreach (Account account in _accountList)
                        {
                            _idAccountDict.Add(account.Id, account);
                            _emailAccountDict.Add(account.Email, account);
                        }
                    }

                    Logger.Info($"Loaded {_accountList.Count} accounts");
                }
            }
            else
            {
                Logger.Warn("Failed to load accounts: accounts are already loaded!");
            }
        }

        private static void SaveAccounts()
        {
            lock (_accountLock)
            {
                if (Directory.Exists(SavedDataDirectory) == false) Directory.CreateDirectory(SavedDataDirectory);
                File.WriteAllText(AccountFilePath, JsonSerializer.Serialize(_accountList));
            }
        }

        public static void SavePlayerData()
        {
            lock (_accountLock)
            {
                if (Directory.Exists(SavedDataDirectory) == false) Directory.CreateDirectory(SavedDataDirectory);
                File.WriteAllText(PlayerDataFilePath, JsonSerializer.Serialize(_playerDataDict));
            }           
        }
    }
}
