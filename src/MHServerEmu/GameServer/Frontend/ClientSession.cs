using MHServerEmu.Common;
using MHServerEmu.GameServer.Frontend.Accounts;

namespace MHServerEmu.GameServer.Frontend
{
    public class ClientSession
    {
        public ulong Id { get; set; }
        public byte[] Key { get; set; }
        public byte[] Token { get; }
        public Account Account { get; }

        public ClientSession(ulong id)
        {
            Id = id;
            Key = Cryptography.GenerateAesKey();
            Token = Cryptography.GenerateToken();
        }

        public ClientSession(ulong id, Account account)
        {
            Id = id;
            Key = Cryptography.GenerateAesKey();
            Token = Cryptography.GenerateToken();
            Account = account;
        }
    }
}
