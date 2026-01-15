using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.System.Random;
using MHServerEmu.Core.System.Time;

namespace MHServerEmu.Grouping.Chat
{
    public class ChatTipManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly List<ChatNormalMessage> _tips = new();

        private readonly GroupingManagerService _groupingManager;

        private int _nextTipIndex;
        private CooldownTimer _tipCooldownTimer;

        public ChatTipManager(GroupingManagerService groupingManager)
        {
            _groupingManager = groupingManager;
        }

        public void Initialize()
        {
            var config = _groupingManager.Config;

            if (config.EnableChatTips == false)
            {
                _tips.Clear();
                return;
            }

            LoadTips(config.ChatTipFileName, config.ServerName, config.ServerPrestigeLevel, config.ChatTipShuffle);

            _nextTipIndex = 0;
            _tipCooldownTimer = new(TimeSpan.FromMinutes(config.ChatTipIntervalMinutes));
        }

        public void Update()
        {
            if (_tips.Count == 0)
                return;

            if (_tipCooldownTimer.Check() == false)
                return;

            ChatNormalMessage tip = _tips[_nextTipIndex++];
            _groupingManager.ClientManager.SendMessageToAll(tip);

            if (_nextTipIndex >= _tips.Count)
                _nextTipIndex = 0;
        }

        private void LoadTips(string fileName, string fromPlayerName, int prestigeLevel, bool shuffle)
        {
            _tips.Clear();

            string tipFilePath = Path.Combine(FileHelper.DataDirectory, fileName);
            if (File.Exists(tipFilePath) == false)
            {
                Logger.Warn($"LoadTips(): File {fileName} not found");
                return;
            }

            using StreamReader reader = new(tipFilePath);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                ChatNormalMessage tipMessage = ChatNormalMessage.CreateBuilder()
                    .SetRoomType(ChatRoomTypes.CHAT_ROOM_TYPE_BROADCAST_ALL_SERVERS)
                    .SetFromPlayerName(fromPlayerName)
                    .SetTheMessage(ChatMessage.CreateBuilder().SetBody(line))
                    .SetPrestigeLevel(prestigeLevel)
                    .Build();

                _tips.Add(tipMessage);
            }

            if (shuffle)
            {
                GRandom rng = new();
                rng.ShuffleList(_tips);
            }

            Logger.Info($"Loaded {_tips.Count} chat tips from {fileName}");
        }
    }
}
