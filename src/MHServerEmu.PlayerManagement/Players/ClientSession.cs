using System.Text;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement.Players
{
    public enum ClientDownloader
    {
        None,
        Robocopy,
        SolidState,
        Steam
    }

    /// <summary>
    /// An implementation of <see cref="IFrontendSession"/> used by the <see cref="PlayerManagerService"/>.
    /// </summary>
    public class ClientSession : IFrontendSession
    {
        public ulong Id { get; set; }
        public object Account { get; set; }

        public ClientDownloader Downloader { get; private set; }
        public string Locale { get; private set; }

        public byte[] Key { get; set; }
        public byte[] Token { get; }
        public TimeSpan CreationTime { get; }

        public TimeSpan Length { get => Clock.UnixTime - CreationTime; }

        /// <summary>
        /// Constructs a new <see cref="ClientSession"/> with the provided data.
        /// </summary>
        public ClientSession(ulong id, DBAccount account, ClientDownloader downloader, string locale)
        {
            Id = id;
            Account = account;

            Downloader = downloader;
            Locale = locale;

            Key = CryptographyHelper.GenerateAesKey();
            Token = CryptographyHelper.GenerateToken();
            CreationTime = Clock.UnixTime;
        }

        public override string ToString()
        {
            return $"SessionId=0x{Id:X}";
        }

        public string GetClientInfo()
        {
            StringBuilder sb = new();
            sb.AppendLine($"SessionId: 0x{Id:X}");
            sb.AppendLine($"Account: {Account}");
            sb.AppendLine($"Downloader: {Downloader}");
            sb.AppendLine($"Locale: {Locale}");
            sb.AppendLine($"Length: {Length:hh\\:mm\\:ss}");
            return sb.ToString();
        }
    }
}
