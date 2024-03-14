using System.Text;
using MHServerEmu.Core.Helpers;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Frontend;

namespace MHServerEmu.PlayerManagement
{
    public enum ClientDownloader
    {
        None,
        Robocopy,
        SolidState,
        Steam
    }

    public class ClientSession : IFrontendSession
    {
        public ulong Id { get; set; }
        public DBAccount Account { get; }

        public ClientDownloader Downloader { get; private set; }
        public string Locale { get; private set; }

        public byte[] Key { get; set; }
        public byte[] Token { get; }
        public DateTime CreationTime { get; }

        public ClientSession(ulong id, DBAccount account, string downloader, string locale)
        {
            Id = id;
            Account = account;
            Downloader = Enum.Parse<ClientDownloader>(downloader);
            Locale = locale;

            Key = CryptographyHelper.GenerateAesKey();
            Token = CryptographyHelper.GenerateToken();
            CreationTime = DateTime.Now;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"SessionId: {Id}");
            sb.AppendLine($"Account: {Account}");
            sb.AppendLine($"Downloader: {Downloader}");
            sb.AppendLine($"Locale: {Locale}");
            sb.AppendLine($"Online Time: {DateTime.Now - CreationTime:hh\\:mm\\:ss}");
            return sb.ToString();
        }
    }
}
