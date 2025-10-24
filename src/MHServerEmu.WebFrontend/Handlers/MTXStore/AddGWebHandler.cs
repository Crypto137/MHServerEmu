using System.Collections.Specialized;
using System.Net;
using System.Text;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers.MTXStore
{
    public class AddGWebHandler : WebHandler
    {
        private const string HtmlTemplateFileName = "add-g.html";

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string HtmlTemplateFilePath = Path.Combine(FileHelper.DataDirectory, "Web", "MTXStore", HtmlTemplateFileName);

        private readonly string _htmlTemplate;

        public AddGWebHandler()
        {
            if (File.Exists(HtmlTemplateFilePath) == false)
            {
                Logger.Warn($"'{HtmlTemplateFileName}' not found, adding Gs via in-game UI will not work");
                _htmlTemplate = string.Empty;
                return;
            }

            _htmlTemplate = File.ReadAllText(HtmlTemplateFilePath);
        }

        protected override Task Get(WebRequestContext context)
        {
            // It seems the client sends a GET when it initializes the embedded browser, but it doesn't seem to be needed for anything.
            return Task.CompletedTask;
        }

        protected override async Task Post(WebRequestContext context)
        {
            if (string.IsNullOrWhiteSpace(_htmlTemplate))
            {
                context.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }

            NameValueCollection request = await context.ReadQueryStringAsync();

            string downloader = request["downloader"];
            string token = request["token"];
            string email = request["email"];

            // TODO: Verify downloader/token/email
            //Logger.Debug($"Post(): downloader={downloader}, token={token}, email={email}");

            StringBuilder sb = new(_htmlTemplate);
            sb.Replace("%REQUEST_DOWNLOADER%", downloader);
            sb.Replace("%REQUEST_TOKEN%", token);
            sb.Replace("%REQUEST_EMAIL%", email);
            string html = sb.ToString();

            await context.SendAsync(html, "text/html");
        }
    }
}
