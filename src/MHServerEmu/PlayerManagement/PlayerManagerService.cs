using Gazillion;
using Google.ProtocolBuffers;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Core.System;
using MHServerEmu.DatabaseAccess;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.Achievements;

namespace MHServerEmu.PlayerManagement
{
    public class PlayerManagerService : IGameService, IFrontendService
    {
        private const ushort MuxChannel = 1;   // All messages come to and from PlayerManager over mux channel 1

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly SessionManager _sessionManager;
        private readonly GameManager _gameManager;
        private readonly object _playerLock = new();
        private readonly List<FrontendClient> _playerList = new();

        public PlayerManagerService()
        {
            _sessionManager = new();
            _gameManager = new();
        }

        #region IGameService Implementation

        public void Run() { }

        public void Shutdown() { }

        public void Handle(ITcpClient tcpClient, GameMessage message)
        {
            var client = (FrontendClient)tcpClient;

            // Timestamp sync messages
            if (message.Id == (byte)ClientToGameServerMessage.NetMessageSyncTimeRequest || message.Id == (byte)ClientToGameServerMessage.NetMessagePing)
            {
                message.GameTimeReceived = Clock.GameTime;
                message.DateTimeReceived = Clock.UnixTime;
            }

            switch ((ClientToGameServerMessage)message.Id)
            {
                // Self-handled messages

                case ClientToGameServerMessage.NetMessageReadyForGameJoin:
                    // NetMessageReadyForGameJoin contains a bug where wipesDataIfMismatchedInDb is marked as required but the client
                    // doesn't include it. To avoid an exception we build a partial message from the data we receive.
                    try
                    {
                        var readyForGameJoin = NetMessageReadyForGameJoin.CreateBuilder().MergeFrom(message.Payload).BuildPartial();
                        OnReadyForGameJoin(client, readyForGameJoin);
                    }
                    catch
                    {
                        Logger.Error("Failed to deserialize NetMessageReadyForGameJoin");
                    }

                    break;

                case ClientToGameServerMessage.NetMessageSyncTimeRequest:
                    if (message.TryDeserialize<NetMessageSyncTimeRequest>(out var syncTimeRequest))
                        OnSyncTimeRequest(client, syncTimeRequest, message.GameTimeReceived, message.DateTimeReceived);
                    break;

                case ClientToGameServerMessage.NetMessagePing:
                    if (message.TryDeserialize<NetMessagePing>(out var ping))
                        OnPing(client, ping, message.GameTimeReceived);
                    break;

                case ClientToGameServerMessage.NetMessageFPS:
                    if (message.TryDeserialize<NetMessageFPS>(out var fps))
                        OnFps(client, fps);
                    break;

                case ClientToGameServerMessage.NetMessageGracefulDisconnect:
                    OnGracefulDisconnect(client);
                    break;

                // Routed messages

                // Game
                case ClientToGameServerMessage.NetMessageUpdateAvatarState:
                case ClientToGameServerMessage.NetMessageCellLoaded:
                case ClientToGameServerMessage.NetMessageAdminCommand:
                case ClientToGameServerMessage.NetMessageChangeCameraSettings:
                case ClientToGameServerMessage.NetMessagePerformPreInteractPower:
                case ClientToGameServerMessage.NetMessageTryActivatePower:
                case ClientToGameServerMessage.NetMessagePowerRelease:
                case ClientToGameServerMessage.NetMessageTryCancelPower:
                case ClientToGameServerMessage.NetMessageTryCancelActivePower:
                case ClientToGameServerMessage.NetMessageContinuousPowerUpdateToServer:
                case ClientToGameServerMessage.NetMessageTryInventoryMove:
                case ClientToGameServerMessage.NetMessageThrowInteraction:
                case ClientToGameServerMessage.NetMessageUseInteractableObject:
                case ClientToGameServerMessage.NetMessageUseWaypoint:
                case ClientToGameServerMessage.NetMessageSwitchAvatar:
                case ClientToGameServerMessage.NetMessageAbilitySlotToAbilityBar:
                case ClientToGameServerMessage.NetMessageAbilityUnslotFromAbilityBar:
                case ClientToGameServerMessage.NetMessageAbilitySwapInAbilityBar:
                case ClientToGameServerMessage.NetMessageSetPlayerGameplayOptions:
                case ClientToGameServerMessage.NetMessageRequestInterestInInventory:
                case ClientToGameServerMessage.NetMessageRequestInterestInAvatarEquipment:
                case ClientToGameServerMessage.NetMessageSelectOmegaBonus:  // This should be within NetMessageOmegaBonusAllocationCommit only in theory
                case ClientToGameServerMessage.NetMessageOmegaBonusAllocationCommit:
                case ClientToGameServerMessage.NetMessageRespecOmegaBonus:
                case ClientToGameServerMessage.NetMessageAssignStolenPower:
                    GetGameByPlayer(client).Handle(client, message);
                    break;

                // Grouping Manager
                case ClientToGameServerMessage.NetMessageChat:
                case ClientToGameServerMessage.NetMessageTell:
                case ClientToGameServerMessage.NetMessageReportPlayer:
                case ClientToGameServerMessage.NetMessageChatBanVote:
                    ServerManager.Instance.RouteMessage(tcpClient, message, ServerType.GroupingManager);
                    break;

                // Billing
                case ClientToGameServerMessage.NetMessageGetCatalog:
                case ClientToGameServerMessage.NetMessageGetCurrencyBalance:
                case ClientToGameServerMessage.NetMessageBuyItemFromCatalog:
                case ClientToGameServerMessage.NetMessageBuyGiftForOtherPlayer:
                case ClientToGameServerMessage.NetMessagePurchaseUnlock:
                case ClientToGameServerMessage.NetMessageGetGiftHistory:
                    ServerManager.Instance.RouteMessage(tcpClient, message, ServerType.Billing);
                    break;

                // Leaderboards
                case ClientToGameServerMessage.NetMessageLeaderboardRequest:
                case ClientToGameServerMessage.NetMessageLeaderboardArchivedInstanceListRequest:
                case ClientToGameServerMessage.NetMessageLeaderboardInitializeRequest:
                    ServerManager.Instance.RouteMessage(tcpClient, message, ServerType.Leaderboard);
                    break;

                default:
                    Logger.Warn($"Handle(): Unhandled message [{message.Id}] {(ClientToGameServerMessage)message.Id}");
                    break;
            }
        }

        public void Handle(ITcpClient client, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages)
                Handle(client, message);
        }

        public string GetStatus()
        {
            return $"Sessions: {_sessionManager.SessionCount}";
        }

        #endregion

        #region IFrontendService Implementation

        public void ReceiveFrontendMessage(FrontendClient client, IMessage message)
        {
            switch (message)
            {
                case InitialClientHandshake handshake: OnInitialClientHandshake(client, handshake); break;
                case ClientCredentials credentials: OnClientCredentials(client, credentials); break;
                default: Logger.Warn($"ReceiveFrontendMessage(): Unhandled message {message.DescriptorForType.Name}"); break;
            }
        }

        public bool AddFrontendClient(FrontendClient client)
        {
            lock (_playerLock)
            {
                // TODO: make this check better
                foreach (FrontendClient player in _playerList)
                {
                    if (player.Session.Account.Id == client.Session.Account.Id)
                        return Logger.WarnReturn(false, "Failed to add player: already added");
                }

                _playerList.Add(client);
                _gameManager.GetAvailableGame().AddPlayer(client);
            }

            return true;
        }

        public bool RemoveFrontendClient(FrontendClient client)
        {
            lock (_playerLock)
            {
                if (_playerList.Contains(client) == false)
                    return Logger.WarnReturn(false, "Failed to remove player: not found");

                Game game = GetGameByPlayer(client);
                game.RemovePlayer(client);

                _playerList.Remove(client);
                _sessionManager.RemoveSession(client.Session.Id);
            }

            if (ConfigManager.PlayerManager.BypassAuth == false) DBManager.UpdateAccountData(client.Session.Account);
            return true;
        }

        #endregion

        #region Player Management

        public void BroadcastMessage(GameMessage message)
        {
            lock (_playerLock)
            {
                foreach (FrontendClient player in _playerList)
                    player.SendMessage(MuxChannel, message);
            }
        }

        public bool TryGetSession(ulong sessionId, out ClientSession session) => _sessionManager.TryGetSession(sessionId, out session);
        public bool TryGetClient(ulong sessionId, out FrontendClient client) => _sessionManager.TryGetClient(sessionId, out client);
        public Game GetGameByPlayer(FrontendClient client) => _gameManager.GetGameById(client.GameId);

        #endregion

        #region Message Handling

        public AuthStatusCode OnLoginDataPB(LoginDataPB loginDataPB, out ClientSession session)
        {
            return _sessionManager.TryCreateSessionFromLoginDataPB(loginDataPB, out session);
        }

        private void OnInitialClientHandshake(FrontendClient client, InitialClientHandshake handshake)
        {
            client.FinishedPlayerManagerHandshake = true;

            // Queue loading
            client.SendMessage(MuxChannel, NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId(0).Build());

            // Send achievement database
            client.SendMessage(MuxChannel, AchievementDatabase.Instance.GetDump());
            // NetMessageQueryIsRegionAvailable regionPrototype: 9833127629697912670 should go in the same packet as AchievementDatabaseDump
        }

        private void OnClientCredentials(FrontendClient client, ClientCredentials credentials)
        {
            Logger.Info($"Received ClientCredentials");

            if (_sessionManager.VerifyClientCredentials(client, credentials) == false)
            {
                Logger.Warn($"Failed to verify client credentials, disconnecting client on {client.Connection}");
                client.Connection.Disconnect();
                return;
            }

            // Respond on successful auth
            if (ConfigManager.PlayerManager.SimulateQueue)
            {
                Logger.Info("Responding with LoginQueueStatus message");
                client.SendMessage(MuxChannel, LoginQueueStatus.CreateBuilder()
                    .SetPlaceInLine(ConfigManager.PlayerManager.QueuePlaceInLine)
                    .SetNumberOfPlayersInLine(ConfigManager.PlayerManager.QueueNumberOfPlayersInLine)
                    .Build());
            }
            else
            {
                Logger.Info("Responding with SessionEncryptionChanged message");
                client.SendMessage(MuxChannel, SessionEncryptionChanged.CreateBuilder()
                    .SetRandomNumberIndex(0)
                    .SetEncryptedRandomNumber(ByteString.Empty)
                    .Build());
            }
        }

        private void OnReadyForGameJoin(FrontendClient client, NetMessageReadyForGameJoin readyForGameJoin)
        {
            Logger.Info($"Received NetMessageReadyForGameJoin from {client.Session.Account}");
            Logger.Trace(readyForGameJoin.ToString());

            // Log the player in
            Logger.Info($"Logging in player {client.Session.Account}");
            client.SendMessage(MuxChannel, NetMessageReadyAndLoggedIn.DefaultInstance); // add report defect (bug) config here

            // Sync time
            client.SendMessage(MuxChannel, NetMessageInitialTimeSync.CreateBuilder()
                .SetGameTimeServerSent(Clock.GameTime.Ticks / 10)
                .SetDateTimeServerSent(Clock.UnixTime.Ticks / 10)
                .Build());
        }

        private void OnSyncTimeRequest(FrontendClient client, NetMessageSyncTimeRequest request, TimeSpan gameTimeReceived, TimeSpan dateTimeReceived)
        {
            //Logger.Debug($"NetMessageSyncTimeRequest:\n{request}");

            var reply = NetMessageSyncTimeReply.CreateBuilder()
                .SetGameTimeClientSent(request.GameTimeClientSent)
                .SetGameTimeServerReceived(gameTimeReceived.Ticks / 10)
                .SetGameTimeServerSent(Clock.GameTime.Ticks / 10)
                .SetDateTimeClientSent(request.DateTimeClientSent)
                .SetDateTimeServerReceived(dateTimeReceived.Ticks / 10)
                .SetDateTimeServerSent(Clock.UnixTime.Ticks / 10)
                .SetDialation(1.0f)
                .SetGametimeDialationStarted(0)
                .SetDatetimeDialationStarted(0)
                .Build();

            //Logger.Debug($"NetMessageSyncTimeReply:\n{reply}");

            client.SendMessage(MuxChannel, reply);
        }

        private void OnPing(FrontendClient client, NetMessagePing ping, TimeSpan gameTimeReceived)
        {
            //Logger.Debug($"NetMessagePing:\n{ping}");

            var response = NetMessagePingResponse.CreateBuilder()
                .SetDisplayOutput(ping.DisplayOutput)
                .SetRequestSentClientTime(ping.SendClientTime)
                .SetRequestSentGameTime(ping.SendGameTime)
                .SetRequestNetReceivedGameTime((ulong)gameTimeReceived.TotalMilliseconds)
                .SetResponseSendTime((ulong)Clock.GameTime.TotalMilliseconds)
                .SetServerTickforecast(0)   // server tick time ms
                .SetGameservername("BOPR-MHVGIS2")
                .SetFrontendname("bopr-mhfes2")
                .Build();

            //Logger.Debug($"NetMessagePingResponse:\n{response}");

            client.SendMessage(MuxChannel, response);
        }

        private void OnFps(FrontendClient client, NetMessageFPS fps)
        {
            //Logger.Debug($"NetMessageFPS:\n{fps}");
        }

        private void OnGracefulDisconnect(FrontendClient client)
        {
            client.SendMessage(MuxChannel, NetMessageGracefulDisconnectAck.DefaultInstance);
        }

        #endregion
    }
}
