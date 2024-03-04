using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Common.Logging;
using MHServerEmu.Frontend;
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

        public void Handle(FrontendClient client, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageLeaderboardInitializeRequest:
                    if (message.TryDeserialize<NetMessageLeaderboardInitializeRequest>(out var initializeRequest))
                        HandleInitializeRequest(client, initializeRequest);
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
            var response = NetMessageLeaderboardInitializeRequestResponse.CreateBuilder();

            ulong instanceId = 1;

            foreach (ulong guid in initializeRequest.LeaderboardIdsList)
            {
                var initData = LeaderboardInitData.CreateBuilder();
                
                initData.SetLeaderboardId(guid);

                initData.SetCurrentInstanceData(LeaderboardInstanceData.CreateBuilder()
                    .SetInstanceId(instanceId++)
                    .SetActivationTimestamp((Clock.UnixTime - TimeSpan.FromDays(1)).Ticks / 10)
                    .SetExpirationTimestamp((Clock.UnixTime + TimeSpan.FromDays(1)).Ticks / 10)
                    .SetState(LeaderboardState.eLBS_Active)
                    .SetVisible(true));

                response.AddLeaderboardInitDataList(initData);
            }

            client.SendMessage(MuxChannel, new(response.Build()));
        }
    }
}
