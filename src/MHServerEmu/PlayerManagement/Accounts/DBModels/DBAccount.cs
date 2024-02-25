using MHServerEmu.Common;
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
        public DBAvatar[] Avatars { get; set; }

        public DBAvatar CurrentAvatar { get => GetAvatar(Player.Avatar); }

        public DBAccount(string email, string playerName, string password, AccountUserLevel userLevel = AccountUserLevel.User)
        {
            Id = IdGenerator.Generate(IdType.Account);
            Email = email;
            PlayerName = playerName;
            PasswordHash = Cryptography.HashPassword(password, out byte[] salt);
            Salt = salt;
            UserLevel = userLevel;
            IsBanned = false;
            IsArchived = false;
            IsPasswordExpired = false;

            InitializeData();
        }

        public DBAccount(string playerName, RegionPrototypeId region, PrototypeId waypoint, AvatarPrototypeId avatar, int volume)
        {
            // Default account for using with BypassAuth
            Id = 0;
            Email = "default@account.mh";
            PlayerName = playerName;
            UserLevel = AccountUserLevel.Admin;

            InitializeData();

            Player.Region = region;
            Player.Waypoint = waypoint;
            Player.Avatar = avatar;
            Player.AOIVolume = volume;
        }

        public DBAccount() { }

        public DBAvatar GetAvatar(AvatarPrototypeId prototype)
        {
            return Avatars.FirstOrDefault(avatar => avatar.Prototype == prototype);
        }

        public override string ToString() => $"{PlayerName} ({Email})";

        private void InitializeData()
        {
            Player = new(Id);
            Avatars = Enum.GetValues(typeof(AvatarPrototypeId)).Cast<AvatarPrototypeId>().Select(prototype => new DBAvatar(Id, prototype)).ToArray();
        }
    }
}
