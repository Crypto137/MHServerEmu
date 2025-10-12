using System.Net;
using System.Text;
using System.Text.Json;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.PlayerManagement;
using MHServerEmu.PlayerManagement.Regions;

namespace MHServerEmu.Auth.Handlers
{
    public class RegionReportWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            WebFrontendOutputFormat outputFormat = WebFrontendHelper.GetOutputFormat(context);  // REMOVEME

            if (ServerManager.Instance.GetGameService(GameServiceType.PlayerManager) is not PlayerManagerService playerManager)
            {
                context.StatusCode = (int)HttpStatusCode.InternalServerError;
                return;
            }

            using RegionReport regionReport = new();
            playerManager.GetRegionReportData(regionReport);

            if (outputFormat == WebFrontendOutputFormat.Html)
            {
                StringBuilder sb = new();
                HtmlBuilder.AppendDataStructure(sb, regionReport);
                await context.SendAsync(true, "Region Report", sb.ToString(), outputFormat);
            }
            else if (outputFormat == WebFrontendOutputFormat.Json)
            {
                string json = JsonSerializer.Serialize(regionReport);
                await context.SendAsync(json);
            }
        }
    }
}
