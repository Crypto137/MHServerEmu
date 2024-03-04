using Gazillion;
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
            Logger.Debug($"NetMessageLeaderboardInitializeRequest:\n{initializeRequest}");
        }
    }
}
