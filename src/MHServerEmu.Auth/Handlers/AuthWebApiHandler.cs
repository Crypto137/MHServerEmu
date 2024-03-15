using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Auth.Handlers
{
    /// <summary>
    /// Handler for web API requests sent to the <see cref="AuthServer"/>.
    /// </summary>
    public class AuthWebApiHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        private readonly string ResponseHtml;
        private readonly string AccountCreateFormHtml;

        /// <summary>
        /// Constructs a new <see cref="AuthWebApiHandler"/> instance.
        /// </summary>
        public AuthWebApiHandler()
        {
            string assetDirectory = Path.Combine(FileHelper.DataDirectory, "Auth");
            ResponseHtml = File.ReadAllText(Path.Combine(assetDirectory, "Response.html"));
            AccountCreateFormHtml = File.ReadAllText(Path.Combine(assetDirectory, "AccountCreateForm.html"));
        }

        /// <summary>
        /// Receives and handles a web API request.
        /// </summary>
        public async Task HandleRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Mask end point name if needed
            string endPointName = HideSensitiveInformation
                ? request.RemoteEndPoint.ToStringMasked()
                : request.RemoteEndPoint.ToString();

            // Parse query string from POST requests
            NameValueCollection queryString = null;
            if (request.HttpMethod == "POST")
            {
                using (StreamReader reader = new(request.InputStream))
                    queryString = HttpUtility.ParseQueryString(reader.ReadToEnd());
            }

            // Handling
            switch (request.Url.LocalPath)
            {
                case "/AccountManagement/Create":   await OnAccountCreate(queryString, response); break;
                case "/ServerStatus":               await OnServerStatus(response); break;

                default:
                    Logger.Warn($"HandleRequestAsync(): Unhandled web API request\nRequest: {request.Url.LocalPath}\nRemoteEndPoint: {endPointName}\nUserAgent: {request.UserAgent}");
                    break;
            }
        }

        /// <summary>
        /// Sends a string as an <see cref="HttpListenerResponse"/>.
        /// </summary>
        private async Task SendTextAsync(string text, HttpListenerResponse response)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            await response.OutputStream.WriteAsync(buffer);
        }

        /// <summary>
        /// Formats a response as an html page and sends it as an <see cref="HttpListenerResponse"/>.
        /// </summary>
        private async Task SendResponseAsync(string title, string text, HttpListenerResponse response)
        {
            StringBuilder sb = new(ResponseHtml);
            sb.Replace("%RESPONSE_TITLE%", title);
            sb.Replace("%RESPONSE_TEXT%", text);
            await SendTextAsync(sb.ToString(), response);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the provided <see cref="string"/> field is valid.
        /// </summary>
        private static bool ValidateField(string field)
        {
            return string.IsNullOrWhiteSpace(field) == false;
        }

        #region Request Handling

        /// <summary>
        /// Handles an account creation web request.
        /// </summary>
        private async Task<bool> OnAccountCreate(NameValueCollection queryString, HttpListenerResponse response)
        {
            // Show account creation form when no parameters are specified in the query string
            if (queryString == null)
            {
                await SendTextAsync(AccountCreateFormHtml, response);
                return true;
            }

            // Validate input
            bool inputIsValid = true;
            inputIsValid &= ValidateField(queryString["email"]);
            inputIsValid &= ValidateField(queryString["playerName"]);
            inputIsValid &= ValidateField(queryString["password"]);

            if (inputIsValid == false)
            {
                await SendResponseAsync("Error", "Input is not valid.", response);
                return false;
            }

            bool success = AccountManager.CreateAccount(queryString["email"].ToLower(), queryString["playerName"], queryString["password"], out string message);
            if (HideSensitiveInformation == false) Logger.Trace(message);

            await SendResponseAsync(success ? "Success" : "Error", message, response);
            return true;
        }

        /// <summary>
        /// Handles a server status web request.
        /// </summary>
        private async Task<bool> OnServerStatus(HttpListenerResponse response)
        {
            string status = ServerManager.Instance.GetServerStatus();
            status = status.Replace("\n", "<br/>");     // Fix line breaks for display in browsers

            await SendResponseAsync("Server Status", status, response);
            return true;
        }

        #endregion
    }
}
