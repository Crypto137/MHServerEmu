using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Metrics;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.WebFrontend.Handlers.WebApi
{
    public class MetricsPerformanceWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            using PerformanceReport report = ObjectPoolManager.Instance.Get<PerformanceReport>();
            MetricsManager.Instance.GetPerformanceReportData(report);
            await context.SendJsonAsync(report);
        }
    }
}
