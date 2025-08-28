﻿using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// <see cref="ServiceMailbox"/> implementation used by individual game instances.
    /// </summary>
    public sealed class GameServiceMailbox : ServiceMailbox
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public Game Game { get; }

        public GameServiceMailbox(Game game)
        {
            Game = game;
        }

        protected override void HandleServiceMessage(IGameServiceMessage message)
        {
            switch (message)
            {
                case ServiceMessage.CreateRegion createRegion:
                    OnCreateRegion(createRegion);
                    break;

                case ServiceMessage.ShutdownRegion shutdownRegion:
                    OnShutdownRegion(shutdownRegion);
                    break;

                case ServiceMessage.DestroyPortal destroyPortal:
                    OnDestroyPortal(destroyPortal);
                    break;

                case ServiceMessage.UnableToChangeRegion unableToChangeRegion:
                    OnUnableToChangeRegion(unableToChangeRegion);
                    break;

                case ServiceMessage.GameAndRegionForPlayer gameAndRegionForPlayer:
                    OnGameAndRegionForPlayer(gameAndRegionForPlayer);
                    break;

                case ServiceMessage.WorldViewSync worldViewSync:
                    OnWorldViewSync(worldViewSync);
                    break;

                case ServiceMessage.PlayerLookupByNameResult playerLookupByNameResult:
                    OnPlayerLookupByNameResult(playerLookupByNameResult);
                    break;

                case ServiceMessage.CommunityBroadcastBatch communityBroadcastBatch:
                    OnCommunityBroadcastBatch(communityBroadcastBatch);
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

        private void OnCreateRegion(in ServiceMessage.CreateRegion createRegion)
        {
            ulong regionId = createRegion.RegionId;
            PrototypeId regionProtoRef = (PrototypeId)createRegion.RegionProtoRef;
            NetStructCreateRegionParams createParams = createRegion.CreateParams;

            Region region = Game.RegionManager.GenerateRegion(regionId, regionProtoRef, createParams);

            ServiceMessage.CreateRegionResult response = new(regionId, region != null);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, response);
        }

        private bool OnShutdownRegion(in ServiceMessage.ShutdownRegion shutdownRegion)
        {
            Logger.Trace($"Received ShutdownRegion for region 0x{shutdownRegion.RegionId:X}");
            return Game.RegionManager.DestroyRegion(shutdownRegion.RegionId);
        }

        private void OnDestroyPortal(in ServiceMessage.DestroyPortal destroyPortal)
        {
            // This portal may already be destroyed if its region was shut down, which is fine.
            Transition portal = Game.EntityManager.GetEntityByDbGuid<Transition>(destroyPortal.Portal.EntityDbId);
            portal?.Destroy();
        }

        private bool OnUnableToChangeRegion(in ServiceMessage.UnableToChangeRegion unableToChangeRegion)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(unableToChangeRegion.PlayerDbId);
            if (player == null) return Logger.WarnReturn(false, "OnUnableToChangeRegion(): player == null");

            PlayerConnection playerConnection = player.PlayerConnection;
            playerConnection.CancelRegionTransfer(unableToChangeRegion.ChangeFailed);
            return true;
        }

        private bool OnGameAndRegionForPlayer(in ServiceMessage.GameAndRegionForPlayer gameAndRegionForPlayer)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(gameAndRegionForPlayer.PlayerDbId);
            if (player == null) return Logger.WarnReturn(false, "OnGameAndRegionForPlayer(): player == null");

            PlayerConnection playerConnection = player.PlayerConnection;
            playerConnection.FinishRegionTransfer(gameAndRegionForPlayer.TransferParams, gameAndRegionForPlayer.WorldViewSyncData);
            return true;
        }

        private bool OnWorldViewSync(in ServiceMessage.WorldViewSync worldViewSync)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(worldViewSync.PlayerDbId);
            if (player == null) return Logger.WarnReturn(false, "OnWorldViewUpdate(): player == null");

            player.PlayerConnection.WorldView.Sync(worldViewSync.SyncData);
            return true;
        }

        private bool OnPlayerLookupByNameResult(in ServiceMessage.PlayerLookupByNameResult playerLookupByNameResult)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerLookupByNameResult.PlayerDbId);
            if (player == null) return Logger.WarnReturn(false, "OnPlayerLookupByNameResult(): player == null");

            ulong remoteJobId = playerLookupByNameResult.RemoteJobId;
            ulong resultPlayerDbId = playerLookupByNameResult.ResultPlayerDbId;
            string resultPlayerName = playerLookupByNameResult.ResultPlayerName;

            player.Community.OnPlayerLookupByNameResult(remoteJobId, resultPlayerDbId, resultPlayerName);
            return true;
        }

        private bool OnCommunityBroadcastBatch(in ServiceMessage.CommunityBroadcastBatch communityBroadcastBatch)
        {
            if (communityBroadcastBatch.PlayerDbId != 0)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(communityBroadcastBatch.PlayerDbId);
                if (player == null) return Logger.WarnReturn(false, "OnPlayerLookupByNameResult(): player == null");

                Community community = player.Community;

                for (int i = 0; i < communityBroadcastBatch.Count; i++)
                {
                    CommunityMemberBroadcast broadcast = communityBroadcastBatch[i];
                    community.ReceiveMemberBroadcast(broadcast);
                }
            }
            else
            {
                foreach (Player player in new PlayerIterator(Game))
                {
                    Community community = player.Community;

                    for (int i = 0; i < communityBroadcastBatch.Count; i++)
                    {
                        CommunityMemberBroadcast broadcast = communityBroadcastBatch[i];
                        community.ReceiveMemberBroadcast(broadcast);
                    }
                }
            }

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
