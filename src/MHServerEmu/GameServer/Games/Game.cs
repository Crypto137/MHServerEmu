using System.Collections.Concurrent;
using System.Diagnostics;
using Gazillion;
using MHServerEmu.Common;
using MHServerEmu.Common.Config;
using MHServerEmu.GameServer.Entities;
using MHServerEmu.GameServer.Regions;
using MHServerEmu.Networking;

namespace MHServerEmu.GameServer.Games
{
    public class Game : IGameMessageHandler
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        public const int TickRate = 20;                 // Ticks per second based on client behavior
        public const long TickTime = 1000 / TickRate;   // ms per tick

        private readonly Stopwatch _tickWatch;
        private int _tickCount;

        public ulong Id { get; }
        public ConcurrentDictionary<FrontendClient, Player> PlayerDict { get; }

        public Game(ulong id)
        {
            _tickWatch = new();
            Id = id;
            PlayerDict = new();

            new Thread(() => Update()).Start();     // Start main game loop
        }

        public void Update()
        {
            while (true)
            {
                _tickWatch.Restart();
                Interlocked.Increment(ref _tickCount);

                lock (this)     // lock to prevent state from being modified mid-update
                {
                    // update here
                }

                _tickWatch.Stop();

                if (_tickWatch.ElapsedMilliseconds > TickTime)
                    Logger.Warn($"Game update took longer ({_tickWatch.ElapsedMilliseconds} ms) than target tick time ({TickTime} ms)");
                else
                    Thread.Sleep((int)(TickTime - _tickWatch.ElapsedMilliseconds));
            }
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage message)
        {
            throw new NotImplementedException();
        }

        public void Handle(FrontendClient client, ushort muxId, GameMessage[] messages)
        {
            foreach (GameMessage message in messages) Handle(client, muxId, message);
        }

        public void AddPlayer(FrontendClient client)
        {
            client.GameId = Id;

            client.SendMessage(1, new(NetMessageQueueLoadingScreen.CreateBuilder().SetRegionPrototypeId(0).Build()));

            client.SendMultipleMessages(1, PacketHelper.LoadMessagesFromPacketFile("NetMessageAchievementDatabaseDump.bin"));

            var chatBroadcastMessage = ChatBroadcastMessage.CreateBuilder()         // Send MOTD
                .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                .SetFromPlayerName(ConfigManager.GroupingManager.MotdPlayerName)
                .SetTheMessage(ChatMessage.CreateBuilder().SetBody(ConfigManager.GroupingManager.MotdText))
                .SetPrestigeLevel(ConfigManager.GroupingManager.MotdPrestigeLevel)
                .Build();

            client.SendMessage(2, new(chatBroadcastMessage));

            client.SendMultipleMessages(1, RegionLoader.GetBeginLoadingMessages(Id, client.CurrentRegion, client.CurrentAvatar));
            client.IsLoading = true;
        }
    }
}
