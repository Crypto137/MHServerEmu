using System.Net;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.PlayerManagement;
using MHServerEmu.PlayerManagement.Regions;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    public class RegionReportWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            if (ServerManager.Instance.GetGameService(GameServiceType.PlayerManager) is not PlayerManagerService playerManager)
            {
                context.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }

            using RegionReport regionReport = new();
            playerManager.GetRegionReportData(regionReport);
            await context.SendJsonAsync(regionReport);
        }
    }
}
