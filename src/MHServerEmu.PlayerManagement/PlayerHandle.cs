using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;
using MHServerEmu.Games.GameData;
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

        private static ulong _nextHandleId = 1;
        private static ulong _nextTransferId = 1;

        private bool _saveNeeded = false;   // Dirty flag for player data
        private NetStructTransferParams _transferParams;

        public ulong HandleId { get; }

        public IFrontendClient Client { get; private set; }
        public ulong PlayerDbId { get => Client.DbId; }
        public DBAccount Account { get => ((IDBAccountOwner)Client).Account; }

        public PlayerHandleState State { get; private set; }
        public GameHandle Game { get; private set; }

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
            Game = game;
            Logger.Info($"Requesting to add player [{this}] to game [{game}]");

            ServiceMessage.GameInstanceClientOp gameInstanceOp = new(GameInstanceClientOpType.Add, Client, game.Id);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, gameInstanceOp);

            return true;
        }

        public bool FinishAddToGame(ulong gameId)
        {
            if (State != PlayerHandleState.PendingAddToGame)
                return Logger.WarnReturn(false, $"FinishAddToGame(): Invalid state {State} for player [{this}]");

            if (Game.Id != gameId)
                Logger.Warn($"FinishAddToGame(): GameId mismatch (expected 0x{Game.Id:X}, got 0x{gameId:X})");

            State = PlayerHandleState.InGame;
            Logger.Info($"Player [{this}] added to game [{Game}]");

            // If this player has successfully gotten into a game, their data will need to be saved once they get out.
            _saveNeeded = true;

            SendRegionTransferParams();

            return true;
        }

        public void RemoveFromCurrentGame()
        {
            if (State != PlayerHandleState.InGame)
                return;

            Game.RemovePlayer(this);
        }

        public bool BeginRemoveFromGame(GameHandle game)
        {
            if (State != PlayerHandleState.InGame)
                return Logger.WarnReturn(false, $"BeginRemoveFromGame(): Invalid state {State} for handle [{this}]");

            if (game != Game)
                Logger.Warn($"BeginRemoveFromGame(): Game mismatch (expected [{Game}], got [{game}])");

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

            if (Game.Id != gameId)
                Logger.Warn($"FinishRemoveFromGame(): GameId mismatch (expected 0x{Game.Id:X}, got 0x{gameId:X})");

            State = PlayerHandleState.Idle;
            Game = null;

            Logger.Info($"Player [{this}] removed from game 0x{gameId:X}");

            if (_saveNeeded)
            {
                SavePlayerData();
                _saveNeeded = false;
            }

            return true;
        }

        public void BeginRegionTransfer()
        {
            if (_transferParams != null)
            {
                Logger.Warn($"BeginRegionTransfer(): Existing transfer {_transferParams.TransferId} found");
                _transferParams = null;
            }

            // HACK: Hardcoded transferparams
            RegionHandle region = PlayerManagerService.Instance.WorldManager.GetOrCreatePublicRegion((PrototypeId)9142075282174842340);
            _transferParams = NetStructTransferParams.CreateBuilder()
                .SetTransferId(_nextTransferId++)
                .SetDestRegionId(region.Id)
                .SetDestRegionProtoId(9142075282174842340)
                .SetDestTarget(NetStructRegionTarget.CreateBuilder()
                    .SetRegionProtoId(9142075282174842340)
                    .SetAreaProtoId(0)
                    .SetCellProtoId(0)
                    .SetEntityProtoId(0))
                .Build();

            Logger.Info($"Player [{this}] beginning region transfer {_transferParams.TransferId}");
        }

        public void SendRegionTransferParams()
        {
            if (_transferParams == null)
            {
                Logger.Warn($"SendRegionTransferParams(): No transfer params for player [{this}]");
                Disconnect();
                return;
            }

            ServiceMessage.GameAndRegionForPlayer message = new(Game.Id, PlayerDbId, _transferParams);
            ServerManager.Instance.SendMessageToService(GameServiceType.GameInstance, message);
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
    }
}
