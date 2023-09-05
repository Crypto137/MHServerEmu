using MHServerEmu.Common;
using MHServerEmu.GameServer.Frontend.Accounts;

namespace MHServerEmu.GameServer.Frontend
{
    public enum ClientDownloader
    {
        None,
        Robocopy,
        SolidState,
        Steam
    }

    public class ClientSession
    {
        public ulong Id { get; set; }
        public Account Account { get; }
        public ClientDownloader Downloader { get; private set; }
        public string Locale { get; private set; }

        public byte[] Key { get; set; }
        public byte[] Token { get; }
        public DateTime CreationDateTime { get; }

        public ClientSession(ulong id, Account account, string downloader, string locale)
        {
            Id = id;
            Account = account;
            Downloader = (ClientDownloader)Enum.Parse(typeof(ClientDownloader), downloader);
            Locale = locale;

            Key = Cryptography.GenerateAesKey();
            Token = Cryptography.GenerateToken();
            CreationDateTime = DateTime.Now;
        }
    }
}
