using System.Collections.Specialized;
using System.Text;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Auth.WebApi
{
    public enum WebApiRequest
    {
        AccountCreate,
        ServerStatus
    }

    public class WebApiHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly string ResponseHtml;
        private readonly string AccountCreateFormHtml;

        public WebApiHandler()
        {
            string assetDirectory = Path.Combine(FileHelper.DataDirectory, "Auth");
            ResponseHtml = File.ReadAllText(Path.Combine(assetDirectory, "Response.html"));
            AccountCreateFormHtml = File.ReadAllText(Path.Combine(assetDirectory, "AccountCreateForm.html"));
        }

        public byte[] HandleRequest(WebApiRequest request, NameValueCollection queryString)
        {
            switch (request)
            {
                case WebApiRequest.AccountCreate: return HandleAccountCreateRequest(queryString);
                case WebApiRequest.ServerStatus: return HandleServerStatusRequest();
                default: Logger.Warn($"Unhandled request {request}"); return Array.Empty<byte>();
            }
        }

        private byte[] HandleAccountCreateRequest(NameValueCollection queryString)
        {
            // Show account creation form when no parameters are specified in the query string
            if (queryString == null) return Encoding.UTF8.GetBytes(AccountCreateFormHtml);

            // Check input
            if ((ValidateField(queryString["email"]) && ValidateField(queryString["playerName"]) && ValidateField(queryString["password"])) == false)
                return GenerateResponse("Error", "Input is not valid.");

            bool success = AccountManager.CreateAccount(queryString["email"].ToLower(), queryString["playerName"], queryString["password"], out string message);
            if (ConfigManager.PlayerManager.HideSensitiveInformation == false) Logger.Trace(message);
            return GenerateResponse(success ? "Success" : "Error", message);
        }

        private byte[] HandleServerStatusRequest()
        {
            return GenerateResponse("Server Status", ServerManager.Instance.GetServerStatus());
        }

        private byte[] GenerateResponse(string title, string text)
        {
            StringBuilder sb = new(ResponseHtml);
            sb.Replace("%RESPONSE_TITLE%", title);
            sb.Replace("%RESPONSE_TEXT%", text);
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static bool ValidateField(string field) => string.IsNullOrWhiteSpace(field) == false;
    }
}
