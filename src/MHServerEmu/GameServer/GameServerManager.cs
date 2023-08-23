using MHServerEmu.Common;
using MHServerEmu.Networking;
using MHServerEmu.GameServer.Frontend;
using MHServerEmu.GameServer.GameInstances;
using MHServerEmu.GameServer.Games;

namespace MHServerEmu.GameServer
{
    public class GameServerManager : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public GameManager GameManager { get; }

        public FrontendService FrontendService { get; }
        public GroupingManagerService GroupingManagerService { get; }
        public GameInstanceService GameInstanceService { get; }

        public long StartTime { get; }      // Used for calculating game time 

        public GameServerManager()
        {
            GameManager = new();

            FrontendService = new(this);
            GroupingManagerService = new(this);
            GameInstanceService = new(this);

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
                        GameInstanceService.Handle(client, muxId, message);
                    }
                    else
                    {
                        //Logger.Trace($"Routing {messages.Length} message(s) on muxId {muxId} to FrontendService");
                        FrontendService.Handle(client, muxId, message);
                    }

                    break;

                case 2:
                    if (client.FinishedGroupingManagerFrontendHandshake)
                    {
                        GroupingManagerService.Handle(client, muxId, message);
                    }
                    else
                    {
                        //Logger.Trace($"Routing {messages.Length} message(s) on muxId {muxId} to FrontendService");
                        FrontendService.Handle(client, muxId, message);
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
                        GameInstanceService.Handle(client, muxId, messages);
                    }
                    else
                    {
                        //Logger.Trace($"Routing {messages.Length} message(s) on muxId {muxId} to FrontendService");
                        FrontendService.Handle(client, muxId, messages);
                    }

                    break;

                case 2:
                    if (client.FinishedGroupingManagerFrontendHandshake)
                    {
                        GroupingManagerService.Handle(client, muxId, messages);
                    }
                    else
                    {
                        //Logger.Trace($"Routing {messages.Length} messages on muxId {muxId} to FrontendService");
                        FrontendService.Handle(client, muxId, messages);
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
