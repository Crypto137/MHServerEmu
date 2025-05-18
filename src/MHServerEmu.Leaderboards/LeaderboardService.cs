using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.DatabaseAccess.SQLite;
using MHServerEmu.Games;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Leaderboards
{
    /// <summary>
    /// Handles leaderboard messages.
    /// </summary>
    public class LeaderboardService : IGameService
    {
        private const ushort MuxChannel = 1;
        private const int UpdateTimeMS = 1000;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private LeaderboardDatabase _database;
        private bool _isRunning;

        private Queue<GameServiceProtocol.LeaderboardScoreUpdateBatch> _pendingScoreUpdateQueue = new();
        private Queue<GameServiceProtocol.LeaderboardScoreUpdateBatch> _scoreUpdateQueue = new();
        private readonly object _scoreUpdateLock = new();

        #region IGameService Implementation

        public void Run()
        {
            var config = ConfigManager.Instance.GetConfig<GameOptionsConfig>();
            _isRunning = config.LeaderboardsEnabled;

            _database = LeaderboardDatabase.Instance;
            if (_isRunning) 
                _database.Initialize(SQLiteLeaderboardDBManager.Instance);

            while (_isRunning)
            {
                // update state for instances
                _database.UpdateState();

                // process updates
                lock (_scoreUpdateLock)
                    (_pendingScoreUpdateQueue, _scoreUpdateQueue) = (_scoreUpdateQueue, _pendingScoreUpdateQueue);

                _database.ProcessLeaderboardScoreUpdateQueue(_scoreUpdateQueue);
                    
                Thread.Sleep(UpdateTimeMS);
            }
        }

        public void Shutdown() 
        {
            _database?.Save();
            _isRunning = false;
        }

        public void ReceiveServiceMessage<T>(in T message) where T : struct, IGameServiceMessage
        {
            switch (message)
            {
                case GameServiceProtocol.RouteMessage routeMailboxMessage:
                    OnRouteMailboxMessage(routeMailboxMessage);
                    break;

                case GameServiceProtocol.LeaderboardScoreUpdateBatch leaderboardScoreUpdateBatch:
                    OnLeaderboardScoreUpdateBatch(leaderboardScoreUpdateBatch);
                    break;

                default:
                    Logger.Warn($"ReceiveServiceMessage(): Unhandled service message type {typeof(T).Name}");
                    break;
            }
        }

        public string GetStatus()
        {
            return $"Active Leaderboards: {(_database != null ? _database.LeaderboardCount : 0)}";
        }

        private void OnRouteMailboxMessage(in GameServiceProtocol.RouteMessage routeMailboxMessage)
        {
            IFrontendClient client = routeMailboxMessage.Client;
            MailboxMessage message = routeMailboxMessage.Message;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageLeaderboardRequest:            OnRequest(client, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        private void OnLeaderboardScoreUpdateBatch(in GameServiceProtocol.LeaderboardScoreUpdateBatch leaderboardScoreUpdateBatch)
        {
            // TODO: Use SpinLock here?
            lock (_scoreUpdateLock)
                _pendingScoreUpdateQueue.Enqueue(leaderboardScoreUpdateBatch);
        }

        #endregion

        private bool OnRequest(IFrontendClient client, MailboxMessage message)
        {
            // TODO: Move message handling to game and send a service message to leaderboards instead

            var request = message.As<NetMessageLeaderboardRequest>();
            if (request == null) return Logger.WarnReturn(false, $"OnRequest(): Failed to retrieve message");

            if (request.HasDataQuery == false)
                return Logger.WarnReturn(false, "OnRequest(): HasDataQuery == false");

            Logger.Trace($"Received NetMessageLeaderboardRequest for {GameDatabase.GetPrototypeNameByGuid((PrototypeGuid)request.DataQuery.LeaderboardId)}");
            
            client.SendMessage(MuxChannel, NetMessageLeaderboardReportClient.CreateBuilder()
                .SetReport(_database.GetLeaderboardReport(request))
                .Build());

            return true;
        }
    }
}
