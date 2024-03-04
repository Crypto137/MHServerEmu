using System.Globalization;
using MHServerEmu.Auth;
using MHServerEmu.Billing;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Grouping;
using MHServerEmu.Leaderboards;
using MHServerEmu.Networking;
using MHServerEmu.PlayerManagement;

namespace MHServerEmu
{
    public class ServerManager : IGameService
    {
        public const string GameVersion = "1.52.0.1700";

        private static readonly Logger Logger = LogManager.CreateLogger();

        public static ServerManager Instance { get; } = new();

        public DateTime StartupTime { get; private set; }

        // Backend
        public GroupingManagerService GroupingManagerService { get; private set; }
        public PlayerManagerService PlayerManagerService { get; private set; }
        public BillingService BillingService { get; private set; }
        public LeaderboardService LeaderboardService { get; private set; }

        // Frontend
        public FrontendServer FrontendServer { get; private set; }
        public AuthServer AuthServer { get; private set; }

        public Thread FrontendServerThread { get; private set; }
        public Thread AuthServerThread { get; private set; }

        private ServerManager() { }

        public void Initialize()
        {
            // Initialize backend services
            GroupingManagerService = new();
            PlayerManagerService = new();
            BillingService = new();
            LeaderboardService = new();

            StartupTime = DateTime.Now;
        }

        #region Server Control

        public void StartServers()
        {
            StartFrontendServer();
            StartAuthServer();
        }

        public void Shutdown()
        {
            if (AuthServer != null)
            {
                Logger.Info("Shutting down AuthServer...");
                AuthServer.Shutdown();
            }

            if (FrontendServer != null)
            {
                Logger.Info("Shutting down FrontendServer...");
                FrontendServer.Shutdown();
            }
        }

        private bool StartFrontendServer()
        {
            if (FrontendServer != null) return false;

            FrontendServer = new FrontendServer();
            FrontendServerThread = new(FrontendServer.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            FrontendServerThread.Start();

            return true;
        }

        private bool StartAuthServer()
        {
            if (AuthServer != null) return false;

            AuthServer = new();
            AuthServerThread = new(AuthServer.Run) { IsBackground = true, CurrentCulture = CultureInfo.InvariantCulture };
            AuthServerThread.Start();

            return true;
        }

        #endregion

        #region Message Handling

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch (muxId)
            {
                case 1:
                    if (client.FinishedPlayerManagerHandshake)
                        PlayerManagerService.Handle(client, muxId, message);
                    else
                        FrontendServer.Handle(client, muxId, message);

                    break;

                case 2:
                    if (client.FinishedGroupingManagerHandshake)
                        GroupingManagerService.Handle(client, muxId, message);
                    else
                        FrontendServer.Handle(client, muxId, message);

                    break;

                default:
                    Logger.Warn($"Unhandled message on muxId {muxId}");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, IEnumerable<GameMessage> messages)
        {
            switch (muxId)
            {
                case 1:
                    if (client.FinishedPlayerManagerHandshake)
                        PlayerManagerService.Handle(client, muxId, messages);
                    else
                        FrontendServer.Handle(client, muxId, messages);

                    break;

                case 2:
                    if (client.FinishedGroupingManagerHandshake)
                        GroupingManagerService.Handle(client, muxId, messages);
                    else
                        FrontendServer.Handle(client, muxId, messages);

                    break;

                default:
                    Logger.Warn($"{messages.Count()} unhandled messages on muxId {muxId}");
                    break;
            }
        }

        #endregion

        #region Misc

        public string GetServerStatus()
        {
            return $"Server Status\nUptime: {DateTime.Now - StartupTime:hh\\:mm\\:ss}\nSessions: {PlayerManagerService.SessionCount}";
        }


        #endregion
    }
}
