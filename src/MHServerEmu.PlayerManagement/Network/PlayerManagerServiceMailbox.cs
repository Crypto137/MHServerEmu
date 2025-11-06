using System.Net;
using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.RateLimiting;
using MHServerEmu.Games.GameData;
using MHServerEmu.PlayerManagement.Auth;
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

                case ServiceMessage.SetDifficultyTierPreference setDifficultyTierPreference:
                    OnSetDifficultyTierPreference(setDifficultyTierPreference);
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

                case ServiceMessage.PartyOperationRequest partyOperationRequest:
                    OnPartyOperationRequest(partyOperationRequest);
                    break;

                case ServiceMessage.PartyBoostUpdate partyBoostUpdate:
                    OnPartyBoostUpdate(partyBoostUpdate);
                    break;

                case ServiceMessage.AuthRequest authRequest:
                    OnAuthRequest(authRequest);
                    break;

                case ServiceMessage.SessionVerificationRequest sessionVerificationRequest:
                    OnSessionVerificationRequest(sessionVerificationRequest);
                    break;

                case ServiceMessage.MTXStoreESBalanceRequest mtxStoreESBalanceRequest:
                    OnMTXStoreESBalanceRequest(mtxStoreESBalanceRequest);
                    break;

                case ServiceMessage.MTXStoreESBalanceGameResponse mtxStoreESBalanceGameResponse:
                    OnMTXStoreESBalanceGameResponse(mtxStoreESBalanceGameResponse);
                    break;

                case ServiceMessage.MTXStoreESConvertRequest mtxStoreESConvertRequest:
                    OnMTXStoreESConvertRequest(mtxStoreESConvertRequest);
                    break;

                case ServiceMessage.MTXStoreESConvertGameResponse mtxStoreESConvertGameResponse:
                    OnMTXStoreESConvertGameResponse(mtxStoreESConvertGameResponse);
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

            PlayerHandle player = _playerManager.ClientManager.GetPlayer(client.DbId);
            if (player == null)
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

            PlayerHandle player = _playerManager.ClientManager.GetPlayer(playerDbId);
            if (player == null)
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
            ulong playerDbId = regionTransferFinished.PlayerDbId;
            ulong transferId = regionTransferFinished.TransferId;

            PlayerHandle player = _playerManager.ClientManager.GetPlayer(playerDbId);
            if (player == null)
                return Logger.WarnReturn(false, $"OnRegionTransferFinished(): No handle found for playerDbId 0x{playerDbId:X}");

            return player.FinishRegionTransfer(regionTransferFinished.TransferId);
        }

        private bool OnClearPrivateStoryRegions(in ServiceMessage.ClearPrivateStoryRegions clearPrivateStoryRegions)
        {
            ulong playerDbId = clearPrivateStoryRegions.PlayerDbId;

            PlayerHandle player = _playerManager.ClientManager.GetPlayer(playerDbId);
            if (player == null)
                return Logger.WarnReturn(false, $"OnClearPrivateStoryRegions(): No handle found for playerDbId 0x{playerDbId:X}");

            player.WorldView.ClearPrivateStoryRegions();
            return true;
        }

        private bool OnSetDifficultyTierPreference(in ServiceMessage.SetDifficultyTierPreference setDifficultyTierPreference)
        {
            ulong playerDbId = setDifficultyTierPreference.PlayerDbId;
            PrototypeId difficultyTierProtoRef = (PrototypeId)setDifficultyTierPreference.DifficultyTierProtoId;

            PlayerHandle player = _playerManager.ClientManager.GetPlayer(playerDbId);
            if (player == null)
                return Logger.WarnReturn(false, $"OnSetDifficultyTierPreference(): No handle found for playerDbId 0x{playerDbId:X}");

            player.SetDifficultyTierPreference(difficultyTierProtoRef);
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
            string oldPlayerName = playerNameChanged.OldPlayerName;
            string newPlayerName = playerNameChanged.NewPlayerName;

            _playerManager.ClientManager.OnPlayerNameChanged(playerDbId, oldPlayerName, newPlayerName);
            PlayerNameCache.Instance.OnPlayerNameChanged(playerDbId);
            _playerManager.CommunityRegistry.OnPlayerNameChanged(playerDbId, newPlayerName);

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

        private bool OnPartyOperationRequest(in ServiceMessage.PartyOperationRequest partyOperationRequest)
        {
            PartyOperationPayload request = partyOperationRequest.Request;

            HashSet<PlayerHandle> playersToNotify = HashSetPool<PlayerHandle>.Instance.Get();

            GroupingOperationResult result = _playerManager.PartyManager.DoPartyOperation(ref request, playersToNotify);

            if (playersToNotify.Count == 0)
                return Logger.WarnReturn(false, "OnPartyOperationRequest(): playersToNotify.Count == 0");

            foreach (PlayerHandle player in playersToNotify)
            {
                if (player.CurrentGame == null)
                    continue;

                ulong gameId = player.CurrentGame.Id;
                ulong playerDbId = player.PlayerDbId;

                ServiceMessage.PartyOperationRequestServerResult message = new(gameId, playerDbId, request, result);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            }

            HashSetPool<PlayerHandle>.Instance.Return(playersToNotify);
            return true;
        }

        private bool OnPartyBoostUpdate(in ServiceMessage.PartyBoostUpdate partyBoostUpdate)
        {
            ulong playerDbId = partyBoostUpdate.PlayerDbId;
            List<ulong> boosts = partyBoostUpdate.Boosts;

            PlayerHandle player = _playerManager.ClientManager.GetPlayer(playerDbId);
            if (player == null)
                return Logger.WarnReturn(false, $"OnPartyBoostUpdate(): No handle found for playerDbId 0x{playerDbId:X}");

            player.SetPartyBoosts(boosts);
            player.CurrentParty?.UpdateMember(player);

            return true;
        }

        private bool OnAuthRequest(in ServiceMessage.AuthRequest authRequest)
        {
            AuthStatusCode statusCode = _playerManager.SessionManager.TryCreateSession(authRequest.LoginDataPB, out AuthTicket authTicket);

            ServiceMessage.AuthResponse response = new(authRequest.RequestId, (int)statusCode, authTicket);
            ServerManager.Instance.SendMessageToService(GameServiceType.WebFrontend, response);

            return true;
        }

        private bool OnSessionVerificationRequest(in ServiceMessage.SessionVerificationRequest sessionVerificationRequest)
        {
            IFrontendClient client = sessionVerificationRequest.Client;
            ClientCredentials clientCredentials = sessionVerificationRequest.ClientCredentials;

            if (_playerManager.SessionManager.VerifyClientCredentials(client, clientCredentials) == false)
            {
                Logger.Warn($"OnClientCredentials(): Failed to verify client credentials, disconnecting client [{client}]");
                client.Disconnect();
                return false;
            }

            _playerManager.LoginQueueManager.EnqueueNewClient(client);
            return true;
        }

        private bool OnMTXStoreESBalanceRequest(in ServiceMessage.MTXStoreESBalanceRequest mtxStoreESBalanceRequest)
        {
            ulong requestId = mtxStoreESBalanceRequest.RequestId;
            string email = mtxStoreESBalanceRequest.Email;
            string token = mtxStoreESBalanceRequest.Token;

            PlayerHandle player = null;

            if (_playerManager.SessionManager.VerifyPlatformTicket(email, token, out ulong playerDbId))
                player = _playerManager.ClientManager.GetPlayer(playerDbId);

            if (player == null || player.State != PlayerHandleState.InGame)
            {
                ServiceMessage.MTXStoreESBalanceResponse response = new(requestId, (int)HttpStatusCode.Forbidden);
                ServerManager.Instance.SendMessageToService(GameServiceType.WebFrontend, response);
                return true;
            }

            Logger.Info($"Authenticated ES balance request from player [{player}]");

            ulong gameId = player.CurrentGame.Id;

            // Route the request to game instance to get up to date balance and conversion ratio
            ServiceMessage.MTXStoreESBalanceGameRequest gameRequest = new(requestId, gameId, playerDbId);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameRequest);

            return true;
        }

        private bool OnMTXStoreESBalanceGameResponse(in ServiceMessage.MTXStoreESBalanceGameResponse mtxStoreESBalanceGameResponse)
        {
            ulong requestId = mtxStoreESBalanceGameResponse.RequestId;
            int currentBalance = mtxStoreESBalanceGameResponse.CurrentBalance;
            float conversionRate = mtxStoreESBalanceGameResponse.ConversionRatio;
            int conversionStep = mtxStoreESBalanceGameResponse.ConversionStep;

            // We should have already handled authentication before routing the request to the game instance, so just route the result back.
            ServiceMessage.MTXStoreESBalanceResponse response = new(requestId, (int)HttpStatusCode.OK, currentBalance, conversionRate, conversionStep);
            ServerManager.Instance.SendMessageToService(GameServiceType.WebFrontend, response);

            return true;
        }

        private bool OnMTXStoreESConvertRequest(in ServiceMessage.MTXStoreESConvertRequest mtxStoreESConvertRequest)
        {
            ulong requestId = mtxStoreESConvertRequest.RequestId;
            string email = mtxStoreESConvertRequest.Email;
            string token = mtxStoreESConvertRequest.Token;
            int amount = mtxStoreESConvertRequest.Amount;

            PlayerHandle player = null;

            if (_playerManager.SessionManager.VerifyPlatformTicket(email, token, out ulong playerDbId))
                player = _playerManager.ClientManager.GetPlayer(playerDbId);

            if (player == null || player.State != PlayerHandleState.InGame)
            {
                ServiceMessage.MTXStoreESConvertResponse response = new(requestId, (int)HttpStatusCode.Forbidden);
                ServerManager.Instance.SendMessageToService(GameServiceType.WebFrontend, response);
                return true;
            }

            Logger.Info($"Authenticated ES conversion request from player [{player}]");

            ulong gameId = player.CurrentGame.Id;

            // Route the conversion request to the game instance the player is currently in to do the conversion.
            ServiceMessage.MTXStoreESConvertGameRequest gameRequest = new(requestId, gameId, playerDbId, amount);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameRequest);

            return true;
        }

        private bool OnMTXStoreESConvertGameResponse(in ServiceMessage.MTXStoreESConvertGameResponse mtxStoreESConvertGameResponse)
        {
            ulong requestId = mtxStoreESConvertGameResponse.RequestId;
            bool result = mtxStoreESConvertGameResponse.Result;

            // We should have already handled authentication before routing the request to the game instance, so just route the result back.
            ServiceMessage.MTXStoreESConvertResponse response = new(requestId, result ? (int)HttpStatusCode.OK : (int)HttpStatusCode.InternalServerError);
            ServerManager.Instance.SendMessageToService(GameServiceType.WebFrontend, response);

            return true;
        }

        #endregion
    }
}
