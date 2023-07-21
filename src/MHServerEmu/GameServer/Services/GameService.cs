using MHServerEmu.Common;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Services
{
    public class GameService
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        protected GameServerManager _gameServerManager;

        public GameService(GameServerManager gameServerManager)
        {
            _gameServerManager = gameServerManager;
        }

        public virtual void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages)
            {
                Logger.Warn($"Unimplemented game service received message id {message.Id} on muxId {muxId}");
            }
        }
    }
}
