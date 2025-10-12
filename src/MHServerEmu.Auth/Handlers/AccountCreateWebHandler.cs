using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.PlayerManagement.Players;
using System.Collections.Specialized;
using System.Net;

namespace MHServerEmu.Auth.Handlers
{
    // TODO: Move frontend logic to a separate AJAX app.
    public class AccountCreateWebHandler : WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        protected override async Task Get(WebRequestContext context)
        {
            WebFrontendOutputFormat outputFormat = WebFrontendHelper.GetOutputFormat(context);  // REMOVEME

            switch (outputFormat)
            {
                case WebFrontendOutputFormat.Html:
                    await context.SendAsync(WebFrontendHelper.AccountCreateFormHtml, outputFormat);
                    break;

                case WebFrontendOutputFormat.Json:
                    context.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
            }
        }

        protected override async Task Post(WebRequestContext context)
        {
            WebFrontendOutputFormat outputFormat = WebFrontendHelper.GetOutputFormat(context);  // REMOVEME

            // TODO: Use JSON for all request bodies.
            NameValueCollection query = context.ReadQueryString();

            string email = query["email"];
            string playerName = query["playerName"];
            string password = query["password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(password))
            {
                await context.SendAsync(false, "Error", "Input is not valid", outputFormat);
                return;
            }

            email = email.ToLower();

            AccountOperationResult result = AccountManager.CreateAccount(email, playerName, password);
            bool isSuccess = result == AccountOperationResult.Success;
            string resultString = AccountManager.GetOperationResultString(result, email, playerName);

            if (HideSensitiveInformation == false)
                Logger.Trace($"Post(): {resultString}");

            await context.SendAsync(isSuccess, "Create Account Result", resultString, outputFormat);
        }
    }
}
