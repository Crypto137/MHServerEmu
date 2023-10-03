using System.Text;
using MHServerEmu.Common;
using MHServerEmu.GameServer.Frontend.Accounts.DBModels;

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
            Downloader = (ClientDownloader)Enum.Parse(typeof(ClientDownloader), downloader);
            Locale = locale;

            Key = Cryptography.GenerateAesKey();
            Token = Cryptography.GenerateToken();
            CreationTime = DateTime.Now;
        }

        public override string ToString()
        {
            using (MemoryStream stream = new())
            using (StreamWriter writer = new(stream))
            {
                writer.WriteLine($"SessionId: {Id}");
                writer.WriteLine($"Account: {Account.Email} ({Account.PlayerName})");
                writer.WriteLine($"Downloader: {Downloader}");
                writer.WriteLine($"Locale: {Locale}");
                writer.WriteLine($"Online Time: {DateTime.Now - CreationTime:hh\\:mm\\:ss}");

                writer.Flush();
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}
