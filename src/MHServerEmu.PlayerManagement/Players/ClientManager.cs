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

            if (TryGetPlayerHandle(client.DbId, out PlayerHandle player) == false)
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

        public bool TryGetPlayerHandle(ulong playerDbId, out PlayerHandle player)
        {
            lock (_playerDict)
                return _playerDict.TryGetValue(playerDbId, out player);
        }

        public void BroadcastMessage(IMessage message)
        {
            lock (_playerDict)
            {
                foreach (PlayerHandle player in _playerDict.Values)
                    player.SendMessage(message);
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
                Logger.Info($"Created new PlayerHandle: [{player}]");

                player.LoadPlayerData();
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

            Logger.Info($"Removed PlayerHandle [{player}]");

            player.OnRemoved();
            return true;
        }

        #endregion
    }
}
