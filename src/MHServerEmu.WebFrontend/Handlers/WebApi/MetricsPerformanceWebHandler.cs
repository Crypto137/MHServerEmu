using MHServerEmu.Core.Metrics;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    public class MetricsPerformanceWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            using var reportHandle = PerformanceReportPool.Get(out PerformanceReport report);
            MetricsManager.Instance.GetPerformanceReportData(report);
            await context.SendJsonAsync(report);
        }
    }
}
