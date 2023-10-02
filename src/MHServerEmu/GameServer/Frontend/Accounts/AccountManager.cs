using Gazillion;
using MHServerEmu.Auth;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
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

        public static readonly DBAccount DefaultAccount = new(ConfigManager.PlayerData.PlayerName, ConfigManager.PlayerData.StartingRegion, ConfigManager.PlayerData.StartingAvatar);

        public static bool IsInitialized { get; }

        static AccountManager()
        {
            IsInitialized = DBManager.IsInitialized;
        }

        public static AuthStatusCode TryGetAccountByLoginDataPB(LoginDataPB loginDataPB, out DBAccount account)
        {
            string email = loginDataPB.EmailAddress.ToLower();

            if (DBManager.TryQueryAccountByEmail(email, out account) == false)
                return AuthStatusCode.IncorrectUsernameOrPassword1;

            if (Cryptography.VerifyPassword(loginDataPB.Password, account.PasswordHash, account.Salt))
            {
                if (account.IsBanned)
                {
                    account = null;
                    return AuthStatusCode.AccountBanned;
                }
                else if (account.IsArchived)
                {
                    account = null;
                    return AuthStatusCode.AccountArchived;
                }
                else if (account.IsPasswordExpired)
                {
                    account = null;
                    return AuthStatusCode.PasswordExpired;
                }
                else
                {
                    return AuthStatusCode.Success;
                }
            }
            else
            {
                account = null;
                return AuthStatusCode.IncorrectUsernameOrPassword1;
            }
        }

        public static bool TryGetAccountByEmail(string email, out DBAccount account) => DBManager.TryQueryAccountByEmail(email, out account);

        public static string CreateAccount(string email, string playerName, string password)
        {
            if (DBManager.TryQueryAccountByEmail(email, out _) == false)
            {
                if (password.Length >= MinimumPasswordLength && password.Length <= MaximumPasswordLength)
                {
                    // Create a new account and insert it into the database
                    DBAccount account = new(email, playerName, password);

                    if (DBManager.InsertAccount(account))
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

        public static string ChangeAccountPlayerName(string email, string playerName)
        {
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account))
            {
                if (DBManager.QueryIsPlayerNameTaken(playerName) == false)
                {
                    account.PlayerName = playerName;
                    DBManager.UpdateAccount(account);
                    return $"Successfully changed player name for account {email} to {playerName}.";
                }
                else
                {
                    return $"Failed to change player name: the name {playerName} is already taken.";
                }
            }
            else
            {
                return $"Failed to change player name: account {email} not found.";
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
                    DBManager.UpdateAccount(account);
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
                DBManager.UpdateAccount(account);
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
                    DBManager.UpdateAccount(account);
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
                    DBManager.UpdateAccount(account);
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
