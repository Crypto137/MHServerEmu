using System.Text;
using System.Text.Json;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.Auth
{
    // TODO: Move frontend logic to a separate client-side AJAX app.
    public enum WebFrontendOutputFormat
    {
        Html,
        Json,
    }

    public static class WebFrontendHelper
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public static readonly string ResponseHtml;
        public static readonly string AccountCreateFormHtml;

        static WebFrontendHelper()
        {
            string assetDirectory = Path.Combine(FileHelper.DataDirectory, "Auth");
            ResponseHtml = File.ReadAllText(Path.Combine(assetDirectory, "Response.html"));
            AccountCreateFormHtml = File.ReadAllText(Path.Combine(assetDirectory, "AccountCreateForm.html"));
        }

        public static WebFrontendOutputFormat GetOutputFormat(WebRequestContext context)
        {
            string outputFormatString = context.RequestQueryString["outputFormat"];

            if (Enum.TryParse(outputFormatString, true, out WebFrontendOutputFormat outputFormat) == false)
                return WebFrontendOutputFormat.Html;

            return outputFormat;
        }

        public static async Task SendAsync(this WebRequestContext context, string response, WebFrontendOutputFormat outputFormat)
        {
            string contentType = "text/plain";

            if (outputFormat == WebFrontendOutputFormat.Html)
                contentType = "text/html";

            await context.SendAsync(response, contentType);
        }

        public static async Task SendAsync(this WebRequestContext context, bool result, string title, string text, WebFrontendOutputFormat outputFormat)
        {
            string response = GenerateResponseString(result, title, text, outputFormat);
            await context.SendAsync(response, outputFormat);
        }

        private static string GenerateResponseString(bool result, string title, string text, WebFrontendOutputFormat outputFormat)
        {
            ResponseData responseData = new(result, title, text);

            switch (outputFormat)
            {
                case WebFrontendOutputFormat.Html:
                    StringBuilder sb = new(ResponseHtml);
                    sb.Replace("%RESPONSE_TITLE%", responseData.Title);
                    sb.Replace("%RESPONSE_TEXT%", responseData.Text);
                    return sb.ToString();

                case WebFrontendOutputFormat.Json:
                    return JsonSerializer.Serialize(responseData);

                default:
                    return Logger.WarnReturn(string.Empty, $"SendResponseAsync(): Unsupported output format {outputFormat}");
            }
        }

        private readonly struct ResponseData(bool result, string title, string text)
        {
            public bool Result { get; } = result;
            public string Title { get; } = title;
            public string Text { get; } = text;
        }
    }
}
