using MHServerEmu.Core.Config;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;
using MHServerEmu.WebFrontend.Handlers;
using MHServerEmu.WebFrontend.Handlers.MTXStore;
using MHServerEmu.WebFrontend.Handlers.WebApi;
using MHServerEmu.WebFrontend.Network;

namespace MHServerEmu.WebFrontend
{
    /// <summary>
    /// Handles HTTP requests from clients.
    /// </summary>
    public class WebFrontendService : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly WebFrontendServiceMailbox _serviceMailbox = new();

        private readonly WebService _webService;
        private List<string> _dashboardEndpoints;

        public GameServiceState State { get; private set; } = GameServiceState.Created;

        /// <summary>
        /// Constructs a new <see cref="WebFrontendService"/> instance.
        /// </summary>
        public WebFrontendService()
        {
            var config = ConfigManager.Instance.GetConfig<WebFrontendConfig>();

            WebServiceSettings webServiceSettings = new()
            {
                Name = "WebFrontend",
                ListenUrl = $"http://{config.Address}:{config.Port}/",
                FallbackHandler = new NotFoundWebHandler(),
            };

            _webService = new(webServiceSettings);

            // Register the protobuf handler to the /Login/IndexPB path for compatibility with legacy reverse proxy setups.
            // We should probably prefer to use /AuthServer/Login/IndexPB because it's more accurate to what Gazillion had.
            ProtobufWebHandler protobufHandler = new(config.EnableLoginRateLimit, TimeSpan.FromMilliseconds(config.LoginRateLimitCostMS), config.LoginRateLimitBurst);
            _webService.RegisterHandler("/Login/IndexPB",            protobufHandler);
            _webService.RegisterHandler("/AuthServer/Login/IndexPB", protobufHandler);

            // MTXStore handlers are used for the Add G panel in the client UI.
            _webService.RegisterHandler("/MTXStore/AddG", new AddGWebHandler());
            _webService.RegisterHandler("/MTXStore/AddG/Submit", new AddGSubmitWebHandler());

            if (config.EnableWebApi)
            {
                InitializeWebBackend();

                if (config.EnableDashboard)
                    InitializeWebDashboard(config.DashboardFileDirectory, config.DashboardUrlPath);
            }
        }

        #region IGameService Implementation

        /// <summary>
        /// Runs this <see cref="WebFrontendService"/> instance.
        /// </summary>
        public void Run()
        {
            _webService.Start();
            State = GameServiceState.Running;

            while (_webService.IsRunning)
            {
                _serviceMailbox.ProcessMessages();
                Thread.Sleep(1);
            }

            State = GameServiceState.Shutdown;
        }

        /// <summary>
        /// Stops listening and shuts down this <see cref="WebFrontendService"/> instance.
        /// </summary>
        public void Shutdown()
        {
            _webService.Stop();
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            _serviceMailbox.PostMessage(message);
        }

        public void GetStatus(Dictionary<string, long> statusDict)
        {
            statusDict["WebFrontendHandlers"] = _webService.HandlerCount;
            statusDict["WebFrontendHandledRequests"] = _webService.HandledRequests;
        }

        #endregion

        public void ReloadDashboard()
        {
            if (_dashboardEndpoints == null)
                return;

            foreach (string localPath in _dashboardEndpoints)
            {
                StaticFileWebHandler fileHandler = _webService.GetHandler(localPath) as StaticFileWebHandler;
                fileHandler?.Load();
            }
        }

        public void ReloadAddGPage()
        {
            AddGWebHandler addGHandler = _webService.GetHandler("/MTXStore/AddG") as AddGWebHandler;
            addGHandler?.Load();
        }

        private void InitializeWebBackend()
        {
            _webService.RegisterHandler("/AccountManagement/Create", new AccountCreateWebHandler());
            _webService.RegisterHandler("/ServerStatus", new ServerStatusWebHandler());
            _webService.RegisterHandler("/RegionReport", new RegionReportWebHandler());
            _webService.RegisterHandler("/Metrics/Performance", new MetricsPerformanceWebHandler());
        }

        private void InitializeWebDashboard(string dashboardDirectoryName, string localPath)
        {
            string dashboardDirectory = Path.Combine(FileHelper.DataDirectory, "Web", dashboardDirectoryName);
            if (Directory.Exists(dashboardDirectory) == false)
            {
                Logger.Warn($"InitializeWebDashboard(): Dashboard directory '{dashboardDirectoryName}' does not exist");
                return;
            }

            string indexFilePath = Path.Combine(dashboardDirectory, "index.html");
            if (File.Exists(indexFilePath) == false)
            {
                Logger.Warn($"InitializeWebDashboard(): Index file not found at '{indexFilePath}'");
                return;
            }

            _dashboardEndpoints = new();

            // Make sure local path starts and ends with slashes.
            if (localPath.StartsWith('/') == false)
                localPath = $"/{localPath}";

            if (localPath.EndsWith('/') == false)
                localPath = $"{localPath}/";

            _webService.RegisterHandler(localPath, new StaticFileWebHandler(indexFilePath));
            _dashboardEndpoints.Add(localPath);

            // Add redirect for requests to our dashboard "directory" that don't have trailing slashes.
            if (localPath.Length > 1)
            {
                string localPathRedirect = localPath[..^1];
                _webService.RegisterHandler(localPathRedirect, new TrailingSlashRedirectWebHandler());
                _dashboardEndpoints.Add(localPathRedirect);
            }

            // Register other files.
            foreach (string filePath in Directory.GetFiles(dashboardDirectory))
            {
                ReadOnlySpan<char> fileName = Path.GetFileName(filePath.AsSpan());
                if (fileName.Equals("index.html", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                string subFilePath = $"{localPath}{fileName}";
                _webService.RegisterHandler(subFilePath, new StaticFileWebHandler(filePath));
                _dashboardEndpoints.Add(subFilePath);
            }

            Logger.Info($"Initialized web dashboard at {localPath}");
        }
    }
}
