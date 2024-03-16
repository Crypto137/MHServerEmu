using System.Text.RegularExpressions;
using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.PlayerManagement.Configs;

namespace MHServerEmu.PlayerManagement
{
    public static class AccountManager
    {
        public static bool IsInitialized { get; }

        public static DBAccount DefaultAccount { get; }

        static AccountManager()
        {
            // Initialize default account from config
            var config = ConfigManager.Instance.GetConfig<DefaultPlayerDataConfig>();
            DefaultAccount = config.InitializeDefaultAccount();

            IsInitialized = DBManager.IsInitialized;
        }

        public static AuthStatusCode TryGetAccountByLoginDataPB(LoginDataPB loginDataPB, out DBAccount account)
        {
            account = null;

            // Try to query an account to check
            string email = loginDataPB.EmailAddress.ToLower();
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount accountToCheck) == false)
                return AuthStatusCode.IncorrectUsernameOrPassword403;

            // Check the account we queried
            if (CryptographyHelper.VerifyPassword(loginDataPB.Password, accountToCheck.PasswordHash, accountToCheck.Salt) == false)
                return AuthStatusCode.IncorrectUsernameOrPassword403;

            if (accountToCheck.IsBanned) return AuthStatusCode.AccountBanned;
            if (accountToCheck.IsArchived) return AuthStatusCode.AccountArchived;
            if (accountToCheck.IsPasswordExpired) return AuthStatusCode.PasswordExpired;

            // Output the account and return success if everything is okay
            account = accountToCheck;
            return AuthStatusCode.Success;
        }

        public static bool TryGetAccountByEmail(string email, out DBAccount account) => DBManager.TryQueryAccountByEmail(email, out account);

        public static bool CreateAccount(string email, string playerName, string password, out string message)
        {
            // Validate input before doing database queries
            if (ValidateEmail(email) == false)
            {
                message = "Failed to create account: email must not be longer than 320 characters.";
                return false;
            }

            if (ValidatePlayerName(playerName) == false)
            {
                message = "Failed to create account: names may contain only up to 16 alphanumeric characters.";
                return false;
            }

            if (ValidatePassword(password) == false)
            {
                message = "Failed to create account: password must between 3 and 64 characters long.";
                return false;
            }

            if (DBManager.TryQueryAccountByEmail(email, out _))
            {
                message = $"Failed to create account: email {email} is already used by another account.";
                return false;
            }

            if (DBManager.QueryIsPlayerNameTaken(playerName))
            {
                message = $"Failed to create account: name {playerName} is already used by another account.";
                return false;
            }

            // Create a new account and insert it into the database
            DBAccount account = new(email, playerName, password);

            if (DBManager.InsertAccount(account) == false)
            {
                message = "Failed to create account: database error.";
                return false;
            }

            message = $"Created a new account: {email} ({playerName}).";
            return true;
        }

        // TODO bool ChangeAccountEmail(string oldEmail, string newEmail, out string message)

        public static bool ChangeAccountPlayerName(string email, string playerName, out string message)
        {
            // Validate input before doing database queries
            if (ValidatePlayerName(playerName) == false)
            {
                message = "Failed to change player name: names may contain only up to 16 alphanumeric characters.";
                return false;
            }

            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
            {
                message = $"Failed to change player name: account {email} not found.";
                return false;
            }

            if (DBManager.QueryIsPlayerNameTaken(playerName))
            {
                message = $"Failed to change player name: name {playerName} is already used by another account.";
                return false;
            }

            // Update player name
            account.PlayerName = playerName;
            DBManager.UpdateAccount(account);
            message = $"Successfully changed player name for account {email} to {playerName}.";
            return true;
        }

        public static bool ChangeAccountPassword(string email, string newPassword, out string message)
        {
            // Validate input before doing database queries
            if (ValidatePassword(newPassword) == false)
            {
                message = "Failed to change password: password must between 3 and 64 characters long.";
                return false;
            }

            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
            {
                message = $"Failed to change password: account {email} not found.";
                return false;
            }

            // Update password
            account.PasswordHash = CryptographyHelper.HashPassword(newPassword, out byte[] salt);
            account.Salt = salt;
            account.IsPasswordExpired = false;
            DBManager.UpdateAccount(account);
            message = $"Successfully changed password for account {email}.";
            return true;
        }

        public static bool SetAccountUserLevel(string email, AccountUserLevel userLevel, out string message)
        {
            // Make sure the specified account exists
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
            {
                message = $"Failed to set user level: account {email} not found.";
                return false;
            }

            // Update user level
            account.UserLevel = userLevel;
            DBManager.UpdateAccount(account);
            message = $"Successfully set user level for account {email} to {userLevel}.";
            return true;
        }

        // Ban and unban are separate methods to make sure we don't accidentally ban or unban someone we didn't intend to.

        public static bool BanAccount(string email, out string message)
        {
            // Checks to make sure we can ban the specified account
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
            {
                message = $"Failed to ban: account {email} not found.";
                return false;
            }

            if (account.IsBanned)
            {
                message = $"Failed to ban: account {email} is already banned.";
                return false;
            }

            // Ban the account
            account.IsBanned = true;
            DBManager.UpdateAccount(account);
            message = $"Successfully banned account {email}.";
            return true;
        }

        public static bool UnbanAccount(string email, out string message)
        {
            // Checks to make sure we can ban the specified account
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
            {
                message = $"Failed to unban: account {email} not found.";
                return false;
            }

            if (account.IsBanned == false)
            {
                message = $"Failed to unban: account {email} is not banned.";
                return false;
            }

            // Unban the account
            account.IsBanned = false;
            DBManager.UpdateAccount(account);
            message = $"Successfully unbanned account {email}.";
            return true;
        }

        private static bool ValidateEmail(string email) => email.Length.IsWithin(1, 320);  // todo: add regex for email
        private static bool ValidatePlayerName(string playerName) => Regex.Match(playerName, "^[a-zA-Z0-9]{1,16}$").Success;    // 1-16 alphanumeric characters
        private static bool ValidatePassword(string password) => password.Length.IsWithin(3, 64);
    }
}
