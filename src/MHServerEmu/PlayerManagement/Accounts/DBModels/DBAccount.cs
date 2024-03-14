using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.System;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.PlayerManagement.Accounts.DBModels
{
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
        public Dictionary<PrototypeId, DBAvatar> Avatars { get; private set; } = new();

        public DBAvatar CurrentAvatar { get => GetAvatar((PrototypeId)Player.Avatar); }

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

        public DBAccount(string playerName, PrototypeId region, PrototypeId waypoint, AvatarPrototypeId avatar, int volume)
        {
            // Default account for using with BypassAuth
            Id = 0;
            Email = "default@account.mh";
            PlayerName = playerName;
            UserLevel = AccountUserLevel.Admin;

            InitializeData();

            Player.Region = region;
            Player.Waypoint = waypoint;
            Player.Avatar = (PrototypeId)avatar;
            Player.AOIVolume = volume;
        }

        public DBAccount() { }

        /// <summary>
        /// Retrieves the <see cref="DBAvatar"/> for the specified <see cref="PrototypeId"/>.
        /// </summary>
        public DBAvatar GetAvatar(PrototypeId prototypeId)
        {
            if (Avatars.TryGetValue(prototypeId, out DBAvatar avatar) == false)
            {
                avatar = new(Id, (AvatarPrototypeId)prototypeId);
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
            foreach (AvatarPrototypeId avatarPrototypeId in Enum.GetValues<AvatarPrototypeId>())
                GetAvatar((PrototypeId)avatarPrototypeId);
        }
    }
}
