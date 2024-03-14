using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
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
        public Dictionary<long, DBAvatar> Avatars { get; private set; } = new();

        public DBAvatar CurrentAvatar { get => GetAvatar(Player.RawAvatar); }

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

        public DBAccount(string playerName, long region, long waypoint, long avatar, int volume)
        {
            // Default account for using with BypassAuth
            Id = 0;
            Email = "default@account.mh";
            PlayerName = playerName;
            UserLevel = AccountUserLevel.Admin;

            InitializeData();

            Player.RawRegion = region;
            Player.RawWaypoint = waypoint;
            Player.RawAvatar = avatar;
            Player.AOIVolume = volume;
        }

        public DBAccount() { }

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
            if (ConfigManager.PlayerManager.HideSensitiveInformation)
            {
                string maskedEmail = $"{Email[0]}****{Email.Substring(Email.IndexOf('@') - 1)}";
                return $"{PlayerName} ({maskedEmail})";
            }

            return $"{PlayerName} ({Email})";
        }

        private void InitializeData()
        {
            Player = new(Id);
        }
    }
}
