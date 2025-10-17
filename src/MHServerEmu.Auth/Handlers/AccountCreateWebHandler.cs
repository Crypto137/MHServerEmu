using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.Auth.Handlers
{
    public class AccountCreateWebHandler : WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        protected override async Task Post(WebRequestContext context)
        {
            AccountCreateQuery query = context.ReadJson<AccountCreateQuery>();

            string email = query.Email;
            string playerName = query.PlayerName;
            string password = query.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(password))
            {
                await context.SendJsonAsync(new ResponseData(false, "Error", "Input is not valid"));
                return;
            }

            email = email.ToLower();

            AccountOperationResult result = AccountManager.CreateAccount(email, playerName, password);
            bool isSuccess = result == AccountOperationResult.Success;
            string resultString = AccountManager.GetOperationResultString(result, email, playerName);

            if (HideSensitiveInformation == false)
                Logger.Trace($"Post(): {resultString}");

            await context.SendJsonAsync(new ResponseData(isSuccess, "Create Account Result", resultString));
        }

        private class AccountCreateQuery
        {
            public string Email { get; init; }
            public string PlayerName { get; init; }
            public string Password { get; init; }
        }

        private readonly struct ResponseData(bool result, string title, string text)
        {
            public bool Result { get; } = result;
            public string Title { get; } = title;
            public string Text { get; } = text;
        }
    }
}
