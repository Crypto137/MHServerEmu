using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.GameInstances
{
    public class GroupingManagerService : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private GameServerManager _gameServerManager;

        public GroupingManagerService(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;   
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch ((ClientToGameServerMessage)message.Id)
            {
                case ClientToGameServerMessage.NetMessageChat:
                    Logger.Trace(NetMessageChat.ParseFrom(message.Content).ToString());
                    break;

                default:
                    Logger.Warn($"Received unhandled message {(ClientToGameServerMessage)message.Id} (id {message.Id})");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }
    }
}
