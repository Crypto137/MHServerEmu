using MHServerEmu.Core.Config;

namespace MHServerEmu.Grouping
{
    public class GroupingManagerConfig : ConfigContainer
    {
        public string MotdPlayerName { get; private set; } = "MHServerEmu";
        public string MotdText { get; private set; } = "Welcome back to Marvel Heroes!";
        public int MotdPrestigeLevel { get; private set; } = 6;
    }
}
