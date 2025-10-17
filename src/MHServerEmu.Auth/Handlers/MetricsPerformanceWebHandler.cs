using MHServerEmu.Core.Metrics;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.Auth.Handlers
{
    public class MetricsPerformanceWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            string report = MetricsManager.Instance.GeneratePerformanceReport(MetricsReportFormat.Json);
            await context.SendAsync(report, "application/json");
        }
    }
}
