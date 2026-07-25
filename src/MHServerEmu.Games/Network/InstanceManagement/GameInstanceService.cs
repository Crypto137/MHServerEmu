using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Leaderboards;

namespace MHServerEmu.Games.Network.InstanceManagement
{
    public class GameInstanceService : IGameService
    {
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
            Verify.IsTrue(gameCount == 0, $"{gameCount} games are still running");

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

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                // V48_FIXME
                case ServiceMessage.PartyOperationRequestServerResult partyOperationRequestServerResult:
                    RouteMessageToGame(partyOperationRequestServerResult.GameId, partyOperationRequestServerResult);
                    break;

                case ServiceMessage.PartyInfoServerUpdate partyInfoServerUpdate:
                    RouteMessageToGame(partyInfoServerUpdate.GameId, partyInfoServerUpdate);
                    break;

                case ServiceMessage.PartyMemberInfoServerUpdate partyMemberInfoServerUpdate:
                    RouteMessageToGame(partyMemberInfoServerUpdate.GameId, partyMemberInfoServerUpdate);
                    break;
#endif

                case ServiceMessage.PartyKickGracePeriod partyKickGracePeriod:
                    RouteMessageToGame(partyKickGracePeriod.GameId, partyKickGracePeriod);
                    break;

                case ServiceMessage.GuildMessageToServer guildMessageToServer:
                    RouteMessageToGame(guildMessageToServer.GameId, guildMessageToServer);
                    break;

                case ServiceMessage.GuildMessageToClient guildMessageToClient:
                    RouteMessageToGame(guildMessageToClient.GameId, guildMessageToClient);
                    break;

                case ServiceMessage.MatchQueueUpdate matchQueueUpdate:
                    RouteMessageToGame(matchQueueUpdate.GameId, matchQueueUpdate);
                    break;

                case ServiceMessage.MatchQueueFlush matchQueueFlush:
                    RouteMessageToGame(matchQueueFlush.GameId, matchQueueFlush);
                    break;

                case ServiceMessage.SetLiveTuningValues setLiveTuningValues:
                    GameManager.BroadcastServiceMessageToGames(setLiveTuningValues);
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
                    Verify.IsTrue(false, $"Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public void GetStatus(Dictionary<string, long> statusDict)
        {
            statusDict["GisGames"] = GameManager.GameCount;
            statusDict["GisPlayers"] = GameManager.PlayerCount;
        }

        private void RouteMessageToGame<T>(ulong gameId, T message) where T: struct, IGameServiceMessage
        {
            if (!Verify.IsTrue(GameManager.TryGetGameById(gameId, out Game game), $"Game 0x{gameId:X} not found, {typeof(T).Name} will not be delivered"))
                return;

            game.ReceiveServiceMessage(message);
        }

        #endregion

        #region Message Handling

        private void OnRouteMessageBuffer(in ServiceMessage.RouteMessageBuffer routeMessageBuffer)
        {
            GameManager.RouteMessageBuffer(routeMessageBuffer.Client, routeMessageBuffer.MessageBuffer);
        }

        private void OnGameInstanceOp(in ServiceMessage.GameInstanceOp gameInstanceOp)
        {
            switch (gameInstanceOp.Type)
            {
                case GameInstanceOpType.Create:
                    GameManager.CreateGame(gameInstanceOp.GameId);
                    break;

                case GameInstanceOpType.Shutdown:
                    GameManager.ShutdownGame(gameInstanceOp.GameId, GameShutdownReason.ShutdownRequested);
                    break;

                default:
                    Verify.IsTrue(false, $"Unhandled operation type {gameInstanceOp.Type}");
                    break;
            }
        }

        private void OnGameInstanceClientOp(in ServiceMessage.GameInstanceClientOp gameInstanceClientOp)
        {
            IFrontendClient client = gameInstanceClientOp.Client;
            ulong gameId = gameInstanceClientOp.GameId;

            switch (gameInstanceClientOp.Type)
            {
                case GameInstanceClientOpType.Add:
                    if (GameManager.AddClientToGame(client, gameId) == false)
                        client.Disconnect();    // Disconnect the client so that it doesn't get stuck waiting to be added to a game
                    break;

                case GameInstanceClientOpType.Remove:
                    GameManager.RemoveClientFromGame(client, gameId);
                    break;

                default:
                    Verify.IsTrue(false, $"Unhandled operation type {gameInstanceClientOp.Type}");
                    break;
            }
        }

        private void OnLeaderboardStateChange(in ServiceMessage.LeaderboardStateChange leaderboardStateChange)
        {
            LeaderboardInfoCache.Instance.UpdateLeaderboardInstance(leaderboardStateChange);
            GameManager.BroadcastServiceMessageToGames(leaderboardStateChange);
        }

        private void OnLeaderboardStateChangeList(in ServiceMessage.LeaderboardStateChangeList leaderboardStateChangeList)
        {
            LeaderboardInfoCache.Instance.UpdateLeaderboardInstances(leaderboardStateChangeList);
        }

        private void OnLeaderboardRewardRequestResponse(in ServiceMessage.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse)
        {
            ulong playerDbId = leaderboardRewardRequestResponse.ParticipantId;
            GameManager.RouteServiceMessageToPlayer(playerDbId, leaderboardRewardRequestResponse);
        }

        #endregion
    }
}
