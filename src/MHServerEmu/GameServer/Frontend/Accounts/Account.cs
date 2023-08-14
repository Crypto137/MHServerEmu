using MHServerEmu.Common;
using System.Text.Json.Serialization;

namespace MHServerEmu.GameServer.Frontend.Accounts
{
    public class Account
    {
        public ulong Id { get; set; }
        public string Email { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] Salt { get; set; }
        public byte UserLevel { get; set; }
        public bool IsBanned { get; set; }
        public bool IsArchived { get; set; }
        public bool IsPasswordExpired { get; set; }

        public Account(ulong id, string email, string password)
        {
            Id = id;
            Email = email;
            PasswordHash = Cryptography.HashPassword(password, out byte[] salt);
            Salt = salt;
            UserLevel = 0;
            IsBanned = false;
            IsArchived = false;
            IsPasswordExpired = false;
        }

        [JsonConstructor]
        public Account(ulong id, string email, byte[] passwordHash, byte[] salt, byte userLevel, bool isBanned, bool isArchived, bool isPasswordExpired)
        {
            Id = id;
            Email = email;
            PasswordHash = passwordHash;
            Salt = salt;
            UserLevel = userLevel;
            IsBanned = isBanned;
            IsArchived = isArchived;
            IsPasswordExpired = isPasswordExpired;
        }
    }
}
