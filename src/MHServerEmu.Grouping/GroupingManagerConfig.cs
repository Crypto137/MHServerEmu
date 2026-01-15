using MHServerEmu.Core.Config;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerConfig : ConfigContainer
    {
        public string ServerName { get; private set; } = "MHServerEmu";
        public int ServerPrestigeLevel { get; private set; } = 6;
        public string MotdText { get; private set; } = "Welcome back to Marvel Heroes!";
        public bool LogPrivateChatRooms { get; private set; } = false;
        public bool EnableChatTips { get; private set; } = false;
        public string ChatTipFileName { get; private set; } = "ChatTips.txt";
        public float ChatTipIntervalMinutes { get; private set; } = 15;
        public bool ChatTipShuffle { get; private set; } = false;
    }
}
