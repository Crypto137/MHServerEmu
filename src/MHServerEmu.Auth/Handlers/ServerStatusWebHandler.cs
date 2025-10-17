using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.Auth.Handlers
{
    public class ServerStatusWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            string serverStatus = ServerManager.Instance.GetServerStatus(false);
            await context.SendJsonAsync(new ResponseData(true, "Server Status", serverStatus));
        }

        private readonly struct ResponseData(bool result, string title, string text)
        {
            public bool Result { get; } = result;
            public string Title { get; } = title;
            public string Text { get; } = text;
        }
    }
}
