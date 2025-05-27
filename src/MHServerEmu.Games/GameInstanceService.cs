using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Leaderboards;

namespace MHServerEmu.Games
{
    public class GameInstanceService : IGameService
    {
        // TODO: Move actual game instance creation here from PlayerManager
        private static readonly Logger Logger = LogManager.CreateLogger();

        #region IGameService

        public void Run()
        {
        }

        public void Shutdown()
        {
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            // TODO: Route messages to the game instance the player is in
            switch (message)
            {
                case GameServiceProtocol.LeaderboardStateChange leaderboardStateChange:
                    LeaderboardInfoCache.Instance.UpdateLeaderboardInstance(leaderboardStateChange);

                    // TODO/FIXME: Right now we have to route this through the PlayerManager because it holds all game instances.
                    // Remove this when game instances are moved here.
                    ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, leaderboardStateChange);
                    break;

                case GameServiceProtocol.LeaderboardStateChangeList leaderboardStateChangeList:
                    LeaderboardInfoCache.Instance.UpdateLeaderboardInstances(leaderboardStateChangeList);
                    break;

                case GameServiceProtocol.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse:
                    // TODO/FIXME: Same as LeaderboardStateChange above
                    ServerManager.Instance.SendMessageToService(ServerType.PlayerManager, leaderboardRewardRequestResponse);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return "Running";
        }

        #endregion
    }
}
