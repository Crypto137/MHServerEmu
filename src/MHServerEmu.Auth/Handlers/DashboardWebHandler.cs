using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.Auth.Handlers
{
    /// <summary>
    /// Serves a dashboard web app to allow users to interact with the server via a web browser.
    /// </summary>
    public class DashboardWebHandler : WebHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static readonly string FilePath = Path.Combine(FileHelper.DataDirectory, "Web", "Dashboard", "index.html");

        private string _dashboard = string.Empty;

        public DashboardWebHandler()
        {
            Load();
        }

        public void Load()
        {
            string path = Path.Combine(FileHelper.DataDirectory, "Web", "Dashboard", "index.html");

            if (File.Exists(path) == false)
            {
                Logger.Warn("Load(): Dashboard file not found");
                return;
            }

            _dashboard = File.ReadAllText(path);
            Logger.Info($"Loaded web dashboard");
        }

        protected override async Task Get(WebRequestContext context)
        {
            await context.SendAsync(_dashboard, "text/html");
        }
    }
}
