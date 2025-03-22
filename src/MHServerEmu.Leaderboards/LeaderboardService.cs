using Gazillion;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Network;
using MHServerEmu.Core.Network.Tcp;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;

namespace MHServerEmu.Leaderboards
{
    /// <summary>
    /// Handles leaderboard messages.
    /// </summary>
    public class LeaderboardService : IGameService
    {
        private const ushort MuxChannel = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly LeaderboardManager _leaderboardManager = new();

        #region IGameService Implementation

        public void Run() { }

        public void Shutdown() { }

        public void Handle(ITcpClient tcpClient, MessagePackage message)
        {
            Logger.Warn($"Handle(): Unhandled MessagePackage");
        }

        public void Handle(ITcpClient client, IReadOnlyList<MessagePackage> messages)
        {
            for (int i = 0; i < messages.Count; i++)
                Handle(client, messages[i]);
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
            return $"Active Leaderboards: {_leaderboardManager.LeaderboardCount}";
        }

        #endregion

        private bool OnInitializeRequest(FrontendClient client, MailboxMessage message)
        {
            var initializeRequest = message.As<NetMessageLeaderboardInitializeRequest>();
            if (initializeRequest == null) return Logger.WarnReturn(false, $"OnInitializeRequest(): Failed to retrieve message");

            Logger.Trace("Received NetMessageLeaderboardInitializeRequest");

            var response = NetMessageLeaderboardInitializeRequestResponse.CreateBuilder();

            foreach (PrototypeGuid guid in initializeRequest.LeaderboardIdsList)
                response.AddLeaderboardInitDataList(_leaderboardManager.GetLeaderboardInitData(guid));

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

            Leaderboard leaderboard = _leaderboardManager.GetLeaderboard((PrototypeGuid)request.DataQuery.LeaderboardId, request.DataQuery.InstanceId);;
            
            client.SendMessage(MuxChannel, NetMessageLeaderboardReportClient.CreateBuilder()
                .SetReport(leaderboard.GetReport(request, client.Session.Account.PlayerName))
                .Build());

            return true;
        }
    }
}
