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

        private static ulong _nextHandleId = 1;     // this is needed primarily for debugging, can potentially be removed later
        private static ulong _nextTransferId = 1;

        private readonly HashSet<PrototypeGuid> _partyBoosts = new();

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
        public bool HasVisitedTown { get; private set; }            // This is used to disable party for players who haven't finished the tutorial.

        public PrototypeId DifficultyTierPreference { get; private set; }

        public MasterParty PendingParty { get; internal set; }
        public MasterParty CurrentParty { get; internal set; }

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

            DifficultyTierPreference = GameDatabase.GlobalsPrototype.DifficultyTierDefault;
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
            if (State != PlayerHandleState.InGame && State != PlayerHandleState.Idle)
                return Logger.WarnReturn(false, $"MigrateSession(): Unable to migrate handle [{this}] while in state {State}");

            Logger.Info($"Migrating handle [{this}] to session [{newClient.Session}]");

            RemoveFromCurrentGame();
            Client.Disconnect();

            ClientSession oldSession = (ClientSession)Client.Session;
            ClientSession newSession = (ClientSession)newClient.Session;
            newSession.Account = oldSession.Account;

            _transferParams = null;

            // Reset migration data to prevent abuse.
            // At this stage the player is still in a game and will try to update MigrationDate on exit. We set the SkipNextUpdate flag here to avoid this.
            MigrationData migrationData = ((DBAccount)newSession.Account).MigrationData;
            migrationData.Reset();
            migrationData.SkipNextUpdate = true;

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

            // Clearing the WorldView will remove all reservations and shut down the private game instance if none of its regions are reserved by other players.
            WorldView.Clear();
        }

        public void SendMessage(IMessage message)
        {
            Client.SendMessage(MuxChannel, message);
        }

        // NOTE: We are locking on the account instance to prevent account data from being modified while
        // it is being written to the database. This could potentially cause deadlocks if not used correctly.

        public bool LoadPlayerData()
        {
            DBAccount account = Account;

            lock (account)
            {
                if (AccountManager.LoadPlayerDataForAccount(account) == false)
                    return Logger.WarnReturn(false, $"LoadPlayerData(): Failed to load player data for account [{account}] from the database");
            }

            Logger.Info($"Loaded player data for account [{account}] from the database");

            // If this is the initial load switch the state to allow this player to be added to a game
            if (State == PlayerHandleState.Created)
                State = PlayerHandleState.Idle;

            return true;
        }

        public bool SavePlayerData()
        {
            if (State == PlayerHandleState.Created)
                return Logger.WarnReturn(false, $"SavePlayerData(): Invalid state {State} for player [{this}]");

            // Skip saving if persistence is disabled.
            if (PlayerManagerService.Instance.Config.EnablePersistence == false)
                return true;

            DBAccount account = Account;

            lock (account)
            {
                if (IsConnected == false)
                    account.Player.LastLogoutTime = (long)Clock.UnixTime.TotalMilliseconds;

                if (IDBManager.Instance.SavePlayerData(account) == false)
                    return Logger.WarnReturn(false, $"SavePlayerData(): Failed to save player data for account [{account}] to the database");
            }

            Logger.Info($"Saved player data for account [{account}] to the database");

            return true;
        }

        public bool BeginAddToGame(GameHandle game)
        {
            if (State != PlayerHandleState.Idle)
                return Logger.WarnReturn(false, $"BeginAddToGame(): Invalid state {State} for player [{this}]");

            State = PlayerHandleState.PendingAddToGame;
            CurrentGame = game;
            Logger.Info($"Requesting to add player [{this}] to game [{game}]");

            ServiceMessage.GameInstanceClientOp gameInstanceOp = new(GameInstanceClientOpType.Add, Client, game.Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }

        public bool FinishAddToGame(ulong gameId)
        {
            if (State != PlayerHandleState.PendingAddToGame)
                return Logger.WarnReturn(false, $"FinishAddToGame(): Invalid state {State} for player [{this}]");

            if (CurrentGame.Id != gameId)
                Logger.Warn($"FinishAddToGame(): GameId mismatch (expected 0x{CurrentGame.Id:X}, got 0x{gameId:X})");

            State = PlayerHandleState.InGame;
            Logger.Info($"Player [{this}] added to game [{CurrentGame}]");

            // If this player has successfully gotten into a game, their data will need to be saved once they get out.
            _saveNeeded = true;

            // Now put the player into the region they are transferring into.
            SendTransferParamsToGame();

            return true;
        }

        public void RemoveFromCurrentGame()
        {
            if (State != PlayerHandleState.InGame)
                return;

            CurrentGame.RemovePlayer(this);
        }

        public bool BeginRemoveFromGame(GameHandle game)
        {
            if (State != PlayerHandleState.InGame)
                return Logger.WarnReturn(false, $"BeginRemoveFromGame(): Invalid state {State} for player [{this}]");

            if (game != CurrentGame)
                Logger.Warn($"BeginRemoveFromGame(): Game mismatch (expected [{CurrentGame}], got [{game}])");

            State = PlayerHandleState.PendingRemoveFromGame;
            Logger.Info($"Requesting to remove player [{this}] from game {game}");

            ServiceMessage.GameInstanceClientOp gameInstanceOp = new(GameInstanceClientOpType.Remove, Client, game.Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }

        public bool FinishRemoveFromGame(ulong gameId)
        {
            // Include PendingAddToGame because we can also get here when GIS fails to add a client to a game for whatever reason.
            if (State != PlayerHandleState.PendingAddToGame && State != PlayerHandleState.PendingRemoveFromGame)
                return Logger.WarnReturn(false, $"FinishRemoveFromGame(): Invalid state {State} for player [{this}]");

            if (CurrentGame.Id != gameId)
                Logger.Warn($"FinishRemoveFromGame(): GameId mismatch (expected 0x{CurrentGame.Id:X}, got 0x{gameId:X})");

            State = PlayerHandleState.Idle;
            CurrentGame = null;

            Logger.Info($"Player [{this}] removed from game 0x{gameId:X}");

            if (_saveNeeded)
            {
                SavePlayerData();
                _saveNeeded = false;
            }

            return true;
        }

        public void TryJoinGame()
        {
            if (_transferParams == null)
            {
                Logger.Warn($"TryJoinGame(): No transfer params for player [{this}]");
                Disconnect();
                return;
            }

            if (_transferRegionReady == false)
                return;

            if (PlayerManagerService.Instance.GameHandleManager.TryGetGameById(_transferGameId, out GameHandle transferGame) == false)
            {
                Logger.Warn($"TryJoinGame(): Failed to get game 0x{_transferGameId:X}");
                Disconnect();
                return;
            }

            transferGame.AddPlayer(this);
        }

        public bool SetPrivateGame(GameHandle privateGame)
        {
            if (PrivateGame != null && PrivateGame.IsRunning)
                return Logger.WarnReturn(false, $"SetPrivateGame(): Cannot assign private game instance [{privateGame}] to player [{this}] because game instance [{PrivateGame}] is already assigned");

            Logger.Info($"Private game instance [{privateGame}] assigned to player [{this}]");
            PrivateGame = privateGame;
            return true;
        }

        public bool BeginRegionTransferToStartTarget()
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
            if (regionProto == null) return Logger.WarnReturn(false, "BeginRegionTransferToStartTarget(): regionProto == null");

            NetStructRegionTarget destTarget = NetStructRegionTarget.CreateBuilder()
                .SetRegionProtoId((ulong)targetProto.Region)
                .SetAreaProtoId((ulong)targetProto.Area)
                .SetCellProtoId((ulong)GameDatabase.GetDataRefByAsset(targetProto.Cell))
                .SetEntityProtoId((ulong)targetProto.Entity)
                .Build();

            NetStructCreateRegionParams createRegionParams = NetStructCreateRegionParams.CreateBuilder()
                .SetLevel(0)
                .SetDifficultyTierProtoId((ulong)GameDatabase.GlobalsPrototype.DifficultyTierDefault)
                .Build();

            return BeginRegionTransferToTarget(0, TeleportContextEnum.TeleportContext_Transition, destTarget, createRegionParams);
        }

        public bool BeginRegionTransferToTarget(ulong requestingGameId, TeleportContextEnum context, NetStructRegionTarget destTarget, NetStructCreateRegionParams createRegionParams)
        {
            PrototypeId regionProtoRef = (PrototypeId)destTarget.RegionProtoId;
            RegionPrototype regionProto = ((PrototypeId)destTarget.RegionProtoId).As<RegionPrototype>();
            if (regionProto == null)
            {
                Logger.Warn("BeginRegionTransferToTarget(): regionProto == null");
                CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_DestinationInaccessible);
                return false;
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
                // We treat match regions as private since there is currently no matchmaking.
                if (regionProto.IsPublic && regionProto.Behavior != RegionBehavior.MatchPlay)
                    region = PlayerManagerService.Instance.WorldManager.GetOrCreatePublicRegion(regionProtoRef, createRegionParams);
                else
                    region = PlayerManagerService.Instance.WorldManager.CreatePrivateRegion(this, regionProtoRef, createRegionParams);

                worldView.AddRegion(region);
            }
            else
            {
                RegionTransferFailure canEnterRegion = CanEnterRegion(region);
                if (canEnterRegion != RegionTransferFailure.eRTF_NoError)
                {
                    CancelRegionTransfer(requestingGameId, canEnterRegion);
                    return false;
                }
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
            return true;
        }

        public bool BeginRegionTransferToLocation(ulong requestingGameId, TeleportContextEnum context, NetStructRegionLocation destLocation)
        {
            RegionHandle region = PlayerManagerService.Instance.WorldManager.GetRegion(destLocation.RegionId);
            if (region == null)
            {
                RegionTransferFailure failureReason = context == TeleportContextEnum.TeleportContext_Bodyslide
                    ? RegionTransferFailure.eRTF_BodyslideRegionUnavailable
                    : RegionTransferFailure.eRTF_DestinationInaccessible;

                CancelRegionTransfer(requestingGameId, failureReason);
                return false;
            }
            else
            {
                RegionTransferFailure canEnterRegion = CanEnterRegion(region);
                if (canEnterRegion != RegionTransferFailure.eRTF_NoError)
                {
                    CancelRegionTransfer(requestingGameId, canEnterRegion);
                    return false;
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
            return false;
        }

        public bool BeginRegionTransferToPlayer(ulong requestingGameId, ulong destPlayerDbId)
        {
            RegionHandle region = null;

            PlayerHandle destPlayer = PlayerManagerService.Instance.ClientManager.GetPlayer(destPlayerDbId);
            region = destPlayer?.ActualRegion;

            if (region == null)
            {
                CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_TargetPlayerUnavailable);
                return false;
            }
            else
            {
                RegionTransferFailure canEnterRegion = CanEnterRegion(region);
                if (canEnterRegion != RegionTransferFailure.eRTF_NoError)
                {
                    CancelRegionTransfer(requestingGameId, canEnterRegion);
                    return false;
                }
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

        public bool FinishRegionTransfer(ulong transferId)
        {
            if (_transferParams == null)
                return Logger.WarnReturn(false, $"FinishRegionTransfer(): Received confirmation for transfer {transferId}, but no transfer is pending for player [{this}]");

            if (_transferParams.TransferId != transferId)
                return Logger.WarnReturn(false, $"FinishRegionTransfer(): Transfer id mismatch for player [{this}]: expected {_transferParams.TransferId}, got {transferId}");

            RegionHandle newRegion = PlayerManagerService.Instance.WorldManager.GetRegion(_transferParams.DestRegionId);
            if (newRegion == null)
                return Logger.ErrorReturn(false, $"FinishRegionTransfer(): Failed to get region 0x{_transferParams.DestRegionId:X} for transfer {transferId} for player [{this}]");

            SetActualRegion(newRegion);
            SetTransferParams(0, null);

            if (newRegion.IsTown)
                HasVisitedTown = true;

            PlayerManagerService.Instance.PartyManager.OnPlayerRegionTransferFinished(this);

            Logger.Info($"Player [{this}] finished region transfer {transferId}");
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
            if (TargetRegion == null || TargetRegion.IsMatch || HasRegionInAnyWorldView(TargetRegion.Id))
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

        public void SetDifficultyTierPreference(PrototypeId difficultyTierProtoRef)
        {
            if (difficultyTierProtoRef == DifficultyTierPreference)
                return;

            DifficultyTierPreference = difficultyTierProtoRef;
            Logger.Trace($"SetDifficultyTierPreference(): player=[{this}], difficulty=[{difficultyTierProtoRef.GetNameFormatted()}]");
        }

        public void GetPartyBoosts(PartyMemberInfo.Builder infoBuilder)
        {
            if (_partyBoosts.Count == 0)
                return;

            foreach (PrototypeGuid partyBoost in _partyBoosts)
                infoBuilder.AddBoosts((ulong)partyBoost);
        }

        public void SetPartyBoosts(List<ulong> boosts)
        {
            _partyBoosts.Clear();

            if (boosts == null)
                return;

            foreach (ulong boost in boosts)
            {
                if (boost == 0)
                {
                    Logger.Warn("SetPartyBoosts(): boost == 0");
                    continue;
                }

                _partyBoosts.Add((PrototypeGuid)boost);
            }
        }

        private void SetTransferParams(ulong gameId, NetStructTransferParams transferParams)
        {
            if (transferParams != null && _transferParams != null)
                Logger.Warn($"SetTransferParams(): Existing transfer {_transferParams.TransferId} found");

            _transferGameId = gameId;
            _transferParams = transferParams;
            _transferRegionReady = false;

            if (_transferParams != null)
                Logger.Info($"Player [{this}] beginning region transfer {_transferParams.TransferId}");
        }

        /// <summary>
        /// Puts this player into the region in the current game instance specified in the current transfer params.
        /// </summary>
        private void SendTransferParamsToGame()
        {
            if (CurrentGame == null)
            {
                Logger.Warn("SendTransferParamsToGame(): CurrentGame == null");
                return;
            }

            if (State != PlayerHandleState.InGame)
            {
                Logger.Warn($"SendTransferParamsToGame(): Invalid state {State} for player [{this}]");
                return;
            }

            if (CurrentGame.Id != _transferGameId)
            {
                Logger.Error($"OnRegionReadyToTransfer(): Game id mismatch for player [{this}] (expected 0x{_transferGameId:X}, got 0x{CurrentGame.Id:X})");
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

            prevRegion?.Unreserve(RegionReservationType.Presence);

            ActualRegion = newRegion;

            // This additional reservation will prevent the region from shutting down if there are still any players in it,
            // even if the region is no longer in any world views for whatever reason.
            newRegion?.Reserve(RegionReservationType.Presence);

            // Community will be updated when we receive a broadcast from the game instance.

            // Remove the previous region from the WorldView if it needs to be shut down.
            if (prevRegion != null && prevRegion.Flags.HasFlag(RegionFlags.ShutdownWhenVacant))
                WorldView.RemoveRegion(prevRegion);
        }

        private RegionTransferFailure CanEnterRegion(RegionHandle region)
        {
            if (region == null)
                return RegionTransferFailure.eRTF_GenericError;

            // TODO: Reevaluate if we need region.IsMatch int the check after we implement matchmaking.
            if ((region.IsPrivate || region.IsMatch) && region != TargetRegion && region.IsFull)
                return RegionTransferFailure.eRTF_Full;

            if (CurrentParty != null && CurrentParty.Type == GroupType.GroupType_Raid)
            {
                if (region.IsPrivateStory || region.IsPrivateNonStory)
                    return RegionTransferFailure.eRTF_RaidsNotAllowed;
            }

            return RegionTransferFailure.eRTF_NoError;
        }
    }
}
