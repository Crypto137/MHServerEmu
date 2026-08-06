using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.PlayerManagement.Auth;
using MHServerEmu.PlayerManagement.Social;

namespace MHServerEmu.PlayerManagement.Players
{
    public class ClientManager
    {
        // This is conceptually similar to NetworkManager, but PlayerHandle can represent a disconnected player that is currently being saved.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, PlayerHandle> _players = new();
        private readonly Dictionary<string, PlayerHandle> _playersByName = new(StringComparer.OrdinalIgnoreCase);

        private readonly PlayerManagerService _playerManager;

        public int PlayerCount { get => _players.Count; }

        public bool AllowNewClients { get; set; } = true;

        public ClientManager(PlayerManagerService playerManager) 
        {
            _playerManager = playerManager;
        }

        public void Update()
        {
            ProcessIdlePlayers();
        }

        #region Ticking

        private void ProcessIdlePlayers()
        {
            foreach (PlayerHandle player in _players.Values)
            {
                if (player.State != PlayerHandleState.Idle)
                    continue;

                if (player.IsConnected)
                {
                    if (player.HasTransferParams == false)
                        player.BeginRegionTransferToStartTarget();

                    player.TryJoinGame();
                }
                else
                {
                    RemovePlayerHandle(player.Client);
                }
            }
        }

        #endregion

        #region Client Management

        public bool AddClient(IFrontendClient client)
        {
            if (DoAddClient(client) == false)
            {
                client.Disconnect();
                return false;
            }

            return true;
        }

        public bool RemoveClient(IFrontendClient client)
        {
            if (!Verify.IsNotNull(client.Session)) return false;
            if (!Verify.IsNotNull(client.Session.Account)) return false;

            _playerManager.SessionManager.RemoveActiveSession(client.Session.Id);

            PlayerHandle player = GetPlayer(client.DbId);
            if (!Verify.IsNotNull(player, $"Failed to get player handle for client [{client}]"))
                return false;

            // When we are handling duplicate logins this handle may already have a different client,
            // in which case removal from game will be handled by the migration process.
            if (client == player.Client)
                player.RemoveFromCurrentGame();

            TimeSpan sessionLength = ((ClientSession)client.Session).Length;
            Logger.Info($"Removed client [{client}] (SessionLength={sessionLength:hh\\:mm\\:ss})");
            return true;
        }

        private bool DoAddClient(IFrontendClient client)
        {
            if (!Verify.IsTrue(AllowNewClients, $"Client [{client}] is not allowed to connect because the server is shutting down"))
                return false;

            if (!Verify.IsNotNull(client.Session, $"Client [{client}] has no valid session assigned"))
                return false;

            if (!Verify.IsNotNull(client.Session.Account, $"Client [{client}] has no valid account assigned"))
                return false;

            if (!Verify.IsTrue(_playerManager.LoginQueueManager.RemovePendingClient(client), $"Client [{client}] is attempting to log in without passing the login queue"))
                return false;

            if (!Verify.IsTrue(CreatePlayerHandle(client, out PlayerHandle player), $"Failed to get or create player handle for client [{client}]"))
                return false;

            Logger.Info($"Added client [{client}]");
            player.SendMessage(NetMessageReadyAndLoggedIn.DefaultInstance);

            return true;
        }

        #endregion

        #region PlayerHandle Management

        public PlayerHandle GetPlayer(ulong playerDbId)
        {
            if (_players.TryGetValue(playerDbId, out PlayerHandle player) == false)
                return null;

            return player;
        }

        public PlayerHandle GetPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
                return null;

            if (_playersByName.TryGetValue(playerName, out PlayerHandle player) == false)
                return null;

            return player;
        }

        public void OnPlayerNameChanged(ulong playerDbId, string oldPlayerName, string newPlayerName)
        {
            if (_players.TryGetValue(playerDbId, out PlayerHandle player) == false)
                return;

            lock (player.Account)
                player.Account.PlayerName = newPlayerName;

            Verify.IsTrue(_playersByName.Remove(oldPlayerName), $"Player 0x{playerDbId:X} is logged in, but doesn't have a name lookup!");

            _playersByName.Add(newPlayerName, player);

            Logger.Info($"Updated name for player 0x{playerDbId:X}: {oldPlayerName} => {newPlayerName}");

            // TODO: Send player name change to the player entity in a game instance
        }

        private bool CreatePlayerHandle(IFrontendClient client, out PlayerHandle player)
        {
            player = null;
            ulong playerDbId = client.DbId;

            if (_players.TryGetValue(playerDbId, out player) == false)
            {
                player = new(client);
                _players.Add(playerDbId, player);
                _playersByName.Add(player.PlayerName, player);
                Logger.Trace($"Created new PlayerHandle: [{player}]");

                player.LoadPlayerData();
                _playerManager.CommunityRegistry.RefreshPlayerStatus(player);

                MasterGuild guild = _playerManager.GuildManager.GetGuildForPlayer(playerDbId);
                guild?.OnMemberOnline(player);
            }
            else
            {
                Logger.Trace($"Reusing existing PlayerHandle: [{player}]");

                if (!Verify.IsTrue(player.MigrateSession(client), $"Failed to migrate existing session to client [{client}], disconnecting\""))
                {
                    client.Disconnect();
                    player = null;
                    return false;
                }
            }

            return true;
        }

        private void RemovePlayerHandle(IFrontendClient client)
        {
            ulong playerDbId = client.DbId;

            if (!Verify.IsTrue(_players.Remove(playerDbId, out PlayerHandle player), $"Client [{client}] is not bound to a PlayerHandle"))
                return;

            _playersByName.Remove(player.PlayerName);

            Logger.Trace($"Removed PlayerHandle [{player}]");

            _playerManager.CommunityRegistry.RefreshPlayerStatus(player);
            player.OnRemoved();
        }

        #endregion
    }
}
