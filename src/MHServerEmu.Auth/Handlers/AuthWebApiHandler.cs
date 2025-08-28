using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Metrics;
using MHServerEmu.Core.Network;
using MHServerEmu.PlayerManagement;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Regions;

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
        public async Task HandleRequestAsync(HttpListenerRequest httpRequest, HttpListenerResponse httpResponse)
        {
            // Mask end point name if needed
            string endPointName = HideSensitiveInformation
                ? httpRequest.RemoteEndPoint.ToStringMasked()
                : httpRequest.RemoteEndPoint.ToString();

            if (Enum.TryParse(httpRequest.QueryString["outputFormat"], true, out AuthWebApiOutputFormat outputFormat) == false)
                outputFormat = AuthWebApiOutputFormat.Html;

            // Parse query string body from POST requests
            NameValueCollection bodyQueryString = null;
            if (httpRequest.HttpMethod == "POST")
            {
                using (StreamReader reader = new(httpRequest.InputStream))
                    bodyQueryString = HttpUtility.ParseQueryString(reader.ReadToEnd());
            }

            // Handling
            switch (httpRequest.Url.LocalPath)
            {
                case "/AccountManagement/Create":   await OnAccountCreate(bodyQueryString, httpResponse, outputFormat); break;
                case "/ServerStatus":               await OnServerStatus(httpResponse, outputFormat); break;
                case "/RegionReport":               await OnRegionReport(httpResponse, outputFormat); break;
                case "/Metrics/Performance":        await OnMetricsPerformance(httpResponse, outputFormat); break;

                default:
                    Logger.Warn($"HandleRequestAsync(): Unhandled web API request\nRequest: {httpRequest.Url.LocalPath}\nRemoteEndPoint: {endPointName}\nUserAgent: {httpRequest.UserAgent}");
                    break;
            }
        }

        /// <summary>
        /// Sends <see cref="ResponseData"/> as an <see cref="HttpListenerResponse"/> using the specified <see cref="AuthWebApiOutputFormat"/>.
        /// </summary>
        private async Task SendResponseAsync(ResponseData responseData, HttpListenerResponse httpResponse, AuthWebApiOutputFormat outputFormat)
        {
            if (outputFormat == AuthWebApiOutputFormat.Html)
                await HttpHelper.SendHtmlAsync(httpResponse, FormatResponseDataHtml(responseData));
            else if (outputFormat == AuthWebApiOutputFormat.Json)
                await HttpHelper.SendPlainTextAsync(httpResponse, JsonSerializer.Serialize(responseData));
            else
                Logger.Warn($"SendResponseAsync(): Unsupported output format {outputFormat}");
        }

        /// <summary>
        /// Formats <see cref="ResponseData"> as an html page.
        /// </summary>
        private string FormatResponseDataHtml(ResponseData responseData)
        {
            StringBuilder sb = new(ResponseHtml);
            sb.Replace("%RESPONSE_TITLE%", responseData.Title);
            sb.Replace("%RESPONSE_TEXT%", responseData.Text);
            return sb.ToString();
        }

        #region Request Handling

        /// <summary>
        /// Handles an account creation web request.
        /// </summary>
        private async Task<bool> OnAccountCreate(NameValueCollection bodyQueryString, HttpListenerResponse httpResponse, AuthWebApiOutputFormat outputFormat)
        {
            // Show account creation form when no parameters are specified in the query string
            if (bodyQueryString == null)
            {
                if (outputFormat == AuthWebApiOutputFormat.Html)
                    await HttpHelper.SendHtmlAsync(httpResponse, AccountCreateFormHtml);
                else if (outputFormat == AuthWebApiOutputFormat.Json)
                    await SendResponseAsync(new(false, "Invalid Request", "This request does not support JSON output."), httpResponse, outputFormat);

                return true;
            }

            // Validate input
            bool inputIsValid = true;
            inputIsValid &= string.IsNullOrWhiteSpace(bodyQueryString["email"]) == false;
            inputIsValid &= string.IsNullOrWhiteSpace(bodyQueryString["playerName"]) == false;
            inputIsValid &= string.IsNullOrWhiteSpace(bodyQueryString["password"]) == false;

            if (inputIsValid == false)
            {
                await SendResponseAsync(new(false, "Error", "Input is not valid."), httpResponse, outputFormat);
                return false;
            }

            (bool result, string text) = AccountManager.CreateAccount(bodyQueryString["email"].ToLower(), bodyQueryString["playerName"], bodyQueryString["password"]);
            if (HideSensitiveInformation == false) Logger.Trace(text);

            ResponseData responseData = new(result, result ? "Success" : "Error", text);

            await SendResponseAsync(responseData, httpResponse, outputFormat);
            return true;
        }

        /// <summary>
        /// Handles a server status web request.
        /// </summary>
        private async Task<bool> OnServerStatus(HttpListenerResponse httpResponse, AuthWebApiOutputFormat outputFormat)
        {
            string serverStatus = ServerManager.Instance.GetServerStatus(false);

            // Fix line breaks for display in browsers
            if (outputFormat == AuthWebApiOutputFormat.Html)
                serverStatus = serverStatus.Replace("\n", "<br/>");

            await SendResponseAsync(new(true, "Server Status", serverStatus), httpResponse, outputFormat);
            return true;
        }

        private async Task<bool> OnRegionReport(HttpListenerResponse httpResponse, AuthWebApiOutputFormat outputFormat)
        {
            if (ServerManager.Instance.GetGameService(GameServiceType.PlayerManager) is not PlayerManagerService playerManager)
                return false;

            using RegionReport regionReport = new();
            playerManager.GetRegionReportData(regionReport);

            if (outputFormat == AuthWebApiOutputFormat.Html)
            {
                StringBuilder sb = new();
                HtmlBuilder.AppendDataStructure(sb, regionReport);
                await SendResponseAsync(new(true, "Region Report", sb.ToString()), httpResponse, outputFormat);
            }
            else if (outputFormat == AuthWebApiOutputFormat.Json)
            {
                string json = JsonSerializer.Serialize(regionReport);
                await HttpHelper.SendPlainTextAsync(httpResponse, json);
            }

            return true;
        }

        private async Task<bool> OnMetricsPerformance(HttpListenerResponse httpResponse, AuthWebApiOutputFormat outputFormat)
        {
            if (outputFormat == AuthWebApiOutputFormat.Html)
            {
                string report = MetricsManager.Instance.GeneratePerformanceReport(MetricsReportFormat.Html);
                await SendResponseAsync(new(true, "Performance Report", report), httpResponse, outputFormat);
            }
            else if (outputFormat == AuthWebApiOutputFormat.Json)
            {
                string report = MetricsManager.Instance.GeneratePerformanceReport(MetricsReportFormat.Json);
                await HttpHelper.SendPlainTextAsync(httpResponse, report);
            }

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
