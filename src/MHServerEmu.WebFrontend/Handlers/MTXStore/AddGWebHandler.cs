using System.Collections.Specialized;
using System.Net;
using System.Text;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers.MTXStore
{
    public class AddGWebHandler : WebHandler
    {
        private const string HtmlTemplateFileName = "add-g.html";
        private const int AuthTimeoutMS = 15000;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string HtmlTemplateFilePath = Path.Combine(FileHelper.DataDirectory, "Web", "MTXStore", HtmlTemplateFileName);

        private readonly WebTaskManager<ServiceMessage.MTXStoreAuthResponse> _authTaskManager;
        private readonly string _htmlTemplate;

        public AddGWebHandler(WebTaskManager<ServiceMessage.MTXStoreAuthResponse> authTaskManager)
        {
            if (File.Exists(HtmlTemplateFilePath) == false)
            {
                Logger.Warn($"'{HtmlTemplateFileName}' not found, adding Gs via in-game UI will not work");
                _htmlTemplate = string.Empty;
                return;
            }

            _authTaskManager = authTaskManager;
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

            Task<ServiceMessage.MTXStoreAuthResponse> authTask = _authTaskManager.CreateTask(out ulong requestId);
            
            ServiceMessage.MTXStoreAuthRequest authRequest = new(requestId, email, token);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, authRequest);

            await Task.WhenAny(authTask, Task.Delay(TimeSpan.FromMilliseconds(AuthTimeoutMS)));
            
            if (authTask.IsCompletedSuccessfully == false)
            {
                Logger.Warn($"Post(): Timeout for request {requestId}");
                _authTaskManager.CancelTask(requestId);
                context.StatusCode = (int)HttpStatusCode.RequestTimeout;
                return;
            }

            ServiceMessage.MTXStoreAuthResponse authResponse = authTask.Result;
            if (authResponse.IsSuccess == false)
            {
                context.StatusCode = (int)HttpStatusCode.Forbidden;
                return;
            }

            StringBuilder sb = new(_htmlTemplate);
            sb.Replace("%REQUEST_DOWNLOADER%", downloader);
            sb.Replace("%REQUEST_TOKEN%", token);
            sb.Replace("%REQUEST_EMAIL%", email);
            sb.Replace("%REQUEST_CURRENT_BALANCE%", $"{authResponse.CurrentBalance}");
            sb.Replace("%REQUEST_CONVERSION_RATIO%", $"{authResponse.ConversionRatio:0.00}");
            string html = sb.ToString();

            await context.SendAsync(html, "text/html");
        }
    }
}
