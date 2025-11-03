using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend.Handlers.MTXStore
{
    public class AddGSubmitWebHandler : WebHandler
    {
        protected override async Task Post(WebRequestContext context)
        {
            AddGRequest request = await context.ReadJsonAsync<AddGRequest>();

            string email = request.Email;
            string token = request.Token;
            int amount = request.Amount;

            ServiceMessage.MTXStoreESConvertResponse convertResponse = await GameServiceTaskManager.Instance.ConvertESAsync(email, token, amount);

            // Right now we communicate the result of the conversion via HTTP status codes and don't send anything else.
            context.StatusCode = convertResponse.StatusCode;
        }

        private readonly struct AddGRequest
        {
            public string Email { get; init; }
            public string Token { get; init; }
            public int Amount { get; init; }
        }
    }
}
