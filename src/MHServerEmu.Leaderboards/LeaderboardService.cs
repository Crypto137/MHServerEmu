using Gazillion;
using MHServerEmu.Core.Config;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.DatabaseAccess.SQLite;
using MHServerEmu.Frontend;
using MHServerEmu.Games;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.Leaderboards;

namespace MHServerEmu.Leaderboards
{
    /// <summary>
    /// Handles leaderboard messages.
    /// </summary>
    public class LeaderboardService : IGameService
    {
        private const ushort MuxChannel = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();
        private LeaderboardDatabase _database;
        private bool _isRunning;

        #region IGameService Implementation

        public void Run()
        {
            var config = ConfigManager.Instance.GetConfig<GameOptionsConfig>();
            _isRunning = config.LeaderboardsEnabled;

            _database = LeaderboardDatabase.Instance;
            if (config.LeaderboardsEnabled) 
                _database.Initialize(SQLiteLDBManager.Instance);

            while (_isRunning)
            {
                // Get uptadequeue from LeaderboardGameDatabase and update
                var updateQueue = LeaderboardGameDatabase.Instance.GetUpdateQueue();
                if (updateQueue.Count > 0)
                {
                    _database.UpdateLeaderboards(updateQueue);
                }
                else 
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public void Shutdown() 
        { 
            _isRunning = false;
        }

        public void Handle(ITcpClient tcpClient, MessagePackage message)
        {
            Logger.Warn($"Handle(): Unhandled MessagePackage");
        }

        public void Handle(ITcpClient client, IEnumerable<MessagePackage> messages)
        {
            foreach (MessagePackage message in messages)
                Handle(client, message);
        }

        public void Handle(ITcpClient tcpClient, MailboxMessage message)
        {
            var client = (FrontendClient)tcpClient;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageLeaderboardInitializeRequest:  OnInitializeRequest(client, message); break;
                case ClientToGameServerMessage.NetMessageLeaderboardRequest:            OnRequest(client, message); break;

                default: Logger.Warn($"Handle(): Unhandled {(ClientToGameServerMessage)message.Id} [{message.Id}]"); break;
            }
        }

        public string GetStatus()
        {
            return $"Active Leaderboards: {_database.LeaderboardCount}";
        }

        #endregion

        private bool OnInitializeRequest(FrontendClient client, MailboxMessage message)
        {
            var initializeRequest = message.As<NetMessageLeaderboardInitializeRequest>();
            if (initializeRequest == null) return Logger.WarnReturn(false, $"OnInitializeRequest(): Failed to retrieve message");

            Logger.Trace("Received NetMessageLeaderboardInitializeRequest");

            var response = NetMessageLeaderboardInitializeRequestResponse.CreateBuilder();

            foreach (var guid in initializeRequest.LeaderboardIdsList)
                if (_database.GetLeaderboardInstances((PrototypeGuid)guid, out var instances))
                {
                    var initDataBuilder = LeaderboardInitData.CreateBuilder().SetLeaderboardId(guid);
                    foreach (var instance in instances)
                    {
                        var instanceData = instance.ToInstanceData();
                        if (instance.State == LeaderboardState.eLBS_Active || instance.State == LeaderboardState.eLBS_Created)
                            initDataBuilder.SetCurrentInstanceData(instanceData);
                        else
                            initDataBuilder.AddArchivedInstanceList(instanceData);
                    }
                    response.AddLeaderboardInitDataList(initDataBuilder.Build());
                }

            client.SendMessage(MuxChannel, response.Build());

            return true;
        }

        private bool OnRequest(FrontendClient client, MailboxMessage message)
        {
            var request = message.As<NetMessageLeaderboardRequest>();
            if (request == null) return Logger.WarnReturn(false, $"OnRequest(): Failed to retrieve message");

            if (request.HasDataQuery == false)
                Logger.WarnReturn(false, "OnRequest(): HasDataQuery == false");

            Logger.Trace($"Received NetMessageLeaderboardRequest for {GameDatabase.GetPrototypeNameByGuid((PrototypeGuid)request.DataQuery.LeaderboardId)}");
            
            client.SendMessage(MuxChannel, NetMessageLeaderboardReportClient.CreateBuilder()
                .SetReport(_database.GetLeaderboardReport(request))
                .Build());

            return true;
        }
    }
}
