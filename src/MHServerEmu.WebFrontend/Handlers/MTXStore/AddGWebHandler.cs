using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;
using System.Collections.Specialized;

namespace MHServerEmu.WebFrontend.Handlers.MTXStore
{
    public class AddGWebHandler : WebHandler
    {
        private const string HtmlFileName = "add-g.html";

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string HtmlFilePath = Path.Combine(FileHelper.DataDirectory, "Web", "MTXStore", HtmlFileName);

        private byte[] _htmlData;

        public AddGWebHandler()
        {
            if (File.Exists(HtmlFilePath) == false)
            {
                Logger.Warn($"'{HtmlFileName}' not found, adding Gs via in-game UI will not work");
                _htmlData = Array.Empty<byte>();
                return;
            }

            _htmlData = File.ReadAllBytes(HtmlFilePath);
        }

        protected override async Task Post(WebRequestContext context)
        {
            // TODO
            NameValueCollection request = context.ReadQueryString();

            string downloader = request["downloader"];
            string token = request["token"];
            string email = request["email"];

            Logger.Debug($"Post(): downloader={downloader}, token={token}, email={email}");
            await context.SendAsync(_htmlData, "text/html");
        }
    }
}
