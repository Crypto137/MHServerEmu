using MHServerEmu.Core.Config;

namespace MHServerEmu.WebFrontend
{
    public class WebFrontendConfig : ConfigContainer
    {
        public string Address { get; private set; } = "localhost";
        public string Port { get; private set; } = "8080";
        public bool EnableLoginRateLimit { get; private set; } = false;
        public int LoginRateLimitCostMS { get; private set; } = 30000;
        public int LoginRateLimitBurst { get; private set; } = 10;
        public bool EnableWebApi { get; private set; } = true;
        public bool EnableDashboard { get; private set; } = true;
        public string DashboardFileDirectory { get; private set; } = "Dashboard";
        public string DashboardUrlPath { get; private set; } = "/";
    }
}
