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

        public void Handle(ITcpClient tcpClient, GameMessage message)
        {
            var client = (FrontendClient)tcpClient;

            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageLeaderboardInitializeRequest:
                    if (message.TryDeserialize<NetMessageLeaderboardInitializeRequest>(out var initializeRequest))
                        OnInitializeRequest(client, initializeRequest);
                    break;

                case ClientToGameServerMessage.NetMessageLeaderboardRequest:
                    if (message.TryDeserialize<NetMessageLeaderboardRequest>(out var request))
                        OnRequest(client, request);
                    break;

                default:
                    Logger.Warn($"Handle(): Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
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
            return $"Active Leaderboards: {_leaderboardManager.LeaderboardCount}";
        }

        #endregion

        private void OnInitializeRequest(FrontendClient client, NetMessageLeaderboardInitializeRequest initializeRequest)
        {
            Logger.Trace("Received NetMessageLeaderboardInitializeRequest");

            var response = NetMessageLeaderboardInitializeRequestResponse.CreateBuilder();

            foreach (PrototypeGuid guid in initializeRequest.LeaderboardIdsList)
                response.AddLeaderboardInitDataList(_leaderboardManager.GetLeaderboardInitData(guid));

            client.SendMessage(MuxChannel, response.Build());
        }

        private void OnRequest(FrontendClient client, NetMessageLeaderboardRequest request)
        {
            if (request.HasDataQuery == false)
            {
                Logger.Warn("OnRequest(): HasDataQuery == false");
                return;
            }

            Logger.Trace($"Received NetMessageLeaderboardRequest for {GameDatabase.GetPrototypeNameByGuid((PrototypeGuid)request.DataQuery.LeaderboardId)}");

            Leaderboard leaderboard = _leaderboardManager.GetLeaderboard((PrototypeGuid)request.DataQuery.LeaderboardId, request.DataQuery.InstanceId);;
            
            client.SendMessage(MuxChannel, NetMessageLeaderboardReportClient.CreateBuilder()
                .SetReport(leaderboard.GetReport(request, client.Session.Account.PlayerName))
                .Build());
        }
    }
}
