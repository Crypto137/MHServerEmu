using Gazillion;
using MHServerEmu.Core.Collections;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// Allows <see cref="IGameService"/> implementations to send <see cref="IGameServiceMessage"/> instances to a <see cref="Game"/>. 
    /// </summary>
    public class ServiceMailbox
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        // IGameServiceMessage are boxed anyway when doing pattern matching, so it should probably be fine.
        // If we encounter performance issues here, replace this with a specialized data structure.
        private readonly DoubleBufferQueue<IGameServiceMessage> _messageQueue = new();

        public Game Game { get; }

        public ServiceMailbox(Game game)
        {
            Game = game;
        }

        /// <summary>
        /// Called from other threads to post an <see cref="IGameServiceMessage"/>
        /// </summary>
        public void PostMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            _messageQueue.Enqueue(message);
        }

        public void ProcessMessages()
        {
            _messageQueue.Swap();

            while (_messageQueue.CurrentCount > 0)
            {
                IGameServiceMessage serviceMessage = _messageQueue.Dequeue();
                HandleServiceMessage(serviceMessage);
            }
        }

        private void HandleServiceMessage(IGameServiceMessage message)
        {
            switch (message)
            {
                case ServiceMessage.GameInstanceCreateRegion gameInstanceCreateRegion:
                    OnGameInstanceCreateRegion(gameInstanceCreateRegion);
                    break;

                case ServiceMessage.GameAndRegionForPlayer gameAndRegionForPlayer:
                    OnGameAndRegionForPlayer(gameAndRegionForPlayer);
                    break;

                case ServiceMessage.LeaderboardStateChange leaderboardStateChange:
                    OnLeaderboardStateChange(leaderboardStateChange);
                    break;

                case ServiceMessage.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse:
                    OnLeaderboardRewardRequestResponse(leaderboardRewardRequestResponse);
                    break;

                default:
                    Logger.Warn($"Unhandled service message type {message.GetType().Name}");
                    break;
            }
        }

        #region Message Handling

        private void OnGameInstanceCreateRegion(in ServiceMessage.GameInstanceCreateRegion gameInstanceCreateRegion)
        {
            ulong regionId = gameInstanceCreateRegion.RegionId;
            PrototypeId regionProtoRef = (PrototypeId)gameInstanceCreateRegion.RegionProtoRef;
            NetStructCreateRegionParams createParams = gameInstanceCreateRegion.CreateParams;

            Region region = Game.RegionManager.GenerateRegion(regionId, regionProtoRef, createParams);

            ServiceMessage.GameInstanceCreateRegionResponse response = new(regionId, region != null);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, response);
        }

        private bool OnGameAndRegionForPlayer(in ServiceMessage.GameAndRegionForPlayer gameAndRegionForPlayer)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(gameAndRegionForPlayer.PlayerDbId);
            if (player == null) return Logger.WarnReturn(false, "OnGameAndRegionForPlayer(): player == null");

            PlayerConnection playerConnection = player.PlayerConnection;
            playerConnection.ReceiveTransferParams(gameAndRegionForPlayer.TransferParams);
            return true;
        }

        private void OnLeaderboardStateChange(in ServiceMessage.LeaderboardStateChange leaderboardStateChange)
        {
            LeaderboardState state = leaderboardStateChange.State;
            bool rewarded = state == LeaderboardState.eLBS_Rewarded;
            bool sendClient = state == LeaderboardState.eLBS_Created
                || state == LeaderboardState.eLBS_Active
                || state == LeaderboardState.eLBS_Expired
                || state == LeaderboardState.eLBS_Rewarded;

            NetMessageLeaderboardStateChange message = null;
            if (sendClient)
                message = leaderboardStateChange.ToProtobuf();

            foreach (var player in new PlayerIterator(Game))
            {
                player.LeaderboardManager.OnUpdateEventContext();

                if (rewarded)
                    player.LeaderboardManager.RequestRewards();

                if (sendClient)
                {
                    //Logger.Debug($"OnLeaderboardStateChange(): Sending [{leaderboardStateChange.InstanceId}][{state}] to {player.GetName()}");
                    player.SendMessage(message);
                }
            }
        }

        private bool OnLeaderboardRewardRequestResponse(in ServiceMessage.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse)
        {
            ulong playerId = leaderboardRewardRequestResponse.ParticipantId;
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(leaderboardRewardRequestResponse.ParticipantId);
            if (player == null)
                return Logger.WarnReturn(false, $"OnLeaderboardRewardRequestResponse(): Player 0x{playerId:X} not found in game [{Game}]");

            player.LeaderboardManager.AddPendingRewards(leaderboardRewardRequestResponse.Entries);
            return true;
        }

        #endregion
    }
}
