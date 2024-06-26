using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu.Auth.Handlers
{
    public enum AuthWebApiOutputFormat
    {
        Html,
        Json
    }

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

            if (Enum.TryParse(request.QueryString["outputFormat"], true, out AuthWebApiOutputFormat outputFormat) == false)
                outputFormat = AuthWebApiOutputFormat.Html;

            // Parse query string body from POST requests
            NameValueCollection bodyQueryString = null;
            if (request.HttpMethod == "POST")
            {
                using (StreamReader reader = new(request.InputStream))
                    bodyQueryString = HttpUtility.ParseQueryString(reader.ReadToEnd());
            }

            // Handling
            switch (request.Url.LocalPath)
            {
                case "/AccountManagement/Create":   await OnAccountCreate(bodyQueryString, response, outputFormat); break;
                case "/ServerStatus":               await OnServerStatus(response, outputFormat); break;

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
        private async Task SendResponseAsync(ResponseData data, HttpListenerResponse response, AuthWebApiOutputFormat outputFormat)
        {
            string output = string.Empty;
            if (outputFormat == AuthWebApiOutputFormat.Html)
            {
                StringBuilder sb = new(ResponseHtml);
                sb.Replace("%RESPONSE_TITLE%", data.Title);
                sb.Replace("%RESPONSE_TEXT%", data.Text);
                output = sb.ToString();
            }
            else if (outputFormat == AuthWebApiOutputFormat.Json)
            {
                output = JsonSerializer.Serialize(data);
            }

            await SendTextAsync(output, response);
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
        private async Task<bool> OnAccountCreate(NameValueCollection bodyQueryString, HttpListenerResponse response, AuthWebApiOutputFormat outputFormat)
        {
            // Show account creation form when no parameters are specified in the query string
            if (bodyQueryString == null)
            {
                if (outputFormat == AuthWebApiOutputFormat.Html)
                    await SendTextAsync(AccountCreateFormHtml, response);
                else if (outputFormat == AuthWebApiOutputFormat.Json)
                    await SendResponseAsync(new(false, "Invalid Request", "This request does not support JSON output."), response, outputFormat);

                return true;
            }

            // Validate input
            bool inputIsValid = true;
            inputIsValid &= ValidateField(bodyQueryString["email"]);
            inputIsValid &= ValidateField(bodyQueryString["playerName"]);
            inputIsValid &= ValidateField(bodyQueryString["password"]);

            if (inputIsValid == false)
            {
                await SendResponseAsync(new(false, "Error", "Input is not valid."), response, outputFormat);
                return false;
            }

            (bool result, string text) = AccountManager.CreateAccount(bodyQueryString["email"].ToLower(), bodyQueryString["playerName"], bodyQueryString["password"]);
            if (HideSensitiveInformation == false) Logger.Trace(text);

            ResponseData responseData = new(result, result ? "Success" : "Error", text);

            await SendResponseAsync(responseData, response, outputFormat);
            return true;
        }

        /// <summary>
        /// Handles a server status web request.
        /// </summary>
        private async Task<bool> OnServerStatus(HttpListenerResponse response, AuthWebApiOutputFormat outputFormat)
        {
            string status = ServerManager.Instance.GetServerStatus();

            // Fix line breaks for display in browsers
            if (outputFormat == AuthWebApiOutputFormat.Html)
                status = status.Replace("\n", "<br/>");

            await SendResponseAsync(new(true, "Server Status", status), response, outputFormat);
            return true;
        }

        #endregion

        private readonly struct ResponseData
        {
            public bool Result { get; }
            public string Title { get; }
            public string Text { get; }

            public ResponseData(bool result, string title, string text)
            {
                Result = result;
                Title = title;
                Text = text;
            }
        }
    }
}
