using MHServerEmu.Core.Metrics;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.Auth.Handlers
{
    public class MetricsPerformanceWebHandler : WebHandler
    {
        protected override async Task Get(WebRequestContext context)
        {
            WebFrontendOutputFormat outputFormat = WebFrontendHelper.GetOutputFormat(context);  // REMOVEME

            if (outputFormat == WebFrontendOutputFormat.Html)
            {
                string report = MetricsManager.Instance.GeneratePerformanceReport(MetricsReportFormat.Html);
                await context.SendAsync(true, "Performance Report", report, outputFormat);
            }
            else if (outputFormat == WebFrontendOutputFormat.Json)
            {
                string report = MetricsManager.Instance.GeneratePerformanceReport(MetricsReportFormat.Json);
                await context.SendAsync(report);
            }
        }
    }
}
