using MHServerEmu.Core.Config;
using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.WebFrontend.Models;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    public class AccountCreateWebHandler : WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected override async Task Post(WebRequestContext context)
        {
            AccountCreateRequest query = await context.ReadJsonAsync<AccountCreateRequest>();

            string email = query.Email;
            string playerName = query.PlayerName;
            string password = query.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(password))
            {
                await context.SendJsonAsync(new AccountOperationResponse(AccountOperationResponse.GenericFailure));
                return;
            }

            // Hash the IP address to prevent it from appearing in logs if needed
            string ipAddressHandle = context.GetIPAddressHandle();

            // Account creation does not require authorization, so just forward the request to the Player Manager.
            ServiceMessage.AccountOperationResponse opResponse = await GameServiceTaskManager.Instance.DoAccountOperationAsync(
                AccountOperation.Create, email, playerName, password);

            int responseCode = opResponse.ResultCode;

            if (responseCode == AccountOperationResponse.Success)
                Logger.Info($"Successfully created account {playerName} (requester={ipAddressHandle})");
            else
                Logger.Info($"Failed to create account {playerName} (requester={ipAddressHandle}, resultCode={responseCode})");

            await context.SendJsonAsync(new AccountOperationResponse(responseCode));
        }

        private readonly struct AccountCreateRequest
        {
            public string Email { get; init; }
            public string PlayerName { get; init; }
            public string Password { get; init; }
        }
    }
}
