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
            // TODO?: Add common interface for routable messages if we switch to class-based service messages.

            switch (message)
            {
                case ServiceMessage.RouteMessageBuffer routeMessageBuffer:
                    OnRouteMessageBuffer(routeMessageBuffer);
                    break;

                case ServiceMessage.GameInstanceOp gameInstanceOp:
                    OnGameInstanceOp(gameInstanceOp);
                    break;

                case ServiceMessage.GameInstanceClientOp gameInstanceClientOp:
                    OnGameInstanceClientOp(gameInstanceClientOp);
                    break;

                case ServiceMessage.CreateRegion createRegion:
                    RouteMessageToGame(createRegion.GameId, createRegion);
                    break;

                case ServiceMessage.ShutdownRegion shutdownRegion:
                    RouteMessageToGame(shutdownRegion.GameId, shutdownRegion);
                    break;

                case ServiceMessage.DestroyPortal destroyPortal:
                    RouteMessageToGame(destroyPortal.GameId, destroyPortal);
                    break;

                case ServiceMessage.UnableToChangeRegion unableToChangeRegion:
                    RouteMessageToGame(unableToChangeRegion.GameId, unableToChangeRegion);
                    break;

                case ServiceMessage.GameAndRegionForPlayer gameAndRegionForPlayer:
                    RouteMessageToGame(gameAndRegionForPlayer.GameId, gameAndRegionForPlayer);
                    break;

                case ServiceMessage.WorldViewSync worldViewUpdate:
                    RouteMessageToGame(worldViewUpdate.GameId, worldViewUpdate);
                    break;

                case ServiceMessage.PlayerLookupByNameResult playerLookupByNameResult:
                    RouteMessageToGame(playerLookupByNameResult.GameId, playerLookupByNameResult);
                    break;

                case ServiceMessage.CommunityBroadcastBatch communityBroadcastBatch:
                    if (communityBroadcastBatch.GameId != 0)
                        RouteMessageToGame(communityBroadcastBatch.GameId, communityBroadcastBatch);
                    else
                        GameManager.BroadcastServiceMessageToGames(communityBroadcastBatch);
                    break;

                case ServiceMessage.PartyOperationRequestServerResult partyOperationRequestServerResult:
                    RouteMessageToGame(partyOperationRequestServerResult.GameId, partyOperationRequestServerResult);
                    break;

                case ServiceMessage.PartyInfoServerUpdate partyInfoServerUpdate:
                    RouteMessageToGame(partyInfoServerUpdate.GameId, partyInfoServerUpdate);
                    break;

                case ServiceMessage.PartyMemberInfoServerUpdate partyMemberInfoServerUpdate:
                    RouteMessageToGame(partyMemberInfoServerUpdate.GameId, partyMemberInfoServerUpdate);
                    break;

                case ServiceMessage.MatchQueueUpdate matchQueueUpdate:
                    RouteMessageToGame(matchQueueUpdate.GameId, matchQueueUpdate);
                    break;

                case ServiceMessage.MatchQueueFlush matchQueueFlush:
                    RouteMessageToGame(matchQueueFlush.GameId, matchQueueFlush);
                    break;

                case ServiceMessage.LeaderboardStateChange leaderboardStateChange:
                    OnLeaderboardStateChange(leaderboardStateChange);
                    break;

                case ServiceMessage.LeaderboardStateChangeList leaderboardStateChangeList:
                    OnLeaderboardStateChangeList(leaderboardStateChangeList);
                    break;

                case ServiceMessage.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse:
                    OnLeaderboardRewardRequestResponse(leaderboardRewardRequestResponse);
                    break;

                case ServiceMessage.MTXStoreESBalanceGameRequest mtxStoreESBalanceGameRequest:
                    RouteMessageToGame(mtxStoreESBalanceGameRequest.GameId, mtxStoreESBalanceGameRequest);
                    break;

                case ServiceMessage.MTXStoreESConvertGameRequest mtxStoreESConvertGameRequest:
                    RouteMessageToGame(mtxStoreESConvertGameRequest.GameId, mtxStoreESConvertGameRequest);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public void GetStatus(Dictionary<string, long> statusDict)
        {
            statusDict["GisGames"] = GameManager.GameCount;
            statusDict["GisPlayers"] = GameManager.PlayerCount;
        }

        private bool RouteMessageToGame<T>(ulong gameId, T message) where T: struct, IGameServiceMessage
        {
            if (GameManager.TryGetGameById(gameId, out Game game) == false)
                return Logger.WarnReturn(false, $"RouteMessageToGame(): Game 0x{gameId:X} not found, {typeof(T).Name} will not be delivered");

            game.ReceiveServiceMessage(message);
            return true;
        }

        #endregion

        #region Message Handling

        private bool OnRouteMessageBuffer(in ServiceMessage.RouteMessageBuffer routeMessageBuffer)
        {
            return GameManager.RouteMessageBuffer(routeMessageBuffer.Client, routeMessageBuffer.MessageBuffer);
        }

        private bool OnGameInstanceOp(in ServiceMessage.GameInstanceOp gameInstanceOp)
        {
            switch (gameInstanceOp.Type)
            {
                case GameInstanceOpType.Create:
                    return GameManager.CreateGame(gameInstanceOp.GameId);

                case GameInstanceOpType.Shutdown:
                    return GameManager.ShutdownGame(gameInstanceOp.GameId, GameShutdownReason.ShutdownRequested);

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceOp(): Unhandled operation type {gameInstanceOp.Type}");
            }
        }

        private bool OnGameInstanceClientOp(in ServiceMessage.GameInstanceClientOp gameInstanceClientOp)
        {
            IFrontendClient client = gameInstanceClientOp.Client;
            ulong gameId = gameInstanceClientOp.GameId;

            switch (gameInstanceClientOp.Type)
            {
                case GameInstanceClientOpType.Add:
                    if (GameManager.AddClientToGame(client, gameId) == false)
                    {
                        // Disconnect the client so that it doesn't get stuck waiting to be added to a game
                        client.Disconnect();
                        return false;
                    }

                    return true;

                case GameInstanceClientOpType.Remove:
                    return GameManager.RemoveClientFromGame(client, gameId);

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): Unhandled operation type {gameInstanceClientOp.Type}");
            }
        }

        private bool OnLeaderboardStateChange(in ServiceMessage.LeaderboardStateChange leaderboardStateChange)
        {
            LeaderboardInfoCache.Instance.UpdateLeaderboardInstance(leaderboardStateChange);
            GameManager.BroadcastServiceMessageToGames(leaderboardStateChange);
            return true;
        }

        private bool OnLeaderboardStateChangeList(in ServiceMessage.LeaderboardStateChangeList leaderboardStateChangeList)
        {
            LeaderboardInfoCache.Instance.UpdateLeaderboardInstances(leaderboardStateChangeList);
            return true;
        }

        private bool OnLeaderboardRewardRequestResponse(in ServiceMessage.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse)
        {
            ulong playerDbId = leaderboardRewardRequestResponse.ParticipantId;
            return GameManager.RouteServiceMessageToPlayer(playerDbId, leaderboardRewardRequestResponse);
        }

        #endregion
    }
}
