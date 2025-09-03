﻿using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System;
using MHServerEmu.PlayerManagement.Players;
using MHServerEmu.PlayerManagement.Regions;
using MHServerEmu.PlayerManagement.Social;

namespace MHServerEmu.PlayerManagement.Network
{
    internal sealed class PlayerManagerServiceMailbox : ServiceMailbox
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly TimeLeakyBucketCollection<ulong> _playerLookupByNameRateLimiter = new(TimeSpan.FromSeconds(60), 20);

        private readonly PlayerManagerService _playerManager;

        public PlayerManagerServiceMailbox(PlayerManagerService playerManager)
        {
            _playerManager = playerManager;
        }

        protected override void HandleServiceMessage(IGameServiceMessage message)
        {
            switch (message)
            {
                case ServiceMessage.AddClient addClient:
                    OnAddClient(addClient);
                    break;

                case ServiceMessage.RemoveClient removeClient:
                    OnRemoveClient(removeClient);
                    break;

                case ServiceMessage.GameInstanceOp gameInstanceOp:
                    OnGameInstanceOp(gameInstanceOp);
                    break;

                case ServiceMessage.GameInstanceClientOp gameInstanceClientOp:
                    OnGameInstanceClientOp(gameInstanceClientOp);
                    break;

                case ServiceMessage.CreateRegionResult createRegionResult:
                    OnCreateRegionResult(createRegionResult);
                    break;

                case ServiceMessage.RequestRegionShutdown requestRegionShutdown:
                    OnRequestRegionShutdown(requestRegionShutdown);
                    break;

                case ServiceMessage.ChangeRegionRequest changeRegionRequest:
                    OnChangeRegionRequest(changeRegionRequest);
                    break;

                case ServiceMessage.RegionTransferFinished regionTransferFinished:
                    OnRegionTransferFinished(regionTransferFinished);
                    break;

                case ServiceMessage.ClearPrivateStoryRegions clearPrivateStoryRegions:
                    OnClearPrivateStoryRegions(clearPrivateStoryRegions);
                    break;

                case ServiceMessage.PlayerLookupByNameRequest playerLookupByNameRequest:
                    OnPlayerLookupByNameRequest(playerLookupByNameRequest);
                    break;

                case ServiceMessage.PlayerNameChanged playerNameChanged:
                    OnPlayerNameChanged(playerNameChanged);
                    break;

                case ServiceMessage.CommunityStatusUpdate communityStatusUpdate:
                    OnCommunityStatusUpdate(communityStatusUpdate);
                    break;

                case ServiceMessage.CommunityStatusRequest communityStatusRequest:
                    OnCommunityStatusRequest(communityStatusRequest);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {message.GetType().Name}");
                    break;
            }
        }

        #region Handlers

        private bool OnAddClient(in ServiceMessage.AddClient addClient)
        {
            IFrontendClient client = addClient.Client;
            return _playerManager.ClientManager.AddClient(client);
        }

        private bool OnRemoveClient(in ServiceMessage.RemoveClient removeClient)
        {
            IFrontendClient client = removeClient.Client;
            return _playerManager.ClientManager.RemoveClient(client);
        }

        private bool OnGameInstanceOp(in ServiceMessage.GameInstanceOp gameInstanceOp)
        {
            GameInstanceOpType type = gameInstanceOp.Type;
            ulong gameId = gameInstanceOp.GameId;

            switch (type)
            {
                case GameInstanceOpType.CreateResponse:
                    _playerManager.GameHandleManager.OnInstanceCreateResponse(gameId);
                    break;

                case GameInstanceOpType.ShutdownNotice:
                    _playerManager.GameHandleManager.OnInstanceShutdownNotice(gameId);
                    break;

                default:
                    Logger.Warn($"OnGameInstanceOp(): Unhandled operation type {type}");
                    break;
            }

            return true;
        }

        private bool OnGameInstanceClientOp(in ServiceMessage.GameInstanceClientOp gameInstanceClientOp)
        {
            IFrontendClient client = gameInstanceClientOp.Client;
            ulong gameId = gameInstanceClientOp.GameId;

            if (_playerManager.ClientManager.TryGetPlayerHandle(client.DbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): No handle found for client [{client}]");

            switch (gameInstanceClientOp.Type)
            {
                case GameInstanceClientOpType.AddResponse:
                    player.FinishAddToGame(gameId);
                    break;

                case GameInstanceClientOpType.RemoveResponse:
                    player.FinishRemoveFromGame(gameId);
                    break;

                default:
                    return Logger.WarnReturn(false, $"OnGameInstanceClientOp(): Unhandled operation type {gameInstanceClientOp.Type}");
            }

            return true;
        }

        private bool OnCreateRegionResult(in ServiceMessage.CreateRegionResult createRegionResponse)
        {
            RegionHandle region = _playerManager.WorldManager.GetRegion(createRegionResponse.RegionId);
            if (region == null)
                return Logger.WarnReturn(false, $"OnCreateRegionResponse(): Region 0x{createRegionResponse.RegionId:X} not found");

            region.OnInstanceCreateResponse(createRegionResponse.Success);
            return true;
        }

        private bool OnRequestRegionShutdown(in ServiceMessage.RequestRegionShutdown requestRegionShutdown)
        {
            RegionHandle region = _playerManager.WorldManager.GetRegion(requestRegionShutdown.RegionId);
            if (region == null)
                return Logger.WarnReturn(false, $"OnRequestRegionShutdown(): Region 0x{requestRegionShutdown.RegionId:X} not found");

            region.RequestShutdown();
            return true;
        }

        private bool OnChangeRegionRequest(in ServiceMessage.ChangeRegionRequest changeRegionRequest)
        {
            ulong requestingGameId = changeRegionRequest.Header.RequestingGameId;
            ulong playerDbId = changeRegionRequest.Header.RequestingPlayerGuid;
            TeleportContextEnum context = changeRegionRequest.Header.Type;

            if (_playerManager.ClientManager.TryGetPlayerHandle(playerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnChangeRegionRequest(): No player handle for dbid 0x{playerDbId:X}");

            if (changeRegionRequest.DestTarget != null)
                return player.BeginRegionTransferToTarget(requestingGameId, context, changeRegionRequest.DestTarget, changeRegionRequest.CreateRegionParams);

            if (changeRegionRequest.DestLocation != null)
                return player.BeginRegionTransferToLocation(requestingGameId, context, changeRegionRequest.DestLocation);

            if (changeRegionRequest.DestPlayerDbId != 0)
                return player.BeginRegionTransferToPlayer(requestingGameId, changeRegionRequest.DestPlayerDbId);

            Logger.Warn($"BeginRegionTransfer(): ChangeRegionRequest for player [{this}] does not include transfer params");
            player.CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_GenericError);
            return false;
        }

        private bool OnRegionTransferFinished(in ServiceMessage.RegionTransferFinished regionTransferFinished)
        {
            if (_playerManager.ClientManager.TryGetPlayerHandle(regionTransferFinished.PlayerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnRegionTransferFinished(): No handle found for playerDbId 0x{regionTransferFinished.PlayerDbId}");

            return player.FinishRegionTransfer(regionTransferFinished.TransferId);
        }

        private bool OnClearPrivateStoryRegions(in ServiceMessage.ClearPrivateStoryRegions clearPrivateStoryRegions)
        {
            if (_playerManager.ClientManager.TryGetPlayerHandle(clearPrivateStoryRegions.PlayerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"OnClearPrivateStoryRegions(): No handle found for playerDbId 0x{clearPrivateStoryRegions.PlayerDbId}");

            player.WorldView.ClearPrivateStoryRegions();
            return true;
        }

        private bool OnPlayerLookupByNameRequest(in ServiceMessage.PlayerLookupByNameRequest playerLookupByNameRequest)
        {
            ulong gameId = playerLookupByNameRequest.GameId;
            ulong playerDbId = playerLookupByNameRequest.PlayerDbId;
            ulong remoteJobId = playerLookupByNameRequest.RemoteJobId;
            string requestPlayerName = playerLookupByNameRequest.RequestPlayerName;

            ulong resultPlayerDbId;
            string resultPlayerName;

            // Rate limit this because it's based on client input, and we may be querying the database. It's okay for this query to fail.
            if (_playerLookupByNameRateLimiter.AddTime(playerDbId))
            {
                PlayerNameCache.Instance.TryGetPlayerDbId(requestPlayerName, out resultPlayerDbId, out resultPlayerName);
            }
            else
            {
                Logger.Warn($"OnPlayerLookupByNameRequest(): Rate limit exceeded for player 0x{playerDbId:X}");
                resultPlayerDbId = 0;
                resultPlayerName = string.Empty;
            }

            ServiceMessage.PlayerLookupByNameResult response = new(gameId, playerDbId, remoteJobId, resultPlayerDbId, resultPlayerName);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, response);

            return true;
        }

        private bool OnPlayerNameChanged(in ServiceMessage.PlayerNameChanged playerNameChanged)
        {
            ulong playerDbId = playerNameChanged.PlayerDbId;
            string newPlayerName = playerNameChanged.NewPlayerName;

            PlayerNameCache.Instance.OnPlayerNameChanged(playerDbId);
            _playerManager.CommunityRegistry.OnPlayerNameChanged(playerDbId, newPlayerName);

            // Update the logged in player
            if (_playerManager.ClientManager.TryGetPlayerHandle(playerDbId, out PlayerHandle player))
            {
                lock (player.Account)
                    player.Account.PlayerName = newPlayerName;
                // TODO: Send player name change to the player entity in a game instance
            }

            return true;
        }

        private bool OnCommunityStatusUpdate(in ServiceMessage.CommunityStatusUpdate communityStatusUpdate)
        {
            CommunityMemberBroadcast broadcast = communityStatusUpdate.Broadcast;
            
            _playerManager.CommunityRegistry.ReceiveMemberBroadcast(broadcast);
            return true;
        }

        private bool OnCommunityStatusRequest(in ServiceMessage.CommunityStatusRequest communityStatusRequest)
        {
            ulong gameId = communityStatusRequest.GameId;
            ulong playerDbId = communityStatusRequest.PlayerDbId;
            List<ulong> members = communityStatusRequest.Members;

            _playerManager.CommunityRegistry.RequestMemberBroadcast(gameId, playerDbId, members);
            return true;
        }

        #endregion
    }
}
