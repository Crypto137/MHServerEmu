using System.Text.Json;
using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Networking;
using static MHServerEmu.Networking.AuthServer;

namespace MHServerEmu.GameServer.Frontend.Accounts
{
    public static class AccountManager
    {
        private const int MinimumPasswordLength = 3;
        private const int MaximumPasswordLength = 64;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string SavedDataDirectory = $"{Directory.GetCurrentDirectory()}\\SavedData";

        private static List<Account> _accountList = new();
        private static Dictionary<ulong, Account> _idAccountDict = new();
        private static Dictionary<string, Account> _emailAccountDict = new();

        public static Account DefaultAccount = new(0, "default@account.mh", "123");

        public static bool IsInitialized { get; private set; }

        static AccountManager()
        {
            LoadAccounts();
            IsInitialized = true;
        }

        public static Account GetAccountByEmail(string email, string password, out ErrorCode? errorCode)
        {
            errorCode = null;

            if (_emailAccountDict.ContainsKey(email) == false)
            {
                errorCode = ErrorCode.IncorrectUsernameOrPassword1;
                return null;
            }

            Account account = _emailAccountDict[email];

            if (Cryptography.VerifyPassword(password, account.PasswordHash, account.Salt))
            {
                if (account.IsBanned)
                {
                    errorCode = ErrorCode.AccountBanned;
                    return null;
                }
                else if (account.IsArchived)
                {
                    errorCode = ErrorCode.AccountArchived;
                    return null;
                }
                else if (account.IsPasswordExpired)
                {
                    errorCode = ErrorCode.PasswordExpired;
                    return null;
                }
                else
                {
                    return account;
                }
            }
            else
            {
                errorCode = ErrorCode.IncorrectUsernameOrPassword1;
                return null;
            }
        }

        public static Account GetAccountByLoginDataPB(LoginDataPB loginDataPB, out ErrorCode? errorCode)
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
                    Account account = new(HashHelper.GenerateUniqueRandomId(_idAccountDict), email, password);
                    _accountList.Add(account);
                    _idAccountDict.Add(account.Id, account);
                    _emailAccountDict.Add(account.Email, account);
                    SaveAccounts();

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

        private static void LoadAccounts()
        {
            if (_accountList.Count == 0)
            {
                if (Directory.Exists(SavedDataDirectory) == false) Directory.CreateDirectory(SavedDataDirectory);

                string path = $"{SavedDataDirectory}\\Accounts.json";
                if (File.Exists(path))
                {
                    _accountList = JsonSerializer.Deserialize<List<Account>>(File.ReadAllText(path));

                    foreach (Account account in _accountList)
                    {
                        _idAccountDict.Add(account.Id, account);
                        _emailAccountDict.Add(account.Email, account);
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
            if (Directory.Exists(SavedDataDirectory) == false) Directory.CreateDirectory(SavedDataDirectory);

            string path = $"{SavedDataDirectory}\\Accounts.json";
            File.WriteAllText(path, JsonSerializer.Serialize(_accountList));
        }
    }
}
