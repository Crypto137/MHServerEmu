using MHServerEmu.Common;
using MHServerEmu.Networking;
using MHServerEmu.GameServer.Frontend;
using MHServerEmu.GameServer.GameInstances;

namespace MHServerEmu.GameServer
{
    public class GameServerManager : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private FrontendService _frontendService;
        private GroupingManagerService _groupingManagerService;
        private GameInstanceService _gameInstanceService;

        public long StartTime { get; }      // Used for calculating game time 

        public GameServerManager()
        {
            _frontendService = new(this);
            _groupingManagerService = new(this);
            _gameInstanceService = new(this, _groupingManagerService);

            StartTime = GetDateTime();
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            switch (muxId)
            {
                case 1:
                    if (client.FinishedPlayerMgrServerFrontendHandshake)
                    {
                        //Logger.Trace($"Routing {messages.Length} message(s) on muxId {muxId} to GameInstanceService");
                        _gameInstanceService.Handle(client, muxId, message);
                    }
                    else
                    {
                        //Logger.Trace($"Routing {messages.Length} message(s) on muxId {muxId} to FrontendService");
                        _frontendService.Handle(client, muxId, message);
                    }

                    break;

                case 2:
                    if (client.FinishedGroupingManagerFrontendHandshake)
                    {
                        _groupingManagerService.Handle(client, muxId, message);
                    }
                    else
                    {
                        //Logger.Trace($"Routing {messages.Length} message(s) on muxId {muxId} to FrontendService");
                        _frontendService.Handle(client, muxId, message);
                    }

                    break;

                default:
                    Logger.Warn($"Unhandled message on muxId {muxId}");
                    break;
            }
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            switch (muxId)
            {
                case 1:
                    if (client.FinishedPlayerMgrServerFrontendHandshake)
                    {
                        //Logger.Trace($"Routing {messages.Length} message(s) on muxId {muxId} to GameInstanceService");
                        _gameInstanceService.Handle(client, muxId, messages);
                    }
                    else
                    {
                        //Logger.Trace($"Routing {messages.Length} message(s) on muxId {muxId} to FrontendService");
                        _frontendService.Handle(client, muxId, messages);
                    }

                    break;

                case 2:
                    if (client.FinishedGroupingManagerFrontendHandshake)
                    {
                        _groupingManagerService.Handle(client, muxId, messages);
                    }
                    else
                    {
                        //Logger.Trace($"Routing {messages.Length} messages on muxId {muxId} to FrontendService");
                        _frontendService.Handle(client, muxId, messages);
                    }

                    break;

                default:
                    Logger.Warn($"{messages.Length} unhandled messages on muxId {muxId}");
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
