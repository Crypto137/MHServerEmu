using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;

namespace MHServerEmu.PlayerManagement.Players
{
    public class ClientManager
    {
        // This is conceptually similar to NetworkManager, but PlayerHandle can represent a disconnected player that is currently being saved.

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<ulong, PlayerHandle> _playerDict = new();
        private readonly Dictionary<string, PlayerHandle> _playersByName = new(StringComparer.OrdinalIgnoreCase);

        private readonly PlayerManagerService _playerManager;

        public int PlayerCount { get => _playerDict.Count; }

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
            lock (_playerDict)
            {
                foreach (PlayerHandle player in _playerDict.Values)
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
        }

        #endregion

        #region Client Management

        public bool AddClient(IFrontendClient client)
        {
            if (DoAddClient(client) == false)
                client.Disconnect();
            return true;
        }

        public bool RemoveClient(IFrontendClient client)
        {
            if (client.Session == null || client.Session.Account == null)
                return Logger.WarnReturn(false, $"OnRemoveClient(): Client [{client}] has no valid session assigned");

            _playerManager.SessionManager.RemoveActiveSession(client.Session.Id);

            PlayerHandle player = GetPlayer(client.DbId);
            if (player == null)
                return Logger.WarnReturn(false, $"OnRemoveClient(): Failed to get player handle for client [{client}]");

            // When we are handling duplicate logins this handle may already have a different client,
            // in which case removal from game will be handled by the migration process.
            if (client == player.Client)
                player.RemoveFromCurrentGame();

            TimeSpan sessionLength = client.Session != null ? ((ClientSession)client.Session).Length : TimeSpan.Zero;
            Logger.Info($"Removed client [{client}] (SessionLength={sessionLength:hh\\:mm\\:ss})");
            return true;
        }

        private bool DoAddClient(IFrontendClient client)
        {
            if (AllowNewClients == false)
                return Logger.WarnReturn(false, $"AddClient(): Client [{client}] is not allowed to connect because the server is shutting down");

            ClientSession session = (ClientSession)client.Session;
            if (session == null || session.Account == null)
                return Logger.WarnReturn(false, $"AddClient(): Client [{client}] has no valid session assigned");

            if (_playerManager.LoginQueueManager.RemovePendingClient(client) == false)
                return Logger.WarnReturn(false, $"AddClient(): Client [{client}] is attempting to log in without passing the login queue");

            if (CreatePlayerHandle(client, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"AddClient(): Failed to get or create player handle for client [{client}]");

            Logger.Info($"Added client [{client}]");
            player.SendMessage(NetMessageReadyAndLoggedIn.DefaultInstance);

            return true;
        }

        #endregion

        #region PlayerHandle Management

        public PlayerHandle GetPlayer(ulong playerDbId)
        {
            lock (_playerDict)
            {
                if (_playerDict.TryGetValue(playerDbId, out PlayerHandle player) == false)
                    return null;

                return player;
            }
        }

        public PlayerHandle GetPlayer(string playerName)
        {
            lock (_playerDict)
            {
                if (_playersByName.TryGetValue(playerName, out PlayerHandle player) == false)
                    return null;

                return player;
            }
        }

        public void BroadcastMessage(IMessage message)
        {
            lock (_playerDict)
            {
                foreach (PlayerHandle player in _playerDict.Values)
                    player.SendMessage(message);
            }
        }

        public void OnPlayerNameChanged(ulong playerDbId, string oldPlayerName, string newPlayerName)
        {
            lock (_playerDict)
            {
                if (_playerDict.TryGetValue(playerDbId, out PlayerHandle player) == false)
                    return;

                lock (player.Account)
                    player.Account.PlayerName = newPlayerName;

                if (_playersByName.Remove(oldPlayerName) == false)
                    Logger.Warn($"OnPlayerNameChanged(): Player 0x{playerDbId:X} is logged in, but doesn't have a name lookup!");

                _playersByName.Add(newPlayerName, player);

                Logger.Info($"Updated name for player 0x{playerDbId:X}: {oldPlayerName} => {newPlayerName}");

                // TODO: Send player name change to the player entity in a game instance
            }

        }

        private bool CreatePlayerHandle(IFrontendClient client, out PlayerHandle player)
        {
            player = null;
            ulong playerDbId = client.DbId;

            if (_playerDict.TryGetValue(playerDbId, out player) == false)
            {
                player = new(client);
                _playerDict.Add(playerDbId, player);
                _playersByName.Add(player.PlayerName, player);
                Logger.Info($"Created new PlayerHandle: [{player}]");

                player.LoadPlayerData();
                _playerManager.CommunityRegistry.RefreshPlayerStatus(player);
            }
            else
            {
                Logger.Info($"Reusing existing PlayerHandle: [{player}]");
                if (player.MigrateSession(client) == false)
                {
                    Logger.Warn($"CreatePlayerHandle(): Failed to migrate existing session to client [{client}], disconnecting");
                    client.Disconnect();
                    player = null;
                    return false;
                }
            }

            return true;
        }

        private bool RemovePlayerHandle(IFrontendClient client)
        {
            ulong playerDbId = client.DbId;

            if (_playerDict.Remove(playerDbId, out PlayerHandle player) == false)
                return Logger.WarnReturn(false, $"RemovePlayer(): Client [{client}] is not bound to a PlayerHandle");

            _playersByName.Remove(player.PlayerName);

            Logger.Info($"Removed PlayerHandle [{player}]");

            _playerManager.CommunityRegistry.RefreshPlayerStatus(player);
            player.OnRemoved();
            return true;
        }

        #endregion
    }
}
