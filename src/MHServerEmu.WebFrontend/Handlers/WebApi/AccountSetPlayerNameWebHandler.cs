using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.WebFrontend.Models;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    internal class AccountSetPlayerNameWebHandler : WebHandler
    {
        public override WebApiAccessType Access { get => WebApiAccessType.AccountManagement; }

        protected override async Task Post(WebRequestContext context)
        {
            AccountOperationRequest query = await context.ReadJsonAsync<AccountOperationRequest>();

            string email = query.Email;
            string playerName = query.PlayerName;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(playerName))
            {
                await context.SendJsonAsync(new AccountOperationResponse(AccountOperationResponse.GenericFailure));
                return;
            }

            ServiceMessage.AccountOperationResponse opResponse = await GameServiceTaskManager.Instance.DoAccountOperationAsync(
                AccountOperation.SetPlayerName, email, playerName);

            int responseCode = opResponse.ResultCode;

            // Restricted API endpoints are already logged by WebHandler, so no need to explicitly log here.

            await context.SendJsonAsync(new AccountOperationResponse(responseCode));
        }
    }
}
