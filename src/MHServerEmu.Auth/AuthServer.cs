using MHServerEmu.Auth.Handlers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Web;

namespace MHServerEmu.Auth
{
    /// <summary>
    /// Handles HTTP requests from clients.
    /// </summary>
    public class AuthServer : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly WebService _webService;

        public GameServiceState State { get; private set; } = GameServiceState.Created;

        /// <summary>
        /// Constructs a new <see cref="AuthServer"/> instance.
        /// </summary>
        public AuthServer()
        {
            var config = ConfigManager.Instance.GetConfig<AuthConfig>();

            WebServiceSettings webServiceSettings = new()
            {
                Name = "AuthWebService",
                ListenUrl = $"http://{config.Address}:{config.Port}/",
                FallbackHandler = new NotFoundWebHandler(),
            };

            _webService = new(webServiceSettings);

            // Register the protobuf handler to the /Login/IndexPB path for compatibility with legacy reverse proxy setups.
            // We should probably prefer to use /AuthServer/Login/IndexPB because it's more accurate to what Gazillion had.
            ProtobufWebHandler protobufHandler = new();
            _webService.RegisterHandler("/Login/IndexPB",            protobufHandler);
            _webService.RegisterHandler("/AuthServer/Login/IndexPB", protobufHandler);

            if (config.EnableWebApi)
            {
                _webService.RegisterHandler("/AccountManagement/Create",    new AccountCreateWebHandler());
                _webService.RegisterHandler("/ServerStatus",                new ServerStatusWebHandler());
                _webService.RegisterHandler("/RegionReport",                new RegionReportWebHandler());
                _webService.RegisterHandler("/Metrics/Performance",         new MetricsPerformanceWebHandler());

                if (config.EnableDashboard)
                    _webService.RegisterHandler("/Dashboard",               new DashboardWebHandler());
            }
        }

        #region IGameService Implementation

        /// <summary>
        /// Runs this <see cref="AuthServer"/> instance.
        /// </summary>
        public void Run()
        {
            _webService.Start();
            State = GameServiceState.Running;

            while (_webService.IsRunning)
                Thread.Sleep(500);

            State = GameServiceState.Shutdown;
        }

        /// <summary>
        /// Stops listening and shuts down this <see cref="AuthServer"/> instance.
        /// </summary>
        public void Shutdown()
        {
            _webService.Stop();
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            // AuthServer should not be handling messages from TCP clients
            switch (message)
            {
                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"IsRunning = {_webService.IsRunning}, ListenUrl = {_webService.Settings.ListenUrl}";
        }

        #endregion

        public void ReloadDashboard()
        {
            DashboardWebHandler dashboard = _webService.GetHandler("/Dashboard") as DashboardWebHandler;
            dashboard.Load();
        }
    }
}
