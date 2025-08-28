using MHServerEmu.Core.Config;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerConfig : ConfigContainer
    {
        public string ServerName { get; private set; } = "MHServerEmu";
        public int ServerPrestigeLevel { get; private set; } = 6;
        public string MotdText { get; private set; } = "Welcome back to Marvel Heroes!";
        public bool LogTells { get; private set; } = false;
    }
}
