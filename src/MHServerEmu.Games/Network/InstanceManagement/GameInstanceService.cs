using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Leaderboards;

namespace MHServerEmu.Games.Network.InstanceManagement
{
    public class GameInstanceService : IGameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly GameManager _gameManager = new();

        public GameInstanceService()
        {
        }

        #region IGameService

        public void Run()
        {
        }

        public void Shutdown()
        {
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
            return "Running";
        }

        #endregion

        private void BroadcastServiceMessageToGames<T>(in T message) where T : struct, IGameServiceMessage
        {
            foreach (Game game in _gameManager)
                game.ReceiveServiceMessage(message);
        }

        #region Message Handling

        private bool OnRouteMessageBuffer(in GameServiceProtocol.RouteMessageBuffer routeMessageBuffer)
        {
            IFrontendClient client = routeMessageBuffer.Client;

            if (_gameManager.TryGetGameForClient(client, out Game game) == false)
                return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): Client [{client}] is not in a game");

            game.ReceiveMessageBuffer(client, routeMessageBuffer.MessageBuffer);
            return true;
        }

        private bool OnGameInstanceOp(in GameServiceProtocol.GameInstanceOp gameInstanceOp)
        {
            switch (gameInstanceOp.Type)
            {
                case GameServiceProtocol.GameInstanceOp.OpType.Create:
                    return _gameManager.CreateGame(gameInstanceOp.GameId);

                case GameServiceProtocol.GameInstanceOp.OpType.Shutdown:
                    return _gameManager.ShutdownGame(gameInstanceOp.GameId);

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceOp(): Unexpected operation type {gameInstanceOp.Type}");
            }
        }

        private bool OnGameInstanceClientOp(in GameServiceProtocol.GameInstanceClientOp gameInstanceClientOp)
        {
            switch (gameInstanceClientOp.Type)
            {
                case GameServiceProtocol.GameInstanceClientOp.OpType.Add:
                    return _gameManager.AddClientToGame(gameInstanceClientOp.Client, gameInstanceClientOp.GameId);

                case GameServiceProtocol.GameInstanceClientOp.OpType.Remove:
                    return _gameManager.RemoveClientFromGame(gameInstanceClientOp.Client, gameInstanceClientOp.GameId);

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): Unexpected operation type {gameInstanceClientOp.Type}");
            }
        }

        private bool OnLeaderboardStateChange(in GameServiceProtocol.LeaderboardStateChange leaderboardStateChange)
        {
            LeaderboardInfoCache.Instance.UpdateLeaderboardInstance(leaderboardStateChange);
            BroadcastServiceMessageToGames(leaderboardStateChange);
            return true;
        }

        private bool OnLeaderboardStateChangeList(in GameServiceProtocol.LeaderboardStateChangeList leaderboardStateChangeList)
        {
            LeaderboardInfoCache.Instance.UpdateLeaderboardInstances(leaderboardStateChangeList);
            return true;
        }

        private bool OnLeaderboardRewardRequestResponse(in GameServiceProtocol.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse)
        {
            ulong participantId = leaderboardRewardRequestResponse.ParticipantId;

            if (_gameManager.TryGetGameForPlayerDbId(participantId, out Game game) == false)
                return Logger.WarnReturn(false, $"OnLeaderboardRewardRequestResponse(): Participant 0x{participantId:X} is not in a game");

            game.ReceiveServiceMessage(leaderboardRewardRequestResponse);
            return true;
        }

        #endregion
    }
}
