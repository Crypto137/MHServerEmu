using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System;

namespace MHServerEmu.DatabaseAccess.Models
{
    public enum AccountUserLevel : byte
    {
        User,
        Moderator,
        Admin
    }

    /// <summary>
    /// Represents an account stored in the account database.
    /// </summary>
    public class DBAccount
    {
        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;
        private static readonly IdGenerator IdGenerator = new(IdType.Player, 0);

        public long Id { get; set; }
        public string Email { get; set; }
        public string PlayerName { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] Salt { get; set; }
        public AccountUserLevel UserLevel { get; set; }
        public bool IsBanned { get; set; }
        public bool IsArchived { get; set; }
        public bool IsPasswordExpired { get; set; }

        public DBPlayer Player { get; set; }

        // NOTE: init is required for collection properties to be compatible with JSON serialization
        public DBEntityCollection Avatars { get; init; } = new();
        public DBEntityCollection TeamUps { get; init; } = new();
        public DBEntityCollection Items { get; init; } = new();
        public DBEntityCollection ControlledEntities { get; init; } = new();

        /// <summary>
        /// Constructs an empty <see cref="DBAccount"/> instance.
        /// </summary>
        public DBAccount() { }

        /// <summary>
        /// Constructs a <see cref="DBAccount"/> instance with the provided data.
        /// </summary>
        public DBAccount(string email, string playerName, string password, AccountUserLevel userLevel = AccountUserLevel.User)
        {
            Id = (long)IdGenerator.Generate();
            Email = email;
            PlayerName = playerName;
            PasswordHash = CryptographyHelper.HashPassword(password, out byte[] salt);
            Salt = salt;
            UserLevel = userLevel;
            IsBanned = false;
            IsArchived = false;
            IsPasswordExpired = false;
        }

        /// <summary>
        /// Constructs a default <see cref="DBAccount"/> instance with the provided data.
        /// </summary>
        public DBAccount(string playerName)
        {
            // Default account is used when BypassAuth is enabled
            Id = 0x2000000000000001;
            Email = "default@mhserveremu";
            PlayerName = playerName;
            UserLevel = AccountUserLevel.Admin;
        }

        public override string ToString()
        {
            string email = HideSensitiveInformation ? $"{Email[0]}****{Email.Substring(Email.IndexOf('@') - 1)}" : Email;
            return $"{PlayerName} (dbId=0x{Id:X}, email={email})";
        }

        public void ClearEntities()
        {
            Avatars.Clear();
            TeamUps.Clear();
            Items.Clear();
            ControlledEntities.Clear();
        }
    }
}
