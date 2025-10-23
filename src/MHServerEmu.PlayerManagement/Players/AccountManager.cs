using System.Text.RegularExpressions;
using Gazillion;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.PlayerManagement.Auth;

namespace MHServerEmu.PlayerManagement.Players
{
    public enum AccountOperationResult
    {
        Success,
        GenericFailure,
        DatabaseError,
        EmailInvalid,
        EmailAlreadyUsed,
        EmailNotFound,
        PlayerNameInvalid,
        PlayerNameAlreadyUsed,
        PasswordInvalid,
        FlagAlreadySet,
        FlagNotSet,
    }

    /// <summary>
    /// Provides <see cref="DBAccount"/> management functions.
    /// </summary>
    public static partial class AccountManager
    {
        private const int EmailMaxLength = 320;
        private const int PasswordMinLength = 3;
        private const int PasswordMaxLength = 64;

        private static readonly Logger Logger = LogManager.CreateLogger();

        /// <summary>
        /// Queries a <see cref="DBAccount"/> using the provided <see cref="LoginDataPB"/> instance.
        /// <see cref="AuthStatusCode"/> indicates the outcome of the query.
        /// </summary>
        public static AuthStatusCode TryGetAccountByLoginDataPB(LoginDataPB loginDataPB, bool useWhitelist, out DBAccount account)
        {
            account = null;

            IDBManager dbManager = IDBManager.Instance;

            // Try to query an account to check
            string email = loginDataPB.EmailAddress.ToLower();
            if (dbManager.TryQueryAccountByEmail(email, out DBAccount accountToCheck) == false)
                return AuthStatusCode.IncorrectUsernameOrPassword403;

            // Check the account we queried if our DB manager requires it
            if (dbManager.VerifyAccounts)
            {
                if (CryptographyHelper.VerifyPassword(loginDataPB.Password, accountToCheck.PasswordHash, accountToCheck.Salt) == false)
                    return AuthStatusCode.IncorrectUsernameOrPassword403;

                if (accountToCheck.Flags.HasFlag(AccountFlags.IsBanned))
                    return AuthStatusCode.AccountBanned;
                
                if (accountToCheck.Flags.HasFlag(AccountFlags.IsArchived))
                    return AuthStatusCode.AccountArchived;
                
                if (accountToCheck.Flags.HasFlag(AccountFlags.IsPasswordExpired))
                    return AuthStatusCode.PasswordExpired;

                if (useWhitelist && accountToCheck.Flags.HasFlag(AccountFlags.IsWhitelisted) == false)
                    return AuthStatusCode.EmailNotVerified;
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
            return IDBManager.Instance.TryQueryAccountByEmail(email, out account);
        }

        public static bool LoadPlayerDataForAccount(DBAccount account)
        {
            return IDBManager.Instance.LoadPlayerData(account);
        }

        /// <summary>
        /// Creates a new <see cref="DBAccount"/> and inserts it into the database. Returns <see langword="true"/> if successful.
        /// </summary>
        public static AccountOperationResult CreateAccount(string email, string playerName, string password)
        {
            IDBManager dbManager = IDBManager.Instance;

            // Validate input before doing database queries
            if (ValidateEmail(email) == false)
                return AccountOperationResult.EmailInvalid;

            if (ValidatePlayerName(playerName) == false)
                return AccountOperationResult.PlayerNameInvalid;

            if (ValidatePassword(password) == false)
                return AccountOperationResult.PasswordInvalid;

            if (dbManager.TryQueryAccountByEmail(email, out _))
                return AccountOperationResult.EmailAlreadyUsed;

            if (dbManager.TryGetPlayerDbIdByName(playerName, out _, out _))
                return AccountOperationResult.PlayerNameAlreadyUsed;

            // Create a new account and insert it into the database
            DBAccount account = new(email, playerName, password);

            if (dbManager.InsertAccount(account) == false)
                return AccountOperationResult.DatabaseError;

            return AccountOperationResult.Success;
        }

        // TODO AccountOperationResult ChangeAccountEmail(string oldEmail, string newEmail)

        /// <summary>
        /// Changes the player name of the <see cref="DBAccount"/> with the specified email. Returns <see langword="true"/> if successful.
        /// </summary>
        public static AccountOperationResult ChangeAccountPlayerName(string email, string newPlayerName)
        {
            IDBManager dbManager = IDBManager.Instance;

            // Validate input before doing database queries
            if (ValidatePlayerName(newPlayerName) == false)
                return AccountOperationResult.PlayerNameInvalid;

            if (dbManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return AccountOperationResult.EmailNotFound;

            if (dbManager.TryGetPlayerDbIdByName(newPlayerName, out _, out _))
                return AccountOperationResult.PlayerNameAlreadyUsed;

            // Write the new name to the database
            string oldPlayerName = account.PlayerName;
            account.PlayerName = newPlayerName;
            dbManager.UpdateAccount(account);

            ServiceMessage.PlayerNameChanged playerNameChanged = new((ulong)account.Id, oldPlayerName, newPlayerName);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, playerNameChanged);
            ServerManager.Instance.SendMessageToService(GameServiceType.GroupingManager, playerNameChanged);

            return AccountOperationResult.Success;
        }

        /// <summary>
        /// Changes the password of the <see cref="DBAccount"/> with the specified email. Returns <see langword="true"/> if successful.
        /// </summary>
        public static AccountOperationResult ChangeAccountPassword(string email, string newPassword)
        {
            IDBManager dbManager = IDBManager.Instance;

            // Validate input before doing database queries
            if (ValidatePassword(newPassword) == false)
                return AccountOperationResult.PasswordInvalid;

            if (dbManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return AccountOperationResult.EmailNotFound;

            // Update the password and write the new hash/salt to the database
            account.PasswordHash = CryptographyHelper.HashPassword(newPassword, out byte[] salt);
            account.Salt = salt;
            account.Flags &= ~AccountFlags.IsPasswordExpired;
            dbManager.UpdateAccount(account);
            return AccountOperationResult.Success;
        }

        /// <summary>
        /// Changes the <see cref="AccountUserLevel"/> of the <see cref="DBAccount"/> with the specified email. Returns <see langword="true"/> if successful.
        /// </summary>
        public static AccountOperationResult SetAccountUserLevel(string email, AccountUserLevel userLevel)
        {
            IDBManager dbManager = IDBManager.Instance;

            // Make sure the specified account exists
            if (dbManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return AccountOperationResult.EmailNotFound;

            // Write the new user level to the database
            account.UserLevel = userLevel;
            dbManager.UpdateAccount(account);
            return AccountOperationResult.Success;
        }

        /// <summary>
        /// Sets the specified <see cref="AccountFlags"/> for the <see cref="DBAccount"/> with the provided email.
        /// </summary>
        public static AccountOperationResult SetFlag(string email, AccountFlags flag)
        {
            if (IDBManager.Instance.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return AccountOperationResult.EmailNotFound;

            return SetFlag(account, flag);
        }

        /// <summary>
        /// Sets the specified <see cref="AccountFlags"/> for the provided <see cref="DBAccount"/>.
        /// </summary>
        public static AccountOperationResult SetFlag(DBAccount account, AccountFlags flag)
        {
            if (account.Flags.HasFlag(flag))
                return AccountOperationResult.FlagAlreadySet;

            // Update flags and write to the database
            Logger.Trace($"Setting flag {flag} for account {account}");
            account.Flags |= flag;
            IDBManager.Instance.UpdateAccount(account);
            return AccountOperationResult.Success;
        }

        /// <summary>
        /// Clears the specified <see cref="AccountFlags"/> for the <see cref="DBAccount"/> with the provided email.
        /// </summary>
        public static AccountOperationResult ClearFlag(string email, AccountFlags flag)
        {
            if (IDBManager.Instance.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return AccountOperationResult.EmailNotFound;

            return ClearFlag(account, flag);
        }

        /// <summary>
        /// Clears the specified <see cref="AccountFlags"/> for the provided <see cref="DBAccount"/>.
        /// </summary>
        public static AccountOperationResult ClearFlag(DBAccount account, AccountFlags flag)
        {
            if (account.Flags.HasFlag(flag) == false)
                return AccountOperationResult.FlagNotSet;

            // Update flags and write to the database
            Logger.Trace($"Clearing flag {flag} for account {account}");
            account.Flags &= ~flag;
            IDBManager.Instance.UpdateAccount(account);
            return AccountOperationResult.Success;
        }

        public static string GetOperationResultString(AccountOperationResult result, string email = null, string playerName = null)
        {
            switch (result)
            {
                case AccountOperationResult.Success:
                    return $"Created account {email} ({playerName}).";

                case AccountOperationResult.EmailInvalid:
                    return $"'{email}' is not a valid email address.";

                case AccountOperationResult.EmailAlreadyUsed:
                    return $"Email {email} is already used by another account.";

                case AccountOperationResult.EmailNotFound:
                    return $"Account with email {email} not found.";

                case AccountOperationResult.PlayerNameInvalid:
                    return "Names may contain only up to 16 alphanumeric characters.";

                case AccountOperationResult.PlayerNameAlreadyUsed:
                    return $"Name {playerName} is already used by another account.";

                case AccountOperationResult.PasswordInvalid:
                    return "Password must between 3 and 64 characters long.";

                default:
                    return result.ToString();
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided email <see cref="string"/> is valid.
        /// </summary>
        private static bool ValidateEmail(string email)
        {
            if (email.Length > EmailMaxLength)
                return false;

            // Validate like the client does on the login screen.
            int atIndex = email.IndexOf('@');
            if (atIndex == -1)
                return false;

            int dotIndex = email.LastIndexOf('.');
            if (dotIndex < atIndex)
                return false;

            int topDomainLength = email.Length - (dotIndex + 1);
            if (topDomainLength < 2)
                return false;

            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided player name <see cref="string"/> is valid.
        /// </summary>
        private static bool ValidatePlayerName(string playerName)
        {
            return GetPlayerNameRegex().Match(playerName).Success;    // 1-16 alphanumeric characters
        }
        
        /// <summary>
        /// Returns <see langword="true"/> if the provided password <see cref="string"/> is valid.
        /// </summary>
        private static bool ValidatePassword(string password)
        {
            return password.Length.IsWithin(PasswordMinLength, PasswordMaxLength);
        }

        [GeneratedRegex(@"^[a-zA-Z0-9]{1,16}$")]
        private static partial Regex GetPlayerNameRegex();
    }
}
