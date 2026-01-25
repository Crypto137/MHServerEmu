using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    public class ServerStatusWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            using var statusDictHandle = DictionaryPool<string, long>.Instance.Get(out Dictionary<string, long> statusDict);
            ServerManager.Instance.GetServerStatus(statusDict);

            await context.SendJsonAsync(statusDict);
        }
    }
}
