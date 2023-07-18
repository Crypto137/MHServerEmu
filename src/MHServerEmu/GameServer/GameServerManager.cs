using MHServerEmu.Common;
using MHServerEmu.Networking;
using MHServerEmu.GameServer.Services.Implementations;

namespace MHServerEmu.GameServer
{
    public class GameServerManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private FrontendService _frontendService;
        private GameInstanceService _gameInstanceService;

        public long StartTime { get; }      // Used for calculating game time 

        public GameServerManager()
        {
            _frontendService = new(this);
            _gameInstanceService = new(this);

            StartTime = GetDateTime();
        }

        public void Handle(FrontendClient client, ushort muxId, byte messageId, byte[] message)
        {
            switch (muxId)
            {
                case 1:
                    if (client.FinishedPlayerMgrServerFrontendHandshake)
                    {
                        Logger.Info($"Routing messageId {messageId} on muxId {muxId} to GameInstanceService");
                        _gameInstanceService.Handle(client, muxId, messageId, message);
                    }
                    else
                    {
                        Logger.Info($"Routing messageId {messageId} on muxId {muxId} to FrontendService");
                        _frontendService.Handle(client, muxId, messageId, message);
                    }

                    break;

                case 2:
                    if (client.FinishedPlayerMgrServerFrontendHandshake)
                    {
                        Logger.Warn($"Unhandled message id {messageId} on muxId {muxId} (most likely for GroupingManagerFrontend)");
                    }
                    else
                    {
                        Logger.Info($"Routing messageId {messageId} on muxId {muxId} to FrontendService");
                        _frontendService.Handle(client, muxId, messageId, message);
                    }

                    break;

                default:
                    Logger.Warn($"Unhandled message id {messageId} on muxId {muxId}");
                    break;
            }
        }

        public long GetDateTime()
        {
            return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds() * 1000;
        }

        public long GetGameTime()
        {
            return GetDateTime() - StartTime;
        }
    }
}
