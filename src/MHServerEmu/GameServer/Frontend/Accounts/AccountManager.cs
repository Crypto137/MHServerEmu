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
            // Checks to make sure we can create an account with the provided data
            if (DBManager.TryQueryAccountByEmail(email, out _))
                return $"Failed to create account: email {email} is already used by another account.";

            if (DBManager.QueryIsPlayerNameTaken(playerName))
                return $"Failed to create account: name {playerName} is already used by another account.";

            if ((password.Length >= MinimumPasswordLength && password.Length <= MaximumPasswordLength) == false)
                return $"Failed to create account: password must between {MinimumPasswordLength} and {MaximumPasswordLength} characters long.";

            // Create a new account and insert it into the database
            DBAccount account = new(email, playerName, password);

            if (DBManager.InsertAccount(account))
                return $"Created a new account: {email} ({playerName}).";
            else
                return "Failed to create account: database error.";
        }

        public static string ChangeAccountPlayerName(string email, string playerName)
        {
            // Checks to make sure we can use the provided email and player name
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return $"Failed to change player name: account {email} not found.";
            
            if (DBManager.QueryIsPlayerNameTaken(playerName))
                return $"Failed to change player name: the name {playerName} is already taken.";

            // Update player name
            account.PlayerName = playerName;
            DBManager.UpdateAccount(account);
            return $"Successfully changed player name for account {email} to {playerName}.";
        }

        public static string ChangeAccountPassword(string email, string newPassword)
        {
            // Checks to make sure we can use the provided email and password
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return $"Failed to change password: account {email} not found.";

            if ((newPassword.Length >= MinimumPasswordLength && newPassword.Length <= MaximumPasswordLength) == false)
                return $"Failed to change password: password must between {MinimumPasswordLength} and {MaximumPasswordLength} characters long.";

            // Update password
            account.PasswordHash = Cryptography.HashPassword(newPassword, out byte[] salt);
            account.Salt = salt;
            account.IsPasswordExpired = false;
            DBManager.UpdateAccount(account);
            return $"Successfully changed password for account {email}.";
        }

        public static string SetAccountUserLevel(string email, AccountUserLevel userLevel)
        {
            // Make sure the specified account exists
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return $"Failed to set user level: account {email} not found.";

            // Update user level
            account.UserLevel = userLevel;
            DBManager.UpdateAccount(account);
            return $"Successfully set user level for account {email} to {userLevel}.";
        }

        // Ban and unban are separate methods to make sure we don't accidentally ban or unban someone we didn't intend to.

        public static string BanAccount(string email)
        {
            // Checks to make sure we can ban the specified account
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return $"Failed to ban: account {email} not found.";

            if (account.IsBanned)
                return $"Failed to ban: account {email} is already banned.";

            // Ban the account
            account.IsBanned = true;
            DBManager.UpdateAccount(account);
            return $"Successfully banned account {email}.";
        }

        public static string UnbanAccount(string email)
        {
            // Checks to make sure we can ban the specified account
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return $"Failed to unban: account {email} not found.";

            if (account.IsBanned == false)
                return $"Failed to unban: account {email} is not banned.";

            // Unban the account
            account.IsBanned = false;
            DBManager.UpdateAccount(account);
            return $"Successfully unbanned account {email}.";
        }
    }
}
