using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Leaderboards;

namespace MHServerEmu.Games.Network.InstanceManagement
{
    public class GameInstanceService : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        internal GameManager GameManager { get; }
        internal GameThreadManager GameThreadManager { get; }

        public GameInstanceConfig Config { get; }

        public GameServiceState State { get; private set; } = GameServiceState.Created;

        public GameInstanceService()
        {
            GameManager = new(this);
            GameThreadManager = new(this);

            Config = ConfigManager.Instance.GetConfig<GameInstanceConfig>();
        }

        #region IGameService

        public void Run()
        {
            GameThreadManager.Initialize();

            State = GameServiceState.Running;
        }

        public void Shutdown()
        {
            // All game instances should be shut down by the PlayerManager before we get here
            int gameCount = GameManager.GameCount;
            if (gameCount > 0)
                Logger.Warn($"Shutdown(): {gameCount} games are still running");

            State = GameServiceState.Shutdown;
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                case GameServiceProtocol.RouteMessageBuffer routeMessageBuffer:
                    OnRouteMessageBuffer(routeMessageBuffer);
                    break;

                case GameServiceProtocol.GameInstanceOp gameInstanceOp:
                    OnGameInstanceOp(gameInstanceOp);
                    break;

                case GameServiceProtocol.GameInstanceClientOp gameInstanceClientOp:
                    OnGameInstanceClientOp(gameInstanceClientOp);
                    break;

                case GameServiceProtocol.LeaderboardStateChange leaderboardStateChange:
                    OnLeaderboardStateChange(leaderboardStateChange);
                    break;

                case GameServiceProtocol.LeaderboardStateChangeList leaderboardStateChangeList:
                    OnLeaderboardStateChangeList(leaderboardStateChangeList);
                    break;

                case GameServiceProtocol.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse:
                    OnLeaderboardRewardRequestResponse(leaderboardRewardRequestResponse);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"Games: {GameManager.GameCount} | Players: {GameManager.PlayerCount}";
        }

        #endregion

        #region Message Handling

        private bool OnRouteMessageBuffer(in GameServiceProtocol.RouteMessageBuffer routeMessageBuffer)
        {
            return GameManager.RouteMessageBuffer(routeMessageBuffer.Client, routeMessageBuffer.MessageBuffer);
        }

        private bool OnGameInstanceOp(in GameServiceProtocol.GameInstanceOp gameInstanceOp)
        {
            switch (gameInstanceOp.Type)
            {
                case GameServiceProtocol.GameInstanceOp.OpType.Create:
                    return GameManager.CreateGame(gameInstanceOp.GameId);

                case GameServiceProtocol.GameInstanceOp.OpType.Shutdown:
                    return GameManager.ShutdownGame(gameInstanceOp.GameId, GameShutdownReason.ShutdownRequested);

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceOp(): Unhandled operation type {gameInstanceOp.Type}");
            }
        }

        private bool OnGameInstanceClientOp(in GameServiceProtocol.GameInstanceClientOp gameInstanceClientOp)
        {
            IFrontendClient client = gameInstanceClientOp.Client;
            ulong gameId = gameInstanceClientOp.GameId;

            switch (gameInstanceClientOp.Type)
            {
                case GameServiceProtocol.GameInstanceClientOp.OpType.Add:
                    if (GameManager.AddClientToGame(client, gameId) == false)
                    {
                        // Disconnect the client so that it doesn't get stuck waiting to be added to a game
                        client.Disconnect();
                        return false;
                    }

                    return true;

                case GameServiceProtocol.GameInstanceClientOp.OpType.Remove:
                    return GameManager.RemoveClientFromGame(client, gameId);

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): Unhandled operation type {gameInstanceClientOp.Type}");
            }
        }

        private bool OnLeaderboardStateChange(in GameServiceProtocol.LeaderboardStateChange leaderboardStateChange)
        {
            LeaderboardInfoCache.Instance.UpdateLeaderboardInstance(leaderboardStateChange);
            GameManager.BroadcastServiceMessageToGames(leaderboardStateChange);
            return true;
        }

        private bool OnLeaderboardStateChangeList(in GameServiceProtocol.LeaderboardStateChangeList leaderboardStateChangeList)
        {
            LeaderboardInfoCache.Instance.UpdateLeaderboardInstances(leaderboardStateChangeList);
            return true;
        }

        private bool OnLeaderboardRewardRequestResponse(in GameServiceProtocol.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse)
        {
            ulong playerDbId = leaderboardRewardRequestResponse.ParticipantId;
            return GameManager.RouteServiceMessageToPlayer(playerDbId, leaderboardRewardRequestResponse);
        }

        #endregion
    }
}
