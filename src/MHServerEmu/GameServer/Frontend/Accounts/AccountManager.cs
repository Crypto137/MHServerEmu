using Gazillion;
using MHServerEmu.Auth;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.Common.Logging;
using MHServerEmu.GameServer.Frontend.Accounts.DBModels;

namespace MHServerEmu.GameServer.Frontend.Accounts
{
    public enum AccountUserLevel : byte
    {
        User,
        Moderator,
        Admin
    }

    public static class AccountManager
    {
        private const int MinimumPasswordLength = 3;
        private const int MaximumPasswordLength = 64;

        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly DBAccount DefaultAccount = new(ConfigManager.PlayerData.PlayerName, ConfigManager.PlayerData.StartingRegion, ConfigManager.PlayerData.StartingAvatar);

        public static bool IsInitialized { get; private set; }

        static AccountManager()
        {
            IsInitialized = true;
        }

        public static DBAccount GetAccountByEmail(string email, string password, out AuthErrorCode? errorCode)
        {
            errorCode = null;

            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
            {
                errorCode = AuthErrorCode.IncorrectUsernameOrPassword1;
                return null;
            }

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

        public static DBAccount GetAccountByLoginDataPB(LoginDataPB loginDataPB, out AuthErrorCode? errorCode)
        {
            string email = loginDataPB.EmailAddress.ToLower();
            return GetAccountByEmail(email, loginDataPB.Password, out errorCode);
        }

        public static string CreateAccount(string email, string playerName, string password)
        {
            if (DBManager.TryQueryAccountByEmail(email, out _) == false)
            {
                if (password.Length >= MinimumPasswordLength && password.Length <= MaximumPasswordLength)
                {
                    // Create a new account and save it to the database
                    DBAccount account = new(email, playerName, password);

                    if (DBManager.CreateAccount(account))
                        return $"Created a new account: {email} ({playerName}).";
                    else
                        return "Failed to create account due to a database error.";
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
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account))
            {
                if (newPassword.Length >= MinimumPasswordLength && newPassword.Length <= MaximumPasswordLength)
                {
                    account.PasswordHash = Cryptography.HashPassword(newPassword, out byte[] salt);
                    account.Salt = salt;
                    account.IsPasswordExpired = false;
                    DBManager.SaveAccount(account);
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

        public static string SetAccountUserLevel(string email, AccountUserLevel userLevel)
        {
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account))
            {
                account.UserLevel = userLevel;
                DBManager.SaveAccount(account);
                return $"Successfully set user level for account {email} to {userLevel}.";
            }
            else
            {
                return $"Failed to set user level: account {email} not found.";
            }
        }

        public static string BanAccount(string email)
        {
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account))
            {
                if (account.IsBanned == false)
                {
                    account.IsBanned = true;
                    DBManager.SaveAccount(account);
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
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account))
            {
                if (account.IsBanned)
                {
                    account.IsBanned = false;
                    DBManager.SaveAccount(account);
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
    }
}
