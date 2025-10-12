using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.Auth.Handlers
{
    public class ServerStatusWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            WebFrontendOutputFormat outputFormat = WebFrontendHelper.GetOutputFormat(context);  // REMOVEME

            string serverStatus = ServerManager.Instance.GetServerStatus(false);

            // Fix line breaks for display in browsers
            if (outputFormat == WebFrontendOutputFormat.Html)
                serverStatus = serverStatus.Replace("\n", "<br/>");

            await context.SendAsync(true, "Server Status", serverStatus, outputFormat);
        }
    }
}
