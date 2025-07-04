using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.DatabaseAccess.Models;

namespace MHServerEmu.PlayerManagement.Handles
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

        public IFrontendClient Client { get; }
        public DBAccount Account { get => ((IDBAccountOwner)Client).Account; }

        public PlayerHandleState State { get; private set; }
        public GameHandle Game { get; private set; }

        public ulong Id { get => Client.DbId; }

        public PlayerHandle(IFrontendClient client)
        {
            ArgumentNullException.ThrowIfNull(client);

            // Ideally this check should be done at compile time, but making PlayerHandle generic would probably overcomplicate things too much.
            if (client is not IDBAccountOwner)
                throw new Exception("Client does not implement IDBAccountOwner.");

            Client = client;
            State = PlayerHandleState.Created;
        }

        public override string ToString()
        {
            return Client.ToString();
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

            Logger.Trace($"Loaded player data for account [{account}] from the database");

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

            Logger.Trace($"Saved player data for account [{account}] to the database");

            return true;
        }

        public bool BeginAddToGame(GameHandle game)
        {
            if (State != PlayerHandleState.Idle)
                return Logger.WarnReturn(false, $"BeginAddToGame(): Invalid state {State} for player [{this}]");

            State = PlayerHandleState.PendingAddToGame;
            Game = game;
            Logger.Trace($"Requesting to add player [{this}] requesting game [{game}]");

            GameServiceProtocol.GameInstanceClientOp gameInstanceOp = new(GameServiceProtocol.GameInstanceClientOp.OpType.Add, Client, game.Id);
            ServerManager.Instance.SendMessageToService(ServerType.GameInstanceServer, gameInstanceOp);

            return true;
        }

        public bool BeginRemoveFromGame(GameHandle game)
        {
            if (State != PlayerHandleState.InGame)
                return Logger.WarnReturn(false, $"BeginRemoveFromGame(): Invalid state {State} for handle [{this}]");

            if (game != Game)
                return Logger.WarnReturn(false, $"BeginRemoveFromGame(): Game mismatch (expected [{Game}], got [{game}])");

            State = PlayerHandleState.PendingRemoveFromGame;
            Logger.Trace($"Requesting to remove player [{this}] from game {game}");

            GameServiceProtocol.GameInstanceClientOp gameInstanceOp = new(GameServiceProtocol.GameInstanceClientOp.OpType.Remove, Client, game.Id);
            ServerManager.Instance.SendMessageToService(ServerType.GameInstanceServer, gameInstanceOp);

            return true;
        }

        public bool FinalizePendingState()
        {
            Logger.Trace($"Handle [{this}] finalizing pending state {State}");

            switch (State)
            {
                case PlayerHandleState.PendingAddToGame:
                    State = PlayerHandleState.InGame;
                    break;

                case PlayerHandleState.PendingRemoveFromGame:
                    State = PlayerHandleState.Idle;
                    Game = null;
                    break;

                default:
                    return Logger.WarnReturn(false, $"FinalizePendingState(): Handle [{this}] is not in a pending state");
            }

            return true;
        }
    }
}
