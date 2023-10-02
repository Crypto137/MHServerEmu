using MHServerEmu.Common;
using MHServerEmu.GameServer.Entities.Avatars;
using MHServerEmu.GameServer.Regions;

namespace MHServerEmu.GameServer.Frontend.Accounts.DBModels
{
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

        public DBAccount(string playerName, RegionPrototype region, AvatarPrototype avatar)
        {
            // Default account for using with BypassAuth
            Id = 0;
            Email = "default@account.mh";
            PlayerName = playerName;
            UserLevel = AccountUserLevel.Admin;

            InitializeData();

            Player.Region = region;
            Player.Avatar = avatar;
        }

        public DBAccount() { }

        public DBAvatar GetAvatar(AvatarPrototype prototype)
        {
            return Avatars.FirstOrDefault(avatar => avatar.Prototype == prototype);
        }

        private void InitializeData()
        {
            Player = new(Id);
            Avatars = Enum.GetValues(typeof(AvatarPrototype)).Cast<AvatarPrototype>().Select(prototype => new DBAvatar(Id, prototype)).ToArray();
        }
    }
}
