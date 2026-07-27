using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.MTXStore;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Communities;

namespace MHServerEmu.Games.Network
{
    /// <summary>
    /// <see cref="ServiceMailbox"/> implementation used by individual game instances.
    /// </summary>
    public sealed class GameServiceMailbox : ServiceMailbox
    {
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

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                // V48_FIXME
                case ServiceMessage.PartyOperationRequestServerResult partyOperationRequestServerResult:
                    OnPartyOperationRequestServerResult(partyOperationRequestServerResult);
                    break;

                case ServiceMessage.PartyInfoServerUpdate partyInfoServerUpdate:
                    OnPartyInfoServerUpdate(partyInfoServerUpdate);
                    break;

                case ServiceMessage.PartyMemberInfoServerUpdate partyMemberInfoServerUpdate:
                    OnPartyMemberInfoServerUpdate(partyMemberInfoServerUpdate);
                    break;

                case ServiceMessage.PartyKickGracePeriod partyKickGracePeriod:
                    OnPartyKickGracePeriod(partyKickGracePeriod);
                    break;
#endif

                case ServiceMessage.GuildMessageToServer guildMessageToServer:
                    OnGuildMessageToServer(guildMessageToServer);
                    break;

                case ServiceMessage.GuildMessageToClient guildMessageToClient:
                    OnGuildMessageToClient(guildMessageToClient);
                    break;

                case ServiceMessage.MatchQueueUpdate matchQueueUpdate:
                    OnMatchQueueUpdate(matchQueueUpdate);
                    break;

                case ServiceMessage.MatchQueueFlush matchQueueFlush:
                    OnMatchQueueFlush(matchQueueFlush);
                    break;

                case ServiceMessage.SetLiveTuningValues setLiveTuningValues:
                    OnSetLiveTuningValues(setLiveTuningValues);
                    break;

                case ServiceMessage.LeaderboardStateChange leaderboardStateChange:
                    OnLeaderboardStateChange(leaderboardStateChange);
                    break;

                case ServiceMessage.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse:
                    OnLeaderboardRewardRequestResponse(leaderboardRewardRequestResponse);
                    break;

                case ServiceMessage.MTXStoreESBalanceGameRequest mtxStoreESBalanceGameRequest:
                    OnMTXStoreESBalanceGameRequest(mtxStoreESBalanceGameRequest);
                    break;

                case ServiceMessage.MTXStoreESConvertGameRequest mtxStoreESConvertGameRequest:
                    OnMTXStoreESConvertGameRequest(mtxStoreESConvertGameRequest);
                    break;

                default:
                    Verify.IsTrue(false, $"Unhandled service message type {message.GetType().Name}");
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

        private void OnShutdownRegion(in ServiceMessage.ShutdownRegion shutdownRegion)
        {
            Game.RegionManager.DestroyRegion(shutdownRegion.RegionId);
        }

        private void OnDestroyPortal(in ServiceMessage.DestroyPortal destroyPortal)
        {
            // This portal may already be destroyed if its region was shut down, which is fine.
            Transition portal = Game.EntityManager.GetEntityByDbGuid<Transition>(destroyPortal.Portal.EntityDbId);
            portal?.Destroy();
        }

        private void OnUnableToChangeRegion(in ServiceMessage.UnableToChangeRegion unableToChangeRegion)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(unableToChangeRegion.PlayerDbId);
            if (!Verify.IsNotNull(player)) return;

            PlayerConnection playerConnection = player.PlayerConnection;
            playerConnection.CancelRegionTransfer(unableToChangeRegion.ChangeFailed);
        }

        private void OnGameAndRegionForPlayer(in ServiceMessage.GameAndRegionForPlayer gameAndRegionForPlayer)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(gameAndRegionForPlayer.PlayerDbId);
            if (!Verify.IsNotNull(player)) return;

            PlayerConnection playerConnection = player.PlayerConnection;
            playerConnection.FinishRegionTransfer(gameAndRegionForPlayer.TransferParams, gameAndRegionForPlayer.WorldViewSyncData);
        }

        private void OnWorldViewSync(in ServiceMessage.WorldViewSync worldViewSync)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(worldViewSync.PlayerDbId);
            if (!Verify.IsNotNull(player)) return;

            player.PlayerConnection.WorldView.Sync(worldViewSync.SyncData);
        }

        private void OnPlayerLookupByNameResult(in ServiceMessage.PlayerLookupByNameResult playerLookupByNameResult)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerLookupByNameResult.PlayerDbId);
            if (!Verify.IsNotNull(player)) return;

            ulong remoteJobId = playerLookupByNameResult.RemoteJobId;
            ulong resultPlayerDbId = playerLookupByNameResult.ResultPlayerDbId;
            string resultPlayerName = playerLookupByNameResult.ResultPlayerName;

            player.Community.OnPlayerLookupByNameResult(remoteJobId, resultPlayerDbId, resultPlayerName);
        }

        private void OnCommunityBroadcastBatch(in ServiceMessage.CommunityBroadcastBatch communityBroadcastBatch)
        {
            if (communityBroadcastBatch.PlayerDbId != 0)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(communityBroadcastBatch.PlayerDbId);
                if (!Verify.IsNotNull(player)) return;

                Community community = player.Community;
                if (!Verify.IsNotNull(community)) return;

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
                    if (!Verify.IsNotNull(community))
                        continue;

                    for (int i = 0; i < communityBroadcastBatch.Count; i++)
                    {
                        CommunityMemberBroadcast broadcast = communityBroadcastBatch[i];
                        community.ReceiveMemberBroadcast(broadcast);
                    }
                }
            }
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        // V48_FIXME
        private void OnPartyOperationRequestServerResult(in ServiceMessage.PartyOperationRequestServerResult partyOperationRequestServerResult)
        {
            ulong playerDbId = partyOperationRequestServerResult.PlayerDbId;
            PartyOperationPayload request = partyOperationRequestServerResult.Request;
            GroupingOperationResult result = partyOperationRequestServerResult.Result;

            Game.PartyManager.OnPartyOperationRequestServerResult(playerDbId, request, result);
        }

        private void OnPartyInfoServerUpdate(in ServiceMessage.PartyInfoServerUpdate partyInfoServerUpdate)
        {
            ulong playerDbId = partyInfoServerUpdate.PlayerDbId;
            ulong groupId = partyInfoServerUpdate.GroupId;
            PartyInfo partyInfo = partyInfoServerUpdate.PartyInfo;

            Game.PartyManager.OnPartyInfoServerUpdate(playerDbId, groupId, partyInfo);
        }

        private void OnPartyMemberInfoServerUpdate(in ServiceMessage.PartyMemberInfoServerUpdate partyMemberInfoServerUpdate)
        {
            ulong playerDbId = partyMemberInfoServerUpdate.PlayerDbId;
            ulong groupId = partyMemberInfoServerUpdate.GroupId;
            ulong memberDbId = partyMemberInfoServerUpdate.MemberDbId;
            PartyMemberEvent memberEvent = partyMemberInfoServerUpdate.MemberEvent;
            PartyMemberInfo memberInfo = partyMemberInfoServerUpdate.MemberInfo;

            Game.PartyManager.OnPartyMemberInfoServerUpdate(playerDbId, groupId, memberDbId, memberEvent, memberInfo);
        }

        private void OnPartyKickGracePeriod(in ServiceMessage.PartyKickGracePeriod partyKickGracePeriod)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(partyKickGracePeriod.PlayerDbId);
            player?.SendMessage(NetMessagePartyKickGracePeriod.CreateBuilder()
                .SetExpireTimeMicroseconds(partyKickGracePeriod.ExpireTimeMicroseconds)
                .SetLeaveReason(partyKickGracePeriod.LeaveReason)
                .Build());
        }
#endif

        private void OnGuildMessageToServer(in ServiceMessage.GuildMessageToServer guildMessageToServer)
        {
            Game.GuildManager.OnGuildMessage(guildMessageToServer.Messages);
        }

        private void OnGuildMessageToClient(in ServiceMessage.GuildMessageToClient guildMessageToClient)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(guildMessageToClient.PlayerDbId);
            if (player == null)
                return;

            NetMessageGuildMessageToClient clientMessage = NetMessageGuildMessageToClient.CreateBuilder()
                .SetMessages(guildMessageToClient.Messages)
                .Build();

            player.SendMessage(clientMessage);
        }

        private void OnMatchQueueUpdate(in ServiceMessage.MatchQueueUpdate matchQueueUpdate)
        {
            ulong playerDbId = matchQueueUpdate.PlayerDbId;
            PrototypeId regionRef = (PrototypeId)matchQueueUpdate.RegionProtoId;
            PrototypeId difficultyTierRef = (PrototypeId)matchQueueUpdate.DifficultyTierProtoId;
            int playersInQueue = matchQueueUpdate.PlayersInQueue;
            ulong groupId = matchQueueUpdate.RegionRequestGroupId;
            List<ServiceMessage.MatchQueueUpdateData> data = matchQueueUpdate.Data;

            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (player == null)
                return;

            if (data == null)
                return;

            foreach (ServiceMessage.MatchQueueUpdateData dataEntry in data)
            {
                ulong updatePlayerDbId = dataEntry.UpdatePlayerGuid;
                string updatePlayerName = dataEntry.UpdatePlayerName ?? string.Empty;
                RegionRequestQueueUpdateVar status = dataEntry.Status;

                player.UpdateMatchQueue(updatePlayerDbId, regionRef, difficultyTierRef, playersInQueue, groupId, status, updatePlayerName);
            }
        }

        private void OnMatchQueueFlush(in ServiceMessage.MatchQueueFlush matchQueueFlush)
        {
            ulong playerDbId = matchQueueFlush.PlayerDbId;

            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (player == null)
                return;

            player.MatchQueueStatus.Flush();
        }

        private void OnSetLiveTuningValues(in ServiceMessage.SetLiveTuningValues setLiveTuningValues)
        {
            List<NetStructLiveTuningSettingProtoEnumValue> settings = setLiveTuningValues.Settings;
            if (!Verify.IsNotNull(settings)) return;

            foreach (NetStructLiveTuningSettingProtoEnumValue setting in settings)
            {
                PrototypeId tuningProtoRef = GameDatabase.GetDataRefByPrototypeGuid((PrototypeGuid)setting.TuningVarProtoId);
                if (!Verify.IsTrue(tuningProtoRef != PrototypeId.Invalid))
                    continue;

                Prototype tuningProto = GameDatabase.GetPrototype<Prototype>(tuningProtoRef);
                if (!Verify.IsNotNull(tuningProto))
                    continue;

                switch (tuningProto)
                {
                    case WorldEntityPrototype worldEntityProto:
                        if (setting.TuningVarEnum == (int)WorldEntityTuningVar.eWETV_Visible)
                        {
                            if (worldEntityProto is AvatarPrototype)
                                break;

                            bool updateAll = setting.TuningVarValue != 0f;

                            foreach (Entity entity in Game.EntityManager)
                            {
                                if (entity.PrototypeDataRef != tuningProtoRef)
                                    continue;

                                if (entity is not WorldEntity worldEntity)
                                    continue;

                                if (worldEntity.IsInWorld == false)
                                    continue;

                                worldEntity.UpdateInterestPolicies(updateAll);
                                worldEntity.UpdateSimulationState();
                            }
                        }

                        break;

                    case MissionPrototype missionProto:
                        if (setting.TuningVarEnum == (int)MissionTuningVar.eMTV_EventInstance)
                        {
                            foreach (Player player in new PlayerIterator(Game))
                            {
                                Region region = player.GetRegion();
                                if (region == null)
                                    continue;

                                Mission mission = player.MissionManager?.MissionByDataRef(tuningProtoRef);
                                if (!Verify.IsNotNull(mission))
                                    continue;

                                mission.RestartMission();
                            }
                        }
                        break;

                    case PublicEventPrototype publicEventProto:
                        // V48_TODO: ePETV_EventInstance
                        break;
                }
            }
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
                    player.SendMessage(message);
            }
        }

        private void OnLeaderboardRewardRequestResponse(in ServiceMessage.LeaderboardRewardRequestResponse leaderboardRewardRequestResponse)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(leaderboardRewardRequestResponse.ParticipantId);
            if (!Verify.IsNotNull(player)) return;

            player.LeaderboardManager.AddPendingRewards(leaderboardRewardRequestResponse.Entries);
        }

        private void OnMTXStoreESBalanceGameRequest(in ServiceMessage.MTXStoreESBalanceGameRequest mtxStoreESBalanceGameRequest)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(mtxStoreESBalanceGameRequest.PlayerDbId);
            if (!Verify.IsNotNull(player)) return;

            int currentBalance = player.Properties[PropertyEnum.Currency, GameDatabase.CurrencyGlobalsPrototype.EternitySplinters];

            var config = ConfigManager.Instance.GetConfig<MTXStoreConfig>();
            float conversionRatio = config.ESToGazillioniteConversionRatio;
            int conversionStep = config.ESToGazillioniteConversionStep;

            ServiceMessage.MTXStoreESBalanceGameResponse response = new(mtxStoreESBalanceGameRequest.RequestId, currentBalance, conversionRatio, conversionStep);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, response);
        }

        private void OnMTXStoreESConvertGameRequest(in ServiceMessage.MTXStoreESConvertGameRequest mtxStoreESConvertGameRequest)
        {
            Player player = Game.EntityManager.GetEntityByDbGuid<Player>(mtxStoreESConvertGameRequest.PlayerDbId);
            if (!Verify.IsNotNull(player)) return;

            int gAmount = player.ConvertEternitySplintersToGazillionite(mtxStoreESConvertGameRequest.Amount);

            ServiceMessage.MTXStoreESConvertGameResponse response = new(mtxStoreESConvertGameRequest.RequestId, gAmount > 0);
            ServerManager.Instance.SendMessageToService(GameServiceType.PlayerManager, response);
        }

        #endregion
    }
}
