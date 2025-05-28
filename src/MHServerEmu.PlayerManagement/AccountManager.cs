using System.Text.RegularExpressions;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement
{
    /// <summary>
    /// Provides <see cref="DBAccount"/> management functions.
    /// </summary>
    public static class AccountManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static IDBManager DBManager { get; private set; }

        /// <summary>
        /// Initializes <see cref="AccountManager"/>.
        /// </summary>
        public static bool Initialize()
        {
            DBManager = IDBManager.Instance;
            return true;
        }

        /// <summary>
        /// Queries a <see cref="DBAccount"/> using the provided <see cref="LoginDataPB"/> instance.
        /// <see cref="AuthStatusCode"/> indicates the outcome of the query.
        /// </summary>
        public static AuthStatusCode TryGetAccountByLoginDataPB(LoginDataPB loginDataPB, out DBAccount account)
        {
            account = null;

            // Try to query an account to check
            string email = loginDataPB.EmailAddress.ToLower();
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount accountToCheck) == false)
                return AuthStatusCode.IncorrectUsernameOrPassword403;

            // Check the account we queried if our DB manager requires it
            if (DBManager.VerifyAccounts)
            {
                if (CryptographyHelper.VerifyPassword(loginDataPB.Password, accountToCheck.PasswordHash, accountToCheck.Salt) == false)
                    return AuthStatusCode.IncorrectUsernameOrPassword403;

                if (accountToCheck.Flags.HasFlag(AccountFlags.IsBanned))
                    return AuthStatusCode.AccountBanned;
                
                if (accountToCheck.Flags.HasFlag(AccountFlags.IsArchived))
                    return AuthStatusCode.AccountArchived;
                
                if (accountToCheck.Flags.HasFlag(AccountFlags.IsPasswordExpired))
                    return AuthStatusCode.PasswordExpired;
            }

            // Output the account and return success if everything is okay
            account = accountToCheck;
            return AuthStatusCode.Success;
        }

        /// <summary>
        /// Queries a <see cref="DBAccount"/> using the provided email. Returns <see langword="true"/> if successful.
        /// </summary>
        public static bool TryGetAccountByEmail(string email, out DBAccount account)
        {
            return DBManager.TryQueryAccountByEmail(email, out account);
        }

        public static bool LoadPlayerDataForAccount(DBAccount account)
        {
            return DBManager.LoadPlayerData(account);
        }

        /// <summary>
        /// Creates a new <see cref="DBAccount"/> and inserts it into the database. Returns <see langword="true"/> if successful.
        /// </summary>
        public static (bool, string) CreateAccount(string email, string playerName, string password)
        {
            // Validate input before doing database queries
            if (ValidateEmail(email) == false)
                return (false, "Failed to create account: email must not be longer than 320 characters.");

            if (ValidatePlayerName(playerName) == false)
                return (false, "Failed to create account: names may contain only up to 16 alphanumeric characters.");

            if (ValidatePassword(password) == false)
                return (false, "Failed to create account: password must between 3 and 64 characters long.");

            if (DBManager.TryQueryAccountByEmail(email, out _))
                return (false, $"Failed to create account: email {email} is already used by another account.");

            if (DBManager.QueryIsPlayerNameTaken(playerName))
                return (false, $"Failed to create account: name {playerName} is already used by another account.");

            // Create a new account and insert it into the database
            DBAccount account = new(email, playerName, password);

            if (DBManager.InsertAccount(account) == false)
                return (false, "Failed to create account: database error.");

            return (true, $"Created a new account: {email} ({playerName}).");
        }

        // TODO (bool, string) ChangeAccountEmail(string oldEmail, string newEmail)

        /// <summary>
        /// Changes the player name of the <see cref="DBAccount"/> with the specified email. Returns <see langword="true"/> if successful.
        /// </summary>
        public static (bool, string) ChangeAccountPlayerName(string email, string playerName)
        {
            // Validate input before doing database queries
            if (ValidatePlayerName(playerName) == false)
                return (false, "Failed to change player name: names may contain only up to 16 alphanumeric characters.");

            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return (false, $"Failed to change player name: account {email} not found.");

            if (DBManager.QueryIsPlayerNameTaken(playerName))
                return (false, $"Failed to change player name: name {playerName} is already used by another account.");

            // Write the new name to the database
            account.PlayerName = playerName;
            DBManager.UpdateAccount(account);
            return (true, $"Successfully changed player name for account {email} to {playerName}.");
        }

        /// <summary>
        /// Changes the password of the <see cref="DBAccount"/> with the specified email. Returns <see langword="true"/> if successful.
        /// </summary>
        public static (bool, string) ChangeAccountPassword(string email, string newPassword)
        {
            // Validate input before doing database queries
            if (ValidatePassword(newPassword) == false)
                return (false, "Failed to change password: password must between 3 and 64 characters long.");

            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return (false, $"Failed to change password: account {email} not found.");

            // Update the password and write the new hash/salt to the database
            account.PasswordHash = CryptographyHelper.HashPassword(newPassword, out byte[] salt);
            account.Salt = salt;
            account.Flags &= ~AccountFlags.IsPasswordExpired;
            DBManager.UpdateAccount(account);
            return (true, $"Successfully changed password for account {email}.");
        }

        /// <summary>
        /// Changes the <see cref="AccountUserLevel"/> of the <see cref="DBAccount"/> with the specified email. Returns <see langword="true"/> if successful.
        /// </summary>
        public static (bool, string) SetAccountUserLevel(string email, AccountUserLevel userLevel)
        {
            // Make sure the specified account exists
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return (false, $"Failed to set user level: account {email} not found.");

            // Write the new user level to the database
            account.UserLevel = userLevel;
            DBManager.UpdateAccount(account);
            return (true, $"Successfully set user level for account {email} to {userLevel}.");
        }

        /// <summary>
        /// Sets the specified <see cref="AccountFlags"/> for the <see cref="DBAccount"/> with the provided email.
        /// </summary>
        public static (bool, string) SetFlag(string email, AccountFlags flag)
        {
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return (false, $"Failed to set flag: account with email {email} not found.");

            return SetFlag(account, flag);
        }

        /// <summary>
        /// Sets the specified <see cref="AccountFlags"/> for the provided <see cref="DBAccount"/>.
        /// </summary>
        public static (bool, string) SetFlag(DBAccount account, AccountFlags flag)
        {
            if (account.Flags.HasFlag(flag))
                return (false, $"Failed to set flag: account {account} already has flag {flag}.");

            // Update flags and write to the database
            Logger.Trace($"Setting flag {flag} for account {account}");
            account.Flags |= flag;
            DBManager.UpdateAccount(account);
            return (true, $"Successfully set flag {flag} for account {account}.");
        }

        /// <summary>
        /// Clears the specified <see cref="AccountFlags"/> for the <see cref="DBAccount"/> with the provided email.
        /// </summary>
        public static (bool, string) ClearFlag(string email, AccountFlags flag)
        {
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return (false, $"Failed to clear flag: account with email {email} not found.");

            return ClearFlag(account, flag);
        }

        /// <summary>
        /// Clears the specified <see cref="AccountFlags"/> for the provided <see cref="DBAccount"/>.
        /// </summary>
        public static (bool, string) ClearFlag(DBAccount account, AccountFlags flag)
        {
            if (account.Flags.HasFlag(flag) == false)
                return (false, $"Failed to clear flag: {flag} is not set for account {account}.");

            // Update flags and write to the database
            Logger.Trace($"Clearing flag {flag} for account {account}");
            account.Flags &= ~flag;
            DBManager.UpdateAccount(account);
            return (true, $"Successfully cleared flag {flag} from account {account}.");
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided email <see cref="string"/> is valid.
        /// </summary>
        private static bool ValidateEmail(string email)
        {
            return email.Length.IsWithin(1, 320);  // todo: add regex for email
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided player name <see cref="string"/> is valid.
        /// </summary>
        private static bool ValidatePlayerName(string playerName)
        {
            return Regex.Match(playerName, "^[a-zA-Z0-9]{1,16}$").Success;    // 1-16 alphanumeric characters
        }
        
        /// <summary>
        /// Returns <see langword="true"/> if the provided password <see cref="string"/> is valid.
        /// </summary>
        private static bool ValidatePassword(string password)
        {
            return password.Length.IsWithin(3, 64);
        }
    }
}
