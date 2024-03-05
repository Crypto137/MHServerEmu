using Gazillion;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
using MHServerEmu.Games.GameData;
using MHServerEmu.Networking;

namespace MHServerEmu.Leaderboards
{
    /// <summary>
    /// Handles leaderboard messages.
    /// </summary>
    public class LeaderboardService : IMessageHandler
    {
        private const ushort MuxChannel = 1;

        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly LeaderboardManager _leaderboardManager = new();

        public void Handle(FrontendClient client, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageLeaderboardInitializeRequest:
                    if (message.TryDeserialize<NetMessageLeaderboardInitializeRequest>(out var initializeRequest))
                        HandleInitializeRequest(client, initializeRequest);
                    break;

                case ClientToGameServerMessage.NetMessageLeaderboardRequest:
                    if (message.TryDeserialize<NetMessageLeaderboardRequest>(out var request))
                        HandleRequest(client, request);
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, IEnumerable<GameMessage> messages)
        {
            foreach (GameMessage message in messages) Handle(client, message);
        }

        private void HandleInitializeRequest(FrontendClient client, NetMessageLeaderboardInitializeRequest initializeRequest)
        {
            Logger.Trace("Received NetMessageLeaderboardInitializeRequest");

            var response = NetMessageLeaderboardInitializeRequestResponse.CreateBuilder();

            foreach (PrototypeGuid guid in initializeRequest.LeaderboardIdsList)
                response.AddLeaderboardInitDataList(_leaderboardManager.GetLeaderboardInitData(guid));

            client.SendMessage(MuxChannel, new(response.Build()));
        }

        private void HandleRequest(FrontendClient client, NetMessageLeaderboardRequest request)
        {
            if (request.HasDataQuery == false)
            {
                Logger.Warn("HandleRequest(): HasDataQuery == false");
                return;
            }

            Logger.Trace($"Received NetMessageLeaderboardRequest for {GameDatabase.GetPrototypeNameByGuid((PrototypeGuid)request.DataQuery.LeaderboardId)}");

            Leaderboard leaderboard = _leaderboardManager.GetLeaderboard((PrototypeGuid)request.DataQuery.LeaderboardId, request.DataQuery.InstanceId);;
            
            client.SendMessage(MuxChannel, new(NetMessageLeaderboardReportClient.CreateBuilder()
                .SetReport(leaderboard.GetReport(request, client.Session.Account.PlayerName))
                .Build()));
        }
    }
}
