using System.Text.Json.Serialization;
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

        public static readonly IdGenerator IdGenerator = new(IdType.Player, 0);

        public ulong Id { get; set; }
        public string Email { get; set; }
        public string PlayerName { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] Salt { get; set; }
        public AccountUserLevel UserLevel { get; set; }
        public bool IsBanned { get; set; }
        public bool IsArchived { get; set; }
        public bool IsPasswordExpired { get; set; }

        public DBPlayer Player { get; set; }
        [JsonInclude]
        public Dictionary<long, DBAvatar> Avatars { get; private set; } = new();

        [JsonIgnore]
        public DBAvatar CurrentAvatar { get => GetAvatar(Player.RawAvatar); }

        [JsonIgnore]
        public List<DBEntity> TEMP_ItemList { get; } = new();

        /// <summary>
        /// Constructs an empty <see cref="DBAccount"/> instance.
        /// </summary>
        public DBAccount() { }

        /// <summary>
        /// Constructs a <see cref="DBAccount"/> instance with the provided data.
        /// </summary>
        public DBAccount(string email, string playerName, string password, AccountUserLevel userLevel = AccountUserLevel.User)
        {
            Id = IdGenerator.Generate();
            Email = email;
            PlayerName = playerName;
            PasswordHash = CryptographyHelper.HashPassword(password, out byte[] salt);
            Salt = salt;
            UserLevel = userLevel;
            IsBanned = false;
            IsArchived = false;
            IsPasswordExpired = false;

            InitializeData();
        }

        /// <summary>
        /// Constructs a default <see cref="DBAccount"/> instance with the provided data.
        /// </summary>
        public DBAccount(string playerName, long region, long waypoint, long avatar, int volume)
        {
            // Default account is used when BypassAuth is enabled
            Id = 0x2000000000000001;
            Email = "default@mhserveremu";
            PlayerName = playerName;
            UserLevel = AccountUserLevel.Admin;

            InitializeData();

            Player.RawRegion = region;
            Player.RawWaypoint = waypoint;
            Player.RawAvatar = avatar;
            Player.AOIVolume = volume;
        }

        /// <summary>
        /// Retrieves the <see cref="DBAvatar"/> for the specified prototype id.
        /// </summary>
        public DBAvatar GetAvatar(long prototypeId)
        {
            if (Avatars.TryGetValue(prototypeId, out DBAvatar avatar) == false)
            {
                avatar = new(Id, prototypeId);
                Avatars.Add(prototypeId, avatar);
            }

            return avatar;
        }

        public override string ToString()
        {
            string email = HideSensitiveInformation ? $"{Email[0]}****{Email.Substring(Email.IndexOf('@') - 1)}" : Email;
            return $"{PlayerName} (dbId=0x{Id:X}, email={email})";
        }

        private void InitializeData()
        {
            Player = new(Id);
        }
    }
}
