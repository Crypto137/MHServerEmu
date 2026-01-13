using MHServerEmu.Core.Extensions;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.WebFrontend.Models;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    public class AccountSetPasswordWebHandler : WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

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

            // Hash the IP address to prevent it from appearing in logs if needed
            string ipAddressHandle = context.GetIPAddressHandle();

            // Account creation does not require authorization, so just forward the request to the Player Manager.
            ServiceMessage.AccountOperationResponse opResponse = await GameServiceTaskManager.Instance.DoAccountOperationAsync(
                AccountOperation.SetPassword, email, null, password);

            int responseCode = opResponse.ResultCode;

            if (responseCode == AccountOperationResponse.Success)
                Logger.Info($"Updated password for account {email} (requester={ipAddressHandle})");
            else
                Logger.Info($"Failed to update password for account {email} (requester={ipAddressHandle}, resultCode={responseCode})");

            await context.SendJsonAsync(new AccountOperationResponse(responseCode));
        }
    }
}
