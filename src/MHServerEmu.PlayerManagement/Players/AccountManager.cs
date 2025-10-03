using System.Text.RegularExpressions;
using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.PlayerManagement.Network;

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
    public static class AccountManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static bool _useWhitelist;

        public static IDBManager DBManager { get; private set; }

        /// <summary>
        /// Initializes <see cref="AccountManager"/>.
        /// </summary>
        public static bool Initialize()
        {
            DBManager = IDBManager.Instance;

            PlayerManagerConfig config = ConfigManager.Instance.GetConfig<PlayerManagerConfig>();
            _useWhitelist = config.UseWhitelist;

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

                if (_useWhitelist && accountToCheck.Flags.HasFlag(AccountFlags.IsWhitelisted) == false)
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
            return DBManager.TryQueryAccountByEmail(email, out account);
        }

        public static bool LoadPlayerDataForAccount(DBAccount account)
        {
            return DBManager.LoadPlayerData(account);
        }

        /// <summary>
        /// Creates a new <see cref="DBAccount"/> and inserts it into the database. Returns <see langword="true"/> if successful.
        /// </summary>
        public static AccountOperationResult CreateAccount(string email, string playerName, string password)
        {
            // Validate input before doing database queries
            if (ValidateEmail(email) == false)
                return AccountOperationResult.EmailInvalid;

            if (ValidatePlayerName(playerName) == false)
                return AccountOperationResult.PlayerNameInvalid;

            if (ValidatePassword(password) == false)
                return AccountOperationResult.PasswordInvalid;

            if (DBManager.TryQueryAccountByEmail(email, out _))
                return AccountOperationResult.EmailAlreadyUsed;

            if (DBManager.TryGetPlayerDbIdByName(playerName, out _, out _))
                return AccountOperationResult.PlayerNameAlreadyUsed;

            // Create a new account and insert it into the database
            DBAccount account = new(email, playerName, password);

            if (DBManager.InsertAccount(account) == false)
                return AccountOperationResult.DatabaseError;

            return AccountOperationResult.Success;
        }

        // TODO AccountOperationResult ChangeAccountEmail(string oldEmail, string newEmail)

        /// <summary>
        /// Changes the player name of the <see cref="DBAccount"/> with the specified email. Returns <see langword="true"/> if successful.
        /// </summary>
        public static AccountOperationResult ChangeAccountPlayerName(string email, string newPlayerName)
        {
            // Validate input before doing database queries
            if (ValidatePlayerName(newPlayerName) == false)
                return AccountOperationResult.PlayerNameInvalid;

            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return AccountOperationResult.EmailNotFound;

            if (DBManager.TryGetPlayerDbIdByName(newPlayerName, out _, out _))
                return AccountOperationResult.PlayerNameAlreadyUsed;

            // Write the new name to the database
            string oldPlayerName = account.PlayerName;
            account.PlayerName = newPlayerName;
            DBManager.UpdateAccount(account);

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
            // Validate input before doing database queries
            if (ValidatePassword(newPassword) == false)
                return AccountOperationResult.PasswordInvalid;

            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return AccountOperationResult.EmailNotFound;

            // Update the password and write the new hash/salt to the database
            account.PasswordHash = CryptographyHelper.HashPassword(newPassword, out byte[] salt);
            account.Salt = salt;
            account.Flags &= ~AccountFlags.IsPasswordExpired;
            DBManager.UpdateAccount(account);
            return AccountOperationResult.Success;
        }

        /// <summary>
        /// Changes the <see cref="AccountUserLevel"/> of the <see cref="DBAccount"/> with the specified email. Returns <see langword="true"/> if successful.
        /// </summary>
        public static AccountOperationResult SetAccountUserLevel(string email, AccountUserLevel userLevel)
        {
            // Make sure the specified account exists
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
                return AccountOperationResult.EmailNotFound;

            // Write the new user level to the database
            account.UserLevel = userLevel;
            DBManager.UpdateAccount(account);
            return AccountOperationResult.Success;
        }

        /// <summary>
        /// Sets the specified <see cref="AccountFlags"/> for the <see cref="DBAccount"/> with the provided email.
        /// </summary>
        public static AccountOperationResult SetFlag(string email, AccountFlags flag)
        {
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
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
            DBManager.UpdateAccount(account);
            return AccountOperationResult.Success;
        }

        /// <summary>
        /// Clears the specified <see cref="AccountFlags"/> for the <see cref="DBAccount"/> with the provided email.
        /// </summary>
        public static AccountOperationResult ClearFlag(string email, AccountFlags flag)
        {
            if (DBManager.TryQueryAccountByEmail(email, out DBAccount account) == false)
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
            DBManager.UpdateAccount(account);
            return AccountOperationResult.Success;
        }

        public static string GetOperationResultString(AccountOperationResult result, string email = null, string playerName = null)
        {
            switch (result)
            {
                case AccountOperationResult.EmailInvalid:
                    return "Email must not be longer than 320 characters.";

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
