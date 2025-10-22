using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    public class ServerStatusWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            Dictionary<string, long> statusDict = DictionaryPool<string, long>.Instance.Get();
            ServerManager.Instance.GetServerStatus(statusDict);

            await context.SendJsonAsync(statusDict);

            DictionaryPool<string, long>.Instance.Return(statusDict);
        }
    }
}
