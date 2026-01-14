using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.WebFrontend.Models;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    public class AccountSetPasswordWebHandler : WebHandler
    {
        public override WebApiAccessType Access { get => WebApiAccessType.AccountManagement; }

        protected override async Task Post(WebRequestContext context)
        {
            AccountOperationRequest query = await context.ReadJsonAsync<AccountOperationRequest>();

            string email = query.Email;
            string password = query.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await context.SendJsonAsync(new AccountOperationResponse(AccountOperationResponse.GenericFailure));
                return;
            }

            ServiceMessage.AccountOperationResponse opResponse = await GameServiceTaskManager.Instance.DoAccountOperationAsync(
                AccountOperation.SetPassword, email, null, password);

            int responseCode = opResponse.ResultCode;

            // Restricted API endpoints are already logged by WebHandler, so no need to explicitly log here.

            await context.SendJsonAsync(new AccountOperationResponse(responseCode));
        }
    }
}
