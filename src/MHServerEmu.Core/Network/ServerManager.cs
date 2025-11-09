using System.Globalization;
using System.Text;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Core.Network
{
    // Services start in this order and are shut down in reverse order.
    public enum GameServiceType
    {
        GameInstance,
        Leaderboard,
        PlayerManager,
        GroupingManager,
        Frontend,
        WebFrontend,
        NumServiceTypes
    }

    public enum GameServiceState
    {
        Created,
        Starting,
        Running,
        ShuttingDown,
        Shutdown,
    }

    public enum ServerManagerState
    {
        Created,
        Starting,
        Running,
        ShuttingDown,
        Shutdown,
    }

    /// <summary>
    /// Manages <see cref="IGameService"/> instances and routes <see cref="IGameServiceMessage"/> instances between them.
    /// </summary>
    public class ServerManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly IGameService[] _services = new IGameService[(int)GameServiceType.NumServiceTypes];
        private readonly Thread[] _serviceThreads = new Thread[(int)GameServiceType.NumServiceTypes];

        private ServerManagerState _state = ServerManagerState.Created;

        public static ServerManager Instance { get; } = new();

        public TimeSpan StartupTime { get; private set; }

        private ServerManager() { }

        /// <summary>
        /// Initializes the <see cref="ServerManager"/> instance.
        /// </summary>
        public void Initialize()
        {
            StartupTime = Clock.UnixTime;
        }

        /// <summary>
        /// Registers an <see cref="IGameService"/> for the specified <see cref="GameServiceType"/>.
        /// </summary>
        public void RegisterGameService(IGameService service, GameServiceType serviceType)
        {
            ArgumentNullException.ThrowIfNull(service);

            int index = (int)serviceType;

            if (index < 0 || index >= _services.Length)
                throw new ArgumentOutOfRangeException($"Invalid service type [{serviceType}].");

            if (_services[index] != null)
                throw new InvalidOperationException($"Service for type [{serviceType}] is already registered.");

            _services[index] = service;

            Logger.Info($"Registered service for type [{serviceType}]");
        }

        /// <summary>
        /// Unregisters the current <see cref="IGameService"/> for the specified <see cref="GameServiceType"/>.
        /// </summary>
        public void UnregisterGameService(GameServiceType serviceType)
        {
            int index = (int)serviceType;

            if (index < 0 || index >= _services.Length)
                throw new ArgumentOutOfRangeException($"Invalid service type [{serviceType}].");

            if (_services[index] == null)
                throw new InvalidOperationException($"No registered service for type [{serviceType}].");

            _services[index] = null;

            Logger.Info($"Unregistered service for type {serviceType}");
        }

        /// <summary>
        /// Returns the registered <see cref="IGameService"/> for the specified <see cref="GameServiceType"/>. Returns <see langword="null"/> if not registered.
        /// </summary>
        public IGameService GetGameService(GameServiceType serviceType)
        {
            int index = (int)serviceType;

            if (index < 0 || index >= _services.Length)
                throw new ArgumentOutOfRangeException($"Invalid service type [{serviceType}].");

            return _services[index];
        }

        /// <summary>
        /// Routes the provided <typeparamref name="T"/> instance to the <see cref="IGameService"/> registered for the specified <see cref="GameServiceType"/>.
        /// </summary>
        public bool SendMessageToService<T>(GameServiceType serviceType, in T message) where T: struct, IGameServiceMessage
        {
            int index = (int)serviceType;

            if (index < 0 || index >= _services.Length)
                throw new ArgumentOutOfRangeException($"Invalid service type [{serviceType}].");

            IGameService service = _services[index];

            if (service == null)
                return Logger.WarnReturn(false, $"RouteMessage(): No service is registered for type [{serviceType}]");

            switch (service.State)
            {
                // Treat Starting and ShuttingDown same as Running because services can exchange confirmations during startup / shutdown.
                case GameServiceState.Starting:
                case GameServiceState.Running:
                case GameServiceState.ShuttingDown:
                    break;

                default:
                    Logger.Warn($"Unexpected state [{service.State}] for type [{serviceType}] when sending [{typeof(T).Name}]");
                    break;
            }

            service.ReceiveServiceMessage(message);

            return true;
        }

        /// <summary>
        /// Runs all registered <see cref="IGameService"/> instances.
        /// </summary>
        public void RunServices()
        {
            if (_state != ServerManagerState.Created)
                throw new InvalidOperationException($"Invalid state {_state} when starting the ServerManager.");

            _state = ServerManagerState.Starting;

            for (int i = 0; i < _services.Length; i++)
            {
                GameServiceType serviceType = (GameServiceType)i;

                IGameService service = _services[i];
                if (service == null)
                    continue;

                if (service.State != GameServiceState.Created)
                    throw new InvalidOperationException($"Invalid service state [{service.State}] for type [{serviceType}].");

                if (_serviceThreads[i] != null)
                    throw new InvalidOperationException($"Service thread already created for type [{serviceType}].");

                Logger.Info($"Starting service for type [{serviceType}]...");

                _serviceThreads[i] = new(_services[i].Run) { Name = $"Service [{serviceType}]", IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
                _serviceThreads[i].Start();

                while (service.State != GameServiceState.Running)
                    Thread.Sleep(1);

                Logger.Info($"Service for type [{serviceType}] started");                
            }

            _state = ServerManagerState.Running;
        }

        /// <summary>
        /// Shuts down all running <see cref="IGameService"/> instances.
        /// </summary>
        public void ShutdownServices()
        {
            // Ignore shutdown requests if already shutting down
            if (_state == ServerManagerState.ShuttingDown)
                return;

            if (_state != ServerManagerState.Running)
                throw new InvalidOperationException($"Invalid state {_state} when shutting down the ServerManager.");

            _state = ServerManagerState.ShuttingDown;

            // Shut down services in reverse
            for (int i = _services.Length - 1; i >= 0; i--)
            {
                GameServiceType serviceType = (GameServiceType)i;

                IGameService service = _services[i];
                if (service == null)
                    continue;

                if (service.State == GameServiceState.Shutdown)
                    continue;

                if (service.State != GameServiceState.Running)
                {
                    Logger.Warn($"ShutdownServices(): Unexpected service state [{service.State}] for type [{serviceType}]");
                    continue;
                }

                Logger.Info($"Shutting down service for type [{serviceType}]...");

                _services[i].Shutdown();

                while (service.State != GameServiceState.Shutdown)
                    Thread.Sleep(1);

                _serviceThreads[i] = null;

                Logger.Info($"Service for type [{serviceType}] shut down");
            }

            Logger.Info("All services shut down");

            _state = ServerManagerState.Shutdown;
        }

        /// <summary>
        /// Adds structured server status data to the provided dictionary.
        /// </summary>
        public void GetServerStatus(Dictionary<string, long> statusDict)
        {
            statusDict["StartupTime"] = (long)StartupTime.TotalSeconds;
            statusDict["CurrentTime"] = (long)Clock.UnixTime.TotalSeconds;

            for (int i = 0; i < _services.Length; i++)
                _services[i]?.GetStatus(statusDict);
        }

        /// <summary>
        /// Returns a <see cref="string"/> representing the current status of all running <see cref="IGameService"/> instances.
        /// </summary>
        public string GetServerStatusString()
        {
            Dictionary<string, long> statusDict = DictionaryPool<string, long>.Instance.Get();
            GetServerStatus(statusDict);

            StringBuilder sb = new();

            foreach (var kvp in statusDict)
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");

            string statusString = sb.ToString();

            DictionaryPool<string, long>.Instance.Return(statusDict);
            return statusString;
        }
    }
}
