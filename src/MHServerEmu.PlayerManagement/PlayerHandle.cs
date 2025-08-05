using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.PlayerManagement.Regions;

namespace MHServerEmu.PlayerManagement
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

        private bool _saveNeeded = false;   // Dirty flag for player data

        private ulong _transferGameId;
        private NetStructTransferParams _transferParams;
        private bool _transferRegionReady;

        public ulong HandleId { get; }

        public IFrontendClient Client { get; private set; }
        public bool IsConnected { get => Client.IsConnected; }
        public ulong PlayerDbId { get => Client.DbId; }
        public DBAccount Account { get => ((IDBAccountOwner)Client).Account; }

        public PlayerHandleState State { get; private set; }
        public GameHandle CurrentGame { get; private set; }

        public bool HasTransferParams { get => _transferParams != null; }

        public PlayerHandle(IFrontendClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            // Ideally this check should be done at compile time, but making PlayerHandle generic would probably overcomplicate things too much.
            if (client is not IDBAccountOwner)
                throw new Exception("Client does not implement IDBAccountOwner.");

            HandleId = _nextHandleId++;
            Client = client;
            State = PlayerHandleState.Created;
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

            DBAccount account = Account;

            lock (account)
            {
                if (AccountManager.DBManager.SavePlayerData(account) == false)
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
                Logger.Warn($"TryJoinGame(): No transfer params for player [{this}]");
                Disconnect();
                return;
            }

            transferGame.AddPlayer(this);
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

            return BeginRegionTransferToTarget(0, destTarget, createRegionParams);
        }

        public bool BeginRegionTransferToTarget(ulong requestingGameId, NetStructRegionTarget destTarget, NetStructCreateRegionParams createRegionParams)
        {
            // TODO: Prioritize WorldView regions

            RegionPrototype regionProto = ((PrototypeId)destTarget.RegionProtoId).As<RegionPrototype>();
            if (regionProto == null) return Logger.WarnReturn(false, "BeginRegionTransferToTarget(): regionProto == null");

            if (regionProto.IsPublic == false)
            {
                Logger.Debug("BeginRegionTransferToTarget(): private regions are not implemented");
                CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_GenericError);
                return false;
            }

            RegionHandle region = PlayerManagerService.Instance.WorldManager.GetOrCreatePublicRegion((PrototypeId)destTarget.RegionProtoId, createRegionParams);
            ulong destGameId = region.Game.Id;

            NetStructTransferParams transferParams = NetStructTransferParams.CreateBuilder()
                .SetTransferId(_nextTransferId++)
                .SetDestRegionId(region.Id)
                .SetDestRegionProtoId((ulong)region.RegionProtoRef)
                .SetDestTarget(destTarget)
                .Build();

            SetTransferParams(region.Game.Id, transferParams);

            if (CurrentGame != null && CurrentGame.Id != destGameId)
                RemoveFromCurrentGame();

            // This needs to be called after we set transfer params because the region may already be ready.
            region.AddPlayer(this);
            return true;
        }

        public bool BeginRegionTransferToLocation(ulong requestingGameId, NetStructRegionLocation destLocation)
        {
            // TODO
            Logger.Debug("BeginRegionTransferToPlayer()");
            CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_DestinationInaccessible);
            return false;
        }

        public bool BeginRegionTransferToPlayer(ulong requestingGameId, ulong destPlayerDbId)
        {
            // TODO
            Logger.Debug("BeginRegionTransferToPlayer()");
            CancelRegionTransfer(requestingGameId, RegionTransferFailure.eRTF_DestinationInaccessible);
            return false;
        }

        public void CancelRegionTransfer(ulong requestingGameId, RegionTransferFailure reason)
        {
            if (requestingGameId == 0)
                return;

            // TODO: Do we need regionProtoId / requiredItemProtoId fields here?
            ChangeRegionFailed changeFailed = ChangeRegionFailed.CreateBuilder().SetReason(reason).Build();
            ServiceMessage.UnableToChangeRegion response = new(requestingGameId, PlayerDbId, changeFailed);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, response);
        }

        public void OnRegionReadyToTransfer()
        {
            _transferRegionReady = true;

            // If this player is already in the game that hosts the region, finish the transfer right away.
            // Otherwise this would be triggered when we receive the confirmation that this player is in the game.
            if (State == PlayerHandleState.InGame)
                SendTransferParamsToGame();
        }

        public bool FinishRegionTransfer(ulong transferId)
        {
            if (_transferParams == null)
                return Logger.WarnReturn(false, $"FinishRegionTransfer(): Received confirmation for transfer {transferId}, but no transfer is pending for player [{this}]");

            if (_transferParams.TransferId != transferId)
                return Logger.WarnReturn(false, $"FinishRegionTransfer(): Transfer id mismatch for player [{this}]: expected {_transferParams.TransferId}, got {transferId}");

            _transferParams = null;
            Logger.Info($"Player [{this}] finished region transfer {transferId}");
            return true;
        }

        private void SetTransferParams(ulong gameId, NetStructTransferParams transferParams)
        {
            if (_transferParams != null)
                Logger.Warn($"SetTransferParams(): Existing transfer {_transferParams.TransferId} found");

            _transferGameId = gameId;
            _transferParams = transferParams;
            _transferRegionReady = false;

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

            ServiceMessage.GameAndRegionForPlayer message = new(_transferGameId, PlayerDbId, _transferParams);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
        }
    }
}
