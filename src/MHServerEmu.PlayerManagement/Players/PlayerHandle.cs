using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.System.Time;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.PlayerManagement.Auth;
using MHServerEmu.PlayerManagement.Games;
using MHServerEmu.PlayerManagement.Matchmaking;
using MHServerEmu.PlayerManagement.Regions;
using MHServerEmu.PlayerManagement.Social;

namespace MHServerEmu.PlayerManagement.Players
{
    public enum PlayerHandleState
    {
        Created,
        Idle,
        InGame,
        PendingAddToGame,
        PendingRemoveFromGame,
    }

    /// <summary>
    /// Represents a connected player.
    /// </summary>
    public class PlayerHandle
    {
        private const ushort MuxChannel = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly TimeSpan RegionGracePeriodDuration = TimeSpan.FromMinutes(3);

        private static ulong _nextHandleId = 1;     // this is needed primarily for debugging, can potentially be removed later
        private static ulong _nextTransferId = 1;

        private readonly HashSet<PrototypeGuid> _partyBoosts = new();
        private readonly RegionRequestQueueCommandHandler _regionRequestQueueCommandHandler;
        private readonly Action<ulong> _gracePeriodRegionExpiredCallback;

        private bool _saveNeeded = false;   // Dirty flag for player data

        private ulong _transferGameId;
        private NetStructTransferParams _transferParams;
        private bool _transferRegionReady;

        public ulong HandleId { get; }

        public WorldView WorldView { get; }

        public IFrontendClient Client { get; private set; }
        public bool IsConnected { get => Client.IsConnected; }
        public ulong PlayerDbId { get => Client.DbId; }
        public DBAccount Account { get => ((IDBAccountOwner)Client).Account; }
        public string PlayerName { get => Account.PlayerName; }
        public TimeSpan LastLogoutTime { get => TimeSpan.FromMilliseconds(Account.Player.LastLogoutTime); }

        public PlayerHandleState State { get; private set; }
        public GameHandle CurrentGame { get; private set; }
        public GameHandle PrivateGame { get; private set; }     // A game instance owned by this player that runs all of their private regions.

        public RegionHandle TargetRegion { get; private set; }      // The region this player needs to be in
        public RegionHandle ActualRegion { get; private set; }      // The region this player is actually in
        public RegionHandle GracePeriodRegion { get; private set; } // The region this player is temporarily allowed to stay in after leaving a party
        public bool HasVisitedTown { get; private set; }            // This is used to disable party for players who haven't finished the tutorial.

        public PrototypeId DifficultyTierPreference { get; private set; }

        public MasterParty PendingParty { get; internal set; }
        public MasterParty CurrentParty { get; internal set; }

        public MasterGuild Guild { get; internal set; }

        public RegionRequestGroup RegionRequestGroup { get; internal set; }

        public bool HasTransferParams { get => _transferParams != null; }

        public PlayerHandle(IFrontendClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            // Ideally this check should be done at compile time, but making PlayerHandle generic would probably overcomplicate things too much.
            if (client is not IDBAccountOwner)
                throw new Exception("Client does not implement IDBAccountOwner.");

            HandleId = _nextHandleId++;
            WorldView = new(this);
            Client = client;
            State = PlayerHandleState.Created;

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
            DifficultyTierPreference = GameDatabase.GlobalsPrototype.DifficultyTierDefault;
#endif

            _regionRequestQueueCommandHandler = new(this);
            _gracePeriodRegionExpiredCallback = OnGracePeriodRegionExpired;
        }

        public override string ToString()
        {
            return $"({HandleId}) {Client}";
        }

        public bool MigrateSession(IFrontendClient newClient)
        {
            // Trying to migrate sessions while in the middle of adding/removing from a game instance is just asking for trouble,
            // so deny the new client and have it try again later. This shouldn't really happen outside of duplicate logins unless
            // something else breaks and the handle is stuck in a pending state.
            if (!Verify.IsTrue(State == PlayerHandleState.Idle || State == PlayerHandleState.InGame, $"Invalid state {State} when trying to migrate handle [{this}]"))
                return false;

            DBAccount account = (DBAccount)Client.Session.Account;
            if (account.MigrationData.IsInErrorState)
                return false;

            ClientSession newSession = (ClientSession)newClient.Session;

            Logger.Trace($"Migrating handle [{this}] to session [{newSession}]");

            RemoveFromCurrentGame();
            Client.Disconnect();
            SetActualRegion(null);  // this fixes the edge case where duplicate login happens while in a hub region

            newSession.Account = account;

            _transferParams = null;

            // Reset migration data to prevent abuse.
            // At this stage the player is still in a game and will try to update MigrationDate on exit. We set the SkipNextUpdate flag here to avoid this.
            account.MigrationData.Reset();
            account.MigrationData.SkipNextUpdate = true;

            Client = newClient;

            return true;
        }

        public void Disconnect()
        {
            Client.Disconnect();
        }

        public void OnRemoved()
        {
            // Cancel pending party invitations or remove from the current party
            PlayerManagerService.Instance.PartyManager.OnPlayerRemoved(this);

            // Remove from region
            SetTargetRegion(null);
            SetActualRegion(null);

            // Remove from guild
            Guild?.OnMemberOffline(this);

            // Remove from matchmaking
            RegionRequestGroup?.RemovePlayer(this);

            // Clearing the WorldView will remove all reservations and shut down the private game instance if none of its regions are reserved by other players.
            WorldView.Clear();
        }

        public void SendMessage(IMessage message)
        {
            Client.SendMessage(MuxChannel, message);
        }

        public void LoadPlayerData()
        {
            DBAccount account = Account;

            using var lockScope = account.Lock();
            if (!Verify.IsTrue(lockScope.LockTaken, LoggingLevel.Error, $"Timed out acquiring lock for [{account}]"))
                return;

            // This is synchronous, but it's not a bottleneck in practice with the player counts we're seeing.
            if (!Verify.IsTrue(AccountManager.LoadPlayerDataForAccount(account), $"Failed to load player data for account [{account}] from the database"))
                return;

            Logger.Info($"Loaded player data for account [{account}] from the database");

            // If this is the initial load switch the state to allow this player to be added to a game
            if (State == PlayerHandleState.Created)
                State = PlayerHandleState.Idle;
        }

        public void SavePlayerData()
        {
            if (!Verify.IsTrue(State != PlayerHandleState.Created, $"Invalid state {State} for player [{this}]"))
                return;

            // Skip saving if persistence is disabled.
            if (PlayerManagerService.Instance.Config.EnablePersistence == false)
                return;

            DBAccount account = Account;

            // Do not save accounts in error state to avoid data corruption
            if (account.MigrationData.IsInErrorState)
                return;

            using var lockScope = account.Lock();
            if (!Verify.IsTrue(lockScope.LockTaken, LoggingLevel.Error, $"Timed out acquiring lock for [{account}]"))
                return;

            if (IsConnected == false)
                account.Player.LastLogoutTime = (long)Clock.UnixTime.TotalMilliseconds;

            // This is synchronous, but it's not a bottleneck in practice with the player counts we're seeing.
            if (!Verify.IsTrue(AccountManager.SavePlayerDataForAccount(account), $"Failed to save player data for account [{account}] to the database"))
                return;

            Logger.Info($"Saved player data for account [{account}] to the database");
        }

        public bool BeginAddToGame(GameHandle game)
        {
            if (!Verify.IsTrue(State == PlayerHandleState.Idle, $"Invalid state {State} for player [{this}]"))
                return false;

            State = PlayerHandleState.PendingAddToGame;
            CurrentGame = game;
            Logger.Trace($"Requesting to add player [{this}] to game [{game}]");

            ServiceMessage.GameInstanceClientOp gameInstanceOp = new(GameInstanceClientOpType.Add, Client, game.Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }

        public void FinishAddToGame(ulong gameId)
        {
            if (!Verify.IsTrue(State == PlayerHandleState.PendingAddToGame, $"Invalid state {State} for player [{this}]"))
                return;

            ulong currentGameId = CurrentGame != null ? CurrentGame.Id : 0;
            Verify.IsTrue(gameId == currentGameId, $"GameId mismatch (expected 0x{currentGameId:X}, got 0x{gameId:X})");

            State = PlayerHandleState.InGame;
            Logger.Trace($"Player [{this}] added to game [{CurrentGame}]");

            // If this player has successfully gotten into a game, their data will need to be saved once they get out.
            _saveNeeded = true;

            // Now put the player into the region they are transferring into.
            SendTransferParamsToGame();
        }

        public void RemoveFromCurrentGame()
        {
            if (State != PlayerHandleState.InGame)
                return;

            CurrentGame.RemovePlayer(this);
        }

        public bool BeginRemoveFromGame(GameHandle game)
        {
            if (!Verify.IsTrue(State == PlayerHandleState.InGame, $"Invalid state {State} for player [{this}]"))
                return false;

            if (!Verify.IsNotNull(game)) return false;

            Verify.IsTrue(game == CurrentGame, $"Game mismatch (expected [{CurrentGame}], got [{game}])");

            State = PlayerHandleState.PendingRemoveFromGame;
            Logger.Trace($"Requesting to remove player [{this}] from game {game}");

            ServiceMessage.GameInstanceClientOp gameInstanceOp = new(GameInstanceClientOpType.Remove, Client, game.Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }

        public void FinishRemoveFromGame(ulong gameId)
        {
            // Include PendingAddToGame because we can also get here when GIS fails to add a client to a game for whatever reason.
            if (!Verify.IsTrue(State == PlayerHandleState.PendingAddToGame || State == PlayerHandleState.PendingRemoveFromGame, $"Invalid state {State} for player [{this}]"))
                return;

            ulong currentGameId = CurrentGame != null ? CurrentGame.Id : 0;
            Verify.IsTrue(gameId == currentGameId, $"GameId mismatch (expected 0x{currentGameId:X}, got 0x{gameId:X})");

            State = PlayerHandleState.Idle;
            CurrentGame = null;

            Logger.Trace($"Player [{this}] removed from game 0x{gameId:X}");

            if (_saveNeeded)
            {
                SavePlayerData();
                _saveNeeded = false;
            }
        }

        public void TryJoinGame()
        {
            if (!Verify.IsNotNull(_transferParams, $"No transfer params for player [{this}]"))
            {
                Disconnect();
                return;
            }

            if (_transferRegionReady == false)
                return;

            bool gameFound = PlayerManagerService.Instance.GameHandleManager.TryGetGameById(_transferGameId, out GameHandle transferGame);
            if (!Verify.IsTrue(gameFound, $"Failed to get game 0x{_transferGameId:X}"))
            {
                Disconnect();
                return;
            }

            transferGame.AddPlayer(this);
        }

        public void SetPrivateGame(GameHandle privateGame)
        {
            if (!Verify.IsTrue(PrivateGame == null || PrivateGame.IsRunning == false, $"Cannot assign private game instance [{privateGame}] to player [{this}] because game instance [{PrivateGame}] is already assigned"))
                return;

            PrivateGame = privateGame;
            Logger.Trace($"Private game instance [{privateGame}] assigned to player [{this}]");
        }

        public void BeginRegionTransferToStartTarget()
        {
            PrototypeId targetProtoRef = (PrototypeId)Account.Player.StartTarget;
            RegionConnectionTargetPrototype targetProto = targetProtoRef.As<RegionConnectionTargetPrototype>();
            if (targetProto == null)
            {
                targetProtoRef = GameDatabase.GlobalsPrototype.DefaultStartTargetStartingRegion;
                targetProto = targetProtoRef.As<RegionConnectionTargetPrototype>();
                Logger.Warn($"BeginRegionTransferToStartTarget(): Invalid start target specified for player [{this}], falling back to default");
            }

            RegionPrototype regionProto = targetProto.Region.As<RegionPrototype>();
            if (!Verify.IsNotNull(regionProto)) return;

            NetStructRegionTarget destTarget = NetStructRegionTarget.CreateBuilder()
                .SetRegionProtoId((ulong)targetProto.Region)
                .SetAreaProtoId((ulong)targetProto.Area)
                .SetCellProtoId((ulong)GameDatabase.GetDataRefByAsset(targetProto.Cell))
                .SetEntityProtoId((ulong)targetProto.Entity)
                .Build();

            NetStructCreateRegionParams createRegionParams = NetStructCreateRegionParams.CreateBuilder()
                .SetLevel(0)
#if GAME_VERSION_1_52 || GAME_VERSION_1_53
                .SetDifficultyTierProtoId((ulong)GameDatabase.GlobalsPrototype.DifficultyTierDefault)
#endif
                .Build();

            BeginRegionTransferToTarget(0, TeleportContextEnum.TeleportContext_Transition, destTarget, createRegionParams);
        }

        public void BeginRegionTransferToTarget(ulong requestingGameId, TeleportContextEnum context, NetStructRegionTarget destTarget, NetStructCreateRegionParams createRegionParams)
        {
            if (CanBeginRegionTransfer(false) == false)
            {
                CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_GenericError);
                return;
            }

            PrototypeId regionProtoRef = (PrototypeId)destTarget.RegionProtoId;
            RegionPrototype regionProto = ((PrototypeId)destTarget.RegionProtoId).As<RegionPrototype>();
            if (!Verify.IsNotNull(regionProto))
            {
                CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_DestinationInaccessible);
                return;
            }

            // Reset WorldView if we are resetting mission progress (e.g. prestige)
            if (context == TeleportContextEnum.TeleportContext_StoryWarp)
                WorldView.Clear();

            // Get the WorldView to use (this player's or party's)
            WorldView worldView = GetCurrentWorldView();

            // Prioritize regions that are already in the WorldView.
            RegionHandle region = worldView.GetMatchingRegion(regionProtoRef, createRegionParams);

            // Create a new region if needed
            if (region == null)
            {
                if (regionProto.IsPublic)
                    region = PlayerManagerService.Instance.WorldManager.GetOrCreatePublicRegion(regionProtoRef, createRegionParams);
                else
                    region = PlayerManagerService.Instance.WorldManager.CreatePrivateRegion(this, regionProtoRef, createRegionParams);

                if (region != null)
                {
                    worldView.AddRegion(region);
                }
                else
                {
                    CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_DestinationInaccessible);
                    return;
                }
            }

            RegionTransferFailure canEnterRegion = CanEnterRegion(region, false);
            if (canEnterRegion != RegionTransferFailure.eRTF_NoError)
            {
                CancelRegionTransfer(requestingGameId, canEnterRegion);
                return;
            }

            ulong destGameId = region.Game.Id;

            NetStructTransferParams transferParams = NetStructTransferParams.CreateBuilder()
                .SetTransferId(_nextTransferId++)
                .SetDestRegionId(region.Id)
                .SetDestRegionProtoId((ulong)region.RegionProtoRef)
                .SetDestTarget(destTarget)
                .Build();

            SetTransferParams(destGameId, transferParams);

            // This needs to be called after we set transfer params because the region may already be ready.
            SetTargetRegion(region);
            region.RequestTransfer(this);
        }

        public void BeginRegionTransferToLocation(ulong requestingGameId, TeleportContextEnum context, NetStructRegionLocation destLocation)
        {
            if (CanBeginRegionTransfer(false) == false)
            {
                CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_GenericError);
                return;
            }

            RegionHandle region = PlayerManagerService.Instance.WorldManager.GetRegion(destLocation.RegionId);
            if (region == null)
            {
                RegionTransferFailure failureReason = context == TeleportContextEnum.TeleportContext_Bodyslide
                    ? RegionTransferFailure.eRTF_BodyslideRegionUnavailable
                    : RegionTransferFailure.eRTF_DestinationInaccessible;

                CancelRegionTransfer(requestingGameId, failureReason);
                return;
            }
            else
            {
                RegionTransferFailure canEnterRegion = CanEnterRegion(region, false);
                if (canEnterRegion != RegionTransferFailure.eRTF_NoError)
                {
                    CancelRegionTransfer(requestingGameId, canEnterRegion);
                    return;
                }
            }

            NetStructTransferParams transferParams = NetStructTransferParams.CreateBuilder()
                .SetTransferId(_nextTransferId++)
                .SetDestRegionId(region.Id)
                .SetDestRegionProtoId((ulong)region.RegionProtoRef)
                .SetDestLocation(destLocation)
                .Build();

            SetTransferParams(region.Game.Id, transferParams);

            // This needs to be called after we set transfer params because the region may already be ready.
            SetTargetRegion(region);
            region.RequestTransfer(this);
        }

        public void BeginRegionTransferToPlayer(ulong requestingGameId, ulong destPlayerDbId)
        {
            if (CanBeginRegionTransfer(false) == false)
            {
                CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_GenericError);
                return;
            }

            RegionHandle region = null;

            PlayerHandle destPlayer = PlayerManagerService.Instance.ClientManager.GetPlayer(destPlayerDbId);
            region = destPlayer?.ActualRegion;

            if (region == null)
            {
                CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_TargetPlayerUnavailable);
                return;
            }

            if (region != ActualRegion)
            {
                if (region.CreateParams.HasEndlessLevel && region.CreateParams.EndlessLevel > 1)
                {
                    CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_EndlessProgressedTooFar);
                    return;
                }

                // This should be handled game-side in most cases, but games rely on community data, which can be outdated.
                // If this request got sent based on outdated community data, interpret it as a queue command.
                if (region.IsMatch)
                {
                    _regionRequestQueueCommandHandler.HandleCommand(PrototypeId.Invalid, PrototypeId.Invalid, PrototypeId.Invalid,
                        RegionRequestQueueCommandVar.eRRQC_RequestToJoinGroup, 0, destPlayerDbId);
                    return;
                }
            }

            RegionTransferFailure canEnterRegion = CanEnterRegion(region, false);
            if (canEnterRegion != RegionTransferFailure.eRTF_NoError)
            {
                CancelRegionTransfer(requestingGameId, canEnterRegion);
                return;
            }

            NetStructTransferParams transferParams = NetStructTransferParams.CreateBuilder()
                .SetTransferId(_nextTransferId++)
                .SetDestRegionId(region.Id)
                .SetDestRegionProtoId((ulong)region.RegionProtoRef)
                .SetDestEntityDbId(destPlayerDbId)
                .Build();

            SetTransferParams(region.Game.Id, transferParams);

            // This needs to be called after we set transfer params because the region may already be ready.
            SetTargetRegion(region);
            region.RequestTransfer(this);
        }

        public bool BeginRegionTransferToMatch(RegionHandle region, int teamIndex)
        {
            // This initiated by the server, so we don't need to send a cancellation here.
            if (CanBeginRegionTransfer(true) == false)
                return false;

            ulong destGameId = region.Game.Id;

            NetStructTransferParams transferParams = NetStructTransferParams.CreateBuilder()
                .SetTransferId(_nextTransferId++)
                .SetDestRegionId(region.Id)
                .SetDestRegionProtoId((ulong)region.RegionProtoRef)
                .SetDestTeamIndex(teamIndex)
                .Build();

            SetTransferParams(destGameId, transferParams);

            // This needs to be called after we set transfer params because the region may already be ready.
            SetTargetRegion(region);
            region.RequestTransfer(this);
            return true;
        }

        public void CancelRegionTransfer(ulong requestingGameId, RegionTransferFailure reason)
        {
            SetTransferParams(0, null);

            if (requestingGameId != 0)
            {
                // TODO: Do we need regionProtoId / requiredItemProtoId fields here?
                ChangeRegionFailed changeFailed = ChangeRegionFailed.CreateBuilder().SetReason(reason).Build();
                ServiceMessage.UnableToChangeRegion response = new(requestingGameId, PlayerDbId, changeFailed);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, response);
            }
        }

        public void OnRegionReadyToTransfer()
        {
            _transferRegionReady = true;

            // If this player is already in the game that hosts the region, finish the transfer right away.
            // Otherwise this would be triggered when we receive the confirmation that this player is in the game.
            if (CurrentGame != null)
            {
                if (CurrentGame.Id == _transferGameId)
                {
                    if (State == PlayerHandleState.InGame)
                        SendTransferParamsToGame();
                }
                else
                {
                    RemoveFromCurrentGame();
                }
            }
        }

        public void FinishRegionTransfer(ulong transferId)
        {
            if (!Verify.IsNotNull(_transferParams, $"Received confirmation for transfer {transferId}, but no transfer is pending for player [{this}]"))
                return;

            if (!Verify.IsTrue(transferId == _transferParams.TransferId, $"Transfer id mismatch for player [{this}]: expected {_transferParams.TransferId}, got {transferId}"))
                return;

            RegionHandle newRegion = PlayerManagerService.Instance.WorldManager.GetRegion(_transferParams.DestRegionId);
            if (!Verify.IsNotNull(newRegion, LoggingLevel.Error, $"Failed to get region 0x{_transferParams.DestRegionId:X} for transfer {transferId} for player [{this}]"))
                return;

            SetActualRegion(newRegion);
            SetTransferParams(0, null);

            if (newRegion.IsTown)
                HasVisitedTown = true;

            PlayerManagerService.Instance.PartyManager.OnPlayerRegionTransferFinished(this);

            // Sync matchmaking status
            if (RegionRequestGroup != null)
            {
                RegionRequestGroup.OnPlayerFinishTransfer(this);
            }
            else
            {
                ServiceMessage.MatchQueueFlush message = new(CurrentGame.Id, PlayerDbId);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            }

            Logger.Trace($"Player [{this}] finished region transfer {transferId}");
        }

        public RegionTransferFailure CanEnterRegion(RegionPrototype regionProto, bool isQueue)
        {
            if (regionProto == null)
                return RegionTransferFailure.eRTF_GenericError;

            if (CurrentParty != null && CurrentParty.Type == GroupType.GroupType_Raid)
            {
                switch (regionProto.Behavior)
                {
                    case RegionBehavior.PrivateStory:
                    case RegionBehavior.PrivateNonStory:
                        return RegionTransferFailure.eRTF_RaidsNotAllowed;

                    case RegionBehavior.MatchPlay:
                        if (regionProto.QueueGroupLimit < GameDatabase.GlobalsPrototype.PlayerRaidMaxSize)
                            return RegionTransferFailure.eRTF_RaidsNotAllowed;
                        break;
                }
            }

            if (isQueue && CurrentParty != null && CurrentParty.MemberCount > regionProto.QueueGroupLimit)
                return RegionTransferFailure.eRTF_Full;

            return RegionTransferFailure.eRTF_NoError;
        }

        public RegionTransferFailure CanEnterRegion(RegionHandle region, bool isQueue)
        {
            if (region == null)
                return RegionTransferFailure.eRTF_GenericError;

            RegionTransferFailure protoResult = CanEnterRegion(region.Prototype, isQueue);
            if (protoResult != RegionTransferFailure.eRTF_NoError)
                return protoResult;

            // For matches we check matchmaking group limit instead of the region itself in prototype checks above.
            if (region.IsPrivate && region != TargetRegion && region.IsFull)
                return RegionTransferFailure.eRTF_Full;

            return RegionTransferFailure.eRTF_NoError;
        }

        public bool CanBeginRegionTransfer(bool isMatchTransfer)
        {
            if (IsConnected == false)
                return false;

            // Do not allow players who accepted a match invite to transfer anywhere but the match region.
            if (RegionRequestGroup != null && isMatchTransfer == false)
            {
                foreach (RegionRequestGroupMember member in RegionRequestGroup)
                {
                    if (member.Player != this)
                        continue;

                    if (member.State == RegionRequestGroupMember.MatchInviteAcceptedState.Instance)
                        return false;
                }
            }

            return true;
        }

        public void SyncWorldView()
        {
            if (CurrentGame == null || State != PlayerHandleState.InGame)
                return;

            List<(ulong, ulong)> worldView = new();
            GetCurrentWorldView().BuildWorldViewCache(worldView);
            ServiceMessage.WorldViewSync message = new(CurrentGame.Id, PlayerDbId, worldView);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }

        /// <summary>
        /// Removes this player from the current region if it's no longer available for the current WorldView.
        /// </summary>
        public void CheckWorldViewRegionAvailability()
        {
            SyncWorldView();

            // Do not remove from the current region we have it in any accessible WorldView or it's a match
            if (TargetRegion == null || TargetRegion.IsMatch || HasRegionInAnyWorldView(TargetRegion.Id) || TargetRegion == GracePeriodRegion)
                return;

            // Return to start target if this region is no longer available.
            BeginRegionTransferToStartTarget();
        }

        public bool HasRegionInAnyWorldView(ulong regionId)
        {
            if (CurrentParty != null)
            {
                if (CurrentParty.WorldView.ContainsRegion(regionId))
                    return true;

                // If any party member has access to this region, it's okay for this player to be there as well.
                foreach (PlayerHandle partyMember in CurrentParty)
                {
                    if (partyMember.WorldView.ContainsRegion(regionId))
                        return true;
                }
            }

            if (WorldView.ContainsRegion(regionId))
                return true;

            return false;
        }

        private WorldView GetCurrentWorldView()
        {
            if (CurrentParty != null)
                return CurrentParty.WorldView;

            return WorldView;
        }

        public void SetGracePeriodRegion(RegionHandle region, GroupLeaveReason leaveReason)
        {
            if (!Verify.IsNotNull(region)) return;

            GracePeriodRegion = region;

            // Schedule grace period expiration
            var eventScheduler = PlayerManagerService.Instance.EventScheduler.GracePeriodRegionExpired;
            eventScheduler.ScheduleEvent(PlayerDbId, RegionGracePeriodDuration, _gracePeriodRegionExpiredCallback, region.Id);
            
            // Notify the player
            if (CurrentGame != null)
            {
                ulong gameExpireTimeMicroseconds = (ulong)(Clock.GameTime + RegionGracePeriodDuration).TotalMicroseconds;
                ServiceMessage.PartyKickGracePeriod message = new(CurrentGame.Id, PlayerDbId, gameExpireTimeMicroseconds, leaveReason);
                ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
            }
        }

        public void OnGracePeriodRegionExpired(ulong regionId)
        {
            if (Verify.IsNotNull(GracePeriodRegion))
            {
                Verify.IsTrue(regionId == GracePeriodRegion.Id);
                GracePeriodRegion = null;
            }

            // This will kick us out of the grace period region if we are currently in it and there is no other reason to be allowed to stay in it.
            CheckWorldViewRegionAvailability();
        }

        public void SetDifficultyTierPreference(PrototypeId difficultyTierProtoRef)
        {
            if (difficultyTierProtoRef == DifficultyTierPreference)
                return;

            DifficultyTierPreference = difficultyTierProtoRef;
#if DEBUG
            Logger.Trace($"SetDifficultyTierPreference(): player=[{this}], difficulty=[{difficultyTierProtoRef.GetNameFormatted()}]");
#endif
        }

#if GAME_VERSION_1_52 || GAME_VERSION_1_53
        // V48_FIXME
        public void GetPartyBoosts(PartyMemberInfo.Builder infoBuilder)
        {
            if (_partyBoosts.Count == 0)
                return;

            foreach (PrototypeGuid partyBoost in _partyBoosts)
                infoBuilder.AddBoosts((ulong)partyBoost);
        }
#endif

        public void SetPartyBoosts(List<ulong> boosts)
        {
            _partyBoosts.Clear();

            if (boosts == null)
                return;

            foreach (ulong boost in boosts)
            {
                PrototypeGuid boostGuid = (PrototypeGuid)boost;

                if (!Verify.IsTrue(boostGuid != PrototypeGuid.Invalid))
                    continue;

                _partyBoosts.Add(boostGuid);
            }
        }

        public void ReceiveRegionRequestQueueCommand(PrototypeId regionRef, PrototypeId difficultyTierRef, PrototypeId metaStateRef,
            RegionRequestQueueCommandVar command, ulong regionRequestGroupId, ulong targetPlayerDbId, int teamSizeOverride)
        {
            _regionRequestQueueCommandHandler.HandleCommand(regionRef, difficultyTierRef, metaStateRef, command, regionRequestGroupId, targetPlayerDbId, teamSizeOverride);
        }

        public void AddToChatRoom(ChatRoomTypes roomType, ulong roomId)
        {
            ServiceMessage.GroupingManagerChatRoomOperation message = new(roomType, roomId, PlayerDbId, ChatRoomOperationType.Add);
            ServerManager.Instance.SendMessageToService(GameServiceType.GroupingManager, message);
        }

        public void RemoveFromChatRoom(ChatRoomTypes roomType, ulong roomId)
        {
            ServiceMessage.GroupingManagerChatRoomOperation message = new(roomType, roomId, PlayerDbId, ChatRoomOperationType.Remove);
            ServerManager.Instance.SendMessageToService(GameServiceType.GroupingManager, message);
        }

        private void SetTransferParams(ulong gameId, NetStructTransferParams newTransferParams)
        {
            Verify.IsTrue(newTransferParams == null || _transferParams == null, $"Existing transfer {_transferParams.TransferId} found");

            _transferGameId = gameId;
            _transferParams = newTransferParams;
            _transferRegionReady = false;

            if (_transferParams != null)
                Logger.Trace($"Player [{this}] beginning region transfer {_transferParams.TransferId}");
        }

        /// <summary>
        /// Puts this player into the region in the current game instance specified in the current transfer params.
        /// </summary>
        private void SendTransferParamsToGame()
        {
            if (!Verify.IsNotNull(CurrentGame)) return;

            if (!Verify.IsTrue(State == PlayerHandleState.InGame, $"Invalid state {State} for player [{this}]"))
                return;

            if (!Verify.IsTrue(CurrentGame.Id == _transferGameId, LoggingLevel.Error, $"Game id mismatch for player [{this}] (expected 0x{_transferGameId:X}, got 0x{CurrentGame.Id:X})"))
            {
                Disconnect();
                return;
            }

            List<(ulong, ulong)> worldViewCache = new();
            GetCurrentWorldView().BuildWorldViewCache(worldViewCache);
            ServiceMessage.GameAndRegionForPlayer message = new(_transferGameId, PlayerDbId, _transferParams, worldViewCache);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }

        private void SetTargetRegion(RegionHandle newRegion)
        {
            if (TargetRegion == newRegion)
                return;

            RegionRequestGroup?.OnPlayerBeginTransfer(this, newRegion);

            RegionHandle prevRegion = TargetRegion;

            prevRegion?.RemovePlayer(this);

            TargetRegion = newRegion;

            // Adding the player here will make them accounted for in the load balancing logic.
            newRegion?.AddPlayer(this);
        }

        private void SetActualRegion(RegionHandle newRegion)
        {
            if (ActualRegion == newRegion)
                return;

            RegionHandle prevRegion = ActualRegion;

            if (prevRegion != null)
            {
                prevRegion.Unreserve(RegionReservationType.Presence);
                RemoveFromChatRoom(ChatRoomTypes.CHAT_ROOM_TYPE_LOCAL, prevRegion.Id);
            }

            ActualRegion = newRegion;

            if (newRegion != null)
            {
                // This additional reservation will prevent the region from shutting down if there are still any players in it,
                // even if the region is no longer in any world views for whatever reason.
                newRegion.Reserve(RegionReservationType.Presence);
                AddToChatRoom(ChatRoomTypes.CHAT_ROOM_TYPE_LOCAL, newRegion.Id);
            }

            // Community will be updated when we receive a broadcast from the game instance.

            Guild?.OnMemberRegionChanged(this, newRegion, prevRegion);

            // Remove the previous region from the WorldView if it needs to be shut down.
            if (prevRegion != null && prevRegion.Flags.HasFlag(RegionFlags.ShutdownWhenVacant))
                WorldView.RemoveRegion(prevRegion);
        }
    }
}
