using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.PlayerManagement.Players;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    public class AccountCreateWebHandler : WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly bool HideSensitiveInformation = ConfigManager.Instance.GetConfig<LoggingConfig>().HideSensitiveInformation;

        protected override async Task Post(WebRequestContext context)
        {
            AccountCreateRequest query = await context.ReadJsonAsync<AccountCreateRequest>();

            string email = query.Email;
            string playerName = query.PlayerName;
            string password = query.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(password))
            {
                await context.SendJsonAsync(new AccountCreateResponse(AccountOperationResult.GenericFailure));
                return;
            }

            email = email.ToLower();

            AccountOperationResult result = AccountManager.CreateAccount(email, playerName, password);

            if (HideSensitiveInformation == false)
                Logger.Trace($"Post(): {AccountManager.GetOperationResultString(result, email, playerName)}");

            await context.SendJsonAsync(new AccountCreateResponse(result));
        }

        private readonly struct AccountCreateRequest
        {
            public string Email { get; init; }
            public string PlayerName { get; init; }
            public string Password { get; init; }
        }

        private readonly struct AccountCreateResponse(AccountOperationResult result)
        {
            public AccountOperationResult Result { get; } = result;
        }
    }
}
