using System.Collections.Specialized;
using System.Net;
using System.Text;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend.Handlers.MTXStore
{
    public class AddGWebHandler : WebHandler
    {
        private const string HtmlTemplateFileName = "add-g.html";

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string HtmlTemplateFilePath = Path.Combine(FileHelper.DataDirectory, "Web", "MTXStore", HtmlTemplateFileName);

        private string _htmlTemplate;

        public AddGWebHandler()
        {
            Load();
        }

        public void Load()
        {
            if (File.Exists(HtmlTemplateFilePath) == false)
            {
                Logger.Warn($"Load(): '{HtmlTemplateFileName}' not found, adding Gs via in-game UI will not work");
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

            ServiceMessage.MTXStoreESBalanceResponse balanceResponse = await GameServiceTaskManager.Instance.GetESBalanceAsync(email, token);

            if (balanceResponse.StatusCode != (int)HttpStatusCode.OK)
            {
                context.StatusCode = balanceResponse.StatusCode;
                return;
            }

            StringBuilder sb = new(_htmlTemplate);
            sb.Replace("%DOWNLOADER%", downloader);
            sb.Replace("%TOKEN%", token);
            sb.Replace("%EMAIL%", email);
            sb.Replace("%CURRENT_BALANCE%", $"{balanceResponse.CurrentBalance}");
            sb.Replace("%CONVERSION_RATIO%", $"{balanceResponse.ConversionRatio:0.00}");
            sb.Replace("%CONVERSION_STEP%", $"{balanceResponse.ConversionStep}");
            string html = sb.ToString();

            await context.SendAsync(html, "text/html");
        }
    }
}
