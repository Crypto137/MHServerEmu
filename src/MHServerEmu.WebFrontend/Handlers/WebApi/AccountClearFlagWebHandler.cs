using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.WebFrontend.Models;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    internal class AccountClearFlagWebHandler : WebHandler
    {
        public override WebApiAccessType Access { get => WebApiAccessType.AccountManagement; }

        protected override async Task Post(WebRequestContext context)
        {
            AccountOperationRequest query = await context.ReadJsonAsync<AccountOperationRequest>();

            string email = query.Email;
            int flags = query.Flags;

            if (string.IsNullOrWhiteSpace(email))
            {
                await context.SendJsonAsync(new AccountOperationResponse(AccountOperationResponse.GenericFailure));
                return;
            }

            ServiceMessage.AccountOperationResponse opResponse = await GameServiceTaskManager.Instance.DoAccountOperationAsync(
                AccountOperation.ClearFlag, email, null, null, byte.MaxValue, flags);

            int responseCode = opResponse.ResultCode;

            // Restricted API endpoints are already logged by WebHandler, so no need to explicitly log here.

            await context.SendJsonAsync(new AccountOperationResponse(responseCode));
        }
    }
}
