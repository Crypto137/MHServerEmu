using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

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
        private static readonly Logger Logger = LogManager.CreateLogger();

        private static ulong _nextHandleId = 1;

        public ulong HandleId { get; }

        public IFrontendClient Client { get; private set; }
        public ulong PlayerDbId { get => Client.DbId; }
        public DBAccount Account { get => ((IDBAccountOwner)Client).Account; }

        public PlayerHandleState State { get; private set; }
        public GameHandle Game { get; private set; }

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
            return $"HandleId={HandleId}, Client=[{Client}]";
        }

        public bool MigrateSession(IFrontendClient newClient)
        {
            // Trying to migrate sessions while in the middle of adding/removing from a game instance is just asking for trouble,
            // so simply deny the new client and have it try again later. This shouldn't really outside of duplicate logins unless
            // something else breaks and the handle is stuck in a pending state.
            if (State != PlayerHandleState.InGame && State != PlayerHandleState.Idle)
                return Logger.WarnReturn(false, $"MigrateSession(): Unable to migrate handle [{this}] while in state {State}");

            Logger.Info($"Migrating handle [{this}] to session [{newClient.Session}]");

            RemoveFromCurrentGame();
            Client.Disconnect();

            ClientSession oldSession = (ClientSession)Client.Session;
            ClientSession newSession = (ClientSession)newClient.Session;
            newSession.Account = oldSession.Account;

            Client = newClient;

            return true;
        }

        public void Disconnect()
        {
            Client.Disconnect();
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

            GameServiceProtocol.GameInstanceClientOp gameInstanceOp = new(GameServiceProtocol.GameInstanceClientOp.OpType.Add, Client, game.Id);
            ServerManager.Instance.SendMessageToService(ServerType.GameInstanceServer, gameInstanceOp);

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

            GameServiceProtocol.GameInstanceClientOp gameInstanceOp = new(GameServiceProtocol.GameInstanceClientOp.OpType.Remove, Client, game.Id);
            ServerManager.Instance.SendMessageToService(ServerType.GameInstanceServer, gameInstanceOp);

            return true;
        }

        public bool FinishRemoveFromGame(ulong gameId)
        {
            if (State != PlayerHandleState.PendingRemoveFromGame)
                return Logger.WarnReturn(false, $"FinishRemoveFromGame(): Invalid state {State} for player [{this}]");

            if (Game.Id != gameId)
                Logger.Warn($"FinishRemoveFromGame(): GameId mismatch (expected 0x{Game.Id:X}, got 0x{gameId:X})");

            State = PlayerHandleState.Idle;
            Game = null;

            Logger.Info($"Player [{this}] removed from game 0x{gameId:X}");

            SavePlayerData();

            return true;
        }
    }
}
